using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Legt den Hauptschlüssel eines RAIDs geschützt in einer lokalen Datei ab und lädt ihn wieder.
    /// <para>
    /// Geht die Datei verloren, ist ohne Wiederherstellungscode alles verloren. Deshalb wird eine vorhandene
    /// Schlüsseldatei nie stillschweigend überschrieben.
    /// </para>
    /// </summary>
    public static class Schluesseldatei
    {
        private const int Laenge = 32;

        /// <summary>
        /// Erzeugt einen neuen Hauptschlüssel und speichert ihn in <paramref name="pfad"/>
        /// </summary>
        /// <returns>Den Wiederherstellungscode; er muss dem Benutzer gezeigt und von ihm aufbewahrt werden</returns>
        /// <exception cref="IOException">Wenn <paramref name="pfad"/> schon existiert</exception>
        public static string Anlegen(string pfad, ISchluesselSchutz schutz)
        {
            var hauptschluessel = RandomNumberGenerator.GetBytes(Laenge);
            try
            {
                Speichern(pfad, schutz.Schuetzen(hauptschluessel), false);
                return Wiederherstellungscode.Erzeugen(hauptschluessel);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hauptschluessel);
            }
        }

        /// <summary>
        /// Legt die Schlüsseldatei aus einem Wiederherstellungscode neu an, z.B. auf einem neuen Rechner
        /// </summary>
        /// <exception cref="FormatException">Bei Tippfehlern im Code</exception>
        /// <exception cref="IOException">Wenn <paramref name="pfad"/> schon existiert</exception>
        public static void Wiederherstellen(string pfad, string code, ISchluesselSchutz schutz)
        {
            var hauptschluessel = Wiederherstellungscode.Lesen(code);
            try
            {
                Speichern(pfad, schutz.Schuetzen(hauptschluessel), false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hauptschluessel);
            }
        }

        /// <summary>
        /// Schützt den Hauptschlüssel neu, z.B. mit einer neuen Passphrase
        /// </summary>
        public static void SchutzWechseln(string pfad, ISchluesselSchutz alt, ISchluesselSchutz neu)
        {
            var hauptschluessel = Entschuetzen(pfad, alt);
            try
            {
                Speichern(pfad, neu.Schuetzen(hauptschluessel), true);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hauptschluessel);
            }
        }

        /// <summary>
        /// Lädt den Hauptschlüssel aus <paramref name="pfad"/>
        /// </summary>
        /// <exception cref="FileNotFoundException">Wenn es die Datei nicht gibt</exception>
        /// <exception cref="CryptographicException">Falsche Passphrase, falsches Benutzerkonto, falsches Verfahren oder beschädigte Datei</exception>
        public static ISchluesselQuelle Laden(string pfad, ISchluesselSchutz schutz)
        {
            var hauptschluessel = Entschuetzen(pfad, schutz);
            try
            {
                return new FesterSchluessel(hauptschluessel);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(hauptschluessel);
            }
        }

        private static byte[] Entschuetzen(string pfad, ISchluesselSchutz schutz)
        {
            if (pfad == null) throw new ArgumentNullException(nameof(pfad));
            if (schutz == null) throw new ArgumentNullException(nameof(schutz));
            if (!File.Exists(pfad)) throw new FileNotFoundException($"Die Schlüsseldatei '{pfad}' fehlt", pfad);

            GeschuetzterSchluessel geschuetzt;
            try
            {
                geschuetzt = JsonSerializer.Deserialize<GeschuetzterSchluessel>(File.ReadAllBytes(pfad));
            }
            catch (JsonException e)
            {
                throw new CryptographicException($"Die Schlüsseldatei '{pfad}' ist beschädigt", e);
            }
            if (geschuetzt?.Format != 1) throw new CryptographicException($"Unbekanntes Format der Schlüsseldatei '{pfad}'");
            if (geschuetzt.Verfahren != schutz.Verfahren)
            {
                throw new CryptographicException($"Die Schlüsseldatei '{pfad}' ist mit '{geschuetzt.Verfahren}' geschützt, nicht mit '{schutz.Verfahren}'");
            }
            if (geschuetzt.Verfahren == "passphrase" &&
                (geschuetzt.SpeicherKiB is < 8 or > 4 * 1024 * 1024 || geschuetzt.Durchlaeufe is < 1 or > 100 || geschuetzt.Parallelitaet is < 1 or > 64))
            {
                throw new CryptographicException($"Unplausible Argon2-Parameter in der Schlüsseldatei '{pfad}'");
            }

            var hauptschluessel = schutz.Entschuetzen(geschuetzt);
            if (hauptschluessel.Length != Laenge) throw new CryptographicException($"Die Schlüsseldatei '{pfad}' ist beschädigt");
            return hauptschluessel;
        }

        private static void Speichern(string pfad, GeschuetzterSchluessel geschuetzt, bool ersetzen)
        {
            if (pfad == null) throw new ArgumentNullException(nameof(pfad));
            if (!ersetzen && File.Exists(pfad)) throw new IOException($"Die Schlüsseldatei '{pfad}' existiert schon und wird nicht überschrieben");

            var ordner = Path.GetDirectoryName(Path.GetFullPath(pfad));
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);

            // Erst in eine Nachbardatei, dann ersetzen: ein Absturz hinterlässt nie eine halbe Schlüsseldatei
            var neu = pfad + ".neu";
            File.WriteAllBytes(neu, JsonSerializer.SerializeToUtf8Bytes(geschuetzt, new JsonSerializerOptions { WriteIndented = true }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(neu, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(neu, pfad, ersetzen);
        }
    }
}
