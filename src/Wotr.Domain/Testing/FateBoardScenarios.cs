using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-14: the Fate of Erebor map and setup rules that change play (EXPANSIONS F01, T32-SETUP): Erebor a Sauron
// Stronghold worth 2 points to the Free Peoples, Dale a neutral Fortification, the Iron Hills a Dwarven City, the Dwarves
// active from the start, and with New Cities Ered Luin and South Rhûn Cities worth a point. Each starts on turn 1 from the
// Fate setup; the pieces a description moves entered their regions earlier in the imagined game.
public static class FateBoardScenarios
{
    private const string Group = "Fate · Board";
    private const string Erebor = "erebor";
    private const string Dale = "dale";
    private const string IronHills = "iron-hills";
    private const string EredLuin = "ered-luin";
    private const string SouthRhun = "south-rhun";

    private static MatchConfiguration Fate => new() { Fate = true };
    private static MatchConfiguration NewCities => new() { Fate = true, NewCities = true };

    public static IEnumerable<TestScenario> All() =>
    [
        Scenario("fate-erebor-victory", "Erebor's two points",
            "Erebor is a Sauron Stronghold now. Its army was destroyed, the Free Peoples hold Mount Gundabad (2 points) and " +
            "the Dwarves are at war. The turn's last die, an Army die, moves the Iron Hills army into Erebor: its capture " +
            "is worth 2 points, and the Victory check gives the Free Peoples a military victory with 4.",
            Side.FreePeoples, EreborVictory),
        Scenario("fate-erebor-muster", "Muster in Erebor",
            "Erebor is a Sauron Stronghold now. Sauron and every Free Peoples nation are at war: a Muster die recruits " +
            "Sauron units in Erebor, where the Dwarves can no longer recruit, or brings the Mouth of Sauron into play there.",
            Side.Shadow, EreborMuster),
        Scenario("fate-erebor-revealed", "Revealed into Erebor",
            "The Fellowship moved one hidden step from Dale and the Hunt revealed it. Place it: in Erebor, a Sauron " +
            "Stronghold now, the Shadow draws one more Hunt tile; the Iron Hills, a Free Peoples City now, cannot take a " +
            "revealed Fellowship.",
            Side.FreePeoples, EreborRevealed),
        Scenario("fate-dale-fortification", "Dale, a neutral Fortification",
            "Dwarves marched into Dale, which belongs to no nation now. Sauron is at war: attack them from Erebor with an " +
            "Army die. Dale is a Fortification, so in the first round the attacker hits only on 6, and there is no siege; " +
            "Dale stays under nobody's control and is worth no points.",
            Side.Shadow, DaleFortification),
        Scenario("fate-dwarves-to-war", "The active Dwarves go to war",
            "The Dwarves start active and stand one step from war; the Shadow has spent its dice. A Muster die advances " +
            "them to At War (a passive nation would stop one step short), and the second Muster die recruits Dwarves in " +
            "the Iron Hills, now a Dwarven City.",
            Side.FreePeoples, DwarvesToWar),
        Scenario("fate-iron-hills-captured", "The Iron Hills fall",
            "The Iron Hills, a Dwarven City now, stand empty and Sauron is at war: an Army die moves the Sauron army of " +
            "Erebor into them, and their capture gives the Shadow 1 point.",
            Side.Shadow, IronHillsCaptured),
        Scenario("fate-iron-hills-sanctuary", "Declared in the Iron Hills",
            "Fellowship phase: the hidden Fellowship, last seen in Dale, has moved one step since and carries 2 Corruption. " +
            "Declared in the Iron Hills, a Free Peoples City now, it heals 1 Corruption; Erebor and Dale no longer heal.",
            Side.FreePeoples, IronHillsSanctuary),
        Scenario("fate-new-cities-ered-luin", "New Cities: Ered Luin",
            "New Cities: Ered Luin is a Dwarven City. It stands empty, a Sauron army waits in North Ered Luin and Sauron is " +
            "at war: an Army die moves it in, and the capture gives the Shadow 1 point.",
            Side.Shadow, EredLuinCaptured),
        Scenario("fate-new-cities-defence", "New Cities: Ered Luin defends",
            "New Cities: Ered Luin is a Dwarven City. Sauron is at war: attack its two Dwarven Regulars from North Ered " +
            "Luin with an Army die. In the first round the attacker hits only on 6, as against any City, and there is no " +
            "siege; without New Cities Ered Luin is a Town and the attack hits on 5.",
            Side.Shadow, EredLuinDefence),
        Scenario("fate-new-cities-south-rhun", "New Cities: South Rhûn",
            "New Cities: South Rhûn is a Southron City. It stands empty, a Dwarven army waits in East Rhûn and the Dwarves " +
            "are at war: an Army die moves it in, and the capture gives the Free Peoples 1 point.",
            Side.FreePeoples, SouthRhunCaptured)
    ];

    private static TestScenario Scenario(string id, string title, string description, Side side,
        Func<string, IRandomSource, MatchState> build) =>
        new(id, Group, title, description, side, build) { Modules = [ExpansionModules.Fate] };

    // The Free Peoples' Army die is the last die of the turn.
    private static MatchState EreborVictory(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Fate)
        .AtWar("dwarves")
        .Clear(Erebor)
        .Conquer(Side.FreePeoples, "north", "mount-gundabad")
        .Dice(Side.FreePeoples, DieFace.Army)
        .Spent(Side.FreePeoples, DieFace.Character, DieFace.Muster, DieFace.Event)
        .Spent(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army,
            DieFace.Character)
        .Act(Side.FreePeoples);

    private static MatchState EreborMuster(string id, IRandomSource random) => ShadowActs(ScenarioBoard.Setup(id, random, Fate)
        .AtWar("sauron", "dwarves", "elves", "gondor", "north", "rohan"), DieFace.Muster);

    // The Fellowship hidden in Dale moves with a Character die; the one Hunt die rolls a 6 and the pool holds only the two
    // reveal tiles without damage, so the Hunt reveals it and the Free Peoples' placement waits.
    private static MatchState EreborRevealed(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, Fate)
            .FellowshipAt(Dale)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
            .Hunt(1, 0)
            .KeepHuntTiles(tile => tile.Pool == "standard" && tile.Value == "0" && tile.Reveal);
        board.Act(Side.FreePeoples);
        var die = board.State.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        board = board.Command(CommandKind.MoveFellowship, new() { DieId = die.Id }, new SequenceRandom(5, 0, 0, 0, 0));
        if (board.State.Pending?.Kind != DecisionKind.FellowshipRoute)
            throw new InvalidOperationException($"The Hunt must reveal the Fellowship, not wait on {board.State.Pending?.Kind}.");
        return board.State;
    }

    // Two Dwarven Elites and a Leader stand in Dale; Erebor's army has two more Regulars and an Elite.
    private static MatchState DaleFortification(string id, IRandomSource random) => ShadowActs(ScenarioBoard.Setup(id, random, Fate)
        .AtWar("sauron")
        .Place("sauron", UnitKind.Regular, Erebor, 2)
        .Place("sauron", UnitKind.Elite, Erebor)
        .Place("dwarves", UnitKind.Elite, Dale, 2)
        .Place("dwarves", UnitKind.Leader, Dale), DieFace.Army);

    // The Free Peoples act twice in a row: the Shadow has spent its dice.
    private static MatchState DwarvesToWar(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Fate)
        .Nation("dwarves", 1, true)
        .Dice(Side.FreePeoples, DieFace.Muster, DieFace.Muster, DieFace.Character, DieFace.Event)
        .Spent(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army,
            DieFace.Character)
        .Act(Side.FreePeoples);

    private static MatchState IronHillsCaptured(string id, IRandomSource random) => ShadowActs(ScenarioBoard.Setup(id, random, Fate)
        .AtWar("sauron")
        .Clear(IronHills), DieFace.Army);

    // The Fellowship phase of turn 1 waits; the Fellowship moved one hidden step from Dale.
    private static MatchState IronHillsSanctuary(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Fate)
        .FellowshipAt(Dale, 1)
        .Corruption(2)
        .State;

    private static MatchState EredLuinCaptured(string id, IRandomSource random) => ShadowActs(ScenarioBoard.Setup(id, random, NewCities)
        .AtWar("sauron")
        .Clear(EredLuin)
        .Place("sauron", UnitKind.Regular, "north-ered-luin", 2), DieFace.Army);

    private static MatchState EredLuinDefence(string id, IRandomSource random) => ShadowActs(ScenarioBoard.Setup(id, random, NewCities)
        .AtWar("sauron")
        .Place("sauron", UnitKind.Regular, "north-ered-luin", 3), DieFace.Army);

    private static MatchState SouthRhunCaptured(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, NewCities)
        .AtWar("dwarves")
        .Clear(SouthRhun)
        .Place("dwarves", UnitKind.Elite, "east-rhun", 2)
        .Place("dwarves", UnitKind.Leader, "east-rhun")
        .Dice(Side.FreePeoples, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
        .Hunt(1, 0)
        .Act(Side.FreePeoples);

    // The Shadow acts first with the named die and five other results, one die in the Hunt Box, as in the dice scenarios.
    private static MatchState ShadowActs(ScenarioBoard board, DieFace first) => board
        .Dice(Side.Shadow, first, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
        .Hunt(1, 0)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Act(Side.Shadow);
}
