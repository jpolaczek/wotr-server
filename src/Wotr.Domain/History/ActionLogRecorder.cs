using Wotr.Contracts;

namespace Wotr.Domain;

internal static class ActionLogRecorder
{
    public static void Setup(MatchState state)
    {
        AssignIdentities(state, 0);
        foreach (var item in state.Events)
        {
            var data = item.Activity ??= HistoryFacts.At(state, ActionContextKind.General);
            data.Action = new() { Id = item.HistoryId!, Turn = state.Turn, Context = data.Context };
            data.Status = ActionLogStatus.Completed;
            data.Automatic = true;
        }
    }

    public static void Record(MatchState before, MatchState after, Side actor, GameCommand command)
    {
        ActionCommandCatalog.Context(command.Kind);
        var first = before.Events.Count;
        BattleReportHistory.PreserveLegacy(before, after);
        var continuation = before.Resolution.ActionLog;
        var primary = ActionDecisionFacts.Ensure(before, after, actor, command, first);
        AssignIdentities(after, first);
        var context = ActionDecisionFacts.Context(before, after, command);
        var costs = Costs(before, after, continuation, actor);
        var root = continuation?.Action ?? Root(before, actor, command, context, costs);
        var status = Ongoing(after) ? ActionLogStatus.InProgress : ActionLogStatus.Completed;
        var parent = continuation?.LastStepId ?? "";
        if (command.Kind == CommandKind.MoveArmies)
            ActionDecisionFacts.Movements(before, after, command);
        for (var i = first; i < after.Events.Count; i++)
        {
            var item = after.Events[i];
            var hasOwnContext = item.Activity is not null;
            var data = item.Activity ??= HistoryFacts.At(after, context.Kind);
            if (item.Kind == "LegacyBattleRound")
                continue;
            data.Action = root;
            data.ParentStepId = parent;
            data.Status = status;
            if (i == primary)
                Decision(before, after, data, actor, command, context, costs);
            else if (!hasOwnContext)
                data.Context = context;
            if (item.OnlyFor is null)
                parent = item.HistoryId!;
        }
        after.Resolution.ActionLog = status == ActionLogStatus.InProgress
            ? new() { Action = root, LastStepId = parent } : null;
    }

    private static void Decision(MatchState before, MatchState after, ActionEvent data, Side actor,
        GameCommand command, ActionContext context, List<ActionCost> costs)
    {
        data.Actor = actor;
        data.Command = command.Kind == CommandKind.SetCombatCardPolicy ? CommandKind.CommitCombatCard : command.Kind;
        data.Automatic = command.Kind == CommandKind.SetCombatCardPolicy;
        data.Turn = before.Turn;
        if (data.Movements.Count == 0)
            data.Context = context;
        data.Costs = costs;
        if (data.Facts.SourceId.Length == 0)
            data.Facts.SourceId = context.DefinitionId;
        ActionBoardChanges.Capture(before, after, data.Facts);
        if (ActionCommandCatalog.PrivateSelection(command.Kind))
            data.Facts.Targets = command.Kind == CommandKind.DrawCorruptionTile
                ? [.. CorruptionRuntime.Current(after)?.Drawn ?? []] : [.. command.Payload.Ids];
        if (command.Kind is CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses)
            data.Facts.Losses = command.Payload.Losses.Select(l => new BattleLossChoice { UnitId = l.UnitId, Kind = l.Kind }).ToList();
        if (command.Kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship)
            data.Facts.FellowshipRoute = [.. command.Payload.Route];
        if (BattleRuntime.Active(after) is { } battle && data.Facts.Battle is null)
            data.Facts.Battle = BattleProjection.Public(after, battle);
    }

    private static List<ActionCost> Costs(MatchState before, MatchState after, ActionLogContinuation? continuation, Side actor)
    {
        after.HistoryCapture?.Complete(after);
        var costs = after.HistoryCapture?.Costs ?? [];
        return costs.Count == 0 && continuation is null && !Ongoing(before)
            ? [new() { Kind = ActionCostKind.Free, Owner = actor }] : costs;
    }

    private static ActionIdentity Root(MatchState before, Side actor, GameCommand command,
        ActionContext context, List<ActionCost> costs) => new()
    {
        Id = EventIdentity.ForRevision(before.Id, before.Revision + 1, "action", 0),
        Initiator = ActionCommandCatalog.StartsAction(before) ? actor : before.Resolution.ActionSequence?.Owner,
        Command = before.Resolution.ActionSequence?.Kind ?? (command.Kind == CommandKind.SetCombatCardPolicy ? CommandKind.CommitCombatCard : command.Kind),
        Turn = before.Turn,
        PartialHistory = !ActionCommandCatalog.StartsAction(before) && Ongoing(before),
        Context = context,
        Costs = costs
    };

    private static bool Ongoing(MatchState state) => state.Winner is null &&
        (state.Resolution.ActionSequence is not null || state.Resolution.PaidDieIds.Count > 0 || state.Resolution.Frames.Count > 0);

    private static void AssignIdentities(MatchState state, int first)
    {
        var ordinals = new Dictionary<string, int>();
        for (var i = first; i < state.Events.Count; i++)
        {
            var item = state.Events[i];
            var scope = item.OnlyFor?.ToString() ?? "public";
            var ordinal = ordinals.GetValueOrDefault(scope);
            item.HistoryId = EventIdentity.ForRevision(state.Id, item.Revision, scope, ordinal);
            ordinals[scope] = ordinal + 1;
        }
    }
}
