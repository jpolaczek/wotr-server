using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record EventLossProcedure(ArmyTarget Target, int Hits, string SourceId, List<int> Rolls, CasualtyCause Cause = CasualtyCause.DestructiveEvent);

public static class EventLosses
{
    public static EventLossProcedure? Current(MatchState state) => state.Resolution.Frames.LastOrDefault()?.EventLoss;

    public static void Begin(EffectContext context, ArmyTarget target, int hits, IReadOnlyList<int> rolls, CasualtyCause cause = CasualtyCause.DestructiveEvent)
    {
        if (hits <= 0)
            return;
        context.State.Resolution.Frames.Add(new() { EventLoss = new(target, hits, context.Execution.SourceId, [.. rolls], cause) });
        context.State.Open(DecisionKind.Losses, target.Owner, [CommandKind.TakeEventLosses], hits);
        context.State.Pending!.SourceId = context.Execution.SourceId;
    }

    public static ArmyLossPlan? Plan(MatchState state, IReadOnlyList<BattleLossChoice> choices) => Current(state) is { } loss
        ? ArmyLossRules.Plan(state, loss.Target, loss.Hits, choices, loss.Cause) : null;

    public static void Apply(MatchState state, ArmyLossPlan plan)
    {
        var loss = Current(state)!;
        ArmyLossRules.ApplyFigures(state, plan);
        state.Resolution.Frames.RemoveAt(state.Resolution.Frames.Count - 1);
        state.Pending = null;
        StrongholdRules.ReconcileOutsideBattle(state, [loss.Target.Region]);
        var activity = HistoryFacts.At(state, ActionContextKind.Army, loss.SourceId, loss.Target.Region);
        activity.Facts.LossDetails = [.. plan.LossFacts ?? []];
        activity.Facts.Hits = plan.AppliedHits;
        activity.Facts.Rolls = [.. loss.Rolls];
        state.Events.Add(new() { Revision = state.Revision, Kind = "EventLosses", Activity = activity,
            Text = $"{loss.Target.Owner} resolved {loss.Hits} hit(s) from {loss.SourceId}." });
    }

    public static void DescribePreview(MatchState state, IReadOnlyList<BattleLossChoice> choices, ActionPreview preview)
    {
        var plan = Plan(state, choices)!;
        StrongholdRules.ReconcileOutsideBattle(plan.Board, [Current(state)!.Target.Region]);
        BattlePreviewRules.DescribeLosses(state, plan, Current(state)!.Hits, false, preview);
        preview.ControlChanges = plan.Board.Control.Where(c => c.Value != state.Control.GetValueOrDefault(c.Key)).ToDictionary(c => c.Key, c => c.Value);
    }

    public static EventLossView? View(MatchState state) => Current(state) is { } loss ? new()
    {
        Owner = loss.Target.Owner,
        RegionId = loss.Target.Region,
        Garrison = loss.Target.Garrison,
        SourceId = loss.SourceId,
        Hits = loss.Hits,
        Rolls = [.. loss.Rolls],
        ParticipantIds = [.. loss.Target.Participants]
    } : null;
}
