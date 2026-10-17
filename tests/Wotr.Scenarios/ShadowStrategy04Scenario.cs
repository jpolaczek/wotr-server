using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards04;
using static CardPackScenario;

internal static class ShadowStrategy04Scenario
{
    private static readonly string[] Names = ["sh-str04-misty", "sh-str04-misty-siege", "sh-str04-orcs-shortage", "sh-str04-horde",
        "sh-str04-monsters", "sh-str04-musterings-excess", "sh-str04-pits", "sh-str04-pits-shortage", "sh-str04-desperate", "sh-str04-onslaught", "sh-str04-misty-excess"];

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        [Names[0]] = Recruit(random, MistyMountains),
        [Names[1]] = MistySiege(random),
        [Names[2]] = Recruit(random, OrcsMultiplying, 2),
        [Names[3]] = Recruit(random, Horde),
        [Names[4]] = Recruit(random, Monsters, 1),
        [Names[5]] = MusteringsExcess(random),
        [Names[6]] = Recruit(random, Pits),
        [Names[7]] = Recruit(random, Pits, 5),
        [Names[8]] = Combat(random, Monsters, true),
        [Names[9]] = Combat(random, MistyMountains, false),
        [Names[10]] = MistyExcess(random)
    };

    private static MatchState Before(IRandomSource random)
    {
        var state = EmptyBoard(random);
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Recruit(IRandomSource random, string card, int reserve = -1)
    {
        var state = Before(random);
        if (reserve >= 0)
            foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Skip(reserve))
                unit.Location = "casualties";
        return PlayEvent(state, card);
    }

    private static MatchState MistySiege(IRandomSource random)
    {
        var state = Before(random);
        for (var i = 0; i < 5; i++)
            Unit(state, "southrons", UnitKind.Regular, "moria");
        foreach (var unit in state.Units.Where(u => u.Location == "moria"))
            unit.Garrison = true;
        Unit(state, "gondor", UnitKind.Regular, "moria");
        state.Control["moria"] = Side.Shadow;
        return PlayEvent(state, MistyMountains);
    }

    private static MatchState MusteringsExcess(IRandomSource random)
    {
        var state = Before(random);
        for (var i = 0; i < 8; i++)
        {
            Unit(state, "southrons", UnitKind.Regular, "nurn");
            Unit(state, "sauron", UnitKind.Regular, "gorgoroth");
        }
        return PlayEvent(state, Musterings);
    }

    private static MatchState MistyExcess(IRandomSource random)
    {
        var state = MistySiege(random);
        var option = new EffectOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.ChooseEffect);
        return Execute(state, CommandKind.ChooseEffect, new()
        {
            Placements = option.Recruitment.Where(g => g.Kind is UnitKind.Elite or UnitKind.Nazgul)
                .SelectMany(g => g.ReserveIds.Take(g.Kind == UnitKind.Elite ? 2 : 1))
                .Select(id => new UnitPlacementChoice { UnitId = id, RegionId = "moria" }).ToList()
        });
    }

    private static MatchState Combat(IRandomSource random, string card, bool leaders)
    {
        var state = Before(random);
        for (var i = 0; i < 3; i++)
        {
            Unit(state, "sauron", UnitKind.Regular, "osgiliath");
            Unit(state, "gondor", UnitKind.Regular, "north-ithilien");
        }
        if (leaders)
        {
            Unit(state, "sauron", UnitKind.Nazgul, "osgiliath");
            Unit(state, "gondor", UnitKind.Leader, "north-ithilien");
        }
        Give(state, card);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = "osgiliath", To = "north-ithilien",
                UnitIds = state.Units.Where(u => u.Location == "osgiliath").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = card });
        return Execute(state, CommandKind.CommitCombatCard, new(), new int[6]);
    }

    public static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states) =>
        CardPackScenario.LayoutViews(states, Names);
}
