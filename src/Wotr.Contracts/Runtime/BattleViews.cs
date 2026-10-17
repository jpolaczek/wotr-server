using System.Collections.Generic;

namespace Wotr.Contracts
{
    public enum BattleLossKind { Remove, Downgrade }
    public enum BattleMode { Field, Siege, Sortie, Relief }

    public sealed class BattleLossChoice
    {
        public string UnitId { get; set; } = "";
        public BattleLossKind Kind { get; set; }
    }

    public sealed class BattleView
    {
        public string Id { get; set; } = "";
        public string RegionId { get; set; } = "";
        public int Round { get; set; }
        public int MandatorySiegeRounds { get; set; }
        public bool CompanionRequiredForCard { get; set; }
        public bool DenethorBlocksCards { get; set; }
        public bool VoluntaryLosses { get; set; }
        public bool ArrivalBeforeBattle { get; set; }
        public string Stage { get; set; } = "";
        public BattleMode Mode { get; set; }
        public bool CardsRevealed { get; set; }
        public bool DefenderSheltered { get; set; }
        public bool WhiteRiderActive { get; set; }
        public List<string> OwnTableCombatCards { get; set; } = new List<string>();
        public BattleForceView Attacker { get; set; } = new BattleForceView();
        public BattleForceView Defender { get; set; } = new BattleForceView();
        public Side? LossOwner { get; set; }
        public int PendingHits { get; set; }
        public string LossSource { get; set; } = "";
        public List<BattleAttackView> SpecialAttacks { get; set; } = new List<BattleAttackView>();
    }

    public sealed class BattleForceView
    {
        public Side Side { get; set; }
        public string RegionId { get; set; } = "";
        public bool Garrison { get; set; }
        public List<string> ParticipantIds { get; set; } = new List<string>();
        public List<UnitView> Units { get; set; } = new List<UnitView>();
        public List<CharacterView> Characters { get; set; } = new List<CharacterView>();
        public int Strength { get; set; }
        public int CaptainBonus { get; set; }
        public int Leadership { get; set; }
        public Dictionary<string, int> ForfeitedLeadership { get; set; } = new Dictionary<string, int>();
        public List<string> CancelledAbilities { get; set; } = new List<string>();
        public int CombatDice { get; set; }
        public int CombatDiceReduction { get; set; }
        public int HitTarget { get; set; }
        public int CombatModifier { get; set; }
        public int RerollModifier { get; set; }
        public int Hits { get; set; }
        public int SpentHits { get; set; }
        public bool CardCommitted { get; set; }
        public string CardBack { get; set; } = "";
        public string? CardId { get; set; }
        public bool CardCancelled { get; set; }
        public List<BattleDieView> Dice { get; set; } = new List<BattleDieView>();
        public List<string> OwnRerollSelection { get; set; } = new List<string>();
    }

    public sealed class BattleDieView
    {
        public string Id { get; set; } = "";
        public int Result { get; set; }
        public int Modifier { get; set; }
        public int HitTarget { get; set; }
        public bool Hit { get; set; }
        public bool ConvertedHit { get; set; }
        public int? Reroll { get; set; }
        public int RerollModifier { get; set; }
        public bool RerollHit { get; set; }
        public bool CannotReroll { get; set; }
    }

    public sealed class BattleAttackView
    {
        public string SourceId { get; set; } = "";
        public Side Side { get; set; }
        public List<BattleDieView> Dice { get; set; } = new List<BattleDieView>();
    }
}
