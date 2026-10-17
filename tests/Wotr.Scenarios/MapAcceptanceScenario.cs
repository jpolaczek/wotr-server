using Wotr.Contracts;
using Wotr.Domain;

internal static class MapAcceptanceScenario
{
    private static readonly Projection Views = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands(), new LordsDiceCommands(),
        new KeeperRingCommands(), new LordsSetupCommands(), new CouncilCommands(), new SovereignCommands(),
        new CorruptionCommands(), new KingsDiceCommands(), new DismayCommands());

    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var result = new Dictionary<string, MatchState>();
        for (var flags = 0; flags < 8; flags++)
            result.Add("map17-" + flags, ExpansionUiFixture.Actions(random, new()
            {
                Lords = (flags & 1) != 0,
                Kings = (flags & 2) != 0,
                Fate = (flags & 4) != 0
            }));
        result.Add("map17-dense", Dense(random));
        return result;
    }

    // Safe projections only: this artifact contains no accounts, tokens, RNG or hidden decks.
    public static Dictionary<string, PlayerView> ProfileViews(Dictionary<string, MatchState> states)
    {
        var result = new Dictionary<string, PlayerView>();
        foreach (var name in new[] { "base", "fate", "newcities", "captured", "whitehand" })
            result.Add(name, Views.For(states["map16-" + name], Side.FreePeoples));
        var before = states["map17-dense"];
        result.Add("dense", Views.For(before, Side.FreePeoples));
        var after = ExpansionUiFixture.Execute(before, CommandKind.MoveArmies, new()
        {
            DieId = ExpansionUiFixture.Die(before, Side.FreePeoples).Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = "westemnet", To = "eastemnet",
                UnitIds = before.Units.Where(u => u.Location == "westemnet").Select(u => u.Id).ToList() }]
        });
        result.Add("moved", Views.For(after, Side.FreePeoples));
        return result;
    }

    private static MatchState Dense(IRandomSource random)
    {
        var state = BattleScenario.Field(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        Put(state, "rohan", UnitKind.Regular, "helms-deep", 3, true);
        Put(state, "rohan", UnitKind.Elite, "helms-deep", 2, true);
        Put(state, "rohan", UnitKind.Leader, "helms-deep", 2, true);
        Put(state, "isengard", UnitKind.Regular, "helms-deep", 7);
        Put(state, "isengard", UnitKind.Elite, "helms-deep", 3);
        Put(state, "sauron", UnitKind.Nazgul, "helms-deep", 3);
        Put(state, "gondor", UnitKind.Regular, "westemnet", 7);
        Put(state, "gondor", UnitKind.Elite, "westemnet", 3);
        Put(state, "gondor", UnitKind.Leader, "westemnet", 4);
        ExpansionUiFixture.Put(state, CharacterRules.Strider, "helms-deep").Garrison = true;
        ExpansionUiFixture.Put(state, "base.character.gimli", "helms-deep").Garrison = true;
        ExpansionUiFixture.Put(state, "base.character.legolas", "helms-deep").Garrison = true;
        state.Control["helms-deep"] = Side.FreePeoples;
        return state;
    }

    private static void Put(MatchState state, string nation, UnitKind kind, string region, int count, bool garrison = false)
    {
        var figures = state.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve")
            .Take(count).ToArray();
        if (figures.Length != count)
            throw new InvalidOperationException("Missing real figures in the dense map fixture.");
        foreach (var figure in figures)
        {
            figure.Location = region;
            figure.Garrison = garrison;
        }
    }
}
