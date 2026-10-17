using Wotr.Contracts;

namespace Wotr.Domain;

// Every scenario offered by the test mode: the standard setup (TEST-01), the base-game dice (TEST-02),
// the Mordor Track (TEST-06) and the endings of the game (TEST-07).
// TEST-05: the base game is the MVP for now; the expansion scenarios (TEST-04) live on the
// expansion-test-scenarios branch.
public static class TestScenarioCatalog
{
    public const string General = "General";

    public static IReadOnlyList<TestScenario> All { get; } =
    [
        new("setup", General, "Standard setup",
            "A new base game from the standard setup. Play any part of the game; nothing is saved.",
            Side.FreePeoples, StandardSetup),
        .. FreePeoplesDiceScenarios.All(),
        .. ShadowDiceScenarios.All(),
        .. MordorTrackScenarios.All(),
        .. GameEndScenarios.All()
    ];

    public static TestScenario? Find(string id) => All.FirstOrDefault(scenario => scenario.Id == id);

    private static MatchState StandardSetup(string matchId, IRandomSource random)
    {
        var state = MatchFactory.Create(matchId, random);
        TurnFlow.Start(state);
        return state;
    }
}
