using System;
using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum Side
    {
        FreePeoples, Shadow
    }
    public enum UnitKind
    {
        Regular, Elite, Leader, Nazgul
    }
    public enum DieFace
    {
        Character, Army, Muster, ArmyMuster, Event, WillOfTheWest, Eye, CardDraw, UseBalrog
    }
    public enum DieLocation
    {
        Available, Resolving, Spent, Hunt, Discarded, Removed, Unrolled, AwaitingChoice, FellowshipBox
    }
    public sealed class BoardData
    {
        public string Version { get; set; } = "";
        public string Source { get; set; } = "";
        public List<RegionDefinition> Regions { get; set; } = new List<RegionDefinition>();
        public List<NationDefinition> Nations { get; set; } = new List<NationDefinition>();
        public List<UnitPlacement> Units { get; set; } = new List<UnitPlacement>();
        public List<BorderDefinition> ImpassableBorders { get; set; } = new List<BorderDefinition>();
        public List<DieDefinition> Dice { get; set; } = new List<DieDefinition>();
        public List<string> Cards { get; set; } = new List<string>();
        public List<string> CharacterDefinitions { get; set; } = new List<string>();
        public List<HuntTileDefinition> HuntTiles { get; set; } = new List<HuntTileDefinition>();
    }
    public sealed class RegionDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Nation { get; set; }
        public Side? OriginalOwner { get; set; }
        public string? Settlement { get; set; }
        public int VictoryPoints { get; set; }
        public bool Coastal { get; set; }
        public List<string> Neighbors { get; set; } = new List<string>();
    }
    public sealed class NationDefinition
    {
        public string Id { get; set; } = "";
        public Side Side { get; set; }
        public bool Active { get; set; }
        public int StepsToWar { get; set; }
    }
    public sealed class UnitPlacement
    {
        public string Id { get; set; } = "";
        public string Nation { get; set; } = "";
        public UnitKind Kind { get; set; }
        // reserve, casualties, removed or a stable region ID; garrison modeled in MatchState.
        public string Location { get; set; } = "reserve";
    }
    public sealed class BorderDefinition
    {
        public string A { get; set; } = "";
        public string B { get; set; } = "";
    }
    public sealed class DieDefinition
    {
        public Side Side { get; set; }
        public int InitialCount { get; set; }
        public int MaximumCount { get; set; }
        public List<DieFace> Faces { get; set; } = new List<DieFace>();
    }
    public sealed class HuntTileDefinition
    {
        public string Id { get; set; } = "";
        public string Pool { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Reveal { get; set; }
        public bool Stop { get; set; }
    }
}
