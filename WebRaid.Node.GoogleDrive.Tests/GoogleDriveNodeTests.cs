using NSubstitute;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using WebRaid.Node.GoogleDrive;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.GoogleDrive.Tests
{
    [Ignore("Noch nicht implementiert, Problem Anmeldung bei Google")]
    public class GoogleDriveNodeTests : NodeTests<Node>
    {
        private string path;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            path = InitTestOrdner("GoogleDriveNodeTests");
            var Name = "thomas";
            configuration["Name"].Returns(Name);

            configuration["Pfad"].Returns(path);

            File.WriteAllText(Path.Combine(path, $"{Name}.json"), @"{""installed"":{""client_id"":""422872381570-7juhdf84ct72g5bmol8frjo93sedmq97.apps.googleusercontent.com"",""project_id"":""temporal-tensor-2015-1"",""auth_uri"":""https://accounts.google.com/o/oauth2/auth"",""token_uri"":""https://oauth2.googleapis.com/token"",""auth_provider_x509_cert_url"":""https://www.googleapis.com/oauth2/v1/certs"",""client_secret"":""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""}}");
        }
        [TearDown]
        public override void TearDown()
        {
            KillTestOrdner(path);
        }

        private static string InitTestOrdner(string testOrdnerName)
        {
            var path = Path.Combine(Path.GetTempPath(), testOrdnerName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);
            return path;
        }
        private static void KillTestOrdner(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

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
            return new Node(configuration, Lf.Logger<Node>());
        }

        protected override void NotContainKey(Node node, string key)
        {
            throw new System.NotImplementedException();
        }
    }
}