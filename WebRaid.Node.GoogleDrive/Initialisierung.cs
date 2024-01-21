using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.GoogleDrive
{
    /// <summary>
    /// Initialisierungserweiterung von <see cref="IServiceCollection"/>  für <see cref="Memory"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Initialisierung
    {
        /// <summary>
        /// Registriert alle in Memory Knoten anhand der Konfiguration
        /// </summary>
        /// <param name="service"><see cref="IServiceCollection"/> in der die Knoten registriert werden sollen</param>
        /// <param name="configuration">Konfiguration in der die Eigenschaften der Knoten enthalten ist</param>
        public static IServiceCollection AddWebRaidMemoryNodes(this IServiceCollection service, IConfiguration configuration)
        {
            var lnc = configuration.GetSection("Node:Memory");

            foreach (var pfad in lnc.GetSection("Namen").GetChildren())
            {
                service.AddSingleton<IRawNode>(s => new Node(pfad, s.GetRequiredService<ILogger<Node>>()));
            }
            return service;
        }
    }
}
