using System.Collections.Generic;

namespace WebRaid.Node.Raid
{
    /// <summary>
    /// Beschreibt eine gespeicherte Datei: Schlüssel, Aufteilung und Ablageort aller Blöcke.
    /// Wird selbst verschlüsselt und auf allen Knoten verteilt abgelegt.
    /// </summary>
    internal sealed class Manifest
    {
        public int Format { get; set; } = 1;

        /// <summary>Wird bei jedem Schreiben hochgezählt (Grundlage für den Rollback-Schutz)</summary>
        public long Version { get; set; }

        /// <summary>Länge des Klartexts in Byte</summary>
        public long Laenge { get; set; }

        public int DatenBloecke { get; set; }
        public int ParitaetsBloecke { get; set; }

        /// <summary>Schlüssel nur für diese Version der Datei</summary>
        public byte[] Schluessel { get; set; }

        /// <summary>Zufällige Kennung dieser Version, aus der die Blockadressen abgeleitet werden</summary>
        public byte[] Kennung { get; set; }

        public List<Segment> Segmente { get; set; } = new List<Segment>();
    }

    internal sealed class Segment
    {
        /// <summary>Länge des verschlüsselten Segments (Klartext + 16 Byte Prüfwert)</summary>
        public int Laenge { get; set; }

        /// <summary>Je Block der Knoten und der SHA-256-Wert; <see langword="null"/>, wenn das Schreiben fehlschlug</summary>
        public List<Block> Bloecke { get; set; } = new List<Block>();
    }

    internal sealed class Block
    {
        public string Knoten { get; set; }
        public byte[] Hash { get; set; }
    }
}
