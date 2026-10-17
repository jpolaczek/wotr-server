using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum MapBorderKind
    {
        Passage, River, Impassable, Coast, Lake, Frame
    }

    public sealed class MapBorderCatalog
    {
        public string Version { get; set; } = "";
        public string GeometryVersion { get; set; } = "";
        public string GeometryHash { get; set; } = "";
        public string AuditHash { get; set; } = "";
        public List<MapBorderPath> Paths { get; set; } = new List<MapBorderPath>();
    }

    public sealed class MapBorderPath
    {
        public string Id { get; set; } = "";
        public string? PositiveNormalOwner { get; set; }
        public string? NegativeNormalOwner { get; set; }
        public string SourceReference { get; set; } = "";
        public List<MapBorderPart> Parts { get; set; } = new List<MapBorderPart>();
    }

    public sealed class MapBorderPart
    {
        public int FromSegment { get; set; }
        public int ToSegment { get; set; }
        public MapBorderKind Kind { get; set; }
    }

    public sealed class MapStyle
    {
        public string Version { get; set; } = "";
        public string Source { get; set; } = "";
        public string Land { get; set; } = "";
        public string Water { get; set; } = "";
        public string Passage { get; set; } = "";
        public string Barrier { get; set; } = "";
        public string Keyline { get; set; } = "";
        public float PassageWidth { get; set; }
        public float BarrierWidth { get; set; }
        public float NationWidth { get; set; }
        public List<MapNationStyle> Nations { get; set; } = new List<MapNationStyle>();
    }

    public sealed class MapNationStyle
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string ColorName { get; set; } = "";
        public string Color { get; set; } = "";
    }
}
