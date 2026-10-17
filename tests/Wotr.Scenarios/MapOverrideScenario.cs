using Wotr.Contracts;
using Wotr.Domain;

internal static class MapOverrideScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["map16-base"] = Position(random, new()),
        ["map16-fate"] = Position(random, new() { Fate = true }),
        ["map16-newcities"] = Position(random, new() { Fate = true, NewCities = true }),
        ["map16-captured"] = Captured(random, false),
        ["map16-whitehand"] = Captured(random, true)
    };

    public static MatchState HistoricalPaidStep()
    {
        // This committed schema-3 save predates contours-2. Do not regenerate it
        // through today's setup: the server must resume the original paid state.
        return StateCodec.Read(RegressionFixtures.Read("paid-army-step"));
    }

    private static MatchState Position(IRandomSource random, MatchConfiguration configuration) =>
        ExpansionUiFixture.Actions(random, configuration);

    private static MatchState Captured(IRandomSource random, bool whiteHand)
    {
        var state = Position(random, new() { Kings = true });
        foreach (var unit in state.Units.Where(u => u.Location == KingsBoard.HelmsDeep))
            BoardRules.ReturnToReserve(unit);
        state.Control[KingsBoard.HelmsDeep] = Side.Shadow;
        if (!whiteHand)
            return state;
        var sovereign = ExpansionUiFixture.Sovereign(state, SovereignCatalog.Theoden);
        state.Units.RemoveAll(u => u.Id == sovereign.Id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = "edoras";
        ExpansionUiFixture.Put(state, CharacterRules.Grey, "edoras");
        ExpansionUiFixture.Die(state, Side.FreePeoples).Face = DieFace.Character;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }
}
