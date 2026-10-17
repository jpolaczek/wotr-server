using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards02;
using static CardPackScenario;

internal static class ShadowStrategy02Scenario
{
    private static readonly string[] Names = ["sh-str02-gather", "sh-str02-lengthens", "sh-str02-moving", "sh-str02-rage-region",
        "sh-str02-rage-move", "sh-str02-return-excess", "sh-str02-corsairs-landing", "sh-str02-corsairs-defense",
        "sh-str02-corsairs-relief", "sh-str02-corsairs-excess", "sh-str02-corsairs-forced", "sh-str02-mumakil", "sh-str02-swarm",
        "sh-str02-corsairs-relief-excess"];

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        [Names[0]] = March(random, Gather),
        [Names[1]] = March(random, Lengthens),
        [Names[2]] = March(random, Moving),
        [Names[3]] = RageRegion(random),
        [Names[4]] = Execute(RageRegion(random), CommandKind.ChooseEffect, new() { Ids = ["hollin"] }),
        [Names[5]] = ReturnExcess(random),
        [Names[6]] = PlayEvent(CorsairArmy(random), Corsairs),
        [Names[7]] = Land(CorsairArmy(random, defenders: 2, target: "dol-amroth"), "dol-amroth"),
        [Names[8]] = Relief(random),
        [Names[9]] = SiegeExcess(random),
        [Names[10]] = BlankRound(Land(CorsairArmy(random, defenders: 2), "pelargir")),
        [Names[11]] = Combat(random, Gather),
        [Names[12]] = Combat(random, Moving),
        [Names[13]] = Execute(Relief(random), CommandKind.RetreatBattle, new() { RegionId = "erech" })
    };

    private static MatchState Shadow(IRandomSource random)
    {
        var state = EmptyBoard(random);
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState March(IRandomSource random, string card)
    {
        var state = Shadow(random);
        Unit(state, "sauron", UnitKind.Regular, "moria");
        Unit(state, "sauron", UnitKind.Elite, "moria");
        Unit(state, "sauron", UnitKind.Regular, "south-dunland");
        if (card == Moving)
        {
            Unit(state, "isengard", UnitKind.Regular, "hollin");
            Unit(state, "southrons", UnitKind.Regular, "north-dunland");
        }
        state.Control["north-dunland"] = Side.FreePeoples;
        return PlayEvent(state, card);
    }

    private static MatchState RageRegion(IRandomSource random)
    {
        var state = Shadow(random);
        foreach (var region in new[] { "north-dunland", "south-dunland" })
            foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite })
                Unit(state, "isengard", kind, region);
        return PlayEvent(state, Rage);
    }

    private static MatchState ReturnExcess(IRandomSource random)
    {
        var state = Shadow(random);
        Character(state, CharacterRules.WitchKing, "osgiliath");
        for (var i = 0; i < 9; i++)
            Unit(state, "sauron", UnitKind.Regular, "angmar");
        return PlayEvent(state, Return);
    }

    private static MatchState CorsairArmy(IRandomSource random, int arrivals = 2, int defenders = 0, string target = "pelargir")
    {
        var state = Shadow(random);
        for (var i = 0; i < arrivals; i++)
            Unit(state, "southrons", UnitKind.Regular, "umbar");
        for (var i = 0; i < defenders; i++)
            Unit(state, "gondor", UnitKind.Regular, target);
        return state;
    }

    private static MatchState Land(MatchState state, string target)
    {
        state = PlayEvent(state, Corsairs);
        return Execute(state, CommandKind.MoveArmies, new()
        {
            Moves = [new() { From = "umbar", To = target,
                UnitIds = state.Units.Where(u => u.Location == "umbar" && !u.Garrison).Select(u => u.Id).ToList() }]
        });
    }

    private static MatchState BlankRound(MatchState state)
    {
        state = Execute(state, CommandKind.CommitCombatCard, new());
        return Execute(state, CommandKind.CommitCombatCard, new(), new int[10]);
    }

    private static MatchState Relief(IRandomSource random)
    {
        var state = CorsairArmy(random, arrivals: 7, defenders: 1, target: "dol-amroth");
        state.Control["dol-amroth"] = Side.Shadow;
        for (var i = 0; i < 5; i++)
            Unit(state, "sauron", UnitKind.Regular, "dol-amroth");
        foreach (var unit in state.Units.Where(u => u.Location == "dol-amroth" && u.Nation == "sauron"))
            unit.Garrison = true;
        return BlankRound(Land(state, "dol-amroth"));
    }

    private static MatchState SiegeExcess(IRandomSource random)
    {
        var state = CorsairArmy(random, arrivals: 8, defenders: 3, target: "dol-amroth");
        foreach (var unit in state.Units.Where(u => u.Location == "dol-amroth"))
            unit.Garrison = true;
        for (var i = 0; i < 4; i++)
            Unit(state, "sauron", UnitKind.Regular, "dol-amroth");
        return Land(state, "dol-amroth");
    }

    private static MatchState Combat(IRandomSource random, string card)
    {
        var state = Shadow(random);
        Unit(state, "southrons", UnitKind.Elite, "osgiliath");
        for (var i = 0; i < 2; i++)
            Unit(state, "sauron", UnitKind.Regular, "osgiliath");
        if (card == Moving)
            Unit(state, "sauron", UnitKind.Nazgul, "osgiliath");
        for (var i = 0; i < 3; i++)
            Unit(state, "gondor", UnitKind.Regular, "north-ithilien");
        Give(state, card);
        var opponent = FreePeoplesStrategyCards01.LastBattle;
        if (card == Moving)
            Give(state, opponent);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            Moves = [new() { From = "osgiliath", To = "north-ithilien",
                UnitIds = state.Units.Where(u => u.Location == "osgiliath").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = card });
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = card == Moving ? opponent : "" },
            card == Moving ? new int[6] : [3, 0, 0, 0, 0, 0]);
    }

    public static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states) => CardPackScenario.LayoutViews(states, Names);
}
