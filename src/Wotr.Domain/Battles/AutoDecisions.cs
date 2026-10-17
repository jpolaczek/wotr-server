using Wotr.Contracts;

namespace Wotr.Domain;

public static class AutoDecisions
{
    public static string? Validate(MatchState state, Side actor, AutoDecisionChange? change)
    {
        if (change is null || BattleRuntime.Active(state) is not { } battle || battle.Id != change.BattleId)
            return "DecisionExpired";
        var player = state.Player(actor);
        if (change.ExpectedVersion != player.AutoDecision.Version || change.Enabled && change.HandVersion != player.HandVersion)
            return "StaleRevision";
        if (change.Enabled && battle.Stage is BattleStage.Finish or BattleStage.Advance or BattleStage.Complete)
            return "DecisionExpired";
        return null;
    }

    public static void Set(MatchState state, Side actor, AutoDecisionChange change)
    {
        var player = state.Player(actor);
        player.AutoDecision.Version++;
        player.AutoDecision.Policy = change.Enabled ? new()
        {
            Kind = AutoDecisionKind.SkipCombatCards,
            Owner = actor,
            BattleId = change.BattleId,
            HandVersion = player.HandVersion
        } : null;
    }

    public static bool CanAnswer(MatchState state)
    {
        if (state.Pending is not { Kind: DecisionKind.CombatCard } pending || BattleRuntime.Current(state) is not { } battle)
            return false;
        var actor = pending.Owner;
        var correctStage = actor == battle.Attacker.Side ? BattleStage.AttackerCard : BattleStage.DefenderCard;
        var player = state.Player(actor);
        return battle.Stage == correctStage && !battle.Force(actor).Round.CardCommitted &&
            player.AutoDecision.Policy is { Kind: AutoDecisionKind.SkipCombatCards } policy &&
            policy.Owner == actor && policy.BattleId == battle.Id && policy.HandVersion == player.HandVersion;
    }

    public static void RecordAnswer(MatchState state, BattleState battle, Side actor)
    {
        var preference = state.Player(actor).AutoDecision;
        preference.LastSkippedBattleId = battle.Id;
        preference.LastSkippedRound = battle.Round;
        // A queued cancellation still compares the same preference version after this answer.
    }

    public static void Expire(PlayerState player)
    {
        if (player.AutoDecision.Policy is null)
            return;
        player.AutoDecision.Policy = null;
        player.AutoDecision.Version++;
    }

    public static void Reconcile(MatchState state)
    {
        foreach (var player in state.Players)
            if (player.AutoDecision.Policy is { } policy && (state.Winner is not null ||
                policy.HandVersion != player.HandVersion || !state.Resolution.Frames.Any(f => f.Battle?.Id == policy.BattleId)))
                Expire(player);
    }

    public static AutoDecisionView View(PlayerState player) => new()
    {
        Version = player.AutoDecision.Version,
        HandVersion = player.HandVersion,
        Policy = player.AutoDecision.Policy is { } policy ? new()
        {
            Kind = policy.Kind,
            Owner = policy.Owner,
            BattleId = policy.BattleId,
            HandVersion = policy.HandVersion
        } : null,
        LastSkippedBattleId = player.AutoDecision.LastSkippedBattleId,
        LastSkippedRound = player.AutoDecision.LastSkippedRound
    };
}
