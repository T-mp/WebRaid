using System;
using System.Collections.Generic;
using System.IO;

namespace WebRaid.Abstraction.VDS
{
    /// <summary>
    /// Schnittstelle zur beschreibung eines "Ordners"
    /// </summary>
    public interface IDirectoryInfo:IFileSystemInfo
    {
        /// <summary>
        /// Löscht diesen Ordner.
        /// Soll "Atomar" erfolgen, das heist entweder komplett oder gar nicht
        /// </summary>
        /// <param name="recursive">bestimmt ob alle unterelemente mit gelöscht werden sollen</param>
        /// <exception cref="InvalidOperationException">Soll ausgelöst werden wenn der Ordner nicht gelöscht werden kann. Z.B. weil noch unterelemente vorhanden sind</exception>
        void Delete(bool recursive);
        /// <summary>
        /// Erzeugt Unterordner
        /// <para>Auch Strukturen / mehrere Ebenen?</para>
        /// </summary>
        /// <param name="path">Relativer Pfad innerhalb dieses Ordners</param>
        /// <returns>Das Info-Objekt des erzeugten Ziel-Ordners</returns>
        IDirectoryInfo CreateSubdirectory(string path);
        /// <summary>
        /// Gibt eine Liste der Dateien in diesem Ordner zurück.
        /// </summary>
        /// <param name="searchPattern">Optionaler Namens-Filter</param>
        /// <param name="searchOption">Bestimmt ob auch Unterordner durchsucht werden sollen</param>
        /// <returns>Liste der Dateien</returns>
        IEnumerable<IFileInfo> GetFiles(string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        /// <summary>
        /// Gibt eine Liste der Enthaltenen Objekte zurück.
        /// </summary>
        /// <param name="searchPattern">Optionaler Namens-Filter</param>
        /// <param name="searchOption">Bestimmt ob auch Unterordner durchsucht werden sollen</param>
        /// <returns>Liste der Objekte</returns>
        IEnumerable<IFileSystemInfo> GetFileSystemInfos(string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        /// <summary>
        /// Gibt eine Liste der Dateien in diesem Ordner zurück.
        /// </summary>
        /// <param name="searchPattern">Optionaler Namens-Filter</param>
        /// <param name="searchOption">Bestimmt ob auch Unterordner durchsucht werden sollen</param>
        /// <returns>Liste der Ordner</returns>
        IEnumerable<IDirectoryInfo> GetDirectories(string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    }
}