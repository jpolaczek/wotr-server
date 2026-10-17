using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed class PreviewService(GameEngine engine)
{
    public ActionPreview Preview(MatchState state, Side viewer, GameCommand command)
    {
        var error = engine.Validate(state, viewer, command);
        var preview = new ActionPreview
        {
            Revision = state.Revision,
            Legal = error is null,
            Error = Visibility.SafeError(error),
            Summary = error is null ? command.Kind.ToString() : "",
            Costs = error is null ? new[] { command.Payload.DieId, command.Payload.RingId, command.Payload.RulerTokenId,
                command.Payload.CouncilTokenId }.Where(id => !string.IsNullOrEmpty(id)).ToList() : [],
            HasUnknownResult = error is null && command.Kind is CommandKind.DrawCard or CommandKind.AllocateHunt or CommandKind.PlayEvent or CommandKind.RespondEffect,
            RemovedIds = error is null && command.Kind == CommandKind.Recruit ? UnaccompaniedLeaders(state, viewer, command.Payload.Placements) : []
        };
        if (error is null && command.Kind == CommandKind.MoveArmies)
            MovementRules.Plan(state, viewer, command.Payload)!.Describe(state, preview);
        if (error is null && command.Kind == CommandKind.FinishActionSequence)
            ActionSequencePreview.Finish(state, viewer, preview);
        if (error is null)
        {
            state = KingsRulerTokens.ValidationState(state, command.Payload);
            if (command.Payload.Stepwise && state.Resolution.ActionSequence is not null)
                preview.Costs.Clear();
            CharacterPreview.Describe(state, viewer, command, preview);
            FellowshipPreviewRules.Describe(state, command, preview);
            HuntPreviewRules.Describe(state, command, preview);
            BattlePreviewRules.Describe(state, command, preview);
            engine.DescribePreview(state, command, preview);
        }
        return preview;
    }

    private static List<string> UnaccompaniedLeaders(MatchState state, Side actor, IReadOnlyList<UnitPlacementChoice> placements) =>
        placements.Where(p => state.Units.Single(u => u.Id == p.UnitId).Kind == UnitKind.Leader &&
            !state.Units.Any(u => u.Location == p.RegionId && BoardRules.Owner(u) == actor && BoardRules.IsArmyUnit(u)))
            .Select(p => p.UnitId).ToList();
}
