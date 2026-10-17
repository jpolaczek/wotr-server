using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.FreePeoplesStrategyCards03;
using static CardPackScenario;

internal static class Strategy03Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["strategy-cirdan"] = Cirdan(random),
        ["strategy-riders"] = Riders(random),
        ["strategy-charge"] = Charge(random)
    };

    private static MatchState Cirdan(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "gondor", UnitKind.Regular, "grey-havens");
        Unit(state, "north", UnitKind.Regular, "dol-amroth");
        return PlayEvent(state, CirdansShips);
    }

    private static MatchState Riders(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Character(state, "base.character.gimli", "eastemnet");
        return PlayEvent(state, RidersOfTheoden);
    }

    private static MatchState Charge(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "elves", UnitKind.Elite, "minas-tirith");
        Unit(state, "elves", UnitKind.Elite, "minas-tirith");
        Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        Give(state, CirdansShips);
        state.Dice[0].Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            Moves = [new() { From = "minas-tirith", To = "lossarnach",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = CirdansShips });
        return Execute(state, CommandKind.CommitCombatCard, new(), 5, 0);
    }
}
