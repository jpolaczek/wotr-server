using Wotr.Contracts;
using static Wotr.Domain.ExpansionBoard;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-13: the Dark Chieftains beyond the Black Serpent's entry and Red Wrath (KINGS pp. 7, 14-16; rules T29, X01):
// Uglúk entering beside a revealed Fellowship, the combat substitutions of each Chieftain with the printed cards that
// change them (Swarm of Bats, Relentless Assault, Desperate Battle), the defender winning the initiative-0 tie against
// Brand, Uglúk's Hunt re-roll bonus and a Council token bringing a Chieftain in without his condition.
public static class KingsChieftainScenarios
{
    private const string Group = "Kings · Dark Chieftains";
    private const string Dale = "dale";
    private const string Rhovanion = "northern-rhovanion";
    private const string Orthanc = "orthanc";
    private const string Ford = "fords-of-isen";
    private const string FreePeoplesCard = "base.fp.strategy.01";
    private const string DesperateBattle = "base.sh.strategy.22";

    private static MatchConfiguration Kings => new() { Kings = true };

    // Each scenario plays Kings alone unless it names Lords as well.
    public static IEnumerable<TestScenario> All() => Scenarios().Select(scenario =>
        scenario.Modules.Count > 0 ? scenario : scenario with { Modules = [ExpansionModules.Kings] });

    private static IEnumerable<TestScenario> Scenarios() =>
    [
        new("kings-ugluk-entry", Group, "Uglúk beside the revealed Fellowship",
            "Rohan is not at war, but the Fellowship has been revealed in Hollin: with a Muster die Uglúk enters with an " +
            "Isengard army, or in Hollin or a free region next to it.",
            Side.Shadow, UglukEntry),
        new("kings-mirkwood-lord-of-the-bats", Group, "The Shadow of Mirkwood's Lord of the Bats",
            "The North's army attacks the Shadow of Mirkwood and two Sauron Regulars; the Shadow committed The Shadow is " +
            "Moving (Swarm of Bats). Before the reveal the Shadow may give up the card's effects and his Leadership: +1 to the " +
            "Leader re-roll at initiative 4, and with Swarm of Bats the Free Peoples card is cancelled at 0.",
            Side.Shadow, (id, random) => NorthAttack(id, random, KingsChieftains.Mirkwood, ShadowStrategyCards02.Moving, false)),
        new("kings-brand-vs-swarm", Group, "Brand against the Swarm: initiative 0",
            "As in the Shadow of Mirkwood's scenario, with Brand Awakened among the attacking North: choose Lord of the Bats, then " +
            "after the reveal Heir of Bard is offered to the Free Peoples; both cancel at initiative 0 and the defender resolves " +
            "first, so the Swarm cancels the Free Peoples card before Brand's replacement acts.",
            Side.Shadow, (id, random) => NorthAttack(id, random, KingsChieftains.Mirkwood, ShadowStrategyCards02.Moving, true)),
        new("kings-serpent-relentless-assault", Group, "The Black Serpent's Red Wrath with Relentless Assault",
            "The North's army attacks the Black Serpent and two Sauron Regulars; the Shadow committed The King Is Revealed " +
            "(Relentless Assault). Before the reveal Red Wrath takes one hit on the Shadow's army and, with Relentless Assault " +
            "printed, adds +2 to the Combat roll instead of +1.",
            Side.Shadow, (id, random) => NorthAttack(id, random, KingsChieftains.Serpent, KingsCards03.King, false)),
        new("kings-ugluk-i-command", Group, "Uglúk's I Command",
            "Uglúk leads Isengard against the Rohan army at the Fords of Isen; the Shadow committed Return to Valinor. Before the " +
            "reveal I Command replaces the card's effects: both armies add +1 to the Combat roll and the Leader re-roll.",
            Side.Shadow, (id, random) => IsengardAttack(id, random, KingsCards03.Valinor)),
        new("kings-ugluk-desperate-battle", Group, "Uglúk's I Command with Desperate Battle",
            "Uglúk leads Isengard against the Rohan army at the Fords of Isen; the Shadow committed a card with Desperate Battle. " +
            "Before the reveal I Command replaces its effects, and with Desperate Battle printed only the Shadow adds +1.",
            Side.Shadow, (id, random) => IsengardAttack(id, random, DesperateBattle)),
        new("kings-ugluk-hunt", Group, "Uglúk's We March Day and Night",
            "The Fellowship stands in Hollin with a Nazgûl, Uglúk is next door in Moria and three dice wait in the Hunt Box: " +
            "move the Fellowship with the Character die, and every die the Shadow re-rolls adds +1.",
            Side.FreePeoples, UglukHunt),
        new("kings-council-chieftain-entry", Group, "A Council token brings in a Chieftain",
            "Lords with the Council of Rivendell and Kings: no Free Peoples nation is at war and the Fellowship is hidden, so no " +
            "Chieftain's condition holds; the Shadow's Council token with a Muster die still brings one in.",
            Side.Shadow, CouncilEntry) { Modules = [ExpansionModules.Kings, ExpansionModules.Lords] }
    ];

    private static MatchState UglukEntry(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.Shadow, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event, DieFace.Army)
        .Hunt(1, 0)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .FellowshipAt("hollin")
        .Reveal()
        .Act(Side.Shadow);

    // The North's army in Dale attacks two Sauron Regulars and a Chieftain next door; both cards are committed and the
    // Chieftain's substitution waits for the Shadow before the reveal.
    private static MatchState NorthAttack(string id, IRandomSource random, string chieftain, string shadowCard, bool brand)
    {
        var board = ScenarioBoard.Setup(id, random, Kings)
            .AtWar("north", "sauron")
            .Place("north", UnitKind.Regular, Dale, 2)
            .Place("sauron", UnitKind.Regular, Rhovanion, 2)
            .Dice(Side.FreePeoples, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event)
            .Hand(Side.FreePeoples, FreePeoplesCard)
            .Hand(Side.Shadow, shadowCard)
            .Change(state =>
            {
                Put(state, chieftain, Rhovanion);
                if (brand)
                    Awaken(state, SovereignCatalog.Brand, Dale);
            });
        board.Act(Side.FreePeoples);
        var state = KingsBattleSteps.Attack(board.State, Side.FreePeoples, Dale, Rhovanion);
        state = KingsBattleSteps.Commit(state, FreePeoplesCard, shadowCard);
        return KingsBattleSteps.Expect(state, Side.Shadow, "CombatReplacement");
    }

    // Uglúk and the Isengard army of Orthanc attack the Rohan army at the Fords; the Shadow's card is committed, the Free
    // Peoples play none, and I Command waits for the Shadow before the reveal.
    private static MatchState IsengardAttack(string id, IRandomSource random, string shadowCard)
    {
        var board = ScenarioBoard.Setup(id, random, Kings)
            .AtWar("isengard", "rohan")
            .Dice(Side.Shadow, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Hand(Side.Shadow, shadowCard)
            .Change(state => Put(state, KingsChieftains.Ugluk, Orthanc));
        board.Act(Side.Shadow);
        var state = KingsBattleSteps.Attack(board.State, Side.Shadow, Orthanc, Ford);
        state = KingsBattleSteps.Commit(state, "", shadowCard);
        return KingsBattleSteps.Expect(state, Side.Shadow, "CombatReplacement");
    }

    private static MatchState UglukHunt(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, Kings)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event)
        .Hunt(2, 1)
        .FellowshipAt("hollin")
        .Place("sauron", UnitKind.Nazgul, "hollin")
        .Change(state => Put(state, KingsChieftains.Ugluk, "moria"))
        .Act(Side.FreePeoples);

    // The Council setup through the engine; the Shadow holds the Advance Politics token, as in lords-council-token.
    private static MatchState CouncilEntry(string id, IRandomSource random)
    {
        var state = Actions(id, random, new() { Lords = true, Council = true, Kings = true });
        state.Lords!.Council!.GrantedTokens = [CouncilTokens.Politics];
        Faces(state, Side.Shadow, DieFace.Muster);
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }
}
