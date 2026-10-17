using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowCharacterCards04;
using static CardPackScenario;

internal static class Shadow04Scenario
{
    private static readonly string[] Names = ["shadow-spells", "shadow-spells-loss", "shadow-grond", "shadow-grond-battle",
        "shadow-palantir-draw", "shadow-palantir-remove", "shadow-captain", "shadow-ringwraiths", "shadow-wormtongue"];

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        [Names[0]] = SpellsSource(random),
        [Names[1]] = PlayEvent(State(random), Spells, 4, 0),
        [Names[2]] = PlayEvent(State(random, true), Grond),
        [Names[3]] = GrondBattle(random),
        [Names[4]] = PalantirDrawChoice(random),
        [Names[5]] = PalantirRemove(random),
        [Names[6]] = PlayEvent(State(random), Captain),
        [Names[7]] = RingwraithsOrder(random),
        [Names[8]] = WormtongueAction(random)
    };

    private static MatchState State(IRandomSource random, bool siege = false)
    {
        var state = EmptyBoard(random);
        var region = siege ? "minas-tirith" : "osgiliath";
        for (var i = 0; i < 3; i++)
        {
            Unit(state, "sauron", UnitKind.Regular, region);
            Unit(state, "gondor", UnitKind.Regular, "minas-tirith");
        }
        foreach (var unit in state.Units.Where(u => u.Location == "minas-tirith" && u.Nation == "gondor"))
            unit.Garrison = siege;
        Unit(state, "sauron", UnitKind.Nazgul, region);
        Character(state, CharacterRules.WitchKing, region);
        Character(state, CharacterRules.Saruman, "orthanc");
        state.Control["minas-tirith"] = Side.FreePeoples;
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState SpellsSource(IRandomSource random)
    {
        var state = State(random);
        Unit(state, "sauron", UnitKind.Regular, "lossarnach");
        Unit(state, "sauron", UnitKind.Nazgul, "lossarnach");
        Unit(state, "gondor", UnitKind.Regular, "north-ithilien");
        return PlayEvent(state, Spells);
    }

    private static MatchState GrondBattle(IRandomSource random, bool palantir = false)
    {
        var state = State(random, true);
        if (palantir)
            Table(state, Palantir);
        Give(state, Grond);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Event;
        state = Execute(state, CommandKind.PlayEvent, new() { CardId = Grond, DieId = die.Id, CardCost = DieFace.Event });
        return Execute(state, CommandKind.AttackArmy, new()
        {
            Moves = [new() { From = "minas-tirith", To = "minas-tirith",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith" && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList(),
                CharacterIds = [CharacterRules.WitchKing] }]
        });
    }

    private static MatchState PalantirDrawChoice(IRandomSource random)
    {
        var state = GrondBattle(random, true);
        for (var round = 1; round <= 3; round++)
        {
            state = Execute(state, CommandKind.CommitCombatCard, new(), new int[6]);
            if (state.Pending?.Kind == DecisionKind.CombatCard)
                state = Execute(state, CommandKind.CommitCombatCard, new(), new int[6]);
            while (state.Pending?.Kind == DecisionKind.BattleReroll)
                state = Execute(state, CommandKind.RerollBattle, new());
        }
        return state;
    }

    private static MatchState PalantirRemove(IRandomSource random)
    {
        var state = State(random);
        Table(state, Palantir);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).ToList();
        dice[0].Face = DieFace.Army;
        dice[1].Face = DieFace.WillOfTheWest;
        return state;
    }

    private static MatchState RingwraithsOrder(IRandomSource random)
    {
        var state = State(random);
        Unit(state, "sauron", UnitKind.Regular, "south-ithilien");
        Unit(state, "sauron", UnitKind.Nazgul, "south-ithilien");
        state = PlayEvent(state, Ringwraiths);
        return Execute(state, CommandKind.MoveShadowCharacters, new());
    }

    private static MatchState WormtongueAction(IRandomSource random)
    {
        var state = State(random);
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        rohan.Active = false;
        rohan.StepsToWar = 1;
        Table(state, Wormtongue);
        Character(state, CharacterRules.Grey, "fords-of-isen");
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Face = DieFace.Character;
        return state;
    }

    private static void Table(MatchState state, string card)
    {
        Give(state, card);
        HandRules.Remove(state.Player(Side.Shadow), card);
        state.Player(Side.Shadow).Table.Add(card);
        state.ActiveEffects.Add(new()
        {
            Id = "table-" + card,
            SourceId = card,
            Owner = Side.Shadow,
            ExpiresWhen = EffectExpiry.CharacterLeavesPlay,
            TargetId = CharacterRules.Saruman,
            Modifier = card == Wormtongue ? EffectModifier.RestrictRohanActivation : EffectModifier.None
        });
    }

    public static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states) =>
        CardPackScenario.LayoutViews(states, Names);
}
