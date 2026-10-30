using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Test.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Raid.Tests
{
    public class SchluesselTests : BasisTestKlasse
    {
        private string ordner;

        [SetUp]
        public void SetUp()
        {
            ordner = Path.Combine(Path.GetTempPath(), "WebRaid-Schluessel-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
        }

        /// <summary>Schwache Argon2-Parameter, damit die Tests schnell bleiben</summary>
        private static PassphraseSchutz Schutz(string passphrase = "Kartoffelsalat mit Würstchen") =>
            new PassphraseSchutz(passphrase) { SpeicherKiB = 1024, Durchlaeufe = 1, Parallelitaet = 1 };

        private string Pfad => Path.Combine(ordner, "schluessel.json");

        #region Wiederherstellungscode

        [Test]
        public void Wiederherstellungscode_HinUndZurueck()
        {
            var schluessel = RandomNumberGenerator.GetBytes(32);

            var code = Wiederherstellungscode.Erzeugen(schluessel);

            code.Should().MatchRegex("^([0-9A-HJKMNP-TV-Z]{4}-){13}[0-9A-HJKMNP-TV-Z]{4}$");
            Wiederherstellungscode.Lesen(code).Should().Equal(schluessel);
        }

        [Test]
        public void Wiederherstellungscode_VerzeihtSchreibweise()
        {
            var schluessel = RandomNumberGenerator.GetBytes(32);
            var code = Wiederherstellungscode.Erzeugen(schluessel);
            var abgetippt = code.ToLowerInvariant().Replace("-", " ").Replace('0', 'o').Replace('1', 'l');

            Wiederherstellungscode.Lesen(abgetippt).Should().Equal(schluessel);
        }

        [Test]
        public void Wiederherstellungscode_Tippfehler_WirftFormatException()
        {
            var code = Wiederherstellungscode.Erzeugen(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()).ToCharArray();
            code[5] = code[5] == 'A' ? 'B' : 'A';

            FluentActions.Invoking(() => Wiederherstellungscode.Lesen(new string(code)))
                .Should().Throw<FormatException>().WithMessage("*Tippfehler*");
        }

        [TestCase("1234-5678")]
        [TestCase("")]
        public void Wiederherstellungscode_FalscheLaenge_WirftFormatException(string code)
        {
            FluentActions.Invoking(() => Wiederherstellungscode.Lesen(code)).Should().Throw<FormatException>();
        }

        [Test]
        public void Wiederherstellungscode_UngueltigesZeichen_WirftFormatException()
        {
            FluentActions.Invoking(() => Wiederherstellungscode.Lesen("U" + new string('0', 55)))
                .Should().Throw<FormatException>().WithMessage("*Zeichen*");
        }

        #endregion

        #region Schlüsseldatei

        [Test]
        public void Anlegen_DannLaden_LiefertDenSchluesselDesCodes()
        {
            var code = Schluesseldatei.Anlegen(Pfad, Schutz());

            var geladen = Schluesseldatei.Laden(Pfad, Schutz());

            geladen.Hauptschluessel.ToArray().Should().Equal(Wiederherstellungscode.Lesen(code));
        }

        [Test]
        public void Anlegen_EnthaeltDenSchluesselNichtImKlartext()
        {
            var code = Schluesseldatei.Anlegen(Pfad, Schutz());

            var datei = File.ReadAllBytes(Pfad);
            datei.AsSpan().IndexOf(Wiederherstellungscode.Lesen(code)).Should().Be(-1);
            File.ReadAllText(Pfad).Should().NotContain(Convert.ToBase64String(Wiederherstellungscode.Lesen(code)));
        }

        [Test]
        public void Anlegen_NurFuerDenBenutzerLesbar()
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.Ignore("Dateirechte nach Unix-Art");
                return;
            }
            Schluesseldatei.Anlegen(Pfad, Schutz());

            File.GetUnixFileMode(Pfad).Should().Be(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        [Test]
        public void Anlegen_DateiExistiert_UeberschreibtNicht()
        {
            Schluesseldatei.Anlegen(Pfad, Schutz());
            var vorher = File.ReadAllBytes(Pfad);

            FluentActions.Invoking(() => Schluesseldatei.Anlegen(Pfad, Schutz())).Should().Throw<IOException>().WithMessage("*nicht überschrieben*");
            FluentActions.Invoking(() => Schluesseldatei.Wiederherstellen(Pfad, Wiederherstellungscode.Erzeugen(new byte[32]), Schutz())).Should().Throw<IOException>();

            File.ReadAllBytes(Pfad).Should().Equal(vorher);
        }

        [Test]
        public void Laden_FalschePassphrase_WirftCryptographicException()
        {
            Schluesseldatei.Anlegen(Pfad, Schutz());

            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz("falsch")))
                .Should().Throw<CryptographicException>().WithMessage("*Passphrase*");
        }

        [Test]
        public void Laden_ManipulierteDatei_WirftCryptographicException()
        {
            Schluesseldatei.Anlegen(Pfad, Schutz());
            var text = File.ReadAllText(Pfad).Replace("\"Durchlaeufe\": 1", "\"Durchlaeufe\": 2");
            File.WriteAllText(Pfad, text);

            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz())).Should().Throw<CryptographicException>();
        }

        [Test]
        public void Laden_UnplausibleParameter_WirftCryptographicException()
        {
            Schluesseldatei.Anlegen(Pfad, Schutz());
            File.WriteAllText(Pfad, File.ReadAllText(Pfad).Replace("\"SpeicherKiB\": 1024", "\"SpeicherKiB\": 999999999"));

            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz())).Should().Throw<CryptographicException>().WithMessage("*Argon2*");
        }

        [Test]
        public void Laden_KeinJson_WirftCryptographicException()
        {
            Directory.CreateDirectory(ordner);
            File.WriteAllText(Pfad, "kaputt");

            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz())).Should().Throw<CryptographicException>().WithMessage("*beschädigt*");
        }

        [Test]
        public void Laden_AnderesVerfahren_WirftCryptographicException()
        {
            Schluesseldatei.Anlegen(Pfad, Schutz());
            File.WriteAllText(Pfad, File.ReadAllText(Pfad).Replace("\"passphrase\"", "\"dpapi\""));

            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz())).Should().Throw<CryptographicException>().WithMessage("*dpapi*");
        }

        [Test]
        public void Laden_OhneDatei_WirftFileNotFoundException()
        {
            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz())).Should().Throw<FileNotFoundException>();
        }

        [Test]
        public void Wiederherstellen_LiefertDenselbenSchluessel()
        {
            var schluessel = RandomNumberGenerator.GetBytes(32);

            Schluesseldatei.Wiederherstellen(Pfad, Wiederherstellungscode.Erzeugen(schluessel), Schutz("neuer Rechner"));

            Schluesseldatei.Laden(Pfad, Schutz("neuer Rechner")).Hauptschluessel.ToArray().Should().Equal(schluessel);
        }

        [Test]
        public void SchutzWechseln_NeuePassphraseGilt_AlteNichtMehr()
        {
            var code = Schluesseldatei.Anlegen(Pfad, Schutz("alt"));

            Schluesseldatei.SchutzWechseln(Pfad, Schutz("alt"), Schutz("neu"));

            Schluesseldatei.Laden(Pfad, Schutz("neu")).Hauptschluessel.ToArray().Should().Equal(Wiederherstellungscode.Lesen(code));
            FluentActions.Invoking(() => Schluesseldatei.Laden(Pfad, Schutz("alt"))).Should().Throw<CryptographicException>();
        }

        [Test]
        public void SchutzWechseln_FalschePassphrase_LaesstDateiUnveraendert()
        {
            Schluesseldatei.Anlegen(Pfad, Schutz("alt"));
            var vorher = File.ReadAllBytes(Pfad);

            FluentActions.Invoking(() => Schluesseldatei.SchutzWechseln(Pfad, Schutz("falsch"), Schutz("neu"))).Should().Throw<CryptographicException>();

            File.ReadAllBytes(Pfad).Should().Equal(vorher);
        }

        [Test]
        public void PassphraseSchutz_LeerePassphrase_WirftArgumentException()
        {
            FluentActions.Invoking(() => new PassphraseSchutz("")).Should().Throw<ArgumentException>();
        }

        [Test]
        public void PassphraseSchutz_StandardparameterPassenZumPi()
        {
            var schutz = new PassphraseSchutz("x");

            schutz.SpeicherKiB.Should().Be(64 * 1024);
            schutz.Parallelitaet.Should().Be(4);
        }

        [Test]
        [Platform("Win")]
        public void Dpapi_HinUndZurueck()
        {
#pragma warning disable CA1416 // nur unter Windows, siehe Platform
            var code = Schluesseldatei.Anlegen(Pfad, new DpapiSchutz());

            Schluesseldatei.Laden(Pfad, new DpapiSchutz()).Hauptschluessel.ToArray().Should().Equal(Wiederherstellungscode.Lesen(code));
#pragma warning restore CA1416
        }

        #endregion

        #region Registrierung

        private RaidKonfiguration Konfiguration(params string[] knoten) => new RaidKonfiguration
        {
            Name = "Familie",
            Knoten = knoten.ToList(),
            DatenBloecke = 2,
            ParitaetsBloecke = 1,
            SegmentGroesse = 64,
            Datenordner = ordner,
            Schutz = "passphrase",
            PassphraseDatei = Path.Combine(ordner, "passphrase")
        };

        private void PassphraseAblegen(string passphrase = "Kartoffelsalat mit Würstchen")
        {
            Directory.CreateDirectory(ordner);
            File.WriteAllText(Path.Combine(ordner, "passphrase"), passphrase + "\n");
        }

        [Test]
        public async Task Erstellen_MitSchluesseldatei_SchreibtUndLiest()
        {
            PassphraseAblegen();
            Schluesseldatei.Anlegen(Path.Combine(ordner, "schluessel.json"), Schutz());
            var knoten = Enumerable.Range(1, 3).Select(i => new TestKnoten($"K{i}")).ToArray();

            var raid = Initialisierung.Erstellen(Konfiguration(), knoten, Lf.Logger<RaidNode>());
            (await raid.Write("datei", GenerateStreamFromString("Hallo Familie"))).Should().BeTrue();

            var neuGestartet = Initialisierung.Erstellen(Konfiguration(), knoten, Lf.Logger<RaidNode>());
            using var leser = new StreamReader(await neuGestartet.Get("datei"));
            (await leser.ReadToEndAsync()).Should().Be("Hallo Familie");
            File.Exists(Path.Combine(ordner, "versionen.json")).Should().BeTrue("das Versionsregister liegt neben der Schlüsseldatei");
        }

        [Test]
        public void Erstellen_OhneSchluesseldatei_WirftFileNotFoundException()
        {
            PassphraseAblegen();

            FluentActions.Invoking(() => Initialisierung.Erstellen(Konfiguration(), new[] { new TestKnoten("K1"), new TestKnoten("K2"), new TestKnoten("K3") }, Lf.Logger<RaidNode>()))
                .Should().Throw<FileNotFoundException>().WithMessage("*Wiederherstellungscode*");
        }

        [Test]
        public void Erstellen_UnbekannterKnoten_WirftMitNamen()
        {
            FluentActions.Invoking(() => Initialisierung.Erstellen(Konfiguration("K1", "Gibtsnicht"), new[] { new TestKnoten("K1") }, Lf.Logger<RaidNode>()))
                .Should().Throw<InvalidOperationException>().WithMessage("*Gibtsnicht*");
        }

        [Test]
        public async Task Erstellen_NurDieGenanntenKnoten()
        {
            PassphraseAblegen();
            Schluesseldatei.Anlegen(Path.Combine(ordner, "schluessel.json"), Schutz());
            var knoten = Enumerable.Range(1, 4).Select(i => new TestKnoten($"K{i}")).ToArray();

            var raid = Initialisierung.Erstellen(Konfiguration("K1", "K2", "K4"), knoten, Lf.Logger<RaidNode>());
            await raid.Write("datei", GenerateStreamFromString("x"));

            knoten[2].Inhalt.Should().BeEmpty();
        }

        [Test]
        public void SchutzErstellen_UnbekanntesVerfahren_Wirft()
        {
            var konfiguration = Konfiguration();
            konfiguration.Schutz = "rot13";

            FluentActions.Invoking(() => konfiguration.SchutzErstellen()).Should().Throw<InvalidOperationException>().WithMessage("*rot13*");
        }

        [Test]
        public void SchutzErstellen_DpapiAusserhalbVonWindows_Wirft()
        {
            if (OperatingSystem.IsWindows()) Assert.Ignore("Nur außerhalb von Windows");
            var konfiguration = Konfiguration();
            konfiguration.Schutz = "dpapi";

            FluentActions.Invoking(() => konfiguration.SchutzErstellen()).Should().Throw<PlatformNotSupportedException>();
        }

        [Test]
        public void Datenordner_OhneAngabe_LiegtImBenutzerprofil()
        {
            var konfiguration = new RaidKonfiguration { Name = "Thomas" };

            konfiguration.SchluesselPfad.Should().Be(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebRaid", "Thomas", "schluessel.json"));
        }

        [Test]
        public async Task AddWebRaidRaidNode_RegistriertRaidAlsINode()
        {
            PassphraseAblegen();
            Schluesseldatei.Anlegen(Path.Combine(ordner, "schluessel.json"), Schutz());
            var konfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["RaidNode:Name"] = "Familie",
                ["RaidNode:Datenordner"] = ordner,
                ["RaidNode:Schutz"] = "passphrase",
                ["RaidNode:PassphraseDatei"] = Path.Combine(ordner, "passphrase"),
                ["RaidNode:Ausfallgruppen:K1"] = "UnitedInternet",
                ["RaidNode:Ausfallgruppen:K2"] = "UnitedInternet"
            }).Build();
            var dienste = new ServiceCollection();
            dienste.AddLogging();
            foreach (var i in Enumerable.Range(1, 4)) dienste.AddSingleton<IRawNode>(new TestKnoten($"K{i}"));

            dienste.AddWebRaidRaidNode(konfiguration);
            using var anbieter = dienste.BuildServiceProvider();

            var node = anbieter.GetRequiredService<INode>();
            node.Should().BeOfType<RaidNode>().Which.Name.Should().Be("Familie");
            (await node.Write("datei", GenerateStreamFromString("x"))).Should().BeTrue();
            anbieter.GetRequiredService<RaidKonfiguration>().Ausfallgruppen.Should().HaveCount(2);
        }

        #endregion
    }
}
