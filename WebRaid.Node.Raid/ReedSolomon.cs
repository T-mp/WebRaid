using System;
using System.Collections.Generic;
using System.Linq;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Systematischer Reed-Solomon-Code über GF(2^8).
    /// Aus <see cref="DatenBloecke"/> Datenblöcken werden <see cref="ParitaetsBloecke"/> Paritätsblöcke berechnet;
    /// aus beliebigen <see cref="DatenBloecke"/> der insgesamt <see cref="DatenBloecke"/> + <see cref="ParitaetsBloecke"/>
    /// Blöcke lässt sich alles wiederherstellen.
    /// </summary>
    public sealed class ReedSolomon
    {
        private readonly byte[,] matrix;

        /// <summary>Anzahl der Datenblöcke (k)</summary>
        public int DatenBloecke { get; }

        /// <summary>Anzahl der Paritätsblöcke (m), also der verkraftbaren Ausfälle</summary>
        public int ParitaetsBloecke { get; }

        /// <summary>Gesamtzahl der Blöcke (k + m)</summary>
        public int Bloecke => DatenBloecke + ParitaetsBloecke;

        /// <summary>
        /// Erstellt einen Code für <paramref name="datenBloecke"/> + <paramref name="paritaetsBloecke"/> Blöcke
        /// </summary>
        public ReedSolomon(int datenBloecke, int paritaetsBloecke)
        {
            if (datenBloecke < 1) throw new ArgumentOutOfRangeException(nameof(datenBloecke), "Mindestens ein Datenblock");
            if (paritaetsBloecke < 0) throw new ArgumentOutOfRangeException(nameof(paritaetsBloecke), "Nicht negativ");
            if (datenBloecke + paritaetsBloecke > 255) throw new ArgumentOutOfRangeException(nameof(paritaetsBloecke), "Höchstens 255 Blöcke insgesamt");

            DatenBloecke = datenBloecke;
            ParitaetsBloecke = paritaetsBloecke;
            matrix = KodierMatrix(datenBloecke, Bloecke);
        }

        /// <summary>
        /// Berechnet die Paritätsblöcke.
        /// </summary>
        /// <param name="bloecke">Alle k + m Blöcke gleicher Länge; die ersten k enthalten die Daten, die übrigen werden überschrieben</param>
        public void Kodieren(byte[][] bloecke)
        {
            Pruefen(bloecke);
            for (var p = 0; p < ParitaetsBloecke; p++)
            {
                var ziel = bloecke[DatenBloecke + p];
                Array.Clear(ziel);
                for (var d = 0; d < DatenBloecke; d++)
                {
                    Galois.MultiplizierenUndAddieren(matrix[DatenBloecke + p, d], bloecke[d], ziel);
                }
            }
        }

        /// <summary>
        /// Stellt fehlende Blöcke wieder her.
        /// </summary>
        /// <param name="bloecke">Alle k + m Blöcke gleicher Länge; fehlende werden neu berechnet</param>
        /// <param name="vorhanden">Welche Blöcke gültig sind</param>
        /// <param name="nurDaten">Nur die Datenblöcke wiederherstellen (reicht zum Lesen)</param>
        /// <exception cref="InvalidOperationException">Wenn weniger als k Blöcke vorhanden sind</exception>
        public void Wiederherstellen(byte[][] bloecke, bool[] vorhanden, bool nurDaten = false)
        {
            Pruefen(bloecke);
            if (vorhanden == null || vorhanden.Length != Bloecke) throw new ArgumentException($"Es werden {Bloecke} Einträge erwartet", nameof(vorhanden));

            var gueltig = Enumerable.Range(0, Bloecke).Where(i => vorhanden[i]).Take(DatenBloecke).ToArray();
            if (gueltig.Length < DatenBloecke)
            {
                throw new InvalidOperationException($"Nur {gueltig.Length} von mindestens {DatenBloecke} Blöcken vorhanden");
            }

            var fehlendeDaten = Enumerable.Range(0, DatenBloecke).Where(i => !vorhanden[i]).ToList();
            if (fehlendeDaten.Count > 0)
            {
                // Zeilen der vorhandenen Blöcke bilden ein invertierbares k x k-System
                var teil = new byte[DatenBloecke, DatenBloecke];
                for (var z = 0; z < DatenBloecke; z++)
                    for (var s = 0; s < DatenBloecke; s++)
                        teil[z, s] = matrix[gueltig[z], s];
                var inverse = Galois.Invertieren(teil);

                foreach (var d in fehlendeDaten)
                {
                    var ziel = bloecke[d];
                    Array.Clear(ziel);
                    for (var z = 0; z < DatenBloecke; z++)
                    {
                        Galois.MultiplizierenUndAddieren(inverse[d, z], bloecke[gueltig[z]], ziel);
                    }
                }
            }

            if (nurDaten) return;
            for (var p = 0; p < ParitaetsBloecke; p++)
            {
                if (vorhanden[DatenBloecke + p]) continue;
                var ziel = bloecke[DatenBloecke + p];
                Array.Clear(ziel);
                for (var d = 0; d < DatenBloecke; d++)
                {
                    Galois.MultiplizierenUndAddieren(matrix[DatenBloecke + p, d], bloecke[d], ziel);
                }
            }
        }

        private void Pruefen(IReadOnlyList<byte[]> bloecke)
        {
            if (bloecke == null || bloecke.Count != Bloecke) throw new ArgumentException($"Es werden {Bloecke} Blöcke erwartet", nameof(bloecke));
            var laenge = bloecke[0]?.Length ?? throw new ArgumentException("Block 0 fehlt", nameof(bloecke));
            if (bloecke.Any(b => b == null || b.Length != laenge)) throw new ArgumentException("Alle Blöcke müssen gleich lang sein", nameof(bloecke));
        }

        /// <summary>
        /// Vandermonde-Matrix, so umgeformt, dass die oberen k Zeilen die Einheitsmatrix sind.
        /// Damit bleiben die Datenblöcke unverändert und jede Auswahl von k Zeilen ist invertierbar.
        /// </summary>
        private static byte[,] KodierMatrix(int k, int n)
        {
            var vandermonde = new byte[n, k];
            for (var z = 0; z < n; z++)
                for (var s = 0; s < k; s++)
                    vandermonde[z, s] = Galois.Potenz((byte)z, s);

            var oben = new byte[k, k];
            for (var z = 0; z < k; z++)
                for (var s = 0; s < k; s++)
                    oben[z, s] = vandermonde[z, s];

            return Galois.Multiplizieren(vandermonde, Galois.Invertieren(oben));
        }
    }

    /// <summary>
    /// Arithmetik im Galois-Feld GF(2^8) mit dem Polynom x^8 + x^4 + x^3 + x^2 + 1 (0x11D)
    /// </summary>
    internal static class Galois
    {
        private static readonly byte[] Exp = new byte[512];
        private static readonly byte[] Log = new byte[256];
        private static readonly byte[] MulTabelle = new byte[256 * 256];

        static Galois()
        {
            var x = 1;
            for (var i = 0; i < 255; i++)
            {
                Exp[i] = (byte)x;
                Log[x] = (byte)i;
                x <<= 1;
                if ((x & 0x100) != 0) x ^= 0x11D;
            }
            for (var i = 255; i < Exp.Length; i++) Exp[i] = Exp[i - 255];

            for (var a = 0; a < 256; a++)
                for (var b = 0; b < 256; b++)
                    MulTabelle[a * 256 + b] = Mal((byte)a, (byte)b);
        }

        public static byte Mal(byte a, byte b) => a == 0 || b == 0 ? (byte)0 : Exp[Log[a] + Log[b]];

        public static byte Inverses(byte a)
        {
            if (a == 0) throw new DivideByZeroException();
            return Exp[255 - Log[a]];
        }

        public static byte Potenz(byte a, int n)
        {
            if (n == 0) return 1;
            if (a == 0) return 0;
            return Exp[Log[a] * n % 255];
        }

        /// <summary>ziel ^= faktor * quelle</summary>
        public static void MultiplizierenUndAddieren(byte faktor, byte[] quelle, byte[] ziel)
        {
            if (faktor == 0) return;
            if (faktor == 1)
            {
                for (var i = 0; i < quelle.Length; i++) ziel[i] ^= quelle[i];
                return;
            }
            var zeile = new ReadOnlySpan<byte>(MulTabelle, faktor * 256, 256);
            for (var i = 0; i < quelle.Length; i++) ziel[i] ^= zeile[quelle[i]];
        }

        public static byte[,] Multiplizieren(byte[,] a, byte[,] b)
        {
            int zeilen = a.GetLength(0), mitte = a.GetLength(1), spalten = b.GetLength(1);
            var ergebnis = new byte[zeilen, spalten];
            for (var z = 0; z < zeilen; z++)
                for (var s = 0; s < spalten; s++)
                {
                    byte summe = 0;
                    for (var i = 0; i < mitte; i++) summe ^= Mal(a[z, i], b[i, s]);
                    ergebnis[z, s] = summe;
                }
            return ergebnis;
        }

        /// <summary>Gauß-Jordan-Elimination</summary>
        public static byte[,] Invertieren(byte[,] matrix)
        {
            var n = matrix.GetLength(0);
            var a = (byte[,])matrix.Clone();
            var inv = new byte[n, n];
            for (var i = 0; i < n; i++) inv[i, i] = 1;

            for (var spalte = 0; spalte < n; spalte++)
            {
                var pivot = spalte;
                while (pivot < n && a[pivot, spalte] == 0) pivot++;
                if (pivot == n) throw new InvalidOperationException("Matrix ist nicht invertierbar");
                if (pivot != spalte)
                {
                    for (var s = 0; s < n; s++)
                    {
                        (a[spalte, s], a[pivot, s]) = (a[pivot, s], a[spalte, s]);
                        (inv[spalte, s], inv[pivot, s]) = (inv[pivot, s], inv[spalte, s]);
                    }
                }

                var faktor = Inverses(a[spalte, spalte]);
                for (var s = 0; s < n; s++)
                {
                    a[spalte, s] = Mal(a[spalte, s], faktor);
                    inv[spalte, s] = Mal(inv[spalte, s], faktor);
                }

                for (var z = 0; z < n; z++)
                {
                    if (z == spalte || a[z, spalte] == 0) continue;
                    var f = a[z, spalte];
                    for (var s = 0; s < n; s++)
                    {
                        a[z, s] ^= Mal(f, a[spalte, s]);
                        inv[z, s] ^= Mal(f, inv[spalte, s]);
                    }
                }
            }
            return inv;
        }
    }
}
