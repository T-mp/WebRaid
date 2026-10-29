using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NWebDav.Server;
using NWebDav.Server.AspNetCore;
using NWebDav.Server.Stores;
using Test.Extensions.Logging;

namespace WebRaid.Node.WebDav.Tests
{
    /// <summary>
    /// Testet den <see cref="WebDavNode"/> gegen einen echten WebDAV-Server (NWebDav im TestServer)
    /// </summary>
    public class WebDavNodeTests : BasisTestKlasse
    {
        private string ordner;
        private IHost host;
        private HttpClient client;

        public WebDavNodeTests() : base(LogLevel.Trace) { }

        [SetUp]
        public async Task Setup()
        {
            ordner = Path.Combine(Path.GetTempPath(), "WebDavNodeTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);

            host = await new HostBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(s => s.Configure<TestServerOptions>(o => o.AllowSynchronousIO = true))
                    .Configure(app =>
                    {
                        var dispatcher = new WebDavDispatcher(new DiskStore(ordner), new RequestHandlerFactory());
                        app.Run(context =>
                        {
                            // NWebDav braucht eine Gegenstelle, der TestServer liefert keine
                            context.Connection.RemoteIpAddress = IPAddress.Loopback;
                            return dispatcher.DispatchRequestAsync(new AspNetCoreContext(context));
                        });
                    }))
                .StartAsync();
            client = host.GetTestClient();
        }

        [TearDown]
        public async Task TearDown()
        {
            client.Dispose();
            await host.StopAsync();
            host.Dispose();
            Directory.Delete(ordner, true);
        }

        private WebDavNode UnterTest(string url = "http://localhost/", HttpClient httpClient = null,
            Dictionary<string, string> weitere = null)
        {
            var werte = new Dictionary<string, string> { ["Name"] = "Test1", ["Url"] = url };
            if (weitere != null) foreach (var w in weitere) werte[w.Key] = w.Value;
            var konfiguration = new ConfigurationBuilder().AddInMemoryCollection(werte).Build();
            return new WebDavNode(konfiguration, httpClient ?? client, Lf.Logger<WebDavNode>());
        }

        private static Stream AlsStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

        private static async Task<string> Lesen(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        [Test]
        public async Task Get_NichtVorhanden_Gibt_null()
        {
            var stream = await UnterTest().Get("GibtEsNicht");

            stream.Should().BeNull();
        }

        [Test]
        public async Task Write_DannGet_Gibt_Inhalt()
        {
            var unterTest = UnterTest();

            (await unterTest.Write("Block1", AlsStream("TestDaten"))).Should().BeTrue();

            (await Lesen(await unterTest.Get("Block1"))).Should().Be("TestDaten");
            File.ReadAllText(Path.Combine(ordner, "Block1")).Should().Be("TestDaten");
        }

        [Test]
        public async Task Write_InUnterordner_LegtOrdnerAn()
        {
            var unterTest = UnterTest();

            (await unterTest.Write("a/b/Block2", AlsStream("Tief"))).Should().BeTrue();
            (await unterTest.Write("a/b/Block3", AlsStream("Daneben"))).Should().BeTrue();

            (await Lesen(await unterTest.Get("a/b/Block2"))).Should().Be("Tief");
            File.ReadAllText(Path.Combine(ordner, "a", "b", "Block3")).Should().Be("Daneben");
        }

        [Test]
        public async Task Write_Ueberschreibt_VorhandenenBlock()
        {
            var unterTest = UnterTest();
            await unterTest.Write("Block1", AlsStream("Alter, längerer Inhalt"));

            await unterTest.Write("Block1", AlsStream("Neu"));

            (await Lesen(await unterTest.Get("Block1"))).Should().Be("Neu");
        }

        [Test]
        public async Task Write_InBasisUnterpfad_LegtDortAb()
        {
            Directory.CreateDirectory(Path.Combine(ordner, "webraid"));
            var unterTest = UnterTest("http://localhost/webraid");

            (await unterTest.Write("x/Block1", AlsStream("Daten"))).Should().BeTrue();

            File.ReadAllText(Path.Combine(ordner, "webraid", "x", "Block1")).Should().Be("Daten");
        }

        [Test]
        public async Task Del_Vorhanden_LoeschtUndGibt_true()
        {
            var unterTest = UnterTest();
            await unterTest.Write("Block1", AlsStream("TestDaten"));

            (await unterTest.Del("Block1")).Should().BeTrue();

            File.Exists(Path.Combine(ordner, "Block1")).Should().BeFalse();
            (await unterTest.Get("Block1")).Should().BeNull();
        }

        [Test]
        public async Task Del_NichtVorhanden_Gibt_false()
        {
            (await UnterTest().Del("GibtEsNicht")).Should().BeFalse();
        }

        [Test]
        public async Task Adresse_MitPunktPunkt_WirdAbgelehnt()
        {
            Func<Task> aktion = () => UnterTest().Get("../geheim");

            await aktion.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public void Konstruktor_HttpZuFremdemRechner_WirdAbgelehnt()
        {
            Action aktion = () => UnterTest("http://webdav.example.org/");

            aktion.Should().Throw<ArgumentException>().WithMessage("*https*");
        }

        [Test]
        public void Konstruktor_HttpZuFremdemRechner_MitAusdruecklicherErlaubnis_Geht()
        {
            Action aktion = () => UnterTest("http://webdav.example.org/",
                weitere: new Dictionary<string, string> { ["UnsicheresHttpErlauben"] = "true" });

            aktion.Should().NotThrow();
        }

        [Test]
        public async Task Anfragen_SendenBasicAnmeldung()
        {
            var aufzeichner = new Aufzeichner(HttpStatusCode.NotFound);
            var unterTest = UnterTest("https://webdav.example.org/", new HttpClient(aufzeichner),
                new Dictionary<string, string> { ["Benutzer"] = "thomas", ["Passwort"] = "geheim" });

            await unterTest.Get("Block1");

            aufzeichner.Letzte.Headers.Authorization.Scheme.Should().Be("Basic");
            Encoding.UTF8.GetString(Convert.FromBase64String(aufzeichner.Letzte.Headers.Authorization.Parameter))
                .Should().Be("thomas:geheim");
            aufzeichner.Letzte.RequestUri.Should().Be(new Uri("https://webdav.example.org/Block1"));
        }

        [Test]
        public async Task Get_ServerFehler_WirftAusnahme()
        {
            var unterTest = UnterTest("https://webdav.example.org/",
                new HttpClient(new Aufzeichner(HttpStatusCode.Unauthorized)));

            Func<Task> aktion = () => unterTest.Get("Block1");

            (await aktion.Should().ThrowAsync<HttpRequestException>())
                .Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public async Task Write_ServerFehler_Gibt_false()
        {
            var unterTest = UnterTest("https://webdav.example.org/",
                new HttpClient(new Aufzeichner(HttpStatusCode.InsufficientStorage)));

            (await unterTest.Write("Block1", AlsStream("TestDaten"))).Should().BeFalse();
        }

        private class Aufzeichner : HttpMessageHandler
        {
            private readonly HttpStatusCode antwort;
            public HttpRequestMessage Letzte { get; private set; }

            public Aufzeichner(HttpStatusCode antwort)
            {
                this.antwort = antwort;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Letzte = request;
                return Task.FromResult(new HttpResponseMessage(antwort));
            }
        }
    }
}
