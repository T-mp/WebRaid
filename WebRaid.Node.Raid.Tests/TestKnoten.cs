using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Raid.Tests
{
    /// <summary>
    /// Ein Speicher im Arbeitsspeicher, der sich ausschalten und manipulieren lässt
    /// </summary>
    internal sealed class TestKnoten : IRawNode
    {
        public ConcurrentDictionary<string, byte[]> Inhalt { get; } = new ConcurrentDictionary<string, byte[]>();

        /// <summary>Jeder Zugriff wirft eine Ausnahme, wie bei einem nicht erreichbaren Server</summary>
        public bool Ausgefallen { get; set; }

        /// <summary>Gelesene Daten werden verändert, wie bei einem manipulierenden Anbieter</summary>
        public bool Verfaelscht { get; set; }

        public TestKnoten(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Task<Stream> Get(string adresse)
        {
            Pruefen();
            if (!Inhalt.TryGetValue(adresse, out var daten)) return Task.FromResult<Stream>(null);
            if (Verfaelscht)
            {
                daten = (byte[])daten.Clone();
                daten[daten.Length - 1] ^= 0xFF;
            }
            return Task.FromResult<Stream>(new MemoryStream(daten, false));
        }

        public async Task<bool> Write(string adresse, Stream input)
        {
            Pruefen();
            using (var kopie = new MemoryStream())
            {
                await input.CopyToAsync(kopie);
                Inhalt[adresse] = kopie.ToArray();
            }
            return true;
        }

        public Task<bool> Del(string adresse)
        {
            Pruefen();
            return Task.FromResult(Inhalt.TryRemove(adresse, out _));
        }

        private void Pruefen()
        {
            if (Ausgefallen) throw new IOException($"{Name} ist nicht erreichbar");
        }
    }
}
