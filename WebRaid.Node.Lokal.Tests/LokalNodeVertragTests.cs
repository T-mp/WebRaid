using System;
using System.IO;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.Lokal.Tests
{
    public class LokalNodeVertragTests : NodeTests<Node>
    {
        private string pfad;

        public override void SetUp()
        {
            base.SetUp();
            pfad = Path.Combine(Path.GetTempPath(), "LokalNodeVertragTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pfad);
            configuration["Pfad"].Returns(pfad);
        }

        public override void TearDown()
        {
            Directory.Delete(pfad, true);
            base.TearDown();
        }

        protected override Node GetNode() => new Node(configuration, Lf.Logger<Node>());

        protected override void AddTestDaten(Node node, string key, string daten) =>
            File.WriteAllText(Path.Combine(pfad, key), daten);

        protected override void ContainKey(Node node, string key) =>
            File.Exists(Path.Combine(pfad, key)).Should().BeTrue();

        protected override void NotContainKey(Node node, string key) =>
            File.Exists(Path.Combine(pfad, key)).Should().BeFalse();
    }
}
