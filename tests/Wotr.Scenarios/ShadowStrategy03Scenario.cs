using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.ShadowStrategyCards03;
using static CardPackScenario;

internal static class ShadowStrategy03Scenario
{
    private static readonly string[] Names = ["sh-str03-half-orcs-region", "sh-str03-olog-reserve", "sh-str03-hill-trolls",
        "sh-str03-new-power", "sh-str03-new-power-shortage", "sh-str03-many-kings", "sh-str03-king-excess", "sh-str03-we-come-loss",
        "sh-str03-new-power-excess"];

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        [Names[0]] = ArmyRecruit(random, HalfOrcs),
        [Names[1]] = Execute(ArmyRecruit(random, OlogHai), CommandKind.ChooseEffect, new() { Ids = ["westemnet"] }),
        [Names[2]] = Trolls(random),
        [Names[3]] = Power(random),
        [Names[4]] = Power(random, shortage: true),
        [Names[5]] = Kings(random),
        [Names[6]] = KingExcess(random),
        [Names[7]] = SpecialLoss(random),
        [Names[8]] = PowerExcess(random)
    };

    private static MatchState Shadow(IRandomSource random)
    {
        var state = EmptyBoard(random);
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private static MatchState ArmyRecruit(IRandomSource random, string card)
    {
        var state = Shadow(random);
        state.Control["westemnet"] = Side.Shadow;
        Unit(state, "southrons", UnitKind.Regular, "westemnet");
        Unit(state, "sauron", UnitKind.Regular, "eastemnet");
        return PlayEvent(state, card);
    }

    private static MatchState Trolls(IRandomSource random)
    {
        var state = Shadow(random);
        foreach (var region in new[] { "moria", "orthanc", "gorgoroth" })
            Unit(state, "sauron", UnitKind.Regular, region);
        return PlayEvent(state, HillTrolls);
    }

    private static MatchState Power(IRandomSource random, bool shortage = false)
    {
        var state = Shadow(random);
        Character(state, CharacterRules.Saruman, "orthanc");
        if (shortage)
        {
            foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite })
                foreach (var unit in state.Units.Where(u => u.Nation == "isengard" && u.Kind == kind && u.Location == "reserve").Skip(2))
                    unit.Location = "casualties";
        }
        return PlayEvent(state, NewPower);
    }

    private static MatchState Kings(IRandomSource random)
    {
        var state = Shadow(random);
        state.Nations.Single(n => n.Id == "southrons").StepsToWar = 2;
        return PlayEvent(state, ManyKings);
    }

    private static MatchState PowerExcess(IRandomSource random)
    {
        var state = Shadow(random);
        Character(state, CharacterRules.Saruman, "orthanc");
        for (var index = 0; index < 9; index++)
            Unit(state, "sauron", UnitKind.Regular, "orthanc");
        state = PlayEvent(state, NewPower);
        var option = new EffectOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.ChooseEffect);
        var regulars = new Queue<string>(option.Recruitment.Single(g => g.Kind == UnitKind.Regular).ReserveIds);
        var plan = new CommandPayload();
        foreach (var region in new[] { "north-dunland", "south-dunland" })
            for (var index = 0; index < 2; index++)
                plan.Placements.Add(new() { UnitId = regulars.Dequeue(), RegionId = region });
        plan.Placements.AddRange(option.Recruitment.Single(g => g.Kind == UnitKind.Elite).ReserveIds.Take(2)
            .Select(id => new UnitPlacementChoice { UnitId = id, RegionId = "orthanc" }));
        return Execute(state, CommandKind.ChooseEffect, plan);
    }

    private static MatchState KingExcess(IRandomSource random)
    {
        var state = Shadow(random);
        Character(state, CharacterRules.Aragorn, "minas-tirith");
        for (var i = 0; i < 6; i++)
            Unit(state, "isengard", UnitKind.Regular, "minas-morgul");
        return PlayEvent(state, KingRevealed);
    }

    private static MatchState SpecialLoss(IRandomSource random)
    {
        var state = Shadow(random);
        Unit(state, "sauron", UnitKind.Elite, "osgiliath");
        Unit(state, "sauron", UnitKind.Regular, "osgiliath");
        for (var i = 0; i < 3; i++)
            Unit(state, "gondor", UnitKind.Regular, "north-ithilien");
        Give(state, HalfOrcs);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            Moves = [new() { From = "osgiliath", To = "north-ithilien", UnitIds = state.Units.Where(u => u.Location == "osgiliath").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = HalfOrcs });
        return Execute(state, CommandKind.CommitCombatCard, new(), 0, 0, 0, 0, 0, 4);
    }

    public static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states) => CardPackScenario.LayoutViews(states, Names);
}
