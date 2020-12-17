using System.IO;
using System.Threading.Tasks;

namespace WebRaid.Abstraction.Speicher
{
    /// <summary>
    /// Ein Speicherknoten-Primitiv
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Eindeutiger Name des Knotens, zur identifizierung in Konfigurationen
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Holt einen Stream zum lesen
        /// </summary>
        /// <param name="adresse">Adresse / Pfad im weiteren Sinn</param>
        /// <returns>
        /// <para>Wenn vorhanden: Ein <see cref="Stream"/> zum lesen</para>
        /// <para>Wenn nicht vorhanden: <see langword="null" /></para>
        /// </returns>
        Task<Stream> Get(string adresse);

        /// <summary>
        /// Schriebt einen Stream an eine Adresse
        /// </summary>
        /// <param name="adresse">Adresse / Pfad im weiteren Sinn</param>
        /// <param name="input"><see cref="Stream"/> der ab der aktuellen Position bis zum Ende geschrieben werden soll</param>
        /// <returns>
        /// <para>Wenn erfolgreich: <see langword="true"/></para>
        /// <para>Wenn nicht erfolgreich: <see langword="false" /></para>
        /// </returns>
        Task<bool> Write(string adresse, Stream input);

        /// <summary>
        /// Löscht die Persistenz mit dieser Adresse
        /// </summary>
        /// <param name="adresse">Adresse / Pfad im weiteren Sinn</param>
        /// <returns>
        /// <para>Wenn erfolgreich: <see langword="true"/></para>
        /// <para>Wenn nicht erfolgreich: <see langword="false" /></para>
        /// </returns>
        Task<bool> Del(string adresse);
    }
}