using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Test.Extensions.Logging;

namespace WebRaid.Node.Raid.Tests
{
    /// <summary>
    /// Rollback-Schutz, Redundanz je Datei, Ausfallgruppen und Schreibsperre
    /// </summary>
    public class RaidNode2bTests : BasisTestKlasse
    {
        private const int Segment = 64;
        private static readonly byte[] Schluessel = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

        public RaidNode2bTests() : base(LogLevel.Debug) { }

        private RaidNode Raid(TestKnoten[] knoten, int k = 2, int m = 1, Action<RaidOptionen> anpassen = null)
        {
            var optionen = new RaidOptionen { DatenBloecke = k, ParitaetsBloecke = m, SegmentGroesse = Segment };
            anpassen?.Invoke(optionen);
            return new RaidNode("Raid", knoten, optionen, new FesterSchluessel(Schluessel), Lf.Logger<RaidNode>());
        }

        private static TestKnoten[] Knoten(int anzahl) => Enumerable.Range(1, anzahl).Select(i => new TestKnoten($"K{i}")).ToArray();

        private static byte[] Daten(int laenge, int saat)
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

        /// <summary>Zustand aller Knoten sichern, um später eine alte Version "zurückzuspielen"</summary>
        private static Dictionary<string, byte[]>[] Sichern(TestKnoten[] knoten) =>
            knoten.Select(k => k.Inhalt.ToDictionary(e => e.Key, e => (byte[])e.Value.Clone())).ToArray();

        private static void Zurueckspielen(TestKnoten[] knoten, Dictionary<string, byte[]>[] stand)
        {
            for (var i = 0; i < knoten.Length; i++)
            {
                knoten[i].Inhalt.Clear();
                foreach (var e in stand[i]) knoten[i].Inhalt[e.Key] = e.Value;
            }
        }

        #region Rollback

        [Test]
        public async Task Get_AnbieterSpielenAlteVersionZurueck_WirftInvalidDataException()
        {
            var knoten = Knoten(3);
            var unterTest = Raid(knoten);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 1)));
            var alt = Sichern(knoten);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 2)));

            Zurueckspielen(knoten, alt);
            Func<Task> aktion = () => unterTest.Get("datei");

            await aktion.Should().ThrowAsync<InvalidDataException>().WithMessage("*Rollback*");
        }

        [Test]
        public async Task Get_GeloeschteDateiTauchtWiederAuf_GibtNull()
        {
            var knoten = Knoten(3);
            var unterTest = Raid(knoten);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 1)));
            var alt = Sichern(knoten);
            await unterTest.Del("datei");

            Zurueckspielen(knoten, alt);

            (await unterTest.Get("datei")).Should().BeNull();
        }

        [Test]
        public async Task Get_AnbieterLoeschenBekannteDatei_WirftIOException()
        {
            var knoten = Knoten(3);
            var unterTest = Raid(knoten);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 1)));

            foreach (var kn in knoten) kn.Inhalt.Clear();
            Func<Task> aktion = () => unterTest.Get("datei");

            await aktion.Should().ThrowAsync<IOException>().WithMessage("*keinem Knoten*");
        }

        [Test]
        public async Task Write_NachRollback_SchreibtHoehereVersionAlsBekannt()
        {
            var knoten = Knoten(3);
            var unterTest = Raid(knoten);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 1)));
            var alt = Sichern(knoten);
            await unterTest.Write("datei", new MemoryStream(Daten(100, 2)));
            Zurueckspielen(knoten, alt);
            var neu = Daten(100, 3);

            (await unterTest.Write("datei", new MemoryStream(neu))).Should().BeTrue();

            (await Lesen(await unterTest.Get("datei"))).Should().Equal(neu);
        }

        [Test]
        public async Task DateiVersionsregister_UeberstehtNeustart()
        {
            var pfad = Path.Combine(Path.GetTempPath(), "RaidNode2bTests", Guid.NewGuid().ToString("N"), "versionen.json");
            try
            {
                var knoten = Knoten(3);
                await Raid(knoten, anpassen: o => o.Versionsregister = new DateiVersionsregister(pfad))
                    .Write("datei", new MemoryStream(Daten(100, 1)));
                var alt = Sichern(knoten);
                await Raid(knoten, anpassen: o => o.Versionsregister = new DateiVersionsregister(pfad))
                    .Write("datei", new MemoryStream(Daten(100, 2)));
                Zurueckspielen(knoten, alt);

                var nachNeustart = Raid(knoten, anpassen: o => o.Versionsregister = new DateiVersionsregister(pfad));
                Func<Task> aktion = () => nachNeustart.Get("datei");

                await aktion.Should().ThrowAsync<InvalidDataException>();
                File.ReadAllText(pfad).Should().NotContain("datei", "das Register darf keine Dateinamen enthalten");
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(pfad)), true);
            }
        }

        #endregion

        #region Redundanz je Datei

        [Test]
        public async Task Write_MitEigenerRedundanz_UeberstehtEntsprechendVieleAusfaelle()
        {
            var knoten = Knoten(5);
            var unterTest = Raid(knoten, k: 3, m: 1);
            var wichtig = Daten(3 * Segment, 1);

            await unterTest.Write("wichtig", new MemoryStream(wichtig), datenBloecke: 2, paritaetsBloecke: 3);
            knoten[0].Ausgefallen = knoten[1].Ausgefallen = true;
            knoten[2].Ausgefallen = true;

            (await Lesen(await unterTest.Get("wichtig"))).Should().Equal(wichtig);
        }

        [Test]
        public async Task Write_RedundanzGroesserAlsKnoten_WirftAusnahme()
        {
            var unterTest = Raid(Knoten(3));

            Func<Task> aktion = () => unterTest.Write("datei", new MemoryStream(new byte[1]), 2, 2);

            await aktion.Should().ThrowAsync<ArgumentException>().WithMessage("*k + m*");
        }

        #endregion

        #region Ausfallgruppen

        [Test]
        public void Konstruktor_ZuWenigeAusfallgruppen_WirftAusnahme()
        {
            var knoten = Knoten(3);

            Action aktion = () => Raid(knoten, k: 2, m: 1,
                anpassen: o => o.Ausfallgruppen = new Dictionary<string, string> { ["K1"] = "UnitedInternet", ["K2"] = "UnitedInternet" });

            aktion.Should().Throw<ArgumentException>().WithMessage("*Ausfallgruppen*");
        }

        [Test]
        public async Task Write_Ausfallgruppen_JeSegmentHoechstensEinBlockProGruppe()
        {
            var knoten = Knoten(4);
            var unterTest = Raid(knoten, k: 2, m: 1,
                anpassen: o => o.Ausfallgruppen = new Dictionary<string, string> { ["K1"] = "UnitedInternet", ["K2"] = "UnitedInternet" });
            var daten = Daten(6 * Segment, 1);
            await unterTest.Write("datei", new MemoryStream(daten));

            // Die ganze Gruppe fällt aus (z.B. Kündigung bei United Internet): m = 1 Gruppe verkraftbar
            knoten[0].Ausgefallen = knoten[1].Ausgefallen = true;

            (await Lesen(await unterTest.Get("datei"))).Should().Equal(daten);
        }

        #endregion

        #region Schreibsperre

        private sealed class TestUhr : TimeProvider
        {
            public DateTimeOffset Jetzt { get; set; } = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
            public override DateTimeOffset GetUtcNow() => Jetzt;
        }

        [Test]
        public async Task Write_AndereInstanzHaeltSperre_WirftIOException()
        {
            var knoten = Knoten(3);
            var uhr = new TestUhr();
            var a = Raid(knoten, anpassen: o => { o.Instanz = "A"; o.Zeit = uhr; });
            var b = Raid(knoten, anpassen: o => { o.Instanz = "B"; o.Zeit = uhr; });
            await a.Write("datei", new MemoryStream(Daten(10, 1)));

            Func<Task> aktion = () => b.Write("andere", new MemoryStream(Daten(10, 2)));

            await aktion.Should().ThrowAsync<IOException>().WithMessage("*anderen Instanz*");
        }

        [Test]
        public async Task Write_SperreAbgelaufen_AndereInstanzDarfSchreiben()
        {
            var knoten = Knoten(3);
            var uhr = new TestUhr();
            var a = Raid(knoten, anpassen: o => { o.Instanz = "A"; o.Zeit = uhr; o.SperrDauer = TimeSpan.FromMinutes(10); });
            var b = Raid(knoten, anpassen: o => { o.Instanz = "B"; o.Zeit = uhr; });
            await a.Write("datei", new MemoryStream(Daten(10, 1)));

            uhr.Jetzt += TimeSpan.FromMinutes(11);

            (await b.Write("andere", new MemoryStream(Daten(10, 2)))).Should().BeTrue();
        }

        [Test]
        public async Task SperreFreigeben_AndereInstanzDarfSofortSchreiben()
        {
            var knoten = Knoten(3);
            var uhr = new TestUhr();
            var a = Raid(knoten, anpassen: o => { o.Instanz = "A"; o.Zeit = uhr; });
            var b = Raid(knoten, anpassen: o => { o.Instanz = "B"; o.Zeit = uhr; });
            await a.Write("datei", new MemoryStream(Daten(10, 1)));

            await a.SperreFreigeben();

            (await b.Write("andere", new MemoryStream(Daten(10, 2)))).Should().BeTrue();
        }

        [Test]
        public async Task Write_GefaelschteSperreOhneSchluessel_WirdIgnoriert()
        {
            var knoten = Knoten(3);
            foreach (var kn in knoten) kn.Inhalt["s/sperre"] = new byte[64];
            var a = Raid(knoten);

            (await a.Write("datei", new MemoryStream(Daten(10, 1)))).Should().BeTrue();
        }

        #endregion
    }
}
