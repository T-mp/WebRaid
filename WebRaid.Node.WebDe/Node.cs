using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.WebDe
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
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public async Task<Stream> Get(string adresse)
        {
            logger.LogDebug($"Get({adresse})");
            AssertAdresseNotIsNullOrWhiteSpace(adresse);

            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<bool> Write(string adresse, Stream input)
        {
            logger.LogDebug($"Write({adresse})");
            AssertAdresseNotIsNullOrWhiteSpace(adresse);
            if (input == null) throw new ArgumentNullException(nameof(input));

            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<bool> Del(string adresse)
        {
            logger.LogDebug($"Del({adresse})");
            AssertAdresseNotIsNullOrWhiteSpace(adresse);

            throw new System.NotImplementedException();
        }

        private void AssertAdresseNotIsNullOrWhiteSpace(string adresse)
        {
            if (adresse == null) throw new ArgumentNullException(nameof(adresse), "Eine Adresse wird benötigt!");
            if (string.IsNullOrWhiteSpace(adresse)) throw new ArgumentOutOfRangeException(nameof(adresse), "Eine Adresse wird benötigt!");
        }
    }
}
