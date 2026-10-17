using Wotr.Contracts;

namespace Wotr.Domain;

public enum BattleStage
{
    AttackerCard, DefenderCard, BeforeRoll, CombatRoll, BeforeReroll, AttackerReroll, DefenderReroll,
    LeaderReroll, AfterReroll, BeforeLosses, AttackerLosses, DefenderLosses, AfterLosses,
    RoundEnd, Continue, Retreat, Finish, Advance, Complete, StrongholdDefense, SiegeContinuation, NextRound, BeforeReveal
}

public sealed class BattleState
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public KingsBattleState? Kings { get; set; }
    public string Id { get; set; } = "";
    public string Region { get; set; } = "";
    public BattleForce Attacker { get; set; } = new();
    public BattleForce Defender { get; set; } = new();
    public int Round { get; set; } = 1;
    public BattleStage Stage { get; set; }
    public BattleMode Mode { get; set; }
    public bool CardsRevealed { get; set; }
    public bool DefenderRetreated { get; set; }
    public bool DefenderSheltered { get; set; }
    public bool ArrivalBeforeBattle { get; set; }
    public int CompletedRounds { get; set; }
    public bool EndedByEffect { get; set; }
    public bool WhiteRiderOffered { get; set; }
    public bool WhiteRiderUsed { get; set; }
    public bool SorcererOffered { get; set; }
    public int MandatorySiegeRounds { get; set; }
    public bool FirstRoundRequiresCompanionCard { get; set; }
    public EffectReturn Return { get; set; }
    public List<CombatWork> Work { get; set; } = [];
    public int NextWork { get; set; }
    public List<SpecialAttackResult> SpecialAttacks { get; set; } = [];
    public BattleForce Force(Side side) => Attacker.Side == side ? Attacker : Defender;
    public BattleForce Opponent(Side side) => Force(TurnFlow.Other(side));
    public string RoundScope => $"{Id}:round:{Round}";
}

public sealed class BattleForce
{
    public int CombatDiceReduction { get; set; }
    public Side Side { get; set; }
    public string Region { get; set; } = "";
    public bool Garrison { get; set; }
    public List<string> Participants { get; set; } = [];
    public BattleRoundState Round { get; set; } = new();
}

public sealed class BattleRoundState
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int LeadershipPenalty { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplacementSource { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool DoubleSixes { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool OrderlyWithdrawal { get; set; }
    public bool FieldDefenseChosen { get; set; }
    public bool Closed { get; set; }
    public bool CardCommitted { get; set; }
    public string CardId { get; set; } = "";
    public bool CardCancelled { get; set; }
    public bool CardFromTable { get; set; }
    public int CombatModifier { get; set; }
    public int RerollModifier { get; set; }
    public int StrengthBonus { get; set; }
    public int CombatDicePenalty { get; set; }
    public int CombatDiceLimit { get; set; } = 5;
    public int RerollLimit { get; set; } = 5;
    public int ExtraHits { get; set; }
    public int SpentHits { get; set; }
    public int CancelHits { get; set; }
    public int SpecialHits { get; set; }
    public HashSet<string> CancelledLeadership { get; set; } = [];
    public HashSet<string> CancelledAbilities { get; set; } = [];
    public Dictionary<string, int> ForfeitedLeadership { get; set; } = [];
    public List<BattleDie> Dice { get; set; } = [];
    public List<string> SelectedRerolls { get; set; } = [];
}

public sealed class BattleDie
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

public sealed record CombatWork(Side Owner, string CardId, string ProgramId, int Initiative);
public sealed record SpecialAttackResult(Side Owner, string SourceId, List<BattleDie> Dice);
public sealed class BattleLossProcedure
{
    public Side Owner { get; set; }
    public int Hits { get; set; }
    public bool Voluntary { get; set; }
    public bool AdditionalAttack { get; set; }
    public string SourceId { get; set; } = "";
}
