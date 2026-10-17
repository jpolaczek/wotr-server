using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards01;
using static CardPackScenario;

internal static class ShadowStrategy01Scenario
{
    private static readonly string[] Names = ["sh-str01-valinor", "sh-str01-valinor-loss", "sh-str01-uruk-hai",
        "sh-str01-uruk-battle", "sh-str01-denethor-leader", "sh-str01-denethor-remove", "sh-str01-denethor-battle",
        "sh-str01-stormcrow", "sh-str01-relentless", "sh-str01-onslaught", "sh-str01-great-host", "sh-str01-stormcrow-dense"];

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        [Names[0]] = ValinorRegions(random),
        [Names[1]] = Execute(ValinorRegions(random), CommandKind.ChooseEffect, new() { Ids = ["rivendell"] }, 5, 0, 0),
        [Names[2]] = PlayEvent(Siege(random), UrukHai),
        [Names[3]] = UrukBattle(random),
        [Names[4]] = DenethorLeader(random),
        [Names[5]] = DenethorRemove(random),
        [Names[6]] = DenethorBattle(random),
        [Names[7]] = StormcrowFigure(random),
        [Names[8]] = Sacrifice(random, Dawn),
        [Names[9]] = Sacrifice(random, UrukHai),
        [Names[10]] = GreatHostLoss(random),
        [Names[11]] = StormcrowFigure(random, dense: true)
    };

    private static MatchState ValinorRegions(IRandomSource random)
    {
        var state = EmptyBoard(random);
        state.Control["grey-havens"] = Side.Shadow;
        foreach (var region in new[] { "rivendell", "lorien" })
        {
            Unit(state, "elves", UnitKind.Regular, region);
            Unit(state, "elves", UnitKind.Regular, region);
            Unit(state, "elves", UnitKind.Elite, region);
            Unit(state, "north", UnitKind.Regular, region);
        }
        TurnFlow.SelectNext(state, Side.Shadow);
        return PlayEvent(state, Valinor);
    }

    private static MatchState Siege(IRandomSource random)
    {
        var state = EmptyBoard(random);
        for (var i = 0; i < 3; i++)
        {
            Unit(state, "isengard", UnitKind.Regular, DenethorRules.Region);
            Unit(state, "gondor", UnitKind.Regular, DenethorRules.Region);
        }
        foreach (var unit in state.Units.Where(u => u.Location == DenethorRules.Region && u.Nation == "gondor"))
            unit.Garrison = true;
        state.Control[DenethorRules.Region] = Side.FreePeoples;
        Character(state, CharacterRules.Saruman, "orthanc");
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState UrukBattle(IRandomSource random) => Attack(PlayEvent(Siege(random), UrukHai), DenethorRules.Region);

    private static MatchState DenethorLeader(IRandomSource random)
    {
        var state = Siege(random);
        foreach (var nation in new[] { "gondor", "rohan" })
        {
            Unit(state, nation, UnitKind.Leader, DenethorRules.Region);
            state.Units.Single(u => u.Location == DenethorRules.Region && u.Nation == nation && u.Kind == UnitKind.Leader).Garrison = true;
        }
        return PlayEvent(state, Denethor);
    }

    private static MatchState DenethorRemove(IRandomSource random)
    {
        var state = PlayEvent(Siege(random), Denethor);
        Character(state, CharacterRules.Grey, DenethorRules.Region);
        state.Fellowship.Guide = CharacterRules.Strider;
        state.Characters.Single(c => c.Id == CharacterRules.Grey).Garrison = true;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).ToList();
        dice[0].Face = DieFace.Army;
        dice[1].Face = DieFace.WillOfTheWest;
        return state;
    }

    private static MatchState DenethorBattle(IRandomSource random)
    {
        var state = DenethorRemove(random);
        TurnFlow.SelectNext(state, Side.Shadow);
        return Attack(state, DenethorRules.Region, paid: true);
    }

    private static MatchState StormcrowFigure(IRandomSource random, bool dense = false)
    {
        var state = EmptyBoard(random);
        Character(state, CharacterRules.Grey, "edoras");
        state.Fellowship.Guide = CharacterRules.Strider;
        var nation = state.Nations.Single(n => n.Id == "rohan");
        nation.StepsToWar = 1;
        AddRohan(state, dense);
        TurnFlow.SelectNext(state, Side.Shadow);
        Give(state, Stormcrow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Muster;
        return Execute(state, CommandKind.PlayEvent, new() { CardId = Stormcrow, DieId = die.Id, CardCost = die.Face, NationId = "rohan" });
    }

    private static void AddRohan(MatchState state, bool dense)
    {
        if (!dense)
        {
            foreach (var region in new[] { "edoras", "helms-deep", "fords-of-isen" })
                foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite, UnitKind.Leader })
                    Unit(state, "rohan", kind, region);
            return;
        }
        for (var i = 0; i < 5; i++)
        {
            Unit(state, "rohan", UnitKind.Regular, "helms-deep");
            Unit(state, "rohan", UnitKind.Elite, "helms-deep");
            Unit(state, "rohan", UnitKind.Regular, "edoras");
        }
        for (var i = 0; i < 4; i++)
            Unit(state, "rohan", UnitKind.Leader, "helms-deep");
    }

    private static MatchState Field(IRandomSource random, int shadowUnits = 2, int freeUnits = 3)
    {
        var state = EmptyBoard(random);
        for (var i = 0; i < shadowUnits; i++)
            Unit(state, "sauron", UnitKind.Regular, "osgiliath");
        for (var i = 0; i < freeUnits; i++)
            Unit(state, "gondor", UnitKind.Regular, "north-ithilien");
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState Sacrifice(IRandomSource random, string card)
    {
        var state = Field(random);
        Unit(state, "sauron", UnitKind.Elite, "osgiliath");
        Give(state, card);
        state = Attack(state, "osgiliath", "north-ithilien", paid: true);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = card });
        return Execute(state, CommandKind.CommitCombatCard, new(), new int[7]);
    }

    private static MatchState GreatHostLoss(IRandomSource random)
    {
        var state = Field(random, 6, 2);
        Give(state, Stormcrow);
        state = Attack(state, "osgiliath", "north-ithilien", paid: true);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = Stormcrow });
        return Execute(state, CommandKind.CommitCombatCard, new(), new int[7]);
    }

    private static MatchState Attack(MatchState state, string from, string? to = null, bool paid = false)
    {
        var payload = new CommandPayload
        {
            Moves = [new() { From = from, To = to ?? from, UnitIds = state.Units
                .Where(u => u.Location == from && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList() }]
        };
        if (paid)
        {
            var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
            die.Face = DieFace.Army;
            payload.DieId = die.Id;
            payload.MovementCost = DieFace.Army;
        }
        return Execute(state, CommandKind.AttackArmy, payload);
    }

    public static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states) =>
        CardPackScenario.LayoutViews(states, Names);
}
