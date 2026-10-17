using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.FreePeoplesStrategyCards04;
using static CardPackScenario;

internal static class Strategy04Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["strategy-swords"] = PlayEvent(EmptyBoard(random), SwordsInEriador),
        ["strategy-eomer"] = EomerChoice(random),
        ["strategy-valour"] = Valour(random)
    };

    private static MatchState EomerChoice(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "rohan", UnitKind.Regular, "helms-deep");
        state.Units.Single(u => u.Location == "helms-deep").Garrison = true;
        Unit(state, "isengard", UnitKind.Regular, "helms-deep");
        return PlayEvent(state, Eomer);
    }

    private static MatchState Valour(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "elves", UnitKind.Elite, "minas-tirith");
        Unit(state, "gondor", UnitKind.Leader, "minas-tirith");
        Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        Unit(state, "sauron", UnitKind.Nazgul, "lossarnach");
        Give(state, DainIronfoot);
        state.Dice[0].Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            Moves = [new() { From = "minas-tirith", To = "lossarnach",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = DainIronfoot });
        return Execute(state, CommandKind.CommitCombatCard, new(), 0, 0);
    }
}
