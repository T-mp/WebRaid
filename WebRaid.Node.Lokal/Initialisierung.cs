using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Lokal
{
    /// <summary>
    /// Initialisierungserweiterung von <see cref="IServiceCollection"/>  für <see cref="WebRaid.Node.Lokal"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Initialisierung
    {
        /// <summary>
        /// Registriert alle lokalen Knoten anhand der Konfiguration
        /// </summary>
        /// <param name="service"><see cref="IServiceCollection"/> in der die Knoten registriert werden sollen</param>
        /// <param name="configuration">Konfiguration in der die Eigenschaften der Knoten enthalten ist</param>
        public static IServiceCollection AddWebRaidLokalNodes(this IServiceCollection service,
            IConfiguration configuration)
        {
            var lnc = configuration.GetSection("Node:Lokal");

            foreach (var pfad in lnc.GetSection("Pfade").GetChildren())
            {
                service.AddSingleton<IRawNode>(s => new Node(pfad, s.GetRequiredService<ILogger<Node>>()));
            }
            return service;
        }
    }
}
