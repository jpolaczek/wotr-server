using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record PositionedEvent(string Id, long Position, int Order, GameEvent Event)
{
    public string ActionId => Event.Activity?.Action.Id ?? Id;
}

public static class ActionLogProjection
{
    public static bool Matches(GameEvent item, Side viewer, ActionLogFilter filter) =>
        Visibility.CanRead(viewer, item.OnlyFor) && (filter == ActionLogFilter.All ||
            item.Activity?.Actor == (filter == ActionLogFilter.You ? viewer : TurnFlow.Other(viewer)));

    public static ActionLogView? Action(string matchId, IEnumerable<PositionedEvent> records, Side viewer, ActionLogFilter filter)
    {
        var visible = records.Where(r => Visibility.CanRead(viewer, r.Event.OnlyFor)).OrderBy(r => r.Position).ToList();
        var selected = visible.Where(r => Matches(r.Event, viewer, filter)).ToList();
        if (selected.Count == 0)
            return null;
        var ids = selected.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        return new()
        {
            MatchId = matchId,
            Action = Summary(visible, selected),
            Steps = selected.Select(r => Step(r, ids)).ToList()
        };
    }

    public static ActionLogStep Step(PositionedEvent record, IReadOnlySet<string> visibleParents)
    {
        var item = record.Event;
        var metadata = item.Activity;
        return new()
        {
            Id = record.Id,
            ActionId = record.ActionId,
            Revision = item.Revision,
            Order = record.Order,
            Kind = item.Kind,
            Text = item.Text,
            Legacy = metadata is null,
            Turn = metadata?.Turn,
            Actor = metadata?.Actor,
            Command = metadata?.Command,
            Automatic = metadata?.Automatic ?? false,
            ParentStepId = metadata is not null && visibleParents.Contains(metadata.ParentStepId) ? metadata.ParentStepId : "",
            Status = metadata?.Status ?? ActionLogStatus.Unknown,
            Context = CopyContext(metadata?.Context),
            Costs = CopyCosts(metadata?.Costs),
            Movements = metadata?.Movements.Select(CopyMovement).ToList() ?? [],
            Facts = metadata is null ? new() : HistoryFacts.Copy(metadata.Facts)
        };
    }

    private static ActionLogSummary Summary(List<PositionedEvent> visible, List<PositionedEvent> selected)
    {
        var root = visible[0].Event.Activity?.Action;
        return new()
        {
            Id = selected[0].ActionId,
            Version = selected[^1].Id,
            Initiator = root?.Initiator,
            Command = root?.Command,
            Turn = root?.Turn,
            PartialHistory = root?.PartialHistory ?? true,
            Legacy = root is null,
            Context = CopyContext(root?.Context),
            Costs = CopyCosts(root?.Costs),
            Status = visible[^1].Event.Activity?.Status ?? ActionLogStatus.Unknown,
            StepCount = selected.Count
        };
    }

    private static ActionContext CopyContext(ActionContext? context) => context is null ? new() : new()
    {
        Kind = context.Kind, Regions = [.. context.Regions], DefinitionId = context.DefinitionId,
        NationId = context.NationId, BattleId = context.BattleId, BattleRound = context.BattleRound,
        HuntChainId = context.HuntChainId
    };

    private static List<ActionCost> CopyCosts(List<ActionCost>? costs) => costs?.Select(cost => new ActionCost
    {
        Kind = cost.Kind, Id = cost.Id, Owner = cost.Owner, Source = cost.Source,
        Face = cost.Face, Use = cost.Use, AbilityId = cost.AbilityId, Count = cost.Count
    }).ToList() ?? [];

    private static ActionMovement CopyMovement(ActionMovement movement) => new()
    {
        Kind = movement.Kind, From = movement.From, To = movement.To, Route = [.. movement.Route],
        Participants = movement.Participants.Select(p => new ActionParticipant
        {
            Id = p.Id, Side = p.Side, NationId = p.NationId, UnitKind = p.UnitKind,
            DefinitionId = p.DefinitionId, Garrison = p.Garrison, Level = p.Level, Leadership = p.Leadership
        }).ToList()
    };
}
