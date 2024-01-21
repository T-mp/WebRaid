using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Test.Extensions.Logging;
using WebRaid.Node.TestsBase;

namespace WebRaid.Node.Memory.Tests
{
    //public class MemoryNodeTests : BasisTestKlasse
    //{
    //    public MemoryNodeTests() : base(LogLevel.Trace) { }
    //    private IConfiguration configuration;

    //    [SetUp]
    //    public void SetUp()
    //    {
    //        configuration = Substitute.For<IConfiguration>();
    //        configuration["Name"].Returns("Test1");
    //    }
    //    [TearDown]
    //    public void TearDown()
    //    {
    //    }
    //    #region Get
    //    [Test]
    //    public async Task Get_NichtVorhanden_Gibt_null()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var stream = await unterTest.Get("GibtEsNicht");

    //        stream.Should().BeNull();
    //    }
    //    [Test]
    //    public async Task Get_Vorhanden_Gibt_InhaltAlsStream()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        unterTest.Speicher.Add("GibtEs", GenerateStreamFromString("TestDaten"));

    //        var stream = await unterTest.Get("GibtEs");

    //        stream.Should().NotBeNull();

    //        var reader = new StreamReader(stream);
    //        var gelesen = await reader.ReadToEndAsync();
    //        gelesen.Should().Be("TestDaten");
    //        reader.Close();
    //    }

    //    [Test]
    //    public void Get_Adresse_Null_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Get(null);

    //        test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
    //    }
    //    [Test]
    //    public void Get_Adresse_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Get("");

    //        test.Should().Throw<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
    //    }
    //    #endregion

    //    #region Write
    //    [Test]
    //    public void Write_Adresse_Null_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Write(null, Stream.Null);

    //        test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "adresse");
    //    }
    //    [Test]
    //    public void Write_Adresse_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Write("", Stream.Null);

    //        test.Should().Throw<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
    //    }
    //    [Test]
    //    public void Write_Input_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Write("adresse", null);

    //        test.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "input");
    //    }

    //    [Test]
    //    public async Task Write_Erstellt()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var resultat = await unterTest.Write("GibtEsNochNicht", GenerateStreamFromString("TestDaten;"));

    //        resultat.Should().BeTrue();

    //        unterTest.Speicher.Should().ContainKey("GibtEsNochNicht");
    //    }
    //    #endregion

    //    #region Del
    //    [Test]
    //    public async Task Del_Loescht()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        unterTest.Speicher.Add("GibtEs", GenerateStreamFromString("TestDaten"));

    //        var resultat = await unterTest.Del("GibtEs");
    //        resultat.Should().BeTrue();

    //        unterTest.Speicher.Should().NotContainKey("GibtEs");
    //    }
    //    [Test]
    //    public async Task Del_NichtVorhandeneDatei_GibtFalseZurueck()
    //    {
    //        var adresse = "GibtEsNicht";

    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        var resultat = await unterTest.Del(adresse);
    //        resultat.Should().BeFalse();

    //        unterTest.Speicher.Should().NotContainKey(adresse);
    //    }
    //    [Test]
    //    public void Del_Adresse_lehr_SchmeistFehler()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        Func<Task> test = async () => await unterTest.Del("");

    //        test.Should().Throw<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
    //    }
    //    #endregion

    //    [Test]
    //    public void NameWirdAusConfigurationGelesen()
    //    {
    //        var unterTest = new Node(configuration, Lf.Logger<Node>());

    //        unterTest.Name.Should().Be("Test1");
    //    }
    //}


    public class MemoryNodeTests : NodeTests<Node>
    {
        protected override Node GetNode()
        {
            return new Node(configuration, Lf.Logger<Node>());
        }

        protected override void AddTestDaten(Node node, string key, string daten)
        {
            node.Speicher.Add(key, GenerateStreamFromString(daten));
        }

        protected override void ContainKey(Node node, string key)
        {
            node.Speicher.Should().ContainKey(key);
        }

        protected override void NotContainKey(Node node, string key)
        {
            node.Speicher.Should().NotContainKey(key);
        }
    }
}