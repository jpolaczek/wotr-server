using Wotr.Contracts;

namespace Wotr.Domain;

// RANK-06 (R35, docs/RANKED_PLAY.md D9): which limit each decision runs on, and the limits of each preset. The engine
// holds no time: the server keeps the deadlines and only asks here how long a decision may take.
public static class MoveClock
{
    // The server's deadline is this much beyond the limit the client shows, so latency and the opponent's presentation
    // never cost a player the game while their clock still shows time.
    public static readonly TimeSpan Grace = TimeSpan.FromSeconds(3);
    // The first decision of a ranked match also covers loading the board.
    public static readonly TimeSpan FirstDecisionBonus = TimeSpan.FromSeconds(15);
    // D10: the warning sounds when this much time is left.
    public static readonly TimeSpan Warning = TimeSpan.FromSeconds(10);

    // Deliberately exhaustive: a new decision kind cannot be used in a ranked match before it is classified here.
    public static MoveClockKind Kind(DecisionKind kind) => kind switch
    {
        DecisionKind.FellowshipPhase or DecisionKind.AllocateHunt or DecisionKind.Action or DecisionKind.ActionSequence or
            DecisionKind.ArmyEvent or DecisionKind.CharacterEvent or DecisionKind.CouncilVersions or
            DecisionKind.CouncilHomelands or DecisionKind.CouncilToken => MoveClockKind.Big,
        DecisionKind.DiscardCards or DecisionKind.Reaction or DecisionKind.CombatCard or DecisionKind.Losses or
            DecisionKind.HuntDamage or DecisionKind.CorruptionTile or DecisionKind.RemoveExcess or DecisionKind.EffectChoice or
            DecisionKind.FellowshipRoute or DecisionKind.Guide or DecisionKind.CompanionSeparation or DecisionKind.HuntReroll or
            DecisionKind.BattleReroll or DecisionKind.BattleContinue or DecisionKind.BattleRetreat or
            DecisionKind.BattleAdvance or DecisionKind.StrongholdDefense or DecisionKind.SiegeContinuation or
            DecisionKind.SpecialDieChoice or DecisionKind.KeepActionDie or DecisionKind.RulerDiscard or
            DecisionKind.Dismay => MoveClockKind.Small,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Decision has no move clock.")
    };

    // D4's presets: Fast 1:30 / 0:20, Standard 3:00 / 0:30, Long 5:00 / 1:00.
    public static TimeSpan Limit(RankedPreset preset, MoveClockKind kind) => (preset, kind) switch
    {
        (RankedPreset.Fast, MoveClockKind.Big) => TimeSpan.FromSeconds(90),
        (RankedPreset.Fast, MoveClockKind.Small) => TimeSpan.FromSeconds(20),
        (RankedPreset.Standard, MoveClockKind.Big) => TimeSpan.FromMinutes(3),
        (RankedPreset.Standard, MoveClockKind.Small) => TimeSpan.FromSeconds(30),
        (RankedPreset.Long, MoveClockKind.Big) => TimeSpan.FromMinutes(5),
        (RankedPreset.Long, MoveClockKind.Small) => TimeSpan.FromMinutes(1),
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown preset.")
    };
}
