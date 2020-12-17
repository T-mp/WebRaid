using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Memory
{
    [ExcludeFromCodeCoverage]
    public static class MemoryNodesInitialisierung
    {
        public static IServiceCollection AddWebRaidMemoryNodes(this IServiceCollection service, IConfiguration configuration)
        {
            var lnc = configuration.GetSection("MemoryNode");

            foreach (var pfad in lnc.GetSection("Namen").GetChildren())
            {
                service.AddSingleton<IRawNode>(s => new MemoryNode(pfad, s.GetRequiredService<ILogger<MemoryNode>>()));
            }
            return service;
        }
    }
}
