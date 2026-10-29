using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Test.Extensions.Logging;

namespace WebRaid.Node.Raid.Tests
{
    public class RaidNodeTests : BasisTestKlasse
    {
        private const int Segment = 64;
        private static readonly byte[] Schluessel = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

        private TestKnoten[] knoten;

        public RaidNodeTests() : base(LogLevel.Debug) { }

        private RaidNode UnterTest(int k = 3, int m = 2, int anzahl = 5, byte[] schluessel = null)
        {
            knoten ??= Enumerable.Range(1, anzahl).Select(i => new TestKnoten($"K{i}")).ToArray();
            return new RaidNode("Raid", knoten,
                new RaidOptionen { DatenBloecke = k, ParitaetsBloecke = m, SegmentGroesse = Segment },
                new FesterSchluessel(schluessel ?? Schluessel), Lf.Logger<RaidNode>());
        }

        [SetUp]
        public void SetUp() => knoten = null;

        private static byte[] Daten(int laenge, int saat = 1)
        {
            var daten = new byte[laenge];
            new Random(saat).NextBytes(daten);
            return daten;
        }

        private static async Task<byte[]> Lesen(Stream stream)
        {
            using (var kopie = new MemoryStream())
            {
                await stream.CopyToAsync(kopie);
                return kopie.ToArray();
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(Segment - 1)]
        [TestCase(Segment)]
        [TestCase(Segment + 1)]
        [TestCase(3 * Segment + 17)]
        public async Task Write_DannGet_GibtDenInhalt(int laenge)
        {
            var unterTest = UnterTest();
            var daten = Daten(laenge);

            (await unterTest.Write("datei", new MemoryStream(daten))).Should().BeTrue();

            (await Lesen(await unterTest.Get("datei"))).Should().Equal(daten);
        }

        [Test]
        public async Task Get_BisZuMKnotenAusgefallen_GibtDenInhalt()
        {
            var unterTest = UnterTest(k: 3, m: 2, anzahl: 5);
            var daten = Daten(5 * Segment + 3);
            await unterTest.Write("datei", new MemoryStream(daten));

            for (var a = 0; a < 5; a++)
                for (var b = a + 1; b < 5; b++)
                {
                    foreach (var kn in knoten) kn.Ausgefallen = false;
                    knoten[a].Ausgefallen = true;
                    knoten[b].Ausgefallen = true;

                    (await Lesen(await unterTest.Get("datei"))).Should().Equal(daten, $"Ausfall von K{a + 1} und K{b + 1}");
                }
        }

        [Test]
        public async Task Get_MehrAlsMKnotenAusgefallen_WirftIOException()
        {
            var unterTest = UnterTest(k: 3, m: 2, anzahl: 5);
            await unterTest.Write("datei", new MemoryStream(Daten(100)));
            knoten[0].Ausgefallen = knoten[1].Ausgefallen = knoten[2].Ausgefallen = true;

            Func<Task> aktion = () => unterTest.Get("datei");

            await aktion.Should().ThrowAsync<IOException>();
        }

        [Test]
        public async Task Get_KnotenLiefertVerfaelschteDaten_WirdErkanntUndKorrigiert()
        {
            var unterTest = UnterTest(k: 3, m: 2, anzahl: 5);
            var daten = Daten(4 * Segment);
            await unterTest.Write("datei", new MemoryStream(daten));
            knoten[1].Verfaelscht = true;
            knoten[3].Verfaelscht = true;

            (await Lesen(await unterTest.Get("datei"))).Should().Equal(daten);
        }

        [Test]
        public async Task Get_NurNochZweiKnoten_MetadatenUndDatenBeiK2Lesbar()
        {
            var unterTest = UnterTest(k: 2, m: 3, anzahl: 5);
            var daten = Daten(3 * Segment);
            await unterTest.Write("datei", new MemoryStream(daten));
            knoten[0].Ausgefallen = knoten[2].Ausgefallen = knoten[4].Ausgefallen = true;

            (await Lesen(await unterTest.Get("datei"))).Should().Equal(daten);
        }

        [Test]
        public async Task Anbieter_SiehtWederInhaltNochDateinamen()
        {
            var unterTest = UnterTest();
            var klartext = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("GeheimerInhalt ", 30)));

            await unterTest.Write("Steuererklaerung.pdf", new MemoryStream(klartext));

            var muster = Encoding.UTF8.GetBytes("GeheimerInhalt");
            foreach (var kn in knoten)
            {
                kn.Inhalt.Keys.Should().NotContain(a => a.Contains("Steuer"));
                foreach (var block in kn.Inhalt.Values)
                {
                    block.AsSpan().IndexOf(muster).Should().Be(-1, $"auf {kn.Name} darf kein Klartext liegen");
                }
            }
        }

        [Test]
        public async Task Get_FalscherHauptschluessel_FindetDateiNicht()
        {
            await UnterTest().Write("datei", new MemoryStream(Daten(100)));
            var andererSchluessel = Enumerable.Repeat((byte)7, 32).ToArray();

            // Mit anderem Schlüssel ergibt sich eine andere Manifest-Adresse: die Datei ist schlicht nicht da
            (await UnterTest(schluessel: andererSchluessel).Get("datei")).Should().BeNull();
        }

        [Test]
        public async Task Get_ManifestAufAllenKnotenBeschaedigt_WirftInvalidDataException()
        {
            var unterTest = UnterTest();
            await unterTest.Write("datei", new MemoryStream(Daten(100)));
            foreach (var kn in knoten)
                foreach (var adresse in kn.Inhalt.Keys.Where(a => a.StartsWith("m/")).ToList())
                {
                    var teil = kn.Inhalt[adresse];
                    teil[^1] ^= 0x01;
                }

            Func<Task> aktion = () => unterTest.Get("datei");

            await aktion.Should().ThrowAsync<InvalidDataException>();
        }

        [Test]
        public async Task Write_Ueberschreiben_EntferntAlteBloecke()
        {
            var unterTest = UnterTest();
            await unterTest.Write("datei", new MemoryStream(Daten(3 * Segment, 1)));
            var vorher = knoten.SelectMany(k => k.Inhalt.Keys.Where(a => a.StartsWith("b/"))).ToHashSet();
            var neu = Daten(2 * Segment, 2);

            await unterTest.Write("datei", new MemoryStream(neu));

            var nachher = knoten.SelectMany(k => k.Inhalt.Keys.Where(a => a.StartsWith("b/"))).ToHashSet();
            nachher.Should().NotIntersectWith(vorher);
            nachher.Should().HaveCount(2 * 5);
            (await Lesen(await unterTest.Get("datei"))).Should().Equal(neu);
        }

        [Test]
        public async Task Write_EinKnotenAusgefallen_GelingtMitEingeschraenkterRedundanz()
        {
            var unterTest = UnterTest(k: 3, m: 2, anzahl: 5);
            var daten = Daten(2 * Segment);
            knoten[4].Ausgefallen = true;

            (await unterTest.Write("datei", new MemoryStream(daten))).Should().BeTrue();

            knoten[4].Ausgefallen = false;
            knoten[0].Ausgefallen = true;
            (await Lesen(await unterTest.Get("datei"))).Should().Equal(daten);
        }

        [Test]
        public async Task Write_ZuVieleKnotenAusgefallen_GibtFalseUndBehaeltAlteVersion()
        {
            var unterTest = UnterTest(k: 3, m: 2, anzahl: 5);
            var alt = Daten(100, 1);
            await unterTest.Write("datei", new MemoryStream(alt));
            knoten[0].Ausgefallen = knoten[1].Ausgefallen = knoten[2].Ausgefallen = true;

            (await unterTest.Write("datei", new MemoryStream(Daten(100, 2)))).Should().BeFalse();

            foreach (var kn in knoten) kn.Ausgefallen = false;
            (await Lesen(await unterTest.Get("datei"))).Should().Equal(alt);
        }

        [Test]
        public async Task Write_ManifestTeilweiseAlt_LiestDieNeuesteVersion()
        {
            var unterTest = UnterTest(k: 2, m: 1, anzahl: 3);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 1)));
            var neu = Daten(100, 2);
            knoten[2].Ausgefallen = true;
            await unterTest.Write("datei", new MemoryStream(neu));

            // K3 hat noch das alte Manifest, K1 und K2 das neue
            knoten[2].Ausgefallen = false;

            (await Lesen(await unterTest.Get("datei"))).Should().Equal(neu);
        }

        [Test]
        public async Task Del_EntferntAllesVonAllenKnoten()
        {
            var unterTest = UnterTest();
            await unterTest.Write("datei", new MemoryStream(Daten(3 * Segment)));

            (await unterTest.Del("datei")).Should().BeTrue();

            foreach (var kn in knoten) kn.Inhalt.Should().BeEmpty();
            (await unterTest.Get("datei")).Should().BeNull();
        }

        [Test]
        public void Konstruktor_ZuWenigeKnotenFuerKPlusM_WirftAusnahme()
        {
            Action aktion = () => UnterTest(k: 3, m: 2, anzahl: 4);

            aktion.Should().Throw<ArgumentException>().WithMessage("*k + m*");
        }

        [Test]
        public void Konstruktor_DoppelteKnotennamen_WirftAusnahme()
        {
            knoten = new[] { new TestKnoten("A"), new TestKnoten("A"), new TestKnoten("B") };

            Action aktion = () => UnterTest(k: 2, m: 1);

            aktion.Should().Throw<ArgumentException>().WithMessage("*eindeutige*");
        }
    }
}
