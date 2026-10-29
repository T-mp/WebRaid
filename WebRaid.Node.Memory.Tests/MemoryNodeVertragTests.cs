using FluentAssertions;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.Memory.Tests
{
    public class MemoryNodeVertragTests : NodeTests<MemoryNode>
    {
        protected override MemoryNode GetNode() => new MemoryNode(configuration, Lf.Logger<MemoryNode>());

        protected override void AddTestDaten(MemoryNode node, string key, string daten) =>
            node.Speicher[key] = GenerateStreamFromString(daten);

        protected override void ContainKey(MemoryNode node, string key) =>
            node.Speicher.Should().ContainKey(key);

        protected override void NotContainKey(MemoryNode node, string key) =>
            node.Speicher.Should().NotContainKey(key);
    }
}
