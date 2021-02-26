namespace WebRaid.VDS
{
    /// <summary>
    /// Generiert "Adressen" für die Speicher-Knoten
    /// </summary>
    public interface IFileAdressenGenerator
    {
        /// <summary>
        /// Gibt eine zufällige nicht näher spezifizierte Adresse zurück
        /// </summary>
        string GetNew();
        /// <summary>
        /// Gibt eine zufällige in <paramref name="blockGröße"/> unterteilte Adresse zurück.
        /// </summary>
        /// <param name="blockGröße">Blockgröße zum unterteilen der Adresse</param>
        string GetNew(int blockGröße);
        /// <summary>
        /// Gibt eine mit <paramref name="separator"/> unterteilte Adresse zurück
        /// </summary>
        /// <param name="separator">Seperierungs-Zeichen</param>
        string GetNew(string separator);

        /// <summary>
        /// Gibt eine zufällige in <paramref name="blockGröße"/> mit <paramref name="separator"/> unterteilte Adresse zurück.
        /// </summary>
        /// <param name="blockGröße">Blockgröße zum unterteilen der Adresse</param>
        /// <param name="separator">Seperierungs-Zeichen</param>
        string GetNew(int blockGröße, string separator);
    }
}