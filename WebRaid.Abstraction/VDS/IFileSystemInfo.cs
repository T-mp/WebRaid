using System;
using System.Collections.Generic;

namespace WebRaid.Abstraction.VDS
{
    /// <summary>
    /// Beschreibt ein Dateisystem-Objekt (Ordner <see cref="IDirectoryInfo"/> oder Datei <see cref="IFileInfo"/>)
    /// </summary>
    public interface IFileSystemInfo
    {
        /// <summary>
        /// Adresse im SpeicherNode
        /// </summary>
        public string Adresse { get; }
        /// <summary>
        /// Vollständiger Name inc. Pfad und Endung
        /// </summary>
        string FullName { get; }
        /// <summary>
        /// Name inc. Endung
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Endung
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// Gibt zurück ob das Element wirklich existiert.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Löscht das Element
        /// </summary>
        /// <exception cref="InvalidOperationException">Soll ausgelöst werden wenn das Element nicht gelöscht werden kann.</exception>
        void Delete();

        /// <summary>
        /// Verzeichnis von Eigenschaften
        /// </summary>
        IList<IFileSystemInfoProperty> Properties { get; }
    }
}