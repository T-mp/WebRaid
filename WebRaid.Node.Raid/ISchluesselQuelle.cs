using System;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Liefert den Hauptschlüssel eines RAIDs.
    /// Woher er kommt (DPAPI, Passphrase, ...) entscheidet die Implementierung.
    /// </summary>
    public interface ISchluesselQuelle
    {
        /// <summary>
        /// Der Hauptschlüssel, 32 Byte
        /// </summary>
        ReadOnlyMemory<byte> Hauptschluessel { get; }
    }

    /// <summary>
    /// Ein fest übergebener Hauptschlüssel, z.B. für Tests
    /// </summary>
    public sealed class FesterSchluessel : ISchluesselQuelle
    {
        private readonly byte[] schluessel;

        /// <summary>
        /// Übernimmt <paramref name="schluessel"/> (32 Byte)
        /// </summary>
        public FesterSchluessel(byte[] schluessel)
        {
            if (schluessel == null || schluessel.Length != 32) throw new ArgumentException("Der Hauptschlüssel muss 32 Byte lang sein", nameof(schluessel));
            this.schluessel = (byte[])schluessel.Clone();
        }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Hauptschluessel => schluessel;
    }
}
