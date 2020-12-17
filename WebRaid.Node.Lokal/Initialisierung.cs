using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Lokal
{
    [ExcludeFromCodeCoverage]
    public static class Initialisierung
    {
        public static IServiceCollection AddWebRaidLokalNodes(this IServiceCollection service,
            IConfiguration configuration)
        {
            var lnc = configuration.GetSection("Node");

            foreach (var pfad in lnc.GetSection("Pfade").GetChildren())
            {
                service.AddSingleton<IRawNode>(s => new Node(pfad, s.GetRequiredService<ILogger<Node>>()));
            }
            return service;
        }
    }
}
