using Wotr.Contracts;
using Wotr.Domain;

internal static class BattleResolutionScenario
{
    internal static MatchState Rerolls(IRandomSource random)
    {
        var state = BattleScenario.Field(random);
        state.Units.First(u => u.Kind == UnitKind.Nazgul && u.Location == "reserve").Location = "lossarnach";
        state = Begin(state);
        return NoCards(state, 0, 1, 5, 2, 3, 5);
    }

    internal static MatchState OrderedLosses(IRandomSource random)
    {
        var state = BattleScenario.Field(random);
        foreach (var leader in state.Units.Where(u => u.Kind == UnitKind.Leader && u.Location == "minas-tirith"))
            BoardRules.ReturnToReserve(leader);
        foreach (var regular in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve"))
            regular.Location = "pelargir";
        return NoCards(Begin(state), 0, 0, 0, 5, 5, 0);
    }

    private static MatchState Begin(MatchState state) => ExpansionUiFixture.Execute(state, CommandKind.AttackArmy, new()
    {
        DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id,
        MovementCost = DieFace.Army,
        Moves = [new()
        {
            From = "minas-tirith", To = "lossarnach",
            UnitIds = state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.Owner(u) == Side.FreePeoples).Select(u => u.Id).ToList()
        }]
    });

    private static MatchState NoCards(MatchState state, params int[] rolls)
    {
        state = ExpansionUiFixture.Execute(state, CommandKind.CommitCombatCard);
        return ExpansionUiFixture.Execute(state, CommandKind.CommitCombatCard, null, rolls);
    }
}
