using Wotr.Contracts;
using Wotr.Domain;

// Inputs reuse the network corpus; exports contain only the owning player's normal projection.
internal static class BoardProfileScenario
{
    private static readonly Projection Views = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands(), new LordsDiceCommands(),
        new KeeperRingCommands(), new LordsSetupCommands(), new CouncilCommands(), new SovereignCommands(),
        new CorruptionCommands(), new KingsDiceCommands(), new DismayCommands());

    public static Dictionary<string, MatchState> Inputs(IRandomSource random, MatchState roster,
        MatchState homelands, MatchState battle, MatchState hunt)
    {
        RequireDecision(homelands, DecisionKind.CouncilHomelands, Side.FreePeoples);
        RequireDecision(battle, DecisionKind.Losses, Side.FreePeoples);
        RequireDecision(hunt, DecisionKind.HuntReroll, Side.Shadow);
        // Loss allocation is a child frame; the actual battle remains its active ancestor.
        if (BattleRuntime.Active(battle) == null || HuntRuntime.Active(hunt) == null)
            throw new InvalidOperationException("Board profiling requires an active battle and Hunt.");
        var lords = ExpansionUiFixture.Start(random, new() { Lords = true });
        var council = ExpansionUiFixture.Start(random, new() { Lords = true, Council = true });
        var fate = ExpansionUiFixture.Start(random, new()
        {
            Lords = true, Council = true, Kings = true, Fate = true, NewCities = true
        });
        return new()
        {
            ["characters-fp"] = roster,
            ["characters-sh"] = roster,
            ["setup-lords-fp"] = lords,
            ["setup-council-fp"] = council,
            ["setup-fate-fp"] = fate,
            ["setup-waiting-sh"] = fate,
            ["homelands-fp"] = homelands,
            ["homelands-sh"] = homelands,
            ["menu-battle"] = battle,
            ["menu-hunt"] = hunt
        };
    }

    public static Dictionary<string, PlayerView> ProfileViews(IReadOnlyDictionary<string, MatchState> inputs) =>
        inputs.ToDictionary(pair => pair.Key, pair => Views.For(pair.Value, SideFor(pair.Key)));

    private static Side SideFor(string kind) => kind.EndsWith("-sh", StringComparison.Ordinal) || kind == "menu-hunt"
        ? Side.Shadow : Side.FreePeoples;

    private static void RequireDecision(MatchState state, DecisionKind kind, Side owner)
    {
        if (state.Pending?.Kind != kind || state.Pending.Owner != owner)
            throw new InvalidOperationException($"Board profiling requires {kind} owned by {owner}.");
    }
}
