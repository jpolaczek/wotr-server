using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class LordsDiceCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind == CommandKind.ChooseSpecialDie;

    public string? Validate(MatchState state, Side actor, GameCommand command) =>
        state.Configuration.Lords && state.Lords is not null && state.Phase == GamePhase.ActionRoll &&
        state.Pending?.Kind == DecisionKind.SpecialDieChoice && state.Lords.ChoiceOrder.Count > 0 && state.Lords.ChoiceOrder[0] == actor &&
        LordsDiceRoll.LegalChoices(state, actor).Contains(command.Payload.DieId) ? null : "IllegalChoice";

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        LordsDiceRoll.Select(state, actor, command.Payload.DieId);
        state.Lords!.ChoiceOrder.RemoveAt(0);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "SpecialDieChosen",
            Text = $"{actor} chose {command.Payload.DieId}."
        });
        if (!LordsDiceRoll.OpenNext(state))
            TurnFlow.CompleteSpecialDice(state);
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending is { Kind: DecisionKind.SpecialDieChoice } pending && pending.Owner == actor)
            yield return new()
            {
                Kind = CommandKind.ChooseSpecialDie,
                EligibleDice = LordsDiceRoll.LegalChoices(state, actor),
                Minimum = 1,
                Maximum = 1
            };
    }

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        preview.Costs.Clear();
        var die = state.Dice.Single(d => d.Id == command.Payload.DieId);
        preview.Summary = die.Face == DieFace.Eye
            ? "Add this Eye to the Hunt Box; set aside your other special dice."
            : "Keep this result available; set aside your other special dice.";
    }
}
