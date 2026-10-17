using Wotr.Contracts;
using Wotr.Domain;

internal static class CouncilHomelandScenario
{
    private static readonly string[] All = ["lords.character.boromir", "lords.character.gimli",
        "lords.character.legolas", "lords.character.merry", "lords.character.pippin"];

    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var initial = ExpansionUiFixture.Start(random, new() { Lords = true, Council = true });
        var result = new Dictionary<string, MatchState>
        {
            ["ui69-empty"] = Choose(initial, []),
            ["ui69-stay"] = Choose(initial, All),
            ["ui69-one-politics"] = Choose(initial, ["lords.character.gimli"]),
            ["ui69-one-travel"] = Choose(initial, ["lords.character.gimli"]),
            ["ui69-two"] = Choose(initial, ["lords.character.boromir", "lords.character.legolas"]),
            ["ui69-max"] = Choose(initial, All)
        };
        foreach (var cities in new[] { false, true })
        {
            var fate = ExpansionUiFixture.Start(random, new() { Lords = true, Council = true, Kings = true,
                Fate = true, NewCities = cities });
            foreach (var region in new[] { "iron-hills", "ered-luin" })
                result.Add($"ui69-fate-{cities}-{region}", Choose(fate, [FateGimli.Alternate]));
            if (!cities)
                result.Add("ui69-layout", Choose(fate, All.Select(id => id == "lords.character.gimli" ? FateGimli.Alternate : id)));
        }
        return result;
    }

    private static MatchState Choose(MatchState initial, IEnumerable<string> alternatives)
    {
        var state = ExpansionUiFixture.Execute(StateCodec.Copy(initial), CommandKind.ChooseCompanionVersions,
            new() { GuideId = CharacterRules.Strider, Ids = [CharacterRules.Grey, .. alternatives] });
        if (state.Pending?.Kind != DecisionKind.CouncilHomelands || state.Phase != GamePhase.Fellowship ||
            state.Turn != 1 || state.Lords!.Council!.GrantedTokens.Count != 0)
            throw new InvalidOperationException("Homeland fixture must use the normal first Fellowship Phase.");
        return StateCodec.Read(StateCodec.Write(state).Replace(state.Id, Guid.NewGuid().ToString("N"), StringComparison.Ordinal));
    }
}
