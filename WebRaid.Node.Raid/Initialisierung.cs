using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Einstellungen des RAIDs im Abschnitt "RaidNode" der Konfiguration
    /// </summary>
    public sealed class RaidKonfiguration
    {
        /// <summary>Name des RAIDs</summary>
        public string Name { get; set; } = "WebRaid";

        /// <summary>Namen der beteiligten Knoten; leer = alle registrierten <see cref="IRawNode"/></summary>
        public List<string> Knoten { get; set; } = new List<string>();

        /// <inheritdoc cref="RaidOptionen.DatenBloecke"/>
        public int DatenBloecke { get; set; } = 2;

        /// <inheritdoc cref="RaidOptionen.ParitaetsBloecke"/>
        public int ParitaetsBloecke { get; set; } = 1;

        /// <inheritdoc cref="RaidOptionen.SegmentGroesse"/>
        public int SegmentGroesse { get; set; } = 1024 * 1024;

        /// <inheritdoc cref="RaidOptionen.Ausfallgruppen"/>
        public Dictionary<string, string> Ausfallgruppen { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Ordner für Schlüsseldatei und Versionsregister; Umgebungsvariablen wie %LOCALAPPDATA% werden aufgelöst.
        /// Ohne Angabe: lokaler Anwendungsdatenordner des Benutzers / WebRaid / Name
        /// </summary>
        public string Datenordner { get; set; }

        /// <summary>"dpapi" (Standard unter Windows) oder "passphrase" (Standard sonst, z.B. auf dem Pi)</summary>
        public string Schutz { get; set; }

        /// <summary>
        /// Datei mit der Passphrase, z.B. "%CREDENTIALS_DIRECTORY%/webraid" bei systemd.
        /// Ohne Angabe wird die Umgebungsvariable WEBRAID_PASSPHRASE gelesen.
        /// </summary>
        public string PassphraseDatei { get; set; }

        /// <summary>Der aufgelöste <see cref="Datenordner"/></summary>
        public string DatenordnerPfad => string.IsNullOrWhiteSpace(Datenordner)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebRaid", Name)
            : Environment.ExpandEnvironmentVariables(Datenordner);

        /// <summary>Pfad der Schlüsseldatei</summary>
        public string SchluesselPfad => Path.Combine(DatenordnerPfad, "schluessel.json");

        /// <summary>Pfad des Versionsregisters</summary>
        public string RegisterPfad => Path.Combine(DatenordnerPfad, "versionen.json");

        /// <summary>
        /// Der Schutz des Hauptschlüssels laut <see cref="Schutz"/>
        /// </summary>
        public ISchluesselSchutz SchutzErstellen()
        {
            var verfahren = string.IsNullOrWhiteSpace(Schutz) ? (OperatingSystem.IsWindows() ? "dpapi" : "passphrase") : Schutz.Trim().ToLowerInvariant();
            switch (verfahren)
            {
                case "dpapi":
                    if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("DPAPI gibt es nur unter Windows; auf anderen Systemen \"passphrase\" verwenden");
                    return new DpapiSchutz();
                case "passphrase":
                    return new PassphraseSchutz(PassphraseLesen());
                default:
                    throw new InvalidOperationException($"Unbekannter Schlüsselschutz '{Schutz}', erlaubt sind \"dpapi\" und \"passphrase\"");
            }
        }

        private string PassphraseLesen()
        {
            if (!string.IsNullOrWhiteSpace(PassphraseDatei))
            {
                return File.ReadAllText(Environment.ExpandEnvironmentVariables(PassphraseDatei)).TrimEnd('\r', '\n');
            }
            var passphrase = Environment.GetEnvironmentVariable("WEBRAID_PASSPHRASE");
            if (string.IsNullOrEmpty(passphrase)) throw new InvalidOperationException("Keine Passphrase: RaidNode:PassphraseDatei setzen oder WEBRAID_PASSPHRASE");
            return passphrase;
        }
    }

    /// <summary>
    /// Registriert den RAID-Knoten aus der Konfiguration
    /// </summary>
    public static class Initialisierung
    {
        /// <summary>
        /// Registriert einen <see cref="RaidNode"/> (auch als <see cref="INode"/>) über die registrierten <see cref="IRawNode"/>,
        /// eingestellt über den Abschnitt "RaidNode". Die Schlüsseldatei muss bereits angelegt sein (<see cref="Schluesseldatei.Anlegen"/>).
        /// </summary>
        public static IServiceCollection AddWebRaidRaidNode(this IServiceCollection service, IConfiguration configuration)
        {
            var konfiguration = configuration.GetSection("RaidNode").Get<RaidKonfiguration>() ?? new RaidKonfiguration();
            service.AddSingleton(konfiguration);
            service.AddSingleton(s => Erstellen(konfiguration, s.GetServices<IRawNode>(), s.GetRequiredService<ILogger<RaidNode>>()));
            service.AddSingleton<INode>(s => s.GetRequiredService<RaidNode>());
            return service;
        }

        /// <summary>
        /// Erstellt den <see cref="RaidNode"/> laut <paramref name="konfiguration"/>
        /// </summary>
        /// <exception cref="FileNotFoundException">Wenn die Schlüsseldatei fehlt</exception>
        public static RaidNode Erstellen(RaidKonfiguration konfiguration, IEnumerable<IRawNode> alleKnoten, ILogger<RaidNode> logger)
        {
            var alle = alleKnoten.ToList();
            var knoten = alle;
            if (konfiguration.Knoten.Count > 0)
            {
                var fehlend = konfiguration.Knoten.Where(n => alle.All(k => k.Name != n)).ToList();
                if (fehlend.Count > 0) throw new InvalidOperationException($"Unbekannte Knoten in RaidNode:Knoten: {string.Join(", ", fehlend)}");
                knoten = konfiguration.Knoten.Select(n => alle.First(k => k.Name == n)).ToList();
            }

            var pfad = konfiguration.SchluesselPfad;
            if (!File.Exists(pfad))
            {
                throw new FileNotFoundException($"Keine Schlüsseldatei für das RAID '{konfiguration.Name}'. Neu anlegen oder aus dem Wiederherstellungscode wiederherstellen.", pfad);
            }

            return new RaidNode(konfiguration.Name, knoten,
                new RaidOptionen
                {
                    DatenBloecke = konfiguration.DatenBloecke,
                    ParitaetsBloecke = konfiguration.ParitaetsBloecke,
                    SegmentGroesse = konfiguration.SegmentGroesse,
                    Ausfallgruppen = konfiguration.Ausfallgruppen,
                    Versionsregister = new DateiVersionsregister(konfiguration.RegisterPfad)
                },
                Schluesseldatei.Laden(pfad, konfiguration.SchutzErstellen()),
                logger);
        }
    }
}
