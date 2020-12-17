using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Memory
{
    /// <summary>
    /// Ein einfacher Speicherknoten der die Dateien im RAM hält.
    /// </summary>
    public class Node : IRawNode
    {
        private readonly ILogger<Node> logger;
        internal readonly Dictionary<string, Stream> Speicher = new Dictionary<string, Stream>();

        /// <summary>
        /// Erstellt einen in Memory Speicherknoten
        /// </summary>
        /// <param name="name">Konfiguration mit "Name" für den Knoten</param>
        /// <param name="logger">Logger, um dem Speicherknoten auf die Finger schauen zu können ;-)</param>
        public Node(IConfiguration name, ILogger<Node> logger)
        {
            this.logger = logger;
            Name = name["Name"];

            logger.LogDebug($"New LokalNode({Name})");
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public async Task<Stream> Get(string adresse)
        {
            logger.LogDebug($"Get({adresse})");
            if (Speicher.ContainsKey(adresse))
            {
                var stream = Speicher[adresse];
                stream.Position = 0;
                return await Task.FromResult(stream);
            }

            logger.LogWarning($"'{adresse}' nicht gefunden!");
            return null;
        }

        /// <inheritdoc />
        public async Task<bool> Write(string adresse, Stream input)
        {
            logger.LogDebug($"Write({adresse})");
            if (!Speicher.ContainsKey(adresse))
            {
                logger.LogTrace("Write:Stream wird angelegt");
                Speicher[adresse] = new MemoryStream();
            }

            Speicher[adresse].Position = 0;
            await input.CopyToAsync(Speicher[adresse])
                .ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc />
        public Task<bool> Del(string adresse)
        {
            logger.LogDebug($"Del({adresse})");
            if (Speicher.ContainsKey(adresse))
            {
                Speicher.Remove(adresse);
                return Task.FromResult(true);
            }

            logger.LogWarning($"'{adresse}' nicht gefunden!");
            return Task.FromResult(false);
        }
    }
}
