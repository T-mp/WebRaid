using System.Linq;

namespace WebRaid.Abstraction.VDS
{
    /// <summary>
    ///   Führt Vorgänge für <see cref="T:System.String" />-Instanzen aus, die Datei- oder Verzeichnispfadinformationen enthalten.
    ///   Und ist, zur strengen Typisierung, eine Kapselung für Pfade.
    /// </summary>
    public class Pfad
    {
        /// <summary>
        ///   Stellt ein Zeichen bereit, das zur Trennung von Verzeichnisebenen in einer Pfadzeichenfolge verwendet wird um eine hierarchische Dateisystemorganisation wiederzugeben.
        /// </summary>
        public const char DirectorySeparatorChar = '/';
        /// <summary>
        ///   Stellt ein Zeichen bereit, das zur Trennung von Verzeichnisebenen in einer Pfadzeichenfolge verwendet wird um eine hierarchische Dateisystemorganisation wiederzugeben.
        ///   Ist mit <see cref="DirectorySeparatorChar"/> identisch.
        /// </summary>
        public const string DirectorySeparatorString = "/";
        /// <summary>
        /// Der Pfad als <see cref="T:System.String" />
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Konstruktor für die Kapselung, zur strengen Typisierung
        /// </summary>
        /// <param name="value">Der Pfad</param>
        public Pfad(string value)
        {
            Value = value;
        }
        /// <summary>
        ///   Gibt die Verzeichnisinformationen für die angegebene Pfadzeichenfolge zurück.
        /// </summary>
        /// <param name="path">
        ///   Der Pfad einer Datei oder eines Verzeichnisses.
        /// </param>
        /// <returns>
        ///   Verzeichnisinformationen für <paramref name="path" /> oder <see langword="null" />, wenn <paramref name="path" /> ein Stammverzeichnis bezeichnet oder NULL ist.
        ///    Gibt <see cref="F:System.String.Empty" /> zurück, wenn <paramref name="path" /> keine Verzeichnisinformationen enthält.
        /// </returns>
        public static string GetDirectoryName(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var parts = path.Split(DirectorySeparatorChar);
            return parts.Length <= 2
                ? DirectorySeparatorString
                : string.Join(DirectorySeparatorString, parts.Take(parts.Length - 1));
        }
        /// <summary>
        ///   Gibt die Erweiterung der angegebenen Pfadzeichenfolge zurück.
        /// </summary>
        /// <param name="path">
        ///   Die Pfadzeichenfolge, aus der die Erweiterung abgerufen werden soll.
        /// </param>
        /// <returns>
        ///   Die Erweiterung des angegebenen Pfads (einschließlich des Punkts ".") oder <see langword="null" /> oder <see cref="F:System.String.Empty" />.
        ///    Wenn <paramref name="path" /> gleich <see langword="null" /> ist, gibt <see cref="Pfad.GetExtension(string)" /><see langword="null" /> zurück.
        ///    Wenn <paramref name="path" /> keine Informationen über die Erweiterung enthält, gibt <see cref="Pfad.GetExtension(string)" /><see cref="F:System.String.Empty" /> zurück.
        /// </returns>
        public static string GetExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var parts = path.Split('.');
            return parts.Length < 2
                ? string.Empty
                : $".{parts.Last()}";
        }
        /// <summary>
        ///   Gibt den Dateinamen und die Erweiterung der angegebenen Pfadzeichenfolge zurück.
        /// </summary>
        /// <param name="path">
        ///   Die Pfadzeichenfolge, aus der der Dateiname und die Erweiterung abgerufen werden sollen.
        /// </param>
        /// <returns>
        ///   Die Zeichen nach dem letzten Verzeichniszeichen in <paramref name="path" />.
        ///    Wenn das letzte Zeichen von <paramref name="path" /> ein Verzeichnistrennzeichen ist, gibt diese Methode <see cref="string.Empty" /> zurück.
        ///    Wenn <paramref name="path" /> gleich <see langword="null" /> ist, gibt die Methode <see langword="null" /> zurück.
        /// </returns>
        public static string GetFileName(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var parts = path.Split(DirectorySeparatorChar);
            return parts.Length < 2
                ? string.Empty
                : $"{parts.Last()}";
        }
        /// <summary>
        ///   Gibt den Dateinamen der angegebenen Pfadzeichenfolge ohne Erweiterung zurück.
        /// </summary>
        /// <param name="path">Der Pfad der Datei.</param>
        /// <returns>
        ///   Die zurückgegebene Zeichenfolge <see cref="Pfad.GetFileName(string)" />, abzüglich des letzten Punkts (.)
        ///    und alle folgenden Zeichen.
        /// </returns>
        public static string GetFileNameWithoutExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var parts = GetFileName(path).Split('.');

            return parts.Length < 2
                ? parts.First()
                : string.Join(".", parts.Take(parts.Length - 1));
        }


        /// <summary>
        ///   Gibt die Verzeichnisinformationen für die angegebene Pfadzeichenfolge zurück.
        /// </summary>
        /// <returns>
        ///   Verzeichnisinformationen für <see cref="Value" /> oder <see langword="null" />, wenn <see cref="Value" /> ein Stammverzeichnis bezeichnet oder NULL ist.
        ///    Gibt <see cref="string.Empty" /> zurück, wenn <see cref="Value" /> keine Verzeichnisinformationen enthält.
        /// </returns>
        public string GetDirectoryName()
        {
            return GetDirectoryName(Value);
        }
        /// <summary>
        ///   Gibt die Erweiterung der angegebenen Pfadzeichenfolge zurück.
        /// </summary>
        /// <returns>
        ///   Die Erweiterung des angegebenen Pfads (einschließlich des Punkts ".") oder <see langword="null" /> oder <see cref="string.Empty" />.
        ///    Wenn <see cref="Value" /> gleich <see langword="null" /> ist, gibt <see cref="Pfad.GetExtension(string)" /><see langword="null" /> zurück.
        ///    Wenn <see cref="Value" /> keine Informationen über die Erweiterung enthält, gibt <see cref="Pfad.GetExtension(string)" /><see cref="string.Empty" /> zurück.
        /// </returns>
        public string GetExtension()
        {
            return GetExtension(Value);
        }
        /// <summary>
        ///   Gibt den Dateinamen und die Erweiterung der angegebenen Pfadzeichenfolge zurück.
        /// </summary>
        /// <returns>
        ///   Die Zeichen nach dem letzten Verzeichniszeichen in <see cref="Value" />.
        ///    Wenn das letzte Zeichen von <see cref="Value" /> ein Verzeichniszeichen ist, gibt diese Methode <see cref="string.Empty" /> zurück.
        ///    Wenn <see cref="Value" /> gleich <see langword="null" /> ist, gibt die Methode <see langword="null" /> zurück.
        /// </returns>
        public string GetFileName()
        {
            return GetFileName(Value);
        }
        /// <summary>
        ///   Gibt den Dateinamen der angegebenen Pfadzeichenfolge ohne Erweiterung zurück.
        /// </summary>
        /// <returns>
        ///   Die zurückgegebene Zeichenfolge <see cref="Pfad.GetFileName(string)" />, abzüglich des letzten Punkts (.)
        ///    und alle folgenden Zeichen.
        /// </returns>
        public string GetFileNameWithoutExtension()
        {
            return GetFileNameWithoutExtension(Value);
        }
    }
}