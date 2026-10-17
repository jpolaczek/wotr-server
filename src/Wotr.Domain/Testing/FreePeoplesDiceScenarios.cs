using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-02: every Free Peoples die and each kind of action it allows (BASE pp. 18-21).
// Each scenario is one example: a legal use from the first decision, then normal play.
public static class FreePeoplesDiceScenarios
{
    private const string Character = "Free Peoples · Character die";
    private const string Army = "Free Peoples · Army die";
    private const string Muster = "Free Peoples · Muster die";
    private const string ArmyMuster = "Free Peoples · Army/Muster die";
    private const string Event = "Free Peoples · Event die";
    private const string Will = "Free Peoples · Will of the West";
    private const string Other = "Free Peoples · Rings and passing";

    public static IEnumerable<TestScenario> All() =>
    [
        Scenario("fp-character-move-fellowship", Character, "Move the Fellowship",
            "Character die: move the Fellowship one step. The Hunt Box holds an allocated die and an Eye, so the Shadow hunts.",
            (id, random) => Turn(id, random, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)),
        Scenario("fp-character-hide-fellowship", Character, "Hide the Fellowship",
            "Character die: the Fellowship is revealed in Rivendell and can hide again.",
            (id, random) => Turn(id, random, board => board.Change(s => s.Fellowship.Hidden = false),
                DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)),
        Scenario("fp-character-separate-companions", Character, "Separate companions",
            "Character die: separate one or more companions from the Fellowship and move them.",
            (id, random) => Turn(id, random, DieFace.Character, DieFace.Character, DieFace.Army, DieFace.Muster)),
        Scenario("fp-character-move-companions", Character, "Move companions",
            "Character die: Legolas in Lorien and Gimli in Erebor have left the Fellowship and can move.",
            (id, random) => Turn(id, random, board => board.Companion("base.character.legolas", "lorien").Companion("base.character.gimli", "erebor"),
                DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)),
        Scenario("fp-character-lead-army", Character, "Move or attack with a leader",
            "Character die: the Minas Tirith army has a Gondor leader; Gondor and Sauron are at war and a Sauron army holds Osgiliath.",
            (id, random) => Turn(id, random, Osgiliath, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Character)),
        Scenario("fp-character-event", Character, "Play a Character event",
            "Character die: Bilbo's Song, Wizard's Staff and Mirror of Galadriel are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.FreePeoples, BilbosSong, "base.fp.character.08", "base.fp.character.13"),
                DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Army)),
        Scenario("fp-army-move", Army, "Move armies",
            "Army die: move up to two different armies.",
            (id, random) => Turn(id, random, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event)),
        Scenario("fp-army-attack", Army, "Attack an army",
            "Army die: Gondor and Sauron are at war; attack the Sauron army in Osgiliath from Minas Tirith.",
            (id, random) => Turn(id, random, Osgiliath, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army)),
        Scenario("fp-army-event", Army, "Play an Army event",
            "Army die: Faramir's Rangers, A Power too Great and The Power of Tom Bombadil are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.FreePeoples, FaramirsRangers, "base.fp.strategy.02", "base.fp.strategy.03"),
                DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Character)),
        Scenario("fp-muster-politics", Muster, "Advance a nation",
            "Muster die: advance a nation on the Political Track. Gondor is passive one step from war and cannot advance.",
            (id, random) => Turn(id, random, board => board.Nation("gondor", 1, false),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event)),
        Scenario("fp-muster-recruit", Muster, "Recruit reinforcements",
            "Muster die: Gondor is at war; recruit in its free cities and strongholds.",
            (id, random) => Turn(id, random, board => board.AtWar("gondor"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event)),
        Scenario("fp-muster-event", Muster, "Play a Muster event",
            "Muster die: Wisdom of Elrond, Book of Mazarbul and Guards of the Citadel are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.FreePeoples, WisdomOfElrond, "base.fp.strategy.04", "base.fp.strategy.14"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Character)),
        Scenario("fp-army-muster", ArmyMuster, "Army or Muster action",
            "Army/Muster die: move, attack, recruit or advance a nation. Gondor and Sauron are at war; a Sauron army holds Osgiliath.",
            (id, random) => Turn(id, random, Osgiliath, DieFace.ArmyMuster, DieFace.Character, DieFace.Event, DieFace.Character)),
        Scenario("fp-event-draw", Event, "Draw an event card",
            "Event die: draw a card from the Character or Strategy deck.",
            (id, random) => Turn(id, random, DieFace.Event, DieFace.Character, DieFace.Army, DieFace.Muster)),
        Scenario("fp-event-play", Event, "Play any event card",
            "Event die: play a Character, Army or Muster event; Bilbo's Song, Faramir's Rangers and Wisdom of Elrond are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.FreePeoples, BilbosSong, FaramirsRangers, WisdomOfElrond),
                DieFace.Event, DieFace.Event, DieFace.Character, DieFace.Army)),
        Scenario("fp-will-any-action", Will, "Will of the West as any result",
            "Will of the West: use it as a Character, Army, Muster or Event result. Gondor is at war and Bilbo's Song is in hand.",
            (id, random) => Turn(id, random, board => board.AtWar("gondor").Hand(Side.FreePeoples, BilbosSong),
                DieFace.WillOfTheWest, DieFace.WillOfTheWest, DieFace.Character, DieFace.Army)),
        Scenario("fp-will-gandalf-white", Will, "Bring in Gandalf the White",
            "Will of the West: Gandalf the Grey has left the Fellowship in Rivendell and Saruman is in play.",
            (id, random) => Turn(id, random, board => board.Companion(CharacterRules.Grey, "rivendell").Enter(CharacterRules.Saruman, "orthanc"),
                DieFace.WillOfTheWest, DieFace.Character, DieFace.Army, DieFace.Muster)),
        Scenario("fp-will-aragorn", Will, "Bring in Aragorn",
            "Will of the West: Strider has left the Fellowship and stands in Minas Tirith.",
            (id, random) => Turn(id, random, board => board.Companion(CharacterRules.Strider, "minas-tirith"),
                DieFace.WillOfTheWest, DieFace.Character, DieFace.Army, DieFace.Muster)),
        Scenario("fp-elven-ring", Other, "Use an Elven Ring",
            "Change one die to another result with an Elven Ring; the Ring then passes to the Shadow.",
            (id, random) => Turn(id, random, DieFace.Muster, DieFace.Muster, DieFace.Army, DieFace.Event)),
        Scenario("fp-pass", Other, "Pass",
            "The Free Peoples have fewer unused dice than the Shadow and may pass.",
            (id, random) => Turn(id, random, board => board.Spent(Side.FreePeoples, DieFace.Character, DieFace.Event), DieFace.Army, DieFace.Muster))
    ];

    public const string BilbosSong = "base.fp.character.12";
    public const string FaramirsRangers = "base.fp.strategy.06";
    public const string WisdomOfElrond = "base.fp.strategy.08";

    private static TestScenario Scenario(string id, string group, string title, string description, Func<string, IRandomSource, MatchState> build) =>
        new(id, group, title, description, Side.FreePeoples, build);

    private static MatchState Turn(string id, IRandomSource random, params DieFace[] dice) => Turn(id, random, _ => { }, dice);

    // The Shadow keeps five results and a Hunt Box with one allocated die and one Eye.
    private static MatchState Turn(string id, IRandomSource random, Action<ScenarioBoard> arrange, params DieFace[] dice)
    {
        var board = ScenarioBoard.Setup(id, random)
            .Dice(Side.FreePeoples, dice)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
            .Hunt(1, 1);
        arrange(board);
        return board.Act(Side.FreePeoples);
    }

    // A Sauron army holds Osgiliath next to the Minas Tirith army and its leader; both nations are at war.
    private static void Osgiliath(ScenarioBoard board) => board
        .AtWar("gondor", "sauron")
        .Clear("osgiliath")
        .Place("sauron", UnitKind.Regular, "osgiliath", 3);
}
