using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-16 (the owner's layout audit, 2026-11-03): the base-game situations the Test scenarios lacked — The Spirit of
// Mordor's Scouts in a field battle at Helm's Deep, Dreadful Spells beside a siege (a field force and a garrison in one
// region), the dense board with Helm's Deep besieged (the seeder's map17-dense, rebuilt here as a legal game), nine
// characters in one region, the Voice of Saruman with a single Muster die and Bilbo's Song's Fateful Strike against a
// Minion with no Nazgûl in the battle. Each starts from the standard setup and reaches its decision through the engine's
// own commands; the pieces placed here entered or moved earlier in the imagined game. Without a module the client shows
// the group in its first tab.
public static class BaseLayoutScenarios
{
    public const string Group = "Layout stress";

    private const string Boromir = "base.character.boromir";
    private const string Gimli = "base.character.gimli";
    private const string Legolas = "base.character.legolas";
    private const string Merry = "base.character.merry";
    private const string HelmsDeep = "helms-deep";
    private const string Westemnet = "westemnet";
    private const string Ford = "fords-of-isen";
    private const string Orthanc = "orthanc";
    private const string Lorien = "lorien";
    private const string ParthCelebrant = "parth-celebrant";
    private const string MinasTirith = "minas-tirith";

    public static IEnumerable<TestScenario> All() =>
    [
        new("layout-scouts-helms-deep", Group, "The Spirit of Mordor · Scouts at Helm's Deep",
            "An Isengard army from Westemnet attacks the Rohan army in Helm's Deep, which stays out to fight a field battle; the " +
            "Shadow played no Combat card. Choose The Spirit of Mordor (Scouts): Helm's Deep lights to withdraw into its " +
            "stronghold and the Fords of Isen to retreat. Check that the battle window keeps Helm's Deep clear.",
            Side.FreePeoples, ScoutsAtHelmsDeep),
        new("layout-dreadful-spells-siege", Group, "Dreadful Spells · A field force and a garrison in one region",
            "A Sauron army with three Nazgûl besieges the Elves in Lórien, so Lórien holds a field force and a garrison, and an " +
            "Elven army stands next door in Parth Celebrant. Play Dreadful Spells with the Character die: the besiegers are " +
            "its only source (three dice), and the target is the garrison inside Lórien or the field army in Parth Celebrant. " +
            "Check that Lórien's region panel keeps the field force and the garrison apart.",
            Side.Shadow, DreadfulSpellsSiege),
        new("layout-dense-helms-deep", Group, "The dense board: Helm's Deep besieged",
            "Ten Isengard units and three Nazgûl besiege Helm's Deep, where three Rohan Regulars, two Elites, two Leaders, " +
            "Strider, Gimli and Legolas hold the stronghold; a Gondor army of six Regulars, three Elites and three Leaders " +
            "stands in Westemnet; every other region keeps its setup. Look at the map and Helm's Deep's region panel; the Army " +
            "die can attack the besiegers from Westemnet.",
            Side.FreePeoples, DenseHelmsDeep),
        new("layout-crowded-region", Group, "Nine characters in one region",
            "Every nation is at war. The seven Companions left the Fellowship, which Gollum guides, and hold besieged Minas " +
            "Tirith with the Gondor army; the Witch-king and the Mouth of Sauron lead the Sauron army besieging it. Look at the " +
            "figures on the map, Minas Tirith's region panel and the battle window of an assault.",
            Side.Shadow, CrowdedRegion),
        new("layout-saruman-voice-one-muster", Group, "The Voice of Saruman with one Muster die",
            "Saruman stands in Orthanc and Isengard is at war; the Shadow rolled exactly one Muster die (and no Army/Muster). " +
            "Both uses of the Voice — a Regular in each Isengard settlement, or two Elites for Regulars in Orthanc — are " +
            "paid by that one die.",
            Side.Shadow, SarumanVoice),
        new("layout-fateful-strike-minion", Group, "Bilbo's Song · Fateful Strike with no Nazgûl",
            "Saruman leads the Isengard army from Orthanc against the Fords of Isen, where the Fellowship stands with Strider, " +
            "Gandalf the Grey, Boromir and Merry; no Nazgûl takes part. The Free Peoples played Fateful Strike, missed every " +
            "Combat die and hit with every Leader re-roll: eliminate Saruman, or skip the effect to eliminate no Minion.",
            Side.FreePeoples, FatefulStrike)
    ];

    // Rohan's Helm's Deep army (a Regular, an Elite and a Leader) is attacked from Westemnet, which the Isengard army
    // took earlier, and stays out of its stronghold; the Shadow commits no card, so the battle waits on the Free Peoples.
    private static MatchState ScoutsAtHelmsDeep(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(ScenarioBoard.Setup(id, random))
            .AtWar("isengard", "rohan")
            .Place("rohan", UnitKind.Elite, HelmsDeep)
            .Place("rohan", UnitKind.Leader, HelmsDeep)
            .Conquer(Side.Shadow, "isengard", Westemnet)
            .Place("isengard", UnitKind.Regular, Westemnet, 3)
            .Place("isengard", UnitKind.Elite, Westemnet)
            .Hand(Side.FreePeoples, FreePeoplesStrategyCards01.SpiritOfMordor);
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, Westemnet, HelmsDeep);
        return LordsBoard.CombatChoice(LordsBoard.Field(state), Side.FreePeoples);
    }

    // The Sauron army besieging Lórien carries three Nazgûl; the Elven reserve stands in Parth Celebrant, next to it.
    private static MatchState DreadfulSpellsSiege(string id, IRandomSource random) =>
        LordsBoard.Rolled(ScenarioBoard.Setup(id, random))
            .AtWar("sauron", "elves")
            .Besiege(Lorien, "sauron", 4)
            .Place("sauron", UnitKind.Nazgul, Lorien, 3)
            .Place("elves", UnitKind.Regular, ParthCelebrant, 2)
            .Place("elves", UnitKind.Elite, ParthCelebrant)
            .Hand(Side.Shadow, ShadowCharacterCards04.Spells)
            .Act(Side.Shadow);

    // The map17-dense fixture's figures on the standard setup: Rohan's garrison of five units with two Leaders and three
    // Companions, a full Isengard stack with three Nazgûl outside it, and Gondor's reserve marching west to relieve it.
    private static MatchState DenseHelmsDeep(string id, IRandomSource random) =>
        LordsBoard.Rolled(ScenarioBoard.Setup(id, random))
            .AtWar("isengard", "sauron", "rohan", "gondor")
            .Place("rohan", UnitKind.Regular, HelmsDeep, 2)
            .Place("rohan", UnitKind.Elite, HelmsDeep, 2)
            .Place("rohan", UnitKind.Leader, HelmsDeep, 2)
            .Companion(CharacterRules.Strider, HelmsDeep)
            .Companion(Gimli, HelmsDeep)
            .Companion(Legolas, HelmsDeep)
            .Besiege(HelmsDeep, "isengard", 6)
            .Place("isengard", UnitKind.Elite, HelmsDeep, 4)
            .Place("sauron", UnitKind.Nazgul, HelmsDeep, 3)
            .Place("gondor", UnitKind.Regular, Westemnet, 6)
            .Place("gondor", UnitKind.Elite, Westemnet, 3)
            .Place("gondor", UnitKind.Leader, Westemnet, 3)
            .Act(Side.FreePeoples);

    // Every Free Peoples nation at war lets the Mouth of Sauron enter; both Minions then marched with the besiegers.
    private static MatchState CrowdedRegion(string id, IRandomSource random) =>
        LordsBoard.Rolled(ScenarioBoard.Setup(id, random))
            .AtWar("sauron", "gondor", "rohan", "elves", "dwarves", "north")
            .GollumGuides(MinasTirith)
            .Besiege(MinasTirith, "sauron", 5)
            .Place("sauron", UnitKind.Elite, MinasTirith, 2)
            .Enter(CharacterRules.WitchKing, MinasTirith)
            .Enter(CharacterRules.Mouth, MinasTirith)
            .Act(Side.Shadow);

    // Saruman entered earlier; this turn's roll shows a single Muster result among the Shadow's seven dice.
    private static MatchState SarumanVoice(string id, IRandomSource random) =>
        ScenarioBoard.Setup(id, random)
            .AtWar("isengard")
            .Enter(CharacterRules.Saruman, Orthanc)
            .Dice(Side.Shadow, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Character)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Act(Side.Shadow);

    // The Fords battle of the base Free Peoples cards (TEST-09) with Saruman leading from Orthanc. Every Combat die rolls a
    // 1 and the Free Peoples' Leader re-roll a 6, so at least two re-roll hits meet a Minion and no Nazgûl.
    private static MatchState FatefulStrike(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(ScenarioBoard.Setup(id, random))
            .AtWar("isengard", "rohan")
            .Enter(CharacterRules.Saruman, Orthanc)
            .Place("rohan", UnitKind.Elite, Ford)
            .Companion(CharacterRules.Strider, Ford)
            .Companion(CharacterRules.Grey, Ford)
            .Companion(Boromir, Ford)
            .Companion(Merry, Ford)
            .FellowshipAt(Ford)
            .Hand(Side.FreePeoples, FreePeoplesCharacterCards02.Bilbo);
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, Orthanc, Ford);
        state = LordsBoard.CombatChoice(state, Side.FreePeoples);
        state = ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = FreePeoplesCharacterCards02.Bilbo });
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Reroll(state);
        if (state.Pending is not { Kind: DecisionKind.EffectChoice, Presentation: "NazgulOrMinion", Owner: Side.FreePeoples })
            throw new InvalidOperationException($"Fateful Strike must wait for its choice, not {state.Pending?.Presentation}.");
        return state;
    }

    // The Free Peoples re-roll every failed die their Leadership allows, each a 6; the Shadow keeps its results.
    private static MatchState Reroll(MatchState state)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        var ids = state.Pending!.Owner == Side.FreePeoples
            ? BattleRoll.Failed(force).Take(BattleRoll.RerollMaximum(state, force)).ToList()
            : [];
        return ExpansionBoard.Execute(state, CommandKind.RerollBattle, new() { Ids = ids }, LordsBoard.Rolls([.. ids.Select(_ => 5)]));
    }
}
