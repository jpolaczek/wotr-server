using System.Collections.Generic;

namespace Wotr.Contracts
{
    // TEST-01: named situations for testing a die, an action or a feature without playing up to it.
    public sealed class TestScenarioView
    {
        public string Id { get; set; } = "";
        public string Group { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Side StartingSide { get; set; }
        // TEST-10: the expansion modules the scenario plays with (ExpansionModules ids); empty for the base game.
        // The server lists and starts a scenario only when every one of them is released.
        public List<string> Modules { get; set; } = new List<string>();
    }

    public sealed class TestScenarioCatalogView
    {
        public List<TestScenarioView> Scenarios { get; set; } = new List<TestScenarioView>();
    }

    // The owner's own session plays OwnerSide; Seat is a session for the other side of the same match,
    // so one client can switch sides. The match is not a saved game and never appears in the game list.
    public sealed class TestSessionView
    {
        public string MatchId { get; set; } = "";
        public TestScenarioView Scenario { get; set; } = new TestScenarioView();
        public Side OwnerSide { get; set; } = Side.FreePeoples;
        public SessionView Seat { get; set; } = new SessionView();
    }
}
