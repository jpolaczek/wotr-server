using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleLosses
{
    public static BattleLossProcedure? Current(MatchState state) => state.Resolution.Frames.LastOrDefault()?.BattleLoss;

    public static void Begin(MatchState state, BattleForce force, int hits, string source, bool voluntary = false, bool additionalAttack = false)
    {
        if (hits <= 0 || !BattleRules.Alive(state, force))
            return;
        state.Resolution.Frames.Add(new() { BattleLoss = new() { Owner = force.Side, Hits = hits, SourceId = source, Voluntary = voluntary, AdditionalAttack = additionalAttack } });
        state.Open(DecisionKind.Losses, force.Side, [CommandKind.TakeBattleLosses], hits);
        state.Pending!.SourceId = source;
        state.Pending.Presentation = voluntary ? "VoluntaryLosses" : "";
    }

    public static ArmyLossPlan? Plan(MatchState state, IReadOnlyList<BattleLossChoice> choices)
    {
        var loss = Current(state);
        var battle = BattleRuntime.Active(state);
        if (loss is null || battle is null)
            return null;
        var force = battle.Force(loss.Owner);
        if (!loss.Voluntary)
            return ArmyLossRules.Plan(state, new(force.Side, force.Region, force.Garrison, force.Participants), loss.Hits, choices, CasualtyCause.Battle);
        if (choices.Count == 0)
            return new(MovementPlan.CopyBoard(state), [.. force.Participants], 0);
        var units = BattleRules.Units(state, force).ToDictionary(u => u.Id);
        if (choices.Any(c => !units.ContainsKey(c.UnitId)))
            return null;
        var hits = choices.Sum(c => c.Kind == BattleLossKind.Remove && units[c.UnitId].Kind == UnitKind.Elite ? 2 : 1);
        return hits <= loss.Hits
            ? ArmyLossRules.Plan(state, new(force.Side, force.Region, force.Garrison, force.Participants), hits, choices, CasualtyCause.Battle)
            : null;
    }

    public static void Apply(MatchState state, ArmyLossPlan plan)
    {
        var loss = Current(state)!;
        var force = BattleRuntime.Active(state)!.Force(loss.Owner);
        ArmyLossRules.ApplyFigures(state, plan);
        force.Participants = plan.Participants;
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        state.Pending = null;
        if (loss.Voluntary)
            EffectRuntime.Current(state)!.Effect!.AppliedHits = plan.AppliedHits;
        var activity = HistoryFacts.Battle(state, BattleRuntime.Active(state)!);
        activity.Facts.SourceId = loss.SourceId;
        activity.Facts.LossDetails = [.. plan.LossFacts ?? []];
        activity.Facts.Hits = plan.AppliedHits;
        state.Events.Add(new() { Revision = state.Revision, Kind = "BattleLosses", Activity = activity,
            Text = $"{loss.Owner} resolved {(loss.Voluntary ? plan.AppliedHits : loss.Hits)} hit(s) from {loss.SourceId}." });
        ActiveEffects.CheckConditions(state);
    }
}
