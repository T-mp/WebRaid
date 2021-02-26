using System;
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
        private IConfiguration configuration;
        private string path;

        public LokalNodeTests()
        {
            lf = new TestLoggerFactory(LogLevel.Trace);
        }

        [SetUp]
        public void SetUp()
        {
            path = InitTestOrdner("LokalNodeTests");

            configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");
            configuration["Pfad"].Returns(path);
        }

        [TearDown]
        public void TearDown()
        {
            KillTestOrdner(path);
        }

        #region Get
        [Test]
        public async Task Get_NichtVorhandeneDatei_Gibt_null()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            var stream = await unterTest.Get("GibtEsNicht");

            stream.Should().BeNull();
        }
        [Test]
        public async Task Get_VorhandeneDatei_Gibt_InhaltAlsStream()
        {
            await File.WriteAllTextAsync(Path.Combine(path, "GibtEs"), "TestDaten");

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var stream = await unterTest.Get("GibtEs");

            stream.Should().NotBeNull();

            var reader = new StreamReader(stream);
            var gelesen = await reader.ReadToEndAsync();
            gelesen.Should().Be("TestDaten");
            reader.Close();
        }

        [Test]
        public void Get_Adresse_Null_SchmeistFehler()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            Func<Task> test = async () => await unterTest.Get(null);

            test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
        }
        [Test]
        public void Get_Adresse_lehr_SchmeistFehler()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            Func<Task> test = async () => await unterTest.Get("");

            test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
        }
        #endregion

        #region Write
        [Test]
        public async Task Write_ErstelltDatei()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            var resultat = await unterTest.Write("GibtEsNochNicht", GenerateStreamFromString("TestDaten;"));

            resultat.Should().BeTrue();
            File.Exists(Path.Combine(path, "GibtEsNochNicht")).Should().BeTrue();
        }

        [Test]
        public void Write_Adresse_Null_SchmeistFehler()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            Func<Task> test = async () => await unterTest.Write(null, Stream.Null);

            test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
        }
        [Test]
        public void Write_Adresse_lehr_SchmeistFehler()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            Func<Task> test = async () => await unterTest.Write("", Stream.Null);

            test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
        }
        [Test]
        public void Write_Input_lehr_SchmeistFehler()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            Func<Task> test = async () => await unterTest.Write("adresse", null);

            test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "input");
        }
        #endregion


        [Test]
        public void NameWirdAusConfigurationGelesen()
        {
            var unterTest = new Node(configuration, lf.Logger<Node>());

            unterTest.Name.Should().Be("Test1");
        }

        [Test]
        public async Task ArbeitsPfadWirdAngelegt()
        {
            var arbeitsPfad = Path.Combine(path, "GibtsNochNicht");
            configuration["Pfad"].Returns(arbeitsPfad);

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var resultat = await unterTest.Write("EineAdresse", GenerateStreamFromString("TestDaten;"));

            resultat.Should().BeTrue();

            Directory.Exists(arbeitsPfad).Should().BeTrue();
            lf.Loggs.Should().Contain(l
                => l.Nachricht.Contains(arbeitsPfad)
                && l.Nachricht.Contains("wird angelegt")
                && l.LogLevel == LogLevel.Information
                );
        }

        [Test]
        public async Task Del_VorhandeneDatei_LoeschtDatei()
        {
            var adresse = "GibtEs";
            var datei = Path.Combine(path, adresse);

            await File.WriteAllTextAsync(datei, "TestDaten");

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var resultat = await unterTest.Del(adresse);
            resultat.Should().BeTrue();

            File.Exists(datei).Should().BeFalse();
        }
        [Test]
        public async Task Del_NichtVorhandeneDatei_GibtFalseZurueck()
        {
            var adresse = "GibtEsNicht";
            var datei = Path.Combine(path, adresse);

            var unterTest = new Node(configuration, lf.Logger<Node>());

            var resultat = await unterTest.Del(adresse);
            resultat.Should().BeFalse();

            File.Exists(datei).Should().BeFalse();
        }

        #region Helper
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
        #endregion
    }
}