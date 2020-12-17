using System;

namespace WebRaid.Abstraction.VDS.Properties
{
    /// <summary>
    /// Property speichert wann des Element zu letzt beschrieben wurde.
    /// <para>Wenn es des Element nicht existiert, sollte dieses Property auch nicht vorhanden sein oder <see cref="DateTime.MinValue"/> beinhalten</para>
    /// </summary>
    public class LastWriteTime : IFileSystemInfoProperty
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="utc">wann des Element zu letzt beschrieben wurde</param>
        public LastWriteTime(DateTime utc)
        {
            Utc = utc;
        }

        /// <summary>
        /// wann des Element zu letzt beschrieben wurde
        /// </summary>
        public DateTime Utc { get; }
    }
}