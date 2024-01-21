using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Test.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.TestsBase
{
    public abstract class NodeTests<TNode> : BasisTestKlasse
        where TNode : class, INode
    {
        public NodeTests() : this(LogLevel.Trace) { }
        public NodeTests(LogLevel level) : base(level) { }
        protected IConfiguration configuration;

        [SetUp]
        public virtual void SetUp()
        {
            configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("Test1");
        }
        [TearDown]
        public virtual void TearDown()
        {
        }

        protected abstract TNode GetNode();
        protected abstract void AddTestDaten(TNode node, string key, string daten);
        protected abstract void ContainKey(TNode node, string key);
        protected abstract void NotContainKey(TNode node, string key);

        #region Get
        [Test]
        public async Task Get_NichtVorhanden_Gibt_null()
        {
            var unterTest = GetNode();

            var stream = await unterTest.Get("GibtEsNicht");

            stream.Should().BeNull();
        }
        [Test]
        public async Task Get_Vorhanden_Gibt_InhaltAlsStream()
        {
            var unterTest = GetNode();

            AddTestDaten(unterTest, "GibtEs", "TestDaten");

            var stream = await unterTest.Get("GibtEs");

            stream.Should().NotBeNull();

            var reader = new StreamReader(stream);
            var gelesen = await reader.ReadToEndAsync();
            gelesen.Should().Be("TestDaten");
            reader.Close();
        }

        [Test]
        public async Task Get_Adresse_Null_SchmeistFehler()
        {
            var unterTest = GetNode();

            Func<Task> test = async () => await unterTest.Get(null);

            await test.Should().ThrowAsync<ArgumentNullException>().Where(e => e.ParamName == "adresse");
        }
        [Test]
        public async Task Get_Adresse_lehr_SchmeistFehler()
        {
            var unterTest = GetNode();

            Func<Task> test = async () => await unterTest.Get("");

            await test.Should().ThrowAsync<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
        }
        #endregion

        #region Write
        [Test]
        public async Task Write_Adresse_Null_SchmeistFehler()
        {
            var unterTest = GetNode();

            Func<Task> test = async () => await unterTest.Write(null, Stream.Null);

            await test.Should().ThrowAsync<ArgumentNullException>().Where(e => e.ParamName == "adresse");
        }
        [Test]
        public async Task Write_Adresse_lehr_SchmeistFehler()
        {
            var unterTest = GetNode();

            Func<Task> test = async () => await unterTest.Write("", Stream.Null);

            await test.Should().ThrowAsync<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
        }
        [Test]
        public async Task Write_Input_lehr_SchmeistFehler()
        {
            var unterTest = GetNode();

            Func<Task> test = async () => await unterTest.Write("adresse", null);

            await test.Should().ThrowAsync<ArgumentNullException>().Where(e => e.ParamName == "input");
        }

        [Test]
        public async Task Write_Erstellt()
        {
            var unterTest = GetNode();

            var resultat = await unterTest.Write("GibtEsNochNicht", GenerateStreamFromString("TestDaten;"));

            resultat.Should().BeTrue();

            ContainKey(unterTest, "GibtEsNochNicht");
        }
        #endregion

        #region Del
        [Test]
        public async Task Del_Loescht()
        {
            var unterTest = GetNode();

            AddTestDaten(unterTest, "GibtEs", "TestDaten");

            var resultat = await unterTest.Del("GibtEs");
            resultat.Should().BeTrue();

            NotContainKey(unterTest, "GibtEs");
        }
        [Test]
        public async Task Del_NichtVorhandeneDatei_GibtFalseZurueck()
        {
            var adresse = "GibtEsNicht";

            var unterTest = GetNode();

            var resultat = await unterTest.Del(adresse);
            resultat.Should().BeFalse();

            NotContainKey(unterTest, adresse);
        }
        [Test]
        public async Task Del_Adresse_lehr_SchmeistFehler()
        {
            var unterTest = GetNode();

            Func<Task> test = async () => await unterTest.Del("");

            await test.Should().ThrowAsync<ArgumentOutOfRangeException>().Where(e => e.ParamName == "adresse");
        }
        #endregion

        [Test]
        public void NameWirdAusConfigurationGelesen()
        {
            var unterTest = GetNode();

            unterTest.Name.Should().Be("Test1");
        }
    }
}