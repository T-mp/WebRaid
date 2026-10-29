using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.WebDav
{
    /// <summary>
    /// Ein Speicherknoten, der die Dateien auf einem WebDAV-Server ablegt
    /// (z.B. Web.de, GMX, Nextcloud).
    /// </summary>
    public class WebDavNode : IRawNode
    {
        private static readonly HttpMethod Mkcol = new HttpMethod("MKCOL");

        private readonly HttpClient httpClient;
        private readonly ILogger<WebDavNode> logger;
        private readonly Uri basis;
        private readonly AuthenticationHeaderValue anmeldung;
        private readonly ConcurrentDictionary<string, bool> vorhandeneOrdner = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Erstellt einen WebDAV-Speicherknoten
        /// </summary>
        /// <param name="konfiguration">
        /// <para>Name: Eindeutiger Name des Knotens</para>
        /// <para>Url: Basis-Adresse auf dem WebDAV-Server, unterhalb der die Dateien abgelegt werden</para>
        /// <para>Benutzer / Passwort: Optional, für die Basic-Anmeldung</para>
        /// <para>UnsicheresHttpErlauben: Optional, erlaubt http:// zu anderen Rechnern als localhost</para>
        /// </param>
        /// <param name="httpClient">Für die Anfragen an den Server</param>
        /// <param name="logger">Logger, um dem Speicherknoten auf die Finger schauen zu können ;-)</param>
        public WebDavNode(IConfiguration konfiguration, HttpClient httpClient, ILogger<WebDavNode> logger)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger;
            Name = konfiguration["Name"];

            var url = konfiguration["Url"];
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException($"Knoten '{Name}': Url fehlt", nameof(konfiguration));
            basis = new Uri(url.EndsWith("/") ? url : url + "/", UriKind.Absolute);

            var unsicherErlaubt = string.Equals(konfiguration["UnsicheresHttpErlauben"], "true", StringComparison.OrdinalIgnoreCase);
            if (basis.Scheme != Uri.UriSchemeHttps && !basis.IsLoopback && !unsicherErlaubt)
            {
                throw new ArgumentException($"Knoten '{Name}': Nur https:// ist erlaubt, sonst gehen Zugangsdaten und Blöcke im Klartext übers Netz", nameof(konfiguration));
            }

            var benutzer = konfiguration["Benutzer"];
            if (!string.IsNullOrEmpty(benutzer))
            {
                var zugang = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{benutzer}:{konfiguration["Passwort"]}"));
                anmeldung = new AuthenticationHeaderValue("Basic", zugang);
            }

            logger.LogDebug($"New WebDavNode({Name}, {basis})");
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        /// <exception cref="HttpRequestException">Wenn der Server mit einem anderen Fehler als 404 antwortet</exception>
        public async Task<Stream> Get(string adresse)
        {
            logger.LogDebug($"Get({adresse})");
            var antwort = await Senden(HttpMethod.Get, adresse, null, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            if (antwort.StatusCode == HttpStatusCode.NotFound)
            {
                antwort.Dispose();
                logger.LogWarning($"'{adresse}' nicht gefunden!");
                return null;
            }

            if (!antwort.IsSuccessStatusCode)
            {
                antwort.Dispose();
                throw new HttpRequestException($"Knoten '{Name}': GET '{adresse}' fehlgeschlagen ({(int)antwort.StatusCode})", null, antwort.StatusCode);
            }

            return await antwort.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> Write(string adresse, Stream input)
        {
            logger.LogDebug($"Write({adresse})");
            if (string.IsNullOrWhiteSpace(adresse)) throw new ArgumentNullException(nameof(adresse));

            if (!await OrdnerAnlegen(adresse).ConfigureAwait(false)) return false;

            using (var antwort = await Senden(HttpMethod.Put, adresse, new StreamContent(input)).ConfigureAwait(false))
            {
                if (antwort.IsSuccessStatusCode) return true;

                logger.LogWarning($"Write({adresse}) fehlgeschlagen: {(int)antwort.StatusCode} {antwort.ReasonPhrase}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> Del(string adresse)
        {
            logger.LogDebug($"Del({adresse})");
            using (var antwort = await Senden(HttpMethod.Delete, adresse, null).ConfigureAwait(false))
            {
                if (antwort.IsSuccessStatusCode) return true;

                if (antwort.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.LogWarning($"'{adresse}' nicht gefunden!");
                }
                else
                {
                    logger.LogWarning($"Del({adresse}) fehlgeschlagen: {(int)antwort.StatusCode} {antwort.ReasonPhrase}");
                }
                return false;
            }
        }

        /// <summary>
        /// Legt die übergeordneten Ordner einer Adresse per MKCOL an, soweit nicht schon bekannt.
        /// </summary>
        private async Task<bool> OrdnerAnlegen(string adresse)
        {
            var teile = Teile(adresse);
            var ordner = "";
            for (var i = 0; i < teile.Length - 1; i++)
            {
                ordner = ordner + teile[i] + "/";
                if (vorhandeneOrdner.ContainsKey(ordner)) continue;

                using (var antwort = await Senden(Mkcol, ordner, null).ConfigureAwait(false))
                {
                    // 405: Ordner gibt es schon
                    if (!antwort.IsSuccessStatusCode && antwort.StatusCode != HttpStatusCode.MethodNotAllowed)
                    {
                        logger.LogWarning($"MKCOL({ordner}) fehlgeschlagen: {(int)antwort.StatusCode} {antwort.ReasonPhrase}");
                        return false;
                    }
                }
                logger.LogTrace($"OrdnerAnlegen: '{ordner}' vorhanden");
                vorhandeneOrdner[ordner] = true;
            }
            return true;
        }

        private Task<HttpResponseMessage> Senden(HttpMethod methode, string adresse, HttpContent inhalt,
            HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            var anfrage = new HttpRequestMessage(methode, Ziel(adresse)) { Content = inhalt };
            if (anmeldung != null) anfrage.Headers.Authorization = anmeldung;
            return httpClient.SendAsync(anfrage, option);
        }

        private Uri Ziel(string adresse)
        {
            var relativ = string.Join("/", Teile(adresse).Select(Uri.EscapeDataString));
            if (adresse.EndsWith("/")) relativ += "/";
            return new Uri(basis, relativ);
        }

        private static string[] Teile(string adresse)
        {
            var teile = adresse.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (teile.Any(t => t == "." || t == ".."))
            {
                throw new ArgumentException($"Adresse '{adresse}' darf kein '.' oder '..' enthalten", nameof(adresse));
            }
            return teile;
        }
    }
}
