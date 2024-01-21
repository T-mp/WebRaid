using NUnit.Framework;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.WebDav.Tests
{
    [Ignore("Noch nicht implementiert")]
    public class WebDavNodeTests : NodeTests<Node>
    {
        protected override void AddTestDaten(Node node, string key, string daten)
        {
            throw new System.NotImplementedException();
        }

        protected override void ContainKey(Node node, string key)
        {
            throw new System.NotImplementedException();
        }

        protected override Node GetNode()
        {
            throw new System.NotImplementedException();
        }

        protected override void NotContainKey(Node node, string key)
        {
            throw new System.NotImplementedException();
        }
    }
}