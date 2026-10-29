using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.WebDav
{
    /// <summary>
    /// Registriert die WebDAV-Knoten aus der Konfiguration
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Initialisierung
    {
        /// <summary>
        /// Legt für jeden Eintrag unter "WebDavNode:Knoten" einen <see cref="WebDavNode"/> an.
        /// </summary>
        public static IServiceCollection AddWebRaidWebDavNodes(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddHttpClient();

            foreach (var knoten in configuration.GetSection("WebDavNode:Knoten").GetChildren())
            {
                service.AddSingleton<IRawNode>(s => new WebDavNode(knoten,
                    s.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(WebDavNode)),
                    s.GetRequiredService<ILogger<WebDavNode>>()));
            }
            return service;
        }
    }
}
