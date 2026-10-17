using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-02: every Shadow die and each kind of action it allows (BASE pp. 18-21, 33-35, 41-43).
public static class ShadowDiceScenarios
{
    private const string Character = "Shadow · Character die";
    private const string Army = "Shadow · Army die";
    private const string Muster = "Shadow · Muster die";
    private const string ArmyMuster = "Shadow · Army/Muster die";
    private const string Event = "Shadow · Event die";
    private const string Eye = "Shadow · Eye";
    private const string Other = "Shadow · Rings";

    public static IEnumerable<TestScenario> All() =>
    [
        Scenario("sh-character-move-minions", Character, "Move Nazgûl and Minions",
            "Character die: the Witch-king is in Minas Morgul and the Nazgûl stand with the Sauron armies.",
            (id, random) => Turn(id, random, board => board.AtWar("sauron", "gondor").Enter(CharacterRules.WitchKing, "minas-morgul"),
                DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)),
        Scenario("sh-character-lead-army", Character, "Move or attack with a Nazgûl",
            "Character die: the Minas Morgul army has a Nazgûl; Sauron and Gondor are at war and a Gondor army holds North Ithilien.",
            (id, random) => Turn(id, random, Ithilien, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Muster, DieFace.Event, DieFace.Character)),
        Scenario("sh-character-event", Character, "Play a Character event",
            "Character die: The Ringwraiths Are Abroad, Flocks of Crebain and The Lidless Eye are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.Shadow, RingwraithsAbroad, "base.sh.character.16", "base.sh.character.18"),
                DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Army, DieFace.Muster, DieFace.Army)),
        Scenario("sh-army-move", Army, "Move armies",
            "Army die: move up to two different armies.",
            (id, random) => Turn(id, random, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Muster, DieFace.Character)),
        Scenario("sh-army-attack", Army, "Attack an army",
            "Army die: Sauron and Gondor are at war; attack the Gondor army in North Ithilien from Minas Morgul.",
            (id, random) => Turn(id, random, Ithilien, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Muster, DieFace.Army)),
        Scenario("sh-army-event", Army, "Play an Army event",
            "Army die: Shadows Gather and The Shadow Lengthens are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.Shadow, ShadowsGather, "base.sh.strategy.08"),
                DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Character, DieFace.Muster, DieFace.Character)),
        Scenario("sh-muster-politics", Muster, "Advance a nation",
            "Muster die: advance a Shadow nation on the Political Track.",
            (id, random) => Turn(id, random, DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Army, DieFace.Character)),
        Scenario("sh-muster-recruit", Muster, "Recruit reinforcements",
            "Muster die: Sauron is at war; recruit in its free strongholds.",
            (id, random) => Turn(id, random, board => board.AtWar("sauron"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Army, DieFace.Character)),
        Scenario("sh-muster-saruman", Muster, "Bring in Saruman",
            "Muster die: Isengard is at war, so Saruman can enter in Orthanc.",
            (id, random) => Turn(id, random, board => board.AtWar("isengard"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Army, DieFace.Character)),
        Scenario("sh-muster-witch-king", Muster, "Bring in the Witch-king",
            "Muster die: Sauron and Gondor are at war, so the Witch-king can enter with a Sauron army.",
            (id, random) => Turn(id, random, board => board.AtWar("sauron", "gondor"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Army, DieFace.Character)),
        Scenario("sh-muster-mouth", Muster, "Bring in the Mouth of Sauron",
            "Muster die: every Free Peoples nation is at war, so the Mouth of Sauron can enter in a Sauron stronghold.",
            (id, random) => Turn(id, random, board => board.AtWar("dwarves", "elves", "gondor", "north", "rohan"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Army, DieFace.Character)),
        Scenario("sh-muster-event", Muster, "Play a Muster event",
            "Muster die: Orcs Multiplying Again, Many Kings to the Service of Mordor and Monsters Roused are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.Shadow, OrcsMultiplying, "base.sh.strategy.17", "base.sh.strategy.22"),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Character, DieFace.Army, DieFace.Character)),
        Scenario("sh-army-muster", ArmyMuster, "Army or Muster action",
            "Army/Muster die: move, attack, recruit or advance a nation. Sauron and Gondor are at war; a Gondor army holds North Ithilien.",
            (id, random) => Turn(id, random, Ithilien, DieFace.ArmyMuster, DieFace.Character, DieFace.Event, DieFace.Character, DieFace.Event, DieFace.Character)),
        Scenario("sh-event-draw", Event, "Draw an event card",
            "Event die: draw a card from the Character or Strategy deck.",
            (id, random) => Turn(id, random, DieFace.Event, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Army, DieFace.Character)),
        Scenario("sh-event-play", Event, "Play any event card",
            "Event die: play a Character, Army or Muster event; The Ringwraiths Are Abroad, Shadows Gather and Orcs Multiplying Again are in hand.",
            (id, random) => Turn(id, random, board => board.Hand(Side.Shadow, RingwraithsAbroad, ShadowsGather, OrcsMultiplying),
                DieFace.Event, DieFace.Event, DieFace.Character, DieFace.Army, DieFace.Character, DieFace.Army)),
        new("sh-eye-hunt-allocation", Eye, "Allocate dice to the Hunt",
            "Start of the Hunt allocation: choose how many dice go to the Hunt Box before the roll. Eye results add to it.",
            Side.Shadow, HuntAllocation),
        Scenario("sh-elven-ring", Other, "Use a captured Elven Ring",
            "The Shadow holds an Elven Ring: change a die to another result (also an Eye), then the Ring leaves the game.",
            (id, random) => Turn(id, random, board => board.Change(state => state.Rings[0].Owner = Side.Shadow),
                DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event, DieFace.Army, DieFace.Character))
    ];

    public const string RingwraithsAbroad = "base.sh.character.23";
    public const string ShadowsGather = "base.sh.strategy.07";
    public const string OrcsMultiplying = "base.sh.strategy.20";

    private static TestScenario Scenario(string id, string group, string title, string description, Func<string, IRandomSource, MatchState> build) =>
        new(id, group, title, description, Side.Shadow, build);

    private static MatchState Turn(string id, IRandomSource random, params DieFace[] dice) => Turn(id, random, _ => { }, dice);

    // The Shadow has six results and one allocated die in the Hunt Box; the Free Peoples keep four results.
    private static MatchState Turn(string id, IRandomSource random, Action<ScenarioBoard> arrange, params DieFace[] dice)
    {
        var board = ScenarioBoard.Setup(id, random)
            .Dice(Side.Shadow, dice)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
        arrange(board);
        return board.Act(Side.Shadow);
    }

    // A Gondor army holds North Ithilien next to the Minas Morgul army and its Nazgûl; both nations are at war.
    private static void Ithilien(ScenarioBoard board) => board
        .AtWar("sauron", "gondor")
        .Clear("osgiliath")
        .Place("gondor", UnitKind.Regular, "north-ithilien", 2);

    // The turn before the roll: the Free Peoples ended their Fellowship phase and the Shadow allocates.
    private static MatchState HuntAllocation(string id, IRandomSource random)
    {
        var state = ScenarioBoard.Setup(id, random).State;
        TurnFlow.OpenHuntAllocation(state);
        return state;
    }
}
