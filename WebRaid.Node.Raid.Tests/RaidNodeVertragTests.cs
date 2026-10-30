using System.Linq;
using FluentAssertions;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.Raid.Tests
{
    public class RaidNodeVertragTests : NodeTests<RaidNode>
    {
        protected override RaidNode GetNode() =>
            new RaidNode(configuration["Name"], Enumerable.Range(1, 3).Select(i => new TestKnoten($"K{i}")),
                new RaidOptionen { DatenBloecke = 2, ParitaetsBloecke = 1 },
                new FesterSchluessel(new byte[32]), Lf.Logger<RaidNode>());

        protected override void AddTestDaten(RaidNode node, string key, string daten) =>
            node.Write(key, GenerateStreamFromString(daten)).GetAwaiter().GetResult().Should().BeTrue();

        protected override void ContainKey(RaidNode node, string key) =>
            node.Get(key).GetAwaiter().GetResult().Should().NotBeNull();

        protected override void NotContainKey(RaidNode node, string key) =>
            node.Get(key).GetAwaiter().GetResult().Should().BeNull();
    }
}
