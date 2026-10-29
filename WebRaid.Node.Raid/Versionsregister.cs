using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Zuletzt bekannter Stand einer Datei
    /// </summary>
    /// <param name="Version">Höchste gesehene Version</param>
    /// <param name="Geloescht">Ob diese Version eine Löschung war</param>
    public sealed record VersionsStand(long Version, bool Geloescht);

    /// <summary>
    /// Merkt sich lokal die höchste bekannte Version jeder Datei.
    /// Damit fällt auf, wenn Anbieter eine ältere, aber gültig verschlüsselte Version ausliefern (Rollback)
    /// oder eine gelöschte Datei wieder auftauchen lassen.
    /// </summary>
    public interface IVersionsRegister
    {
        /// <summary>Der bekannte Stand, oder <see langword="null"/>, wenn die Datei hier noch nie gesehen wurde</summary>
        VersionsStand Lesen(string schluessel);

        /// <summary>Merkt sich <paramref name="stand"/>, sofern er nicht älter ist als der bekannte</summary>
        void Merken(string schluessel, VersionsStand stand);
    }

    /// <summary>
    /// Register nur im Arbeitsspeicher; schützt innerhalb einer Laufzeit des Dienstes
    /// </summary>
    public class SpeicherVersionsregister : IVersionsRegister
    {
        /// <summary>Die bekannten Stände</summary>
        protected readonly ConcurrentDictionary<string, VersionsStand> Staende = new ConcurrentDictionary<string, VersionsStand>();

        /// <inheritdoc />
        public VersionsStand Lesen(string schluessel) => Staende.TryGetValue(schluessel, out var stand) ? stand : null;

        /// <inheritdoc />
        public virtual void Merken(string schluessel, VersionsStand stand)
        {
            Staende.AddOrUpdate(schluessel, stand, (_, alt) => stand.Version > alt.Version || (stand.Version == alt.Version && stand.Geloescht) ? stand : alt);
        }
    }

    /// <summary>
    /// Register, das in einer lokalen Datei gespeichert wird und damit Neustarts übersteht
    /// </summary>
    public sealed class DateiVersionsregister : SpeicherVersionsregister
    {
        private readonly string pfad;
        private readonly object sperre = new object();

        /// <summary>
        /// Lädt das Register aus <paramref name="pfad"/>, falls vorhanden
        /// </summary>
        public DateiVersionsregister(string pfad)
        {
            this.pfad = pfad ?? throw new ArgumentNullException(nameof(pfad));
            var ordner = Path.GetDirectoryName(Path.GetFullPath(pfad));
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
            if (File.Exists(pfad))
            {
                var gelesen = JsonSerializer.Deserialize<Dictionary<string, VersionsStand>>(File.ReadAllBytes(pfad));
                foreach (var eintrag in gelesen) Staende[eintrag.Key] = eintrag.Value;
            }
        }

        /// <inheritdoc />
        public override void Merken(string schluessel, VersionsStand stand)
        {
            var vorher = Lesen(schluessel);
            base.Merken(schluessel, stand);
            if (Equals(Lesen(schluessel), vorher)) return;
            lock (sperre)
            {
                // Erst in eine Nachbardatei, dann ersetzen: ein Absturz hinterlässt nie eine halbe Datei
                var neu = pfad + ".neu";
                File.WriteAllBytes(neu, JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, VersionsStand>(Staende)));
                File.Move(neu, pfad, true);
            }
        }
    }
}
