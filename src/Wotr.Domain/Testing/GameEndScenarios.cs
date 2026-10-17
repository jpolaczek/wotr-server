using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-07 (SIGNALS_UX E): the four ways a base game ends, each one command away (BASE p. 44, LOGIC B19/B20). The
// situations use real pieces and the real entry to Mordor; only the Hunt pool keeps the tiles that make the ending
// certain, and the victory comes from the ordinary rules: the Crack of Doom, Corruption 12 and the Victory check
// at the end of the turn.
public static class GameEndScenarios
{
    private const string Group = "Game end";
    // Strongholds of the Free Peoples worth 10 points, away from Rivendell where the Fellowship starts.
    private static readonly string[] FreePeoplesStrongholds = ["minas-tirith", "dol-amroth", "helms-deep", "erebor", "lorien"];
    // Strongholds of the Shadow worth 4 points.
    private static readonly string[] ShadowStrongholds = ["orthanc", "dol-guldur"];

    public static IEnumerable<TestScenario> All() =>
    [
        new("fp-ring-destroyed", Group, "The Ring is destroyed",
            "The Fellowship stands on step 4 of the Mordor Track with 6 Corruption, and only 0-damage Hunt tiles are in the pool. Moving the Fellowship with a Character die reaches the Crack of Doom: the Free Peoples win.",
            Side.FreePeoples, RingDestroyed),
        new("sh-ring-corrupted", Group, "The Ring-bearers are corrupted",
            "The Fellowship stands on step 1 of the Mordor Track with 11 Corruption, and only damaging Hunt tiles are in the pool. Moving the Fellowship draws one; taking the damage with the Ring corrupts the Ring-bearers: the Shadow wins.",
            Side.FreePeoples, RingCorrupted),
        new("sh-military", Group, "Shadow military victory",
            "The Shadow holds Minas Tirith, Dol Amroth, Helm's Deep, Erebor and Lorien (10 Victory Points) and has the last action die of the turn. Using it ends the actions, and the Victory check gives the Shadow a military victory.",
            Side.Shadow, ShadowMilitary),
        new("fp-military", Group, "Free Peoples military victory",
            "The Free Peoples hold Orthanc and Dol Guldur (4 Victory Points) and have the last action die of the turn. Using it ends the actions, and the Victory check gives the Free Peoples a military victory.",
            Side.FreePeoples, FreePeoplesMilitary)
    ];

    private static MatchState RingDestroyed(string id, IRandomSource random) =>
        OnTheTrack(id, random, step: 4, corruption: 6)
            .KeepHuntTiles(tile => tile.Pool == "standard" && tile.Value == "0" && !tile.Stop)
            .Act(Side.FreePeoples);

    private static MatchState RingCorrupted(string id, IRandomSource random) =>
        OnTheTrack(id, random, step: 1, corruption: 11)
            .KeepHuntTiles(tile => tile.Pool == "standard" && int.TryParse(tile.Value, out var damage) && damage > 0 && !tile.Stop)
            .Act(Side.FreePeoples);

    private static MatchState ShadowMilitary(string id, IRandomSource random) =>
        ScenarioBoard.Setup(id, random)
            .Conquer(Side.Shadow, "sauron", FreePeoplesStrongholds)
            .Dice(Side.Shadow, DieFace.Army)
            .Spent(Side.Shadow, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army, DieFace.Character)
            .Spent(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Act(Side.Shadow);

    private static MatchState FreePeoplesMilitary(string id, IRandomSource random) =>
        ScenarioBoard.Setup(id, random)
            .Conquer(Side.FreePeoples, "gondor", ShadowStrongholds)
            .Dice(Side.FreePeoples, DieFace.Army)
            .Spent(Side.FreePeoples, DieFace.Character, DieFace.Muster, DieFace.Event)
            .Spent(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army, DieFace.Character)
            .Act(Side.FreePeoples);

    // The Fellowship on the Mordor Track through the real entry, then set to the step and Corruption of the ending.
    private static ScenarioBoard OnTheTrack(string id, IRandomSource random, int step, int corruption) =>
        MordorTrackScenarios.AtTheGate(id, random)
            .EnterMordor(random)
            .Change(state =>
            {
                state.Fellowship.MordorStep = step;
                state.Fellowship.Corruption = corruption;
            })
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Character, DieFace.Army, DieFace.Muster)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster);
}
