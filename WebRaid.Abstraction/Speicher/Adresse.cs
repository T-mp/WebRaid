using System;

namespace WebRaid.Abstraction.Speicher
{
    /// <summary>
    /// Gemeinsame Prüfungen für Adressen in <see cref="INode"/>-Implementierungen
    /// </summary>
    public static class Adresse
    {
        /// <summary>
        /// Stellt sicher, dass eine Adresse angegeben wurde
        /// </summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="adresse"/> <see langword="null"/> ist</exception>
        /// <exception cref="ArgumentOutOfRangeException">Wenn <paramref name="adresse"/> leer ist oder nur aus Leerzeichen besteht</exception>
        public static void Pruefen(string adresse)
        {
            if (adresse == null) throw new ArgumentNullException(nameof(adresse), "Eine Adresse wird benötigt!");
            if (string.IsNullOrWhiteSpace(adresse)) throw new ArgumentOutOfRangeException(nameof(adresse), "Eine Adresse wird benötigt!");
        }
    }
}
