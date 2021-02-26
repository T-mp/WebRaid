using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using Test.Extensions.Logging;

namespace WebRaid.Node.Memory.Tests
{
    public class MemoryNodeTests : BasisTestKlasse
    {
        public MemoryNodeTests() : base(LogLevel.Trace) { }

        [Test]
        public async Task Get_NichtVorhanden_Gibt_null()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");

            var unterTest = new Node(configuration, Lf.Logger<Node>());

            var stream = await unterTest.Get("GibtEsNicht");

            stream.Should().BeNull();
        }
        [Test]
        public async Task Get_Vorhanden_Gibt_InhaltAlsStream()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");

            var unterTest = new Node(configuration, Lf.Logger<Node>());

            unterTest.Speicher.Add("GibtEs", GenerateStreamFromString("TestDaten"));

            var stream = await unterTest.Get("GibtEs");

            stream.Should().NotBeNull();

            var reader = new StreamReader(stream);
            var gelesen = await reader.ReadToEndAsync();
            gelesen.Should().Be("TestDaten");
            reader.Close();
        }

        [Test]
        public async Task Write_Erstellt()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");

            var unterTest = new Node(configuration, Lf.Logger<Node>());

            var resultat = await unterTest.Write("GibtEsNochNicht", GenerateStreamFromString("TestDaten;"));

            resultat.Should().BeTrue();

            unterTest.Speicher.Should().ContainKey("GibtEsNochNicht");
        }

        [Test]
        public async Task Del_Loescht()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");

            var unterTest = new Node(configuration, Lf.Logger<Node>());

            unterTest.Speicher.Add("GibtEs", GenerateStreamFromString("TestDaten"));

            var resultat = await unterTest.Del("GibtEs");
            resultat.Should().BeTrue();

            unterTest.Speicher.Should().NotContainKey("GibtEs");
        }

        [Test]
        public void NameWirdAusConfigurationGelesen()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");

            var unterTest = new Node(configuration, Lf.Logger<Node>());

            unterTest.Name.Should().Be("Test1");
        }
    }
}