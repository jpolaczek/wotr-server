using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class MapGeometry
    {
        public string Version { get; set; } = "";
        public float Width { get; set; }
        public float Height { get; set; }
        public MapGeometryTransform Transform { get; set; } = new MapGeometryTransform();
        public Dictionary<string, string> SourceHashes { get; set; } = new Dictionary<string, string>();
        public List<RegionGeometry> Regions { get; set; } = new List<RegionGeometry>();
        public List<MapBoundaryGeometry> Boundaries { get; set; } = new List<MapBoundaryGeometry>();
    }

    public sealed class MapGeometryTransform
    {
        public float[] SourceViewBox { get; set; } = Array.Empty<float>();
        public int[] PngSize { get; set; } = Array.Empty<int>();
        public float[] SourceOffset { get; set; } = Array.Empty<float>();
        public float SourceScale { get; set; }
        public float PngScale { get; set; }
        public float[] PngOffset { get; set; } = Array.Empty<float>();
    }

    public sealed class RegionGeometry
    {
        public string Id { get; set; } = "";
        public int MaskNumber { get; set; }
        public List<MapContourRing> Rings { get; set; } = new List<MapContourRing>();
        public float[] Anchor { get; set; } = Array.Empty<float>();
    }

    public sealed class MapContourRing
    {
        public bool Hole { get; set; }
        // Explicitly closed: the final vertex equals the first. Shells have
        // positive signed area in screen coordinates; holes have negative area.
        public float[][] Points { get; set; } = Array.Empty<float[]>();
    }

    public sealed class MapBoundaryGeometry
    {
        public string Id { get; set; } = "";
        public int SourceIndex { get; set; }
        // Null means water/outside, never a synthetic selectable region.
        public string?[] Owners { get; set; } = Array.Empty<string?>();
        public float[][] Points { get; set; } = Array.Empty<float[]>();
    }
}
