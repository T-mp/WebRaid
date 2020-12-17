using System.IO;

namespace WebRaid.Abstraction.VDS
{
    /// <summary>
    /// Beschreibt eine "Datei"
    /// </summary>
    public interface IFileInfo: IFileSystemInfo
    {
        /// <summary>
        /// Größe der Datei
        /// </summary>
        ulong Length { get; }
        /// <summary>
        /// Bestimmt ob die Datei beschrieben werden kann
        /// </summary>
        bool IsReadOnly { get; set; }
        /// <summary>
        /// Gibt einen entsprechende Stream zurück
        /// </summary>
        /// <returns></returns>
        Stream Open();
    }
}