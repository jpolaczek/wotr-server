using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class TurnOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor)
            yield break;
        switch (state.Pending.Kind)
        {
            case DecisionKind.ActionSequence:
                foreach (var option in ActionSequenceOptions.For(state, actor))
                    yield return option;
                break;
            case DecisionKind.FellowshipPhase:
                yield return new()
                {
                    Kind = CommandKind.ContinueFellowship
                };
                break;
            case DecisionKind.AllocateHunt:
                yield return new()
                {
                    Kind = CommandKind.AllocateHunt,
                    EligibleIds = DiceRules.HuntCandidates(state),
                    Minimum = DiceRules.HuntMinimum(state),
                    Maximum = DiceRules.HuntMaximum(state)
                };
                break;
            case DecisionKind.DiscardCards:
                yield return new()
                {
                    Kind = CommandKind.DiscardCards,
                    EligibleIds = [.. state.Player(actor).Hand],
                    Minimum = 1,
                    Maximum = state.Pending.RequiredCount
                };
                break;
            case DecisionKind.Action:
                foreach (var option in ActionOptions(state, actor))
                    yield return option;
                break;
        }
    }

    private static IEnumerable<ActionOption> ActionOptions(MatchState state, Side actor)
    {
        var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available).ToList();
        var costs = CardDrawCost.Options(state, actor);
        var drawing = costs.Select(c => c.DieId).Distinct().ToList();
        var decks = state.Player(actor).Decks.Where(d => d.Value.Count > 0).Select(d => d.Key).ToList();
        yield return new()
        {
            Kind = CommandKind.DrawCard,
            Enabled = drawing.Count > 0 && decks.Count > 0,
            EligibleDice = drawing,
            DrawCosts = costs,
            Targets = decks,
            Reason = decks.Count == 0 ? "Both decks are empty." : drawing.Count == 0 ? DrawReason(state.Configuration.Lords) : ""
        };
        yield return new()
        {
            Kind = CommandKind.DiscardDie,
            EligibleDice = dice.Select(d => d.Id).ToList()
        };
        yield return new()
        {
            Kind = CommandKind.Pass,
            Enabled = TurnCommands.CanPass(state, actor),
            Reason = "Requires fewer unused dice than your opponent."
        };
        var rings = state.Rings.Where(r => r.Owner == actor).Select(r => r.Id).ToList();
        var ringEnabled = rings.Count > 0 && RingRules.WithinLimit(state, actor);
        yield return new()
        {
            Kind = CommandKind.UseRing,
            Enabled = ringEnabled,
            EligibleIds = rings,
            EligibleDice = dice.Where(d => RingRules.CanChange(d, actor)).Select(d => d.Id).ToList(),
            Faces = [.. RingRules.Faces(actor)],
            Reason = !KingsRulerTokens.RingAllowed(state, actor) ? "A Ruler token was used this turn."
                : state.Player(actor).RingUsedThisTurn ? "One Ring per player each turn." : "No Ring available."
        };
    }

    private static string DrawReason(bool lords) => lords
        ? "Requires an Event, Card Draw or Will of the West die."
        : "Requires an Event or Will of the West die.";
}
