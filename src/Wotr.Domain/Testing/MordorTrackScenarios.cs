using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-06: the Fellowship's way through Mordor (BASE pp. 43-44, LOGIC B19/B20, T17): entering the track
// from Minas Morgul, walking it with Character dice and the last step before the Crack of Doom.
public static class MordorTrackScenarios
{
    private const string Group = "Free Peoples · Mordor Track";
    private const string Gate = "minas-morgul";

    public static IEnumerable<TestScenario> All() =>
    [
        Scenario("fp-mordor-enter", "Enter Mordor",
            "Fellowship phase: the hidden Fellowship stands in Minas Morgul with 3 Corruption and may enter the Mordor Track. The turn then continues with the Hunt and a normal roll.",
            (id, random) => AtTheGate(id, random).State),
        Scenario("fp-mordor-track", "Walk the Mordor Track",
            "The Fellowship has just entered the Mordor Track (step 0, 3 Corruption). Three Character dice and Will of the West: each step draws a Hunt tile; a turn without an attempt adds Corruption.",
            (id, random) => AtTheGate(id, random)
                .EnterMordor(random)
                .Dice(Side.FreePeoples, DieFace.Character, DieFace.Character, DieFace.Character, DieFace.WillOfTheWest)
                .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
                .Hunt(1, 1)
                .Act(Side.FreePeoples)),
        Scenario("fp-mordor-crack", "One step from the Crack of Doom",
            "The Fellowship stands on step 4 of the Mordor Track with 6 Corruption. A step that is not stopped reaches the Crack of Doom and destroys the Ring if Corruption stays below 12.",
            (id, random) => AtTheGate(id, random)
                .EnterMordor(random)
                .Change(state =>
                {
                    state.Fellowship.MordorStep = 4;
                    state.Fellowship.Corruption = 6;
                })
                .Dice(Side.FreePeoples, DieFace.Character, DieFace.Character, DieFace.Army, DieFace.Muster)
                .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
                .Hunt(1, 1)
                .Act(Side.FreePeoples))
    ];

    private static TestScenario Scenario(string id, string title, string description, Func<string, IRandomSource, MatchState> build) =>
        new(id, Group, title, description, Side.FreePeoples, build);

    // The Fellowship phase of turn 1 with the hidden Fellowship in Minas Morgul, as after its journey.
    internal static ScenarioBoard AtTheGate(string id, IRandomSource random) =>
        ScenarioBoard.Setup(id, random).Change(state =>
        {
            state.Fellowship.RegionId = Gate;
            state.Fellowship.Progress = 0;
            state.Fellowship.Corruption = 3;
        });
}
