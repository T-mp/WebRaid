using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebRaid.Abstraction.Speicher;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Einstellungen eines <see cref="RaidNode"/>
    /// </summary>
    public sealed class RaidOptionen
    {
        /// <summary>Anzahl der Datenblöcke je Segment (k)</summary>
        public int DatenBloecke { get; set; } = 2;

        /// <summary>Anzahl der Paritätsblöcke je Segment (m) = verkraftbare Ausfälle</summary>
        public int ParitaetsBloecke { get; set; } = 1;

        /// <summary>Größe der Klartext-Segmente in Byte</summary>
        public int SegmentGroesse { get; set; } = 1024 * 1024;

        /// <summary>
        /// Knotenname → Ausfallgruppe. Knoten einer Gruppe können gemeinsam ausfallen (z.B. Web.de und GMX, beide United Internet).
        /// Je Segment liegt höchstens ein Block in einer Gruppe, „m Ausfälle“ zählt also Gruppen.
        /// Knoten ohne Eintrag bilden je eine eigene Gruppe.
        /// </summary>
        public Dictionary<string, string> Ausfallgruppen { get; set; } = new Dictionary<string, string>();

        /// <summary>Lokales Register gegen Rollback; ohne Angabe nur im Arbeitsspeicher</summary>
        public IVersionsRegister Versionsregister { get; set; }

        /// <summary>
        /// Kennung dieser Installation für die Schreibsperre. Bleibt über Neustarts gleich,
        /// damit ein neu gestarteter Dienst nicht von seiner eigenen alten Sperre ausgesperrt wird.
        /// </summary>
        public string Instanz { get; set; } = $"{Environment.MachineName}/{Environment.UserName}";

        /// <summary>So lange gilt die Schreibsperre ohne Erneuerung</summary>
        public TimeSpan SperrDauer { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Uhr, austauschbar für Tests</summary>
        public TimeProvider Zeit { get; set; } = TimeProvider.System;
    }

    /// <summary>
    /// Fasst mehrere <see cref="IRawNode"/> zu einem ausfallsicheren, verschlüsselten Speicher zusammen.
    /// <para>
    /// Jede Datei wird in Segmente geteilt, jedes Segment mit ChaCha20-Poly1305 und einem Schlüssel nur für
    /// diese Dateiversion verschlüsselt und per Reed-Solomon in k Daten- und m Paritätsblöcke zerlegt.
    /// Jeder Block landet auf einem anderen Knoten; bis zu m Knoten dürfen ausfallen oder falsche Daten liefern.
    /// </para>
    /// <para>
    /// Die Beschreibung der Datei (Manifest) wird mit dem Hauptschlüssel verschlüsselt und so auf alle Knoten
    /// verteilt, dass zwei beliebige Knoten genügen, um sie zu lesen. Kein einzelner Knoten sieht Dateinamen,
    /// Inhalte oder den Zusammenhang der Blöcke.
    /// </para>
    /// </summary>
    public class RaidNode : INode
    {
        private const int NonceLaenge = 12;
        private const int TagLaenge = 16;
        private const int KopfLaenge = 3;
        private const string SperrAdresse = "s/sperre";

        private readonly IReadOnlyList<IRawNode> knoten;
        private readonly Dictionary<string, IRawNode> knotenNachName;
        private readonly ReedSolomon datenCode;
        private readonly ReedSolomon metaCode;
        private readonly int segmentGroesse;
        private readonly byte[] manifestSchluessel;
        private readonly byte[] adressSchluessel;
        private readonly byte[] sperrSchluessel;
        private readonly IReadOnlyList<IRawNode[]> gruppen;
        private readonly IVersionsRegister register;
        private readonly string instanz;
        private readonly TimeSpan sperrDauer;
        private readonly TimeProvider zeit;
        private readonly SemaphoreSlim schreibSperre = new SemaphoreSlim(1, 1);
        private DateTimeOffset sperreErneuernAb = DateTimeOffset.MinValue;
        private readonly ILogger<RaidNode> logger;

        /// <summary>
        /// Erstellt einen RAID-Knoten über <paramref name="knoten"/>
        /// </summary>
        /// <param name="name">Eindeutiger Name</param>
        /// <param name="knoten">Die zugrundeliegenden Speicher, mindestens k + m, alle mit verschiedenem Namen</param>
        /// <param name="optionen">k, m und Segmentgröße</param>
        /// <param name="schluessel">Quelle des Hauptschlüssels</param>
        /// <param name="logger">Logger, um dem Speicherknoten auf die Finger schauen zu können ;-)</param>
        public RaidNode(string name, IEnumerable<IRawNode> knoten, RaidOptionen optionen, ISchluesselQuelle schluessel, ILogger<RaidNode> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (optionen == null) throw new ArgumentNullException(nameof(optionen));
            if (schluessel == null) throw new ArgumentNullException(nameof(schluessel));
            this.knoten = knoten?.ToList() ?? throw new ArgumentNullException(nameof(knoten));
            Name = name;

            if (this.knoten.Count == 0) throw new ArgumentException("Mindestens ein Knoten wird benötigt", nameof(knoten));
            if (this.knoten.Count > 255) throw new ArgumentException("Höchstens 255 Knoten", nameof(knoten));
            if (this.knoten.Select(k => k.Name).Distinct().Count() != this.knoten.Count) throw new ArgumentException("Die Knoten brauchen eindeutige Namen", nameof(knoten));
            if (optionen.SegmentGroesse < 1) throw new ArgumentOutOfRangeException(nameof(optionen), "SegmentGroesse muss positiv sein");
            knotenNachName = this.knoten.ToDictionary(k => k.Name);
            var zuordnung = optionen.Ausfallgruppen ?? new Dictionary<string, string>();
            gruppen = this.knoten
                .GroupBy(k => zuordnung.TryGetValue(k.Name, out var gruppe) ? "g:" + gruppe : "k:" + k.Name)
                .Select(g => g.ToArray())
                .ToList();
            PruefeRedundanz(optionen.DatenBloecke, optionen.ParitaetsBloecke, nameof(knoten));

            register = optionen.Versionsregister ?? new SpeicherVersionsregister();
            instanz = optionen.Instanz ?? throw new ArgumentNullException(nameof(optionen), "Instanz fehlt");
            sperrDauer = optionen.SperrDauer;
            zeit = optionen.Zeit ?? TimeProvider.System;

            datenCode = new ReedSolomon(optionen.DatenBloecke, optionen.ParitaetsBloecke);
            var metaDaten = Math.Min(2, this.knoten.Count);
            metaCode = new ReedSolomon(metaDaten, this.knoten.Count - metaDaten);
            segmentGroesse = optionen.SegmentGroesse;

            var haupt = schluessel.Hauptschluessel.Span;
            if (haupt.Length != 32) throw new ArgumentException("Der Hauptschlüssel muss 32 Byte lang sein", nameof(schluessel));
            manifestSchluessel = HKDF.DeriveKey(HashAlgorithmName.SHA256, haupt.ToArray(), 32, Array.Empty<byte>(), Encoding.UTF8.GetBytes("WebRaid/Manifest"));
            adressSchluessel = HKDF.DeriveKey(HashAlgorithmName.SHA256, haupt.ToArray(), 32, Array.Empty<byte>(), Encoding.UTF8.GetBytes("WebRaid/Adresse"));
            sperrSchluessel = HKDF.DeriveKey(HashAlgorithmName.SHA256, haupt.ToArray(), 32, Array.Empty<byte>(), Encoding.UTF8.GetBytes("WebRaid/Sperre"));

            logger.LogDebug($"New RaidNode({Name}, k={datenCode.DatenBloecke}, m={datenCode.ParitaetsBloecke}, Knoten={string.Join(",", knotenNachName.Keys)}, Ausfallgruppen={gruppen.Count})");
        }

        private void PruefeRedundanz(int datenBloecke, int paritaetsBloecke, string parameter)
        {
            if (datenBloecke + paritaetsBloecke > gruppen.Count)
            {
                throw new ArgumentException($"k + m = {datenBloecke + paritaetsBloecke} Blöcke brauchen mindestens so viele Knoten in verschiedenen Ausfallgruppen, vorhanden: {gruppen.Count}", parameter);
            }
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        /// <exception cref="IOException">Wenn zu viele Blöcke fehlen oder beschädigt sind</exception>
        /// <exception cref="InvalidDataException">Wenn die Daten nicht entschlüsselt werden können (falscher Schlüssel oder Manipulation)</exception>
        public async Task<Stream> Get(string adresse)
        {
            logger.LogDebug($"Get({adresse})");
            Adresse.Pruefen(adresse);

            var manifest = await ManifestLaden(adresse).ConfigureAwait(false);
            if (manifest == null)
            {
                logger.LogWarning($"'{adresse}' nicht gefunden!");
                return null;
            }

            var code = manifest.DatenBloecke == datenCode.DatenBloecke && manifest.ParitaetsBloecke == datenCode.ParitaetsBloecke
                ? datenCode
                : new ReedSolomon(manifest.DatenBloecke, manifest.ParitaetsBloecke);

            var ausgabe = new MemoryStream();
            using (var aead = new ChaCha20Poly1305(manifest.Schluessel))
            {
                for (var s = 0; s < manifest.Segmente.Count; s++)
                {
                    var chiffrat = await SegmentLesen(manifest, code, s).ConfigureAwait(false);
                    var klartext = new byte[chiffrat.Length - TagLaenge];
                    try
                    {
                        aead.Decrypt(SegmentNonce(s), chiffrat.AsSpan(0, klartext.Length), chiffrat.AsSpan(klartext.Length), klartext, SegmentDaten(manifest.Kennung, s));
                    }
                    catch (CryptographicException e)
                    {
                        throw new InvalidDataException($"'{adresse}': Segment {s} lässt sich nicht entschlüsseln", e);
                    }
                    ausgabe.Write(klartext);
                }
            }

            ausgabe.Position = 0;
            return ausgabe;
        }

        /// <inheritdoc />
        /// <returns>
        /// <para><see langword="true"/>, wenn jedes Segment auf mindestens k Knoten und das Manifest auf mindestens zwei Knoten liegt</para>
        /// <para>Sonst <see langword="false"/>; eine vorherige Version bleibt dann erhalten</para>
        /// </returns>
        public Task<bool> Write(string adresse, Stream input) =>
            Write(adresse, input, datenCode.DatenBloecke, datenCode.ParitaetsBloecke);

        /// <summary>
        /// Schreibt mit eigener Redundanz, z.B. je Ordner unterschiedlich
        /// </summary>
        /// <param name="adresse">Adresse / Pfad im weiteren Sinn</param>
        /// <param name="input"><see cref="Stream"/> der ab der aktuellen Position bis zum Ende geschrieben werden soll</param>
        /// <param name="datenBloecke">k für diese Datei</param>
        /// <param name="paritaetsBloecke">m für diese Datei = verkraftbare Ausfälle</param>
        /// <exception cref="IOException">Wenn eine andere Instanz das RAID gerade beschreibt</exception>
        public async Task<bool> Write(string adresse, Stream input, int datenBloecke, int paritaetsBloecke)
        {
            logger.LogDebug($"Write({adresse}, k={datenBloecke}, m={paritaetsBloecke})");
            Adresse.Pruefen(adresse);
            if (input == null) throw new ArgumentNullException(nameof(input));
            var code = datenBloecke == datenCode.DatenBloecke && paritaetsBloecke == datenCode.ParitaetsBloecke
                ? datenCode
                : new ReedSolomon(datenBloecke, paritaetsBloecke);
            PruefeRedundanz(datenBloecke, paritaetsBloecke, nameof(paritaetsBloecke));

            await schreibSperre.WaitAsync().ConfigureAwait(false);
            try
            {
                await SperreSichern().ConfigureAwait(false);
                return await Schreiben(adresse, input, code).ConfigureAwait(false);
            }
            finally
            {
                schreibSperre.Release();
            }
        }

        private async Task<bool> Schreiben(string adresse, Stream input, ReedSolomon code)
        {
            Manifest alt;
            try
            {
                alt = await ManifestLaden(adresse).ConfigureAwait(false);
            }
            catch (Exception e) when (e is InvalidDataException || e is IOException)
            {
                logger.LogWarning(e, $"Write({adresse}): Vorherige Version nicht lesbar, sie wird ersetzt");
                alt = null;
            }

            var bekannt = register.Lesen(ManifestAdresse(adresse));
            var manifest = new Manifest
            {
                Version = Math.Max(alt?.Version ?? 0, bekannt?.Version ?? 0) + 1,
                DatenBloecke = code.DatenBloecke,
                ParitaetsBloecke = code.ParitaetsBloecke,
                Schluessel = RandomNumberGenerator.GetBytes(32),
                Kennung = RandomNumberGenerator.GetBytes(16)
            };

            using (var aead = new ChaCha20Poly1305(manifest.Schluessel))
            {
                var puffer = new byte[segmentGroesse];
                var gelesen = await Fuellen(input, puffer).ConfigureAwait(false);
                for (var s = 0; gelesen > 0; s++)
                {
                    var chiffrat = new byte[gelesen + TagLaenge];
                    aead.Encrypt(SegmentNonce(s), puffer.AsSpan(0, gelesen), chiffrat.AsSpan(0, gelesen), chiffrat.AsSpan(gelesen), SegmentDaten(manifest.Kennung, s));

                    var segment = await SegmentSchreiben(manifest.Kennung, s, chiffrat, code).ConfigureAwait(false);
                    manifest.Segmente.Add(segment);
                    manifest.Laenge += gelesen;

                    var geschrieben = segment.Bloecke.Count(b => b != null);
                    if (geschrieben < code.DatenBloecke)
                    {
                        logger.LogWarning($"Write({adresse}): Segment {s} nur auf {geschrieben} von mindestens {code.DatenBloecke} Knoten geschrieben");
                        await BloeckeLoeschen(manifest).ConfigureAwait(false);
                        return false;
                    }
                    if (geschrieben < code.Bloecke)
                    {
                        logger.LogWarning($"Write({adresse}): Segment {s} nur auf {geschrieben} von {code.Bloecke} Knoten, Redundanz eingeschränkt");
                    }

                    gelesen = await Fuellen(input, puffer).ConfigureAwait(false);
                }
            }

            if (!await ManifestSchreiben(adresse, manifest).ConfigureAwait(false))
            {
                await BloeckeLoeschen(manifest).ConfigureAwait(false);
                return false;
            }

            register.Merken(ManifestAdresse(adresse), new VersionsStand(manifest.Version, false));
            if (alt != null) await BloeckeLoeschen(alt).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> Del(string adresse)
        {
            logger.LogDebug($"Del({adresse})");
            Adresse.Pruefen(adresse);

            await schreibSperre.WaitAsync().ConfigureAwait(false);
            try
            {
                var manifest = await ManifestLaden(adresse).ConfigureAwait(false);
                if (manifest == null)
                {
                    logger.LogWarning($"'{adresse}' nicht gefunden!");
                    return false;
                }

                await SperreSichern().ConfigureAwait(false);
                var manifestAdresse = ManifestAdresse(adresse);
                register.Merken(manifestAdresse, new VersionsStand(manifest.Version, true));
                await BloeckeLoeschen(manifest).ConfigureAwait(false);
                await Task.WhenAll(knoten.Select(k => Sicher(() => k.Del(manifestAdresse), $"Del({manifestAdresse}) auf {k.Name}"))).ConfigureAwait(false);
                return true;
            }
            finally
            {
                schreibSperre.Release();
            }
        }

        /// <summary>
        /// Gibt die Schreibsperre frei, damit eine andere Instanz sofort schreiben kann
        /// </summary>
        public async Task SperreFreigeben()
        {
            await schreibSperre.WaitAsync().ConfigureAwait(false);
            try
            {
                var daten = SperreKodieren(zeit.GetUtcNow());
                await Task.WhenAll(knoten.Select(k => Sicher(() => k.Write(SperrAdresse, new MemoryStream(daten, false)), $"Write({SperrAdresse}) auf {k.Name}"))).ConfigureAwait(false);
                sperreErneuernAb = DateTimeOffset.MinValue;
            }
            finally
            {
                schreibSperre.Release();
            }
        }

        #region Segmente

        /// <summary>Je Segment liegen alle Blöcke in verschiedenen Ausfallgruppen; die Gruppen rotieren über die Segmente</summary>
        private IRawNode KnotenFuer(int segment, int block)
        {
            var gruppe = gruppen[(block + segment) % gruppen.Count];
            return gruppe[segment % gruppe.Length];
        }

        private async Task<Segment> SegmentSchreiben(byte[] kennung, int s, byte[] chiffrat, ReedSolomon code)
        {
            var bloecke = Zerlegen(chiffrat, code);
            code.Kodieren(bloecke);

            var ergebnisse = await Task.WhenAll(Enumerable.Range(0, code.Bloecke).Select(async j =>
            {
                var ziel = KnotenFuer(s, j);
                var ok = await Sicher(() => ziel.Write(BlockAdresse(kennung, s, j), new MemoryStream(bloecke[j], false)),
                    $"Write(Segment {s}, Block {j}) auf {ziel.Name}").ConfigureAwait(false);
                return ok ? new Block { Knoten = ziel.Name, Hash = SHA256.HashData(bloecke[j]) } : null;
            })).ConfigureAwait(false);

            return new Segment { Laenge = chiffrat.Length, Bloecke = ergebnisse.ToList() };
        }

        private async Task<byte[]> SegmentLesen(Manifest manifest, ReedSolomon code, int s)
        {
            var segment = manifest.Segmente[s];
            var blockLaenge = BlockLaenge(segment.Laenge, code.DatenBloecke);
            var bloecke = new byte[code.Bloecke][];
            var vorhanden = new bool[code.Bloecke];

            await Task.WhenAll(Enumerable.Range(0, code.Bloecke).Select(async j =>
            {
                bloecke[j] = new byte[blockLaenge];
                var block = segment.Bloecke[j];
                if (block == null || !knotenNachName.TryGetValue(block.Knoten, out var quelle)) return;

                var daten = await Lesen(quelle, BlockAdresse(manifest.Kennung, s, j)).ConfigureAwait(false);
                if (daten == null) return;
                if (daten.Length != blockLaenge || !SHA256.HashData(daten).AsSpan().SequenceEqual(block.Hash))
                {
                    logger.LogWarning($"Segment {s}, Block {j} auf {quelle.Name} ist beschädigt");
                    return;
                }
                bloecke[j] = daten;
                vorhanden[j] = true;
            })).ConfigureAwait(false);

            var anzahl = vorhanden.Count(v => v);
            if (anzahl < code.DatenBloecke)
            {
                throw new IOException($"Segment {s}: nur {anzahl} von mindestens {code.DatenBloecke} Blöcken lesbar");
            }
            if (anzahl < code.Bloecke)
            {
                logger.LogWarning($"Segment {s}: {code.Bloecke - anzahl} Block/Blöcke fehlen, werden rekonstruiert");
            }
            if (vorhanden.Take(code.DatenBloecke).Any(v => !v)) code.Wiederherstellen(bloecke, vorhanden, nurDaten: true);

            return Zusammensetzen(bloecke, code.DatenBloecke, segment.Laenge);
        }

        private Task BloeckeLoeschen(Manifest manifest)
        {
            var aufgaben = new List<Task>();
            for (var s = 0; s < manifest.Segmente.Count; s++)
            {
                for (var j = 0; j < manifest.Segmente[s].Bloecke.Count; j++)
                {
                    var block = manifest.Segmente[s].Bloecke[j];
                    var ziel = block != null && knotenNachName.TryGetValue(block.Knoten, out var k) ? k : KnotenFuer(s, j);
                    var adresse = BlockAdresse(manifest.Kennung, s, j);
                    aufgaben.Add(Sicher(() => ziel.Del(adresse), $"Del({adresse}) auf {ziel.Name}"));
                }
            }
            return Task.WhenAll(aufgaben);
        }

        private static byte[] SegmentNonce(long segment)
        {
            var nonce = new byte[NonceLaenge];
            BinaryPrimitives.WriteInt64BigEndian(nonce.AsSpan(4), segment);
            return nonce;
        }

        private static byte[] SegmentDaten(byte[] kennung, long segment)
        {
            var daten = new byte[kennung.Length + 8];
            kennung.CopyTo(daten, 0);
            BinaryPrimitives.WriteInt64BigEndian(daten.AsSpan(kennung.Length), segment);
            return daten;
        }

        private static string BlockAdresse(byte[] kennung, int segment, int block)
        {
            var daten = new byte[kennung.Length + 8];
            kennung.CopyTo(daten, 0);
            BinaryPrimitives.WriteInt32BigEndian(daten.AsSpan(kennung.Length), segment);
            BinaryPrimitives.WriteInt32BigEndian(daten.AsSpan(kennung.Length + 4), block);
            return "b/" + Convert.ToHexStringLower(SHA256.HashData(daten), 0, 16);
        }

        #endregion

        #region Manifest

        private string ManifestAdresse(string adresse) =>
            "m/" + Convert.ToHexStringLower(HMACSHA256.HashData(adressSchluessel, Encoding.UTF8.GetBytes(adresse)), 0, 16);

        private async Task<bool> ManifestSchreiben(string adresse, Manifest manifest)
        {
            var manifestAdresse = ManifestAdresse(adresse);
            var json = JsonSerializer.SerializeToUtf8Bytes(manifest);

            // Länge | Nonce | Chiffrat | Prüfwert
            var nutzlast = new byte[4 + NonceLaenge + json.Length + TagLaenge];
            BinaryPrimitives.WriteInt32BigEndian(nutzlast, nutzlast.Length - 4);
            var nonce = nutzlast.AsSpan(4, NonceLaenge);
            RandomNumberGenerator.Fill(nonce);
            using (var aead = new ChaCha20Poly1305(manifestSchluessel))
            {
                aead.Encrypt(nonce, json, nutzlast.AsSpan(4 + NonceLaenge, json.Length), nutzlast.AsSpan(4 + NonceLaenge + json.Length), Encoding.UTF8.GetBytes(manifestAdresse));
            }

            var bloecke = Zerlegen(nutzlast, metaCode);
            metaCode.Kodieren(bloecke);

            var ergebnisse = await Task.WhenAll(knoten.Select((ziel, i) =>
            {
                // Kopf: Index, k, Anzahl - damit bleibt das Manifest lesbar, auch wenn sich die Knotenliste ändert
                var teil = new byte[KopfLaenge + bloecke[i].Length];
                teil[0] = (byte)i;
                teil[1] = (byte)metaCode.DatenBloecke;
                teil[2] = (byte)metaCode.Bloecke;
                bloecke[i].CopyTo(teil, KopfLaenge);
                return Sicher(() => ziel.Write(manifestAdresse, new MemoryStream(teil, false)), $"Write({manifestAdresse}) auf {ziel.Name}");
            })).ConfigureAwait(false);

            var geschrieben = ergebnisse.Count(e => e);
            if (geschrieben < metaCode.DatenBloecke)
            {
                logger.LogWarning($"Write({adresse}): Manifest nur auf {geschrieben} Knoten geschrieben");
                return false;
            }
            if (geschrieben < metaCode.Bloecke)
            {
                logger.LogWarning($"Write({adresse}): Manifest nur auf {geschrieben} von {metaCode.Bloecke} Knoten");
            }
            return true;
        }

        /// <returns>Das neueste lesbare Manifest, oder <see langword="null"/>, wenn kein Knoten eines hat</returns>
        private async Task<Manifest> ManifestLaden(string adresse)
        {
            var manifestAdresse = ManifestAdresse(adresse);
            var bekannt = register.Lesen(manifestAdresse);
            var teile = (await Task.WhenAll(knoten.Select(k => Lesen(k, manifestAdresse))).ConfigureAwait(false))
                .Where(t => t != null && t.Length > KopfLaenge && t[1] >= 1 && t[2] >= t[1] && t[0] < t[2])
                .ToList();
            if (teile.Count == 0)
            {
                if (bekannt != null && !bekannt.Geloescht)
                {
                    throw new IOException($"'{adresse}' (Version {bekannt.Version}) ist auf keinem Knoten mehr zu finden");
                }
                return null;
            }

            Manifest neuestes = null;
            foreach (var auswahl in Auswahlen(teile))
            {
                var manifest = ManifestEntschluesseln(auswahl, manifestAdresse);
                if (manifest != null && (neuestes == null || manifest.Version > neuestes.Version)) neuestes = manifest;
            }

            if (neuestes == null)
            {
                throw new InvalidDataException($"Die Metadaten zu '{adresse}' lassen sich nicht lesen (falscher Schlüssel oder beschädigt)");
            }

            if (bekannt != null)
            {
                if (bekannt.Geloescht && neuestes.Version <= bekannt.Version)
                {
                    logger.LogWarning($"'{adresse}' wurde gelöscht, ein Knoten liefert noch Version {neuestes.Version}");
                    return null;
                }
                if (neuestes.Version < bekannt.Version)
                {
                    throw new InvalidDataException($"'{adresse}': Knoten liefern Version {neuestes.Version}, bekannt ist {bekannt.Version} (Rollback?)");
                }
            }
            register.Merken(manifestAdresse, new VersionsStand(neuestes.Version, false));
            return neuestes;
        }

        /// <summary>Alle Kombinationen aus k Teilen mit passendem Kopf und verschiedenem Index</summary>
        private static IEnumerable<byte[][]> Auswahlen(List<byte[]> teile)
        {
            foreach (var gruppe in teile.GroupBy(t => (k: t[1], n: t[2], laenge: t.Length)))
            {
                var liste = gruppe.GroupBy(t => t[0]).Select(g => g.First()).ToList();
                var k = gruppe.Key.k;
                if (liste.Count < k) continue;
                if (k == 1)
                {
                    foreach (var t in liste) yield return new[] { t };
                }
                else
                {
                    for (var a = 0; a < liste.Count; a++)
                        for (var b = a + 1; b < liste.Count; b++)
                        {
                            if (k == 2) yield return new[] { liste[a], liste[b] };
                        }
                }
            }
        }

        private Manifest ManifestEntschluesseln(byte[][] auswahl, string manifestAdresse)
        {
            try
            {
                var k = auswahl[0][1];
                var n = auswahl[0][2];
                var code = new ReedSolomon(k, n - k);
                var blockLaenge = auswahl[0].Length - KopfLaenge;
                var bloecke = Enumerable.Range(0, n).Select(_ => new byte[blockLaenge]).ToArray();
                var vorhanden = new bool[n];
                foreach (var t in auswahl)
                {
                    t.AsSpan(KopfLaenge).CopyTo(bloecke[t[0]]);
                    vorhanden[t[0]] = true;
                }
                code.Wiederherstellen(bloecke, vorhanden, nurDaten: true);

                var nutzlast = Zusammensetzen(bloecke, k, k * blockLaenge);
                var laenge = BinaryPrimitives.ReadInt32BigEndian(nutzlast);
                if (laenge < NonceLaenge + TagLaenge || laenge > nutzlast.Length - 4) return null;

                var nonce = nutzlast.AsSpan(4, NonceLaenge);
                var chiffratLaenge = laenge - NonceLaenge - TagLaenge;
                var json = new byte[chiffratLaenge];
                using (var aead = new ChaCha20Poly1305(manifestSchluessel))
                {
                    aead.Decrypt(nonce, nutzlast.AsSpan(4 + NonceLaenge, chiffratLaenge), nutzlast.AsSpan(4 + NonceLaenge + chiffratLaenge, TagLaenge), json, Encoding.UTF8.GetBytes(manifestAdresse));
                }
                return JsonSerializer.Deserialize<Manifest>(json);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        #endregion

        #region Schreibsperre

        /// <summary>
        /// Stellt sicher, dass nur diese Instanz das RAID beschreibt.
        /// Auf jedem Knoten liegt ein mit dem Hauptschlüssel signierter Eintrag „Instanz, gültig bis“.
        /// </summary>
        private async Task SperreSichern()
        {
            var jetzt = zeit.GetUtcNow();
            if (jetzt < sperreErneuernAb) return;

            var eintraege = await Task.WhenAll(knoten.Select(k => Lesen(k, SperrAdresse))).ConfigureAwait(false);
            var fremd = eintraege.Select(SperreDekodieren).FirstOrDefault(e => e != null && e.Instanz != instanz && e.Bis > jetzt);
            if (fremd != null)
            {
                throw new IOException($"Das RAID '{Name}' wird bis {fremd.Bis:u} von einer anderen Instanz beschrieben");
            }

            var daten = SperreKodieren(jetzt + sperrDauer);
            var gesetzt = (await Task.WhenAll(knoten.Select(k => Sicher(() => k.Write(SperrAdresse, new MemoryStream(daten, false)), $"Write({SperrAdresse}) auf {k.Name}"))).ConfigureAwait(false))
                .Count(ok => ok);
            if (gesetzt < metaCode.DatenBloecke)
            {
                throw new IOException($"Die Schreibsperre für '{Name}' ließ sich nur auf {gesetzt} Knoten setzen");
            }
            sperreErneuernAb = jetzt + sperrDauer / 2;
        }

        private sealed record SperrEintrag(string Instanz, DateTimeOffset Bis);

        private byte[] SperreKodieren(DateTimeOffset bis)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(new SperrEintrag(instanz, bis));
            return HMACSHA256.HashData(sperrSchluessel, json).Concat(json).ToArray();
        }

        private SperrEintrag SperreDekodieren(byte[] daten)
        {
            if (daten == null || daten.Length <= 32) return null;
            var json = daten.AsSpan(32);
            if (!CryptographicOperations.FixedTimeEquals(daten.AsSpan(0, 32), HMACSHA256.HashData(sperrSchluessel, json))) return null;
            try
            {
                return JsonSerializer.Deserialize<SperrEintrag>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #endregion

        #region Hilfen

        private static int BlockLaenge(int laenge, int k) => Math.Max(1, (laenge + k - 1) / k);

        private static byte[][] Zerlegen(byte[] daten, ReedSolomon code)
        {
            var blockLaenge = BlockLaenge(daten.Length, code.DatenBloecke);
            var bloecke = new byte[code.Bloecke][];
            for (var i = 0; i < code.Bloecke; i++)
            {
                bloecke[i] = new byte[blockLaenge];
                if (i >= code.DatenBloecke) continue;
                var start = i * blockLaenge;
                if (start < daten.Length) daten.AsSpan(start, Math.Min(blockLaenge, daten.Length - start)).CopyTo(bloecke[i]);
            }
            return bloecke;
        }

        private static byte[] Zusammensetzen(byte[][] bloecke, int k, int laenge)
        {
            var daten = new byte[laenge];
            var blockLaenge = bloecke[0].Length;
            for (var i = 0; i < k; i++)
            {
                var start = i * blockLaenge;
                if (start >= laenge) break;
                bloecke[i].AsSpan(0, Math.Min(blockLaenge, laenge - start)).CopyTo(daten.AsSpan(start));
            }
            return daten;
        }

        private static async Task<int> Fuellen(Stream input, byte[] puffer)
        {
            var gelesen = 0;
            while (gelesen < puffer.Length)
            {
                var n = await input.ReadAsync(puffer.AsMemory(gelesen)).ConfigureAwait(false);
                if (n == 0) break;
                gelesen += n;
            }
            return gelesen;
        }

        /// <returns>Den Inhalt, oder <see langword="null"/>, wenn nicht vorhanden oder der Knoten nicht erreichbar ist</returns>
        private async Task<byte[]> Lesen(IRawNode quelle, string adresse)
        {
            try
            {
                var stream = await quelle.Get(adresse).ConfigureAwait(false);
                if (stream == null) return null;
                using (stream)
                using (var kopie = new MemoryStream())
                {
                    await stream.CopyToAsync(kopie).ConfigureAwait(false);
                    return kopie.ToArray();
                }
            }
            catch (Exception e) when (e is not ArgumentException)
            {
                logger.LogWarning(e, $"Get({adresse}) auf {quelle.Name} fehlgeschlagen");
                return null;
            }
        }

        /// <summary>Ein ausgefallener Knoten ist ein erwarteter Fall, keine Ausnahme</summary>
        private async Task<bool> Sicher(Func<Task<bool>> aktion, string beschreibung)
        {
            try
            {
                return await aktion().ConfigureAwait(false);
            }
            catch (Exception e) when (e is not ArgumentException)
            {
                logger.LogWarning(e, $"{beschreibung} fehlgeschlagen");
                return false;
            }
        }

        #endregion
    }
}
