using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Lokal
{
    /// <summary>
    /// Ein einfacher Speicherknoten der die Dateien in einem lokalen Dateisystem ablegt
    /// </summary>
    public class Node : IRawNode
    {
        private readonly ILogger<Node> logger;

        /// <summary>
        /// Erstellt einen lokalen Speicherknoten
        /// </summary>
        /// <param name="pfad">Pfad im lokalen Dateisystem</param>
        /// <param name="logger">Logger, um dem Speicherknoten auf die Finger schauen zu können ;-)</param>
        public Node(IConfiguration pfad, ILogger<Node> logger)
        {
            this.logger = logger;
            Name = pfad["Name"];
            this.pfad = pfad["Pfad"];

            logger.LogDebug($"New Node({Name})");
        }

        private readonly string pfad;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public async Task<Stream> Get(string adresse)
        {
            logger.LogDebug($"Get({adresse})");
            var path = Path.Combine(pfad, adresse);
            if (File.Exists(path))
            {
                return await Task.FromResult(File.OpenRead(path));
            }

            logger.LogWarning($"'{adresse}' nicht gefunden!");
            return null;
        }

        /// <inheritdoc />
        public async Task<bool> Write(string adresse, Stream input)
        {
            logger.LogDebug($"Write({adresse})");
            if (string.IsNullOrWhiteSpace(adresse)) throw new ArgumentNullException(nameof(adresse));
            var path = Path.Combine(pfad, adresse);
            var ordner = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(ordner)) throw new DirectoryNotFoundException($"Pfad:'{path}' konnte nicht verarbeitet werden");
            logger.LogTrace($"Write:path='{path}'");
            if (!Directory.Exists(ordner))
            {
                logger.LogTrace($"Write:ordner='{ordner}' wird angelegt.");
                Directory.CreateDirectory(ordner);
            }
            using (var outputStream = File.OpenWrite(path))
            {
                await input.CopyToAsync(outputStream)
                      .ConfigureAwait(false);
                outputStream.Close();
            }

            return true;
        }

        /// <inheritdoc />
        public Task<bool> Del(string adresse)
        {
            logger.LogDebug($"Del({adresse})");
            var path = Path.Combine(pfad, adresse);
            if (File.Exists(path))
            {
                File.Delete(adresse);
                return Task.FromResult(true);
            }

            logger.LogWarning($"'{adresse}' nicht gefunden!");
            return Task.FromResult(false);
        }
    }
}