using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NWebDav.Server;
using NWebDav.Server.AspNetCore;
using NWebDav.Server.Stores;

namespace WebRaid.Node.WebDav.Tests
{
    /// <summary>
    /// Ein echter WebDAV-Server (NWebDav im TestServer), der in einen temporären Ordner speichert
    /// </summary>
    internal sealed class WebDavTestServer : IAsyncDisposable
    {
        private readonly IHost host;

        public string Ordner { get; }
        public HttpClient Client { get; }

        private WebDavTestServer(string ordner, IHost host)
        {
            Ordner = ordner;
            this.host = host;
            Client = host.GetTestClient();
        }

        public static async Task<WebDavTestServer> Starten()
        {
            var ordner = Path.Combine(Path.GetTempPath(), "WebDavNodeTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);

            var host = await new HostBuilder()
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

            return new WebDavTestServer(ordner, host);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await host.StopAsync();
            host.Dispose();
            Directory.Delete(Ordner, true);
        }
    }
}
