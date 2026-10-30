using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Der Hauptschlüssel als Text zum Ausdrucken, z.B. „7K3M-...“ (14 Gruppen zu je 4 Zeichen).
    /// Damit lässt sich ein RAID auf einem neuen Rechner wieder öffnen, wenn die Schlüsseldatei verloren ist.
    /// <para>
    /// Kodiert wird mit Crockford-Base32 (ohne I, L, O, U), damit beim Abtippen nichts verwechselt wird;
    /// eine Prüfsumme fängt Tippfehler ab.
    /// </para>
    /// </summary>
    public static class Wiederherstellungscode
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private const byte Version = 1;
        private const int SchluesselLaenge = 32;
        private const int PruefLaenge = 2;
        private const int GruppenLaenge = 4;

        /// <summary>
        /// Erzeugt den Code für <paramref name="hauptschluessel"/>
        /// </summary>
        public static string Erzeugen(ReadOnlySpan<byte> hauptschluessel)
        {
            if (hauptschluessel.Length != SchluesselLaenge) throw new ArgumentException("Der Hauptschlüssel muss 32 Byte lang sein", nameof(hauptschluessel));

            var daten = new byte[1 + SchluesselLaenge + PruefLaenge];
            daten[0] = Version;
            hauptschluessel.CopyTo(daten.AsSpan(1));
            Pruefsumme(daten.AsSpan(0, 1 + SchluesselLaenge)).CopyTo(daten.AsSpan(1 + SchluesselLaenge));

            var text = Kodieren(daten);
            return string.Join("-", Enumerable.Range(0, text.Length / GruppenLaenge).Select(g => text.Substring(g * GruppenLaenge, GruppenLaenge)));
        }

        /// <summary>
        /// Liest einen Code, Groß-/Kleinschreibung, Leerzeichen und Bindestriche egal
        /// </summary>
        /// <exception cref="FormatException">Bei Tippfehlern oder unbekannter Version</exception>
        public static byte[] Lesen(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            var daten = Dekodieren(code);
            if (daten.Length != 1 + SchluesselLaenge + PruefLaenge) throw new FormatException("Der Wiederherstellungscode hat die falsche Länge");

            var erwartet = Pruefsumme(daten.AsSpan(0, 1 + SchluesselLaenge));
            if (!erwartet.AsSpan().SequenceEqual(daten.AsSpan(1 + SchluesselLaenge))) throw new FormatException("Der Wiederherstellungscode enthält einen Tippfehler");
            if (daten[0] != Version) throw new FormatException($"Unbekannte Version {daten[0]} des Wiederherstellungscodes");

            return daten.AsSpan(1, SchluesselLaenge).ToArray();
        }

        private static byte[] Pruefsumme(ReadOnlySpan<byte> daten)
        {
            var eingabe = Encoding.UTF8.GetBytes("WebRaid/Wiederherstellungscode").Concat(daten.ToArray()).ToArray();
            return SHA256.HashData(eingabe).AsSpan(0, PruefLaenge).ToArray();
        }

        private static string Kodieren(byte[] daten)
        {
            var text = new StringBuilder();
            int puffer = 0, bits = 0;
            foreach (var b in daten)
            {
                puffer = (puffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    text.Append(Alphabet[(puffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) text.Append(Alphabet[(puffer << (5 - bits)) & 31]);
            return text.ToString();
        }

        private static byte[] Dekodieren(string code)
        {
            var ergebnis = new System.Collections.Generic.List<byte>();
            int puffer = 0, bits = 0;
            foreach (var zeichen in code.ToUpperInvariant())
            {
                if (zeichen == '-' || char.IsWhiteSpace(zeichen)) continue;
                var wert = Alphabet.IndexOf(zeichen switch { 'O' => '0', 'I' or 'L' => '1', _ => zeichen });
                if (wert < 0) throw new FormatException($"Ungültiges Zeichen '{zeichen}' im Wiederherstellungscode");
                puffer = ((puffer << 5) | wert) & 0xFFFF;
                bits += 5;
                if (bits >= 8)
                {
                    ergebnis.Add((byte)(puffer >> (bits - 8)));
                    bits -= 8;
                }
            }
            return ergebnis.ToArray();
        }
    }
}
