using System;

namespace WebRaid.Abstraction.VDS.Properties
{
    /// <summary>
    /// Property speichert wann des Element erstellt wurde.
    /// <para>Wenn es des Element nicht existiert, sollte dieses Property auch nicht vorhanden sein oder <see cref="DateTime.MinValue"/> beinhalten</para>
    /// </summary>
    public class CreationTime : IFileSystemInfoProperty
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="utc">wann des Element erstellt wurde</param>
        public CreationTime(DateTime utc)
        {
            Utc = utc;
        }

        /// <summary>
        /// wann des Element erstellt wurde
        /// </summary>
        public DateTime Utc { get; }
    }
}