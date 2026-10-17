using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum HuntResponse { Ring, Guide, RandomCompanion }

    public sealed class HuntDieView
    {
        public string Id { get; set; } = "";
        public int First { get; set; }
        public int Result { get; set; }
        public bool Rerolled { get; set; }
        public bool Success { get; set; }
    }

    public sealed class HuntView
    {
        public string SourceId { get; set; } = "";
        public string Stage { get; set; } = "";
        public int Bonus { get; set; }
        public int? MovementBonus { get; set; }
        public int? EffectBonus { get; set; }
        public int RerollBonus { get; set; }
        public bool Mordor { get; set; }
        public int EyeDamage { get; set; }
        public List<HuntDieView> Dice { get; set; } = new List<HuntDieView>();
        public List<string> RerollSources { get; set; } = new List<string>();
        public string TileId { get; set; } = "";
        public int? TileDie { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public bool DamagePending { get; set; }
        public bool CorruptionOnly { get; set; }
        public bool SeparatesCompanions { get; set; }
        public bool Reveal { get; set; }
        public bool Stop { get; set; }
        public bool CasualtyTaken { get; set; }
        public bool GuideReturns { get; set; }
        public int PoolCount { get; set; }
        // UI-141: the printed face of TileId, for its artwork.
        public HuntTileFaceView? Face { get; set; }
    }

    // UI-141: the printed face of a Hunt tile: its pool (standard, special-fp, special-sh, smeagol), value
    // (a number, "eye" or "die"), the Reveal and Stop marks and the box it comes from (base, lords, kings).
    // Count is how many tiles of a list share this face.
    public sealed class HuntTileFaceView
    {
        public string Pool { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Reveal { get; set; }
        public bool Stop { get; set; }
        public string Source { get; set; } = "";
        public int Count { get; set; } = 1;
    }

    // UI-141: the public composition of the Hunt pool, grouped by face; never the drawing order. Tiles also holds
    // the tiles this viewer saw leave the pool face down (Unseen of them: Corruption tiles on a sovereign it
    // cannot read, an unrevealed corruption draw), so the list never tells their identity. Count is the number of
    // tiles in the pool; Prepared are special tiles in play that join the pool when the Fellowship enters Mordor.
    public sealed class HuntPoolView
    {
        public List<HuntTileFaceView> Tiles { get; set; } = new List<HuntTileFaceView>();
        public int Count { get; set; }
        public int Unseen { get; set; }
        public List<HuntTileFaceView> Prepared { get; set; } = new List<HuntTileFaceView>();
    }
}
