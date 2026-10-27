using Wotr.Contracts;

namespace Wotr.Domain;

// Every scenario offered by the test mode: the standard setup (TEST-01), the base-game dice (TEST-02),
// the Mordor Track (TEST-06), the endings of the game (TEST-07), each base-game card's event and combat card (TEST-09)
// and, since TEST-10, the expansions' dice and actions (TEST-04, restored) and, since UI-266, the Fate cards with their
// own choices, since UI-349 Gimli's Fate mechanics and since UI-347, UI-348 and UI-278 the Lords and Kings decisions of the owner's playtest;
// since TEST-14 every Fate card's Combat side, Gimli's remaining abilities, the Fate map rules and Kings with Fate, and
// since TEST-13 every Kings card's event and combat and each Kings mechanic at the step it acts; the
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
        .. FateCardCombatScenarios.All(),
        .. FateCardPlayScenarios.All(),
        .. FateGimliAbilityScenarios.All(),
        .. FateBoardScenarios.All(),
        .. LordsDecisionScenarios.All(),
        .. KingsDecisionScenarios.All(),
        .. KingsCardDecisionScenarios.All(),
        .. KingsCardScenarios.All(),
        .. KingsSovereignScenarios.All(),
        .. KingsCorruptionScenarios.All(),
        .. KingsRulerScenarios.All(),
        .. KingsChieftainScenarios.All(),
        .. KingsVariantScenarios.All()
    ];

    public static TestScenario? Find(string id) => All.FirstOrDefault(scenario => scenario.Id == id);

    private static MatchState StandardSetup(string matchId, IRandomSource random)
    {
        var state = MatchFactory.Create(matchId, random);
        TurnFlow.Start(state);
        return state;
    }
}
