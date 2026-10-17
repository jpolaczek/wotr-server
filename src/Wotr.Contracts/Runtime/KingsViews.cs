using System.Collections.Generic;

namespace Wotr.Contracts
{
    public sealed class CombatReplacementView
    {
        public Side Side { get; set; }
        public string SourceId { get; set; } = "";
    }

    public sealed class RulerTokenView
    {
        public string Id { get; set; } = "";
        public string SovereignId { get; set; } = "";
        public string Location { get; set; } = "";
        public DieFace Face { get; set; }
    }

    public sealed class RulerTokenActionOption
    {
        public string TokenId { get; set; } = "";
        public string AbilityId { get; set; } = "";
        public ActionOption Action { get; set; } = new ActionOption();
    }

    public sealed class RulerTravelOption
    {
        public string AbilityId { get; set; } = "";
        public List<string> Dice { get; set; } = new List<string>();
        public int HuntDiceReduction { get; set; }
        public int RangeBonus { get; set; }
        public List<TravelOption> Travel { get; set; } = new List<TravelOption>();
    }
    public sealed class RulerRecruitmentOption
    {
        public string LocalRegion { get; set; } = "";
        public bool LocalAllowsElite { get; set; }
        public int SettlementCount { get; set; }
        public bool LocalMustDiffer { get; set; }
        public List<string> Settlements { get; set; } = new List<string>();
    }
    public enum SovereignStatus { Initial, Awakened, Corrupted, Removed }

    public sealed class SovereignView
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string DefinitionId { get; set; } = "";
        public string Nation { get; set; } = "";
        public string RegionId { get; set; } = "";
        public bool Garrison { get; set; }
        public SovereignStatus Status { get; set; }
        public int Resistance { get; set; }
        public int? Corruption { get; set; }
        public bool AttemptedThisTurn { get; set; }
        public List<CorruptionTileView> Tiles { get; set; } = new List<CorruptionTileView>();
    }

    public sealed class CorruptionTileView
    {
        // Hidden backs use an attachment slot, never the physical tile's value-bearing ID.
        public string Id { get; set; } = "";
        public string TileId { get; set; } = "";
        public int? Value { get; set; }
        public bool Reveal { get; set; }
        public bool Kings { get; set; }
    }

    public sealed class CorruptionAttemptView
    {
        public bool Sequential { get; set; }
        public string SovereignId { get; set; } = "";
        public string DieId { get; set; } = "";
        public int DrawnCount { get; set; }
        public int DrawLimit { get; set; }
        public List<string> RevealedTiles { get; set; } = new List<string>();
        public List<CorruptionTileView> OwnChoices { get; set; } = new List<CorruptionTileView>();
    }
}
