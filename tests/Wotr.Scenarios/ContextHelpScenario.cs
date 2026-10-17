using Wotr.Contracts;
using Wotr.Domain;

internal static class ContextHelpScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ui61-ring"] = BaseTurnScenario.Create(random)["turn-ring-politics"],
        ["ui61-full"] = ExpansionUiFixture.Actions(random, new MatchConfiguration
        {
            Lords = true, Kings = true, Council = true, Fate = true,
            VisibleCorruption = true, SequentialCorruption = true, NewCities = true
        })
    };
}
