using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Test.Extensions.Logging;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.Lokal.Tests
{
    //public class LokalNodeTests : BasisTestKlasse
    //{
    //    private IConfiguration configuration;
    //    private string path;

    //    public LokalNodeTests() : base(LogLevel.Trace) { }

    //    [SetUp]
    //    public virtual void SetUp()
    //    {
    //        path = InitTestOrdner("LokalNodeTests");

    //        configuration = Substitute.For<IConfiguration>();
    //        configuration["Name"].Returns("Test1");
    //        configuration["Pfad"].Returns(path);
    //    }

    //    [TearDown]
    //    public virtual void TearDown()
    //    {
    //        KillTestOrdner(path);
    //    }

    //    #region Get
    //    [Test]
    //    public async Task Get_NichtVorhandeneDatei_Gibt_null()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var stream = await unterTest.Get("GibtEsNicht");

    //        stream.Should().BeNull();
    //    }
    //    [Test]
    //    public async Task Get_VorhandeneDatei_Gibt_InhaltAlsStream()
    //    {
    //        await File.WriteAllTextAsync(Path.Combine(path, "GibtEs"), "TestDaten");

    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var stream = await unterTest.Get("GibtEs");

    //        stream.Should().NotBeNull();

    //        var reader = new StreamReader(stream);
    //        var gelesen = await reader.ReadToEndAsync();
    //        gelesen.Should().Be("TestDaten");
    //        reader.Close();
    //    }

    //    [Test]
    //    public virtual void Get_Adresse_Null_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Get(null);

    //        test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
    //    }
    //    [Test]
    //    public virtual void Get_Adresse_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Get("");

    //        test.Should().Throw<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
    //    }
    //    #endregion

    //    #region Write
    //    [Test]
    //    public async Task Write_ErstelltDatei()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var resultat = await unterTest.Write("GibtEsNochNicht", GenerateStreamFromString("TestDaten;"));

    //        resultat.Should().BeTrue();
    //        File.Exists(Path.Combine(path, "GibtEsNochNicht")).Should().BeTrue();
    //    }

    //    [Test]
    //    public virtual void Write_Adresse_Null_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Write(null, Stream.Null);

    //        test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
    //    }
    //    [Test]
    //    public virtual void Write_Adresse_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Write("", Stream.Null);

    //        test.Should().Throw<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
    //    }
    //    [Test]
    //    public virtual void Write_Input_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Write("adresse", null);

    //        test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "input");
    //    }
    //    #endregion


    //    [Test]
    //    public virtual void NameWirdAusConfigurationGelesen()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        unterTest.Name.Should().Be("Test1");
    //    }

    //    [Test]
    //    public async Task ArbeitsPfadWirdAngelegt()
    //    {
    //        var arbeitsPfad = Path.Combine(path, "GibtsNochNicht");
    //        configuration["Pfad"].Returns(arbeitsPfad);

    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var resultat = await unterTest.Write("EineAdresse", GenerateStreamFromString("TestDaten;"));

    //        resultat.Should().BeTrue();

    //        Directory.Exists(arbeitsPfad).Should().BeTrue();
    //        Lf.Loggs.Should().Contain(l
    //            => l.Nachricht.Contains(arbeitsPfad)
    //            && l.Nachricht.Contains("wird angelegt")
    //            && l.LogLevel == LogLevel.Information
    //            );
    //    }

    //    #region Del
    //    [Test]
    //    public async Task Del_VorhandeneDatei_LoeschtDatei()
    //    {
    //        var adresse = "GibtEs";
    //        var datei = Path.Combine(path, adresse);

    //        await File.WriteAllTextAsync(datei, "TestDaten");

    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var resultat = await unterTest.Del(adresse);
    //        resultat.Should().BeTrue();

    //        File.Exists(datei).Should().BeFalse();
    //    }
    //    [Test]
    //    public async Task Del_NichtVorhandeneDatei_GibtFalseZurueck()
    //    {
    //        var adresse = "GibtEsNicht";
    //        var datei = Path.Combine(path, adresse);

    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var resultat = await unterTest.Del(adresse);
    //        resultat.Should().BeFalse();

    //        File.Exists(datei).Should().BeFalse();
    //    }
    //    [Test]
    //    public virtual void Del_Adresse_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Del("");

    //        test.Should().Throw<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
    //    }
    //    #endregion

    //    #region Helper
    //    private static string InitTestOrdner(string testOrdnerName)
    //    {
    //        var path = Path.Combine(Path.GetTempPath(), testOrdnerName);
    //        if (Directory.Exists(path))
    //        {
    //            Directory.Delete(path, true);
    //        }
    //        Directory.CreateDirectory(path);
    //        return path;
    //    }
    //    private static void KillTestOrdner(string path)
    //    {
    //        if (Directory.Exists(path))
    //        {
    //            Directory.Delete(path, true);
    //        }
    //    }
    //    public static Stream GenerateStreamFromString(string s)
    //    {
    //        var stream = new MemoryStream();
    //        var writer = new StreamWriter(stream);
    //        writer.Write(s);
    //        writer.Flush();
    //        stream.Position = 0;
    //        return stream;
    //    }
    //    #endregion
    //}

    public class LokalNodeTests : NodeTests<Node>
    {
        private string path;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            path = InitTestOrdner("LokalNodeTests");

            configuration["Pfad"].Returns(path);
        }
        [TearDown]
        public override void TearDown()
        {
            KillTestOrdner(path);
        }

        [Test]
        public async Task ArbeitsPfadWirdAngelegt()
        {
            var arbeitsPfad = Path.Combine(path, "GibtsNochNicht");
            configuration["Pfad"].Returns(arbeitsPfad);

            var unterTest = new Node(configuration, Lf.Logger<Node>());

            var resultat = await unterTest.Write("EineAdresse", GenerateStreamFromString("TestDaten;"));

            resultat.Should().BeTrue();

            Directory.Exists(arbeitsPfad).Should().BeTrue();
            Lf.Loggs.Should().Contain(l
                => l.Nachricht.Contains(arbeitsPfad)
                && l.Nachricht.Contains("wird angelegt")
                && l.LogLevel == LogLevel.Information
                );
        }

        protected override Node GetNode()
        {
            return new Node(configuration, Lf.Logger<Node>());
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
            File.WriteAllText(Path.Combine(path, key), daten);
        }

        protected override void ContainKey(Node node, string key)
        {
            var datei = Path.Combine(path, key);
            File.Exists(datei).Should().BeTrue();
        }

        protected override void NotContainKey(Node node, string key)
        {
            var datei = Path.Combine(path, key);
            File.Exists(datei).Should().BeFalse();
        }
    }
}