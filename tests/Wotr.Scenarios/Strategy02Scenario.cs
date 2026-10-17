using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static CardPackScenario;

internal static class Strategy02Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["strategy-march"] = March(random),
        ["strategy-woses"] = Woses(random),
        ["strategy-relief"] = Relief(random),
        ["strategy-confusion"] = Confusion(random)
    };

    private static MatchState March(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "gondor", UnitKind.Regular, "north-ithilien");
        Character(state, "base.character.gimli", "north-ithilien");
        return PlayEvent(state, ThroughDayNight);
    }

    private static MatchState Woses(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "rohan", UnitKind.Regular, "helms-deep");
        Unit(state, "rohan", UnitKind.Leader, "helms-deep");
        Character(state, "base.character.legolas", "helms-deep");
        foreach (var unit in state.Units.Where(u => u.Location == "helms-deep"))
            unit.Garrison = true;
        state.Characters.Single().Garrison = true;
        Unit(state, "isengard", UnitKind.Regular, "helms-deep");
        return PlayEvent(state, PathsOfWoses);
    }

    private static MatchState Relief(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "rohan", UnitKind.Regular, "osgiliath");
        Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        Unit(state, "gondor", UnitKind.Elite, "minas-tirith");
        foreach (var unit in state.Units.Where(u => u.Location == "minas-tirith"))
            unit.Garrison = true;
        for (var i = 0; i < 8; i++)
            Unit(state, "sauron", UnitKind.Regular, "minas-tirith");
        return PlayEvent(state, HelpUnlookedFor);
    }

    private static MatchState Confusion(IRandomSource random)
    {
        var state = EmptyBoard(random);
        Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        Unit(state, "gondor", UnitKind.Leader, "minas-tirith");
        Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        Unit(state, "sauron", UnitKind.Nazgul, "lossarnach");
        Give(state, WisdomOfElrond);
        state.Dice[0].Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice[0].Id,
            Moves = [new() { From = "minas-tirith", To = "lossarnach", UnitIds = state.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = WisdomOfElrond });
        return Execute(state, CommandKind.CommitCombatCard, new(), 0, 0, 1);
    }
}
