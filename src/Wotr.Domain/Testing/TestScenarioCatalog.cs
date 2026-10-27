using Wotr.Contracts;

namespace Wotr.Domain;

// Every scenario offered by the test mode: the standard setup (TEST-01), the base-game dice (TEST-02),
// the Mordor Track (TEST-06), the endings of the game (TEST-07), each base-game card's event and combat card (TEST-09)
// and, since TEST-10, the expansions' dice and actions (TEST-04, restored) and, since UI-266, the Fate cards with their
// own choices, since UI-349 Gimli's Fate mechanics and since UI-347, UI-348 and UI-278 the Lords and Kings decisions of the owner's playtest,
// and since TEST-12 each Lords card's event and combat card and every Lords mechanic; the
// server offers a scenario only when its modules are released.
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
        .. GameEndScenarios.All(),
        .. CardScenarios.All(),
        .. ExpansionDiceScenarios.All(),
        .. FateCardScenarios.All(),
        .. FateGimliScenarios.All(),
        .. LordsDecisionScenarios.All(),
        .. LordsCardScenarios.All(),
        .. LordsCharacterScenarios.All(),
        .. LordsSmeagolScenarios.All(),
        .. LordsCouncilScenarios.All(),
        .. LordsKingsScenarios.All(),
        .. KingsDecisionScenarios.All(),
        .. KingsCardDecisionScenarios.All()
    ];

    public static TestScenario? Find(string id) => All.FirstOrDefault(scenario => scenario.Id == id);

    private static MatchState StandardSetup(string matchId, IRandomSource random)
    {
        var state = MatchFactory.Create(matchId, random);
        TurnFlow.Start(state);
        return state;
    }
}
