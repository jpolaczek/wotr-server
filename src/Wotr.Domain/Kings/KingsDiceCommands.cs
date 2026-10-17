using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class KingsDiceCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind == CommandKind.DiscardRulerDie;

    public string? Validate(MatchState state, Side actor, GameCommand command) =>
        state.Configuration.Kings && state.Kings is { RulerDiscardOrder.Count: > 0 } && state.Phase == GamePhase.ActionRoll &&
        state.Pending?.Kind == DecisionKind.RulerDiscard && state.Kings.RulerDiscardOrder[0] == actor &&
        KingsDice.DiscardCandidates(state, actor).Contains(command.Payload.DieId) ? null : "IllegalChoice";

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        KingsDice.Discard(state, actor, command.Payload.DieId);
        if (!KingsDice.OpenNext(state))
            TurnFlow.BeginActions(state);
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending is { Kind: DecisionKind.RulerDiscard } pending && pending.Owner == actor)
            yield return new() { Kind = CommandKind.DiscardRulerDie, EligibleDice = KingsDice.DiscardCandidates(state, actor), Minimum = 1, Maximum = 1 };
    }

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        preview.Summary = "Set this die aside until the next Recover phase.";
        preview.RemovedIds.Add(command.Payload.DieId);
    }
}
