using Wotr.Contracts;
using Wotr.Domain;

internal static class CharacterRosterScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui66-entry"] = Entry(random),
        ["ui66-hover"] = CharacterScenario.Create(random)["travel-drag"],
        ["ui66-full"] = Full(random),
        ["ui66-empty"] = ExpansionUiFixture.Actions(random, new MatchConfiguration())
    };

    private static MatchState Entry(IRandomSource random)
    {
        var state = CharacterScenario.Create(random)["entry"];
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == CharacterRules.Saruman));
        return state;
    }

    private static MatchState Full(IRandomSource random)
    {
        var state = ExpansionUiMatrix.ResourceState(random);
        foreach (var id in new[] { "gimli", "legolas", "boromir", "merry", "pippin" })
            ExpansionUiFixture.Put(state, "base.character." + id, "rivendell");
        ExpansionUiFixture.Put(state, KingsChieftains.Mirkwood, "dol-guldur");
        ExpansionUiFixture.Put(state, KingsChieftains.Ugluk, "orthanc");
        return state;
    }
}
