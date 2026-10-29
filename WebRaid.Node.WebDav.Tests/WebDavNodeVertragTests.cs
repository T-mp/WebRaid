using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.WebDav.Tests
{
    public class WebDavNodeVertragTests : NodeTests<WebDavNode>
    {
        private WebDavTestServer server;

        [SetUp]
        public async Task ServerStarten()
        {
            server = await WebDavTestServer.Starten();
        }

        [TearDown]
        public async Task ServerStoppen()
        {
            await server.DisposeAsync();
        }

        protected override WebDavNode GetNode()
        {
            var konfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["Name"] = "Test1", ["Url"] = "http://localhost/" })
                .Build();
            return new WebDavNode(konfiguration, server.Client, Lf.Logger<WebDavNode>());
        }

        protected override void AddTestDaten(WebDavNode node, string key, string daten) =>
            File.WriteAllText(Path.Combine(server.Ordner, key), daten);

        protected override void ContainKey(WebDavNode node, string key) =>
            File.Exists(Path.Combine(server.Ordner, key)).Should().BeTrue();

        protected override void NotContainKey(WebDavNode node, string key) =>
            File.Exists(Path.Combine(server.Ordner, key)).Should().BeFalse();
    }
}
