using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Der Hauptschlüssel, wie er geschützt in der Schlüsseldatei steht
    /// </summary>
    public sealed class GeschuetzterSchluessel
    {
        /// <summary>Version des Dateiformats</summary>
        public int Format { get; set; } = 1;

        /// <summary>Name des Verfahrens, siehe <see cref="ISchluesselSchutz.Verfahren"/></summary>
        public string Verfahren { get; set; }

        /// <summary>Salz für die Schlüsselableitung (nur Passphrase)</summary>
        public byte[] Salz { get; set; }

        /// <summary>Argon2id: Speicherbedarf in KiB</summary>
        public int SpeicherKiB { get; set; }

        /// <summary>Argon2id: Durchläufe</summary>
        public int Durchlaeufe { get; set; }

        /// <summary>Argon2id: Parallelität</summary>
        public int Parallelitaet { get; set; }

        /// <summary>Nonce der Verschlüsselung (nur Passphrase)</summary>
        public byte[] Nonce { get; set; }

        /// <summary>Der geschützte Hauptschlüssel</summary>
        public byte[] Daten { get; set; }
    }

    /// <summary>
    /// Schützt den Hauptschlüssel, solange er auf der Platte liegt
    /// </summary>
    public interface ISchluesselSchutz
    {
        /// <summary>Kurzname, wird in der Schlüsseldatei vermerkt</summary>
        string Verfahren { get; }

        /// <summary>Schützt <paramref name="hauptschluessel"/></summary>
        GeschuetzterSchluessel Schuetzen(byte[] hauptschluessel);

        /// <summary>Gibt den Hauptschlüssel wieder frei</summary>
        /// <exception cref="CryptographicException">Falsche Passphrase, falsches Benutzerkonto oder beschädigte Datei</exception>
        byte[] Entschuetzen(GeschuetzterSchluessel geschuetzt);
    }

    /// <summary>
    /// Schutz über die Windows-DPAPI: die Datei lässt sich nur unter demselben Windows-Benutzerkonto öffnen,
    /// dafür startet der Dienst ohne Eingabe.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class DpapiSchutz : ISchluesselSchutz
    {
        private static readonly byte[] Entropie = Encoding.UTF8.GetBytes("WebRaid/Hauptschluessel");

        /// <inheritdoc />
        public string Verfahren => "dpapi";

        /// <inheritdoc />
        public GeschuetzterSchluessel Schuetzen(byte[] hauptschluessel) => new GeschuetzterSchluessel
        {
            Verfahren = Verfahren,
            Daten = ProtectedData.Protect(hauptschluessel, Entropie, DataProtectionScope.CurrentUser)
        };

        /// <inheritdoc />
        public byte[] Entschuetzen(GeschuetzterSchluessel geschuetzt) =>
            ProtectedData.Unprotect(geschuetzt.Daten, Entropie, DataProtectionScope.CurrentUser);
    }

    /// <summary>
    /// Schutz über eine Passphrase (z.B. auf dem Raspberry Pi): Argon2id leitet daraus einen Schlüssel ab,
    /// mit dem der Hauptschlüssel per ChaCha20-Poly1305 verschlüsselt wird.
    /// </summary>
    public sealed class PassphraseSchutz : ISchluesselSchutz
    {
        private static readonly byte[] Zusatzdaten = Encoding.UTF8.GetBytes("WebRaid/Hauptschluessel/1");
        private readonly byte[] passphrase;

        /// <summary>Argon2id-Speicherbedarf beim Anlegen in KiB (Standard 64 MiB, passt auf einen Pi 4)</summary>
        public int SpeicherKiB { get; init; } = 64 * 1024;

        /// <summary>Argon2id-Durchläufe beim Anlegen</summary>
        public int Durchlaeufe { get; init; } = 3;

        /// <summary>Argon2id-Parallelität beim Anlegen (der Pi 4 hat vier Kerne)</summary>
        public int Parallelitaet { get; init; } = 4;

        /// <summary>
        /// Schutz mit <paramref name="passphrase"/>
        /// </summary>
        public PassphraseSchutz(string passphrase)
        {
            if (string.IsNullOrEmpty(passphrase)) throw new ArgumentException("Die Passphrase darf nicht leer sein", nameof(passphrase));
            this.passphrase = Encoding.UTF8.GetBytes(passphrase);
        }

        /// <inheritdoc />
        public string Verfahren => "passphrase";

        /// <inheritdoc />
        public GeschuetzterSchluessel Schuetzen(byte[] hauptschluessel)
        {
            var geschuetzt = new GeschuetzterSchluessel
            {
                Verfahren = Verfahren,
                Salz = RandomNumberGenerator.GetBytes(16),
                SpeicherKiB = SpeicherKiB,
                Durchlaeufe = Durchlaeufe,
                Parallelitaet = Parallelitaet,
                Nonce = RandomNumberGenerator.GetBytes(12)
            };
            var daten = new byte[hauptschluessel.Length + 16];
            using (var aead = new ChaCha20Poly1305(Ableiten(geschuetzt)))
            {
                aead.Encrypt(geschuetzt.Nonce, hauptschluessel, daten.AsSpan(0, hauptschluessel.Length), daten.AsSpan(hauptschluessel.Length), Zusatzdaten);
            }
            geschuetzt.Daten = daten;
            return geschuetzt;
        }

        /// <inheritdoc />
        public byte[] Entschuetzen(GeschuetzterSchluessel geschuetzt)
        {
            if (geschuetzt.Daten == null || geschuetzt.Daten.Length < 16) throw new CryptographicException("Die Schlüsseldatei ist beschädigt");
            var laenge = geschuetzt.Daten.Length - 16;
            var hauptschluessel = new byte[laenge];
            try
            {
                using var aead = new ChaCha20Poly1305(Ableiten(geschuetzt));
                aead.Decrypt(geschuetzt.Nonce, geschuetzt.Daten.AsSpan(0, laenge), geschuetzt.Daten.AsSpan(laenge), hauptschluessel, Zusatzdaten);
            }
            catch (AuthenticationTagMismatchException e)
            {
                throw new CryptographicException("Falsche Passphrase oder beschädigte Schlüsseldatei", e);
            }
            return hauptschluessel;
        }

        private byte[] Ableiten(GeschuetzterSchluessel geschuetzt)
        {
            using var argon = new Argon2id(passphrase)
            {
                Salt = geschuetzt.Salz,
                MemorySize = geschuetzt.SpeicherKiB,
                Iterations = geschuetzt.Durchlaeufe,
                DegreeOfParallelism = geschuetzt.Parallelitaet
            };
            return argon.GetBytes(32);
        }
    }
}
