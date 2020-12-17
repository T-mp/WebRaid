using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebRaid.Abstraction;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Memory
{
    public class MemoryNode : IRawNode
    {
        private readonly ILogger<MemoryNode> logger;
        internal readonly Dictionary<string, Stream> Speicher = new Dictionary<string, Stream>();

        public MemoryNode(IConfiguration name, ILogger<MemoryNode> logger)
        {
            this.logger = logger;
            Name = name["Name"];

            logger.LogDebug($"New LokalNode({Name})");
        }

        public string Name { get; }
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

        public async Task<bool> Write(string adresse, Stream input)
        {
            logger.LogDebug($"Write({adresse})");
            if (!Speicher.ContainsKey(adresse))
            {
                logger.LogTrace($"Write:Stream wird angelegt");
                Speicher[adresse] = new MemoryStream();
            }

            Speicher[adresse].Position = 0;
            await input.CopyToAsync(Speicher[adresse])
                .ConfigureAwait(false);

            return true;
        }

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
