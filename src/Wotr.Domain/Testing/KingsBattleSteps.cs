using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-13: the steps of a battle a Kings scenario plays through the engine before its first decision: the attack as the
// side's legal option lists it, the Combat cards, and rounds that roll only misses (ExpansionBoard's fixed source).
internal static class KingsBattleSteps
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    // The side attacks with its whole army in the region (figures and the characters with them), paid with an Army die.
    public static MatchState Attack(MatchState state, Side side, string from, string to)
    {
        var army = Views.For(state, side).Options.First(o => o.Kind == CommandKind.AttackArmy && o.Enabled).Armies
            .Single(a => a.RegionId == from && !a.Garrison);
        var die = state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        return ExpansionBoard.Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to, UnitIds = [.. army.UnitIds], CharacterIds = [.. army.CharacterIds] }]
        });
    }

    // Each side commits its named Combat card ("" for none) while the battle waits on a Combat card choice.
    public static MatchState Commit(MatchState state, string freePeoples, string shadow)
    {
        while (state.Pending?.Kind == DecisionKind.CombatCard)
            state = ExpansionBoard.Execute(state, CommandKind.CommitCombatCard,
                new() { CardId = state.Pending.Owner == Side.FreePeoples ? freePeoples : shadow });
        return state;
    }

    // The side plays no Combat card, so the other side's choice follows.
    public static MatchState NoCard(MatchState state, Side side)
    {
        if (state.Pending is not { Kind: DecisionKind.CombatCard } pending || pending.Owner != side)
            throw new InvalidOperationException($"{side} must be at its Combat card choice.");
        return ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = "" });
    }

    // The battle goes on without cards or re-rolls, and every die misses, until it waits on the named decision.
    public static MatchState RunTo(MatchState state, DecisionKind kind)
    {
        for (var step = 0; state.Pending?.Kind != kind; step++)
        {
            if (step > 12)
                throw new InvalidOperationException($"The battle did not reach {kind}.");
            state = state.Pending?.Kind switch
            {
                DecisionKind.CombatCard => ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = "" }),
                DecisionKind.BattleReroll => ExpansionBoard.Execute(state, CommandKind.RerollBattle),
                _ => throw new InvalidOperationException($"Unexpected battle decision {state.Pending?.Kind}.")
            };
        }
        return state;
    }

    public static MatchState Expect(MatchState state, Side owner, string presentation)
    {
        if (state.Pending?.Owner != owner || state.Pending.Presentation != presentation)
            throw new InvalidOperationException($"The scenario must wait for {owner}'s {presentation}, not {state.Pending?.Kind} " +
                $"{state.Pending?.Presentation} of {state.Pending?.Owner}.");
        return state;
    }
}
