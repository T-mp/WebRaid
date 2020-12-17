using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Test.Extensions.Logging;

namespace WebRaid.Node.Lokal.Tests
{
    public class LokalNodeTests
    {
        private readonly TestLoggerFactory lf;

        public LokalNodeTests()
        {
            lf = new TestLoggerFactory(LogLevel.Trace);
        }
        [Test]
        public async Task GetStream_NichtVorhandeneDatei_Gibt_null()
        {
            var path = Path.Combine(Path.GetTempPath(), "LokalNodeTests");
            Directory.CreateDirectory(path);

            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");
            configuration["Pfad"].Returns(path);

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var stream = await unterTest.Get("GibtEsNicht");

            stream.Should().BeNull();

            Directory.Delete(path, true);
        }
        [Test]
        public async Task GetStream_VorhandeneDatei_Gibt_InhaltAlsStream()
        {
            var path = InitTestOrdner("LokalNodeTests");

            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");
            configuration["Pfad"].Returns(path);

            await File.WriteAllTextAsync(Path.Combine(path, "GibtEs"), "TestDaten");

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var stream = await unterTest.Get("GibtEs");

            stream.Should().NotBeNull();

            var reader = new StreamReader(stream);
            var gelesen = await reader.ReadToEndAsync();
            gelesen.Should().Be("TestDaten");
            reader.Close();

            KillTestOrdner(path);
        }

        [Test]
        public async Task WriteStream_ErstelltDatei()
        {
            var path = InitTestOrdner("LokalNodeTests");

            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");
            configuration["Pfad"].Returns(path);

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var resultat = await unterTest.Write("GibtEsNochNicht", GenerateStreamFromString("TestDaten;"));

            resultat.Should().BeTrue();
            File.Exists(Path.Combine(path, "GibtEsNochNicht")).Should().BeTrue();

            KillTestOrdner(path);
        }

        [Test]
        public void NameWirdAusConfigurationGelesen()
        {
            var path = InitTestOrdner("LokalNodeTests");

            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");
            configuration["Pfad"].Returns(path);

            var unterTest = new Node(configuration, lf.Logger<Node>());

            unterTest.Name.Should().Be("Test1");

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

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}