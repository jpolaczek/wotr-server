using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// WEB-01 (owner, 2026-11-04): the scenes the website's pictures show, also open to anyone in the Test scenarios screen.
// The war at its height: crowded regions on every front, two sieges and every character of the expansions; and a great
// siege of Minas Tirith at its Combat cards. The pieces stand where a long game could have brought them; nothing here is
// played through the engine except the attack that opens the siege.
public static class ShowcaseScenarios
{
    public const string Group = "Kings · Showcase";

    private const string Roster = "layout-full-roster";
    private const string Boromir = "base.character.boromir";
    private const string MinasTirith = "minas-tirith";
    private const string HelmsDeep = "helms-deep";
    private const string Osgiliath = "osgiliath";
    private const string Ford = "fords-of-isen";
    private const string Rhovanion = "northern-rhovanion";
    private const string Harondor = "west-harondor";
    private const string FreePeoplesCard = "base.fp.strategy.01";

    public static IEnumerable<TestScenario> All() =>
    [
        new("showcase-war", Group, "The war at its height",
            "Fate, Lords and Kings: Minas Tirith and Helm's Deep besieged, great armies at Osgiliath, the Fords of Isen, North " +
            "Ithilien, Northern Rhovanion and West Harondor, the Free Peoples answering from Lossarnach, Westemnet, Dale, the " +
            "Iron Hills and the Woodland Realm, every character of the expansions in play and the Fellowship in the Dead Marshes.",
            Side.FreePeoples, (id, _) => War(id))
        {
            Modules = [ExpansionModules.Kings, ExpansionModules.Lords, ExpansionModules.Fate]
        },
        new("showcase-siege", Group, "The great siege of Minas Tirith",
            "Lords and Kings: the Witch-king, Gothmog, two Nazgûl and nine Sauron and Southron units attack Boromir, the Awakened " +
            "Denethor and the Gondor army inside Minas Tirith. The Shadow chooses its Combat card.",
            Side.Shadow, Siege)
        {
            Modules = [ExpansionModules.Kings, ExpansionModules.Lords]
        }
    ];

    private static MatchState War(string id)
    {
        var board = ScenarioBoard.Of(TestScenarioCatalog.Find(Roster)!.Create(id));
        Gondor(board);
        Rohan(board);
        North(board);
        South(board);
        return board.FellowshipAt("dead-marshes", 3).Corruption(3).State;
    }

    private static void Gondor(ScenarioBoard board)
    {
        Reinforce(board, "gondor", UnitKind.Elite, MinasTirith, 1);
        Reinforce(board, "gondor", UnitKind.Leader, MinasTirith, 1);
        board.Clear(Osgiliath);
        March(board, "morannon", MinasTirith);
        board.Besiege(MinasTirith, "sauron", 0);
        Reinforce(board, "sauron", UnitKind.Elite, MinasTirith, 2);
        board.Relocate(LordsCharacters.WitchKing, MinasTirith);
        March(board, "minas-morgul", Osgiliath);
        Reinforce(board, "sauron", UnitKind.Regular, Osgiliath, 3);
        Reinforce(board, "sauron", UnitKind.Elite, Osgiliath, 1);
        board.Relocate(LordsCharacters.Gothmog, Osgiliath);
        March(board, "gorgoroth", "north-ithilien");
        March(board, "nurn", "north-ithilien");
        Reinforce(board, "sauron", UnitKind.Nazgul, "north-ithilien", 2);
        Reinforce(board, "gondor", UnitKind.Regular, "lossarnach", 4);
        Reinforce(board, "gondor", UnitKind.Elite, "lossarnach", 1);
        Reinforce(board, "gondor", UnitKind.Leader, "lossarnach", 1);
    }

    private static void Rohan(ScenarioBoard board)
    {
        board.Clear(Ford);
        Reinforce(board, "isengard", UnitKind.Regular, Ford, 5);
        Reinforce(board, "isengard", UnitKind.Elite, Ford, 2);
        board.Relocate(KingsChieftains.Ugluk, Ford);
        Reinforce(board, "rohan", UnitKind.Elite, HelmsDeep, 2);
        Reinforce(board, "rohan", UnitKind.Leader, HelmsDeep, 1);
        board.Besiege(HelmsDeep, "isengard", 0);
        Reinforce(board, "isengard", UnitKind.Regular, HelmsDeep, 3);
        Reinforce(board, "isengard", UnitKind.Elite, HelmsDeep, 2);
        Reinforce(board, "rohan", UnitKind.Regular, "westemnet", 3);
        Reinforce(board, "rohan", UnitKind.Elite, "westemnet", 1);
        Reinforce(board, "rohan", UnitKind.Leader, "westemnet", 1);
    }

    private static void North(ScenarioBoard board)
    {
        March(board, "north-rhun", Rhovanion);
        March(board, "south-rhun", Rhovanion);
        Reinforce(board, "southrons", UnitKind.Regular, Rhovanion, 2);
        Reinforce(board, "north", UnitKind.Regular, "dale", 3);
        Reinforce(board, "north", UnitKind.Elite, "dale", 1);
        Reinforce(board, "north", UnitKind.Leader, "dale", 1);
        Reinforce(board, "dwarves", UnitKind.Regular, "iron-hills", 2);
        Reinforce(board, "dwarves", UnitKind.Elite, "iron-hills", 1);
        Reinforce(board, "elves", UnitKind.Regular, "woodland-realm", 2);
        Reinforce(board, "elves", UnitKind.Elite, "woodland-realm", 2);
        Reinforce(board, "elves", UnitKind.Leader, "woodland-realm", 1);
        Reinforce(board, "sauron", UnitKind.Elite, "dol-guldur", 1);
        Reinforce(board, "sauron", UnitKind.Nazgul, "dol-guldur", 1);
    }

    private static void South(ScenarioBoard board)
    {
        March(board, "near-harad", Harondor);
        March(board, "far-harad", Harondor);
        board.Relocate(KingsChieftains.Serpent, Harondor);
        Reinforce(board, "gondor", UnitKind.Regular, "pelargir", 2);
        Reinforce(board, "gondor", UnitKind.Leader, "dol-amroth", 1);
    }

    private static MatchState Siege(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random, LordsBoard.WithKings))
            .AtWar("sauron", "southrons", "gondor")
            .Companion(Boromir, MinasTirith)
            .Place("gondor", UnitKind.Elite, MinasTirith)
            .Place("gondor", UnitKind.Leader, MinasTirith)
            .Change(state => Awaken(state, SovereignCatalog.Denethor, MinasTirith))
            .Besiege(MinasTirith, "sauron", 5)
            .Place("sauron", UnitKind.Elite, MinasTirith, 3)
            .Place("southrons", UnitKind.Elite, MinasTirith)
            .Place("sauron", UnitKind.Nazgul, MinasTirith, 2)
            .Enter(CharacterRules.WitchKing, MinasTirith)
            .Change(state => LordsBoard.Put(state, LordsCharacters.Gothmog, MinasTirith))
            .Hand(Side.FreePeoples, FreePeoplesCard)
            .Hand(Side.Shadow, ShadowStrategyCards02.Moving)
            .Change(state => DrawFromEachDeck(state, Side.Shadow));
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, MinasTirith, MinasTirith);
        return LordsBoard.CombatChoice(state, Side.Shadow);
    }

    // The top card of each of the side's decks, so the Combat card choice has a full hand to choose from.
    private static void DrawFromEachDeck(MatchState state, Side side)
    {
        var player = state.Players.Single(p => p.Side == side);
        foreach (var deck in player.Decks.Values.Where(deck => deck.Count > 0))
        {
            player.Hand.Add(deck[0]);
            deck.RemoveAt(0);
        }
        player.HandVersion++;
    }

    // As many of the pieces as the reserve still holds: the picture needs a crowd, not an exact count.
    private static void Reinforce(ScenarioBoard board, string nation, UnitKind kind, string region, int count)
    {
        foreach (var piece in board.State.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count))
            piece.Location = region;
    }

    // A whole army has moved between two regions earlier in the imagined game.
    private static void March(ScenarioBoard board, string from, string to)
    {
        foreach (var piece in board.State.Units.Where(u => u.Location == from))
            piece.Location = to;
    }
}
