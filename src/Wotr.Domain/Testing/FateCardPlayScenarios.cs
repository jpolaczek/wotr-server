using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-14: Fate card plays beside the six event scenarios (UI-266). Orcs Multiplying Again with Erebor in Free Peoples
// hands leaves one legal second region and one plan, which the server applies without a click (EXP-15 D8); with Kings of
// Middle-earth, Bardings and Dwarves of the Iron Hills take the slots of King Brand's Men and Dáin Ironfoot's Guard, and
// Brand and Dáin are not sovereigns (EXPANSIONS X03, KINGS pp. 18-19).
public static class FateCardPlayScenarios
{
    private const string KingsGroup = "Fate · Kings";

    private static MatchConfiguration Fate => new() { Fate = true };
    private static MatchConfiguration KingsAndFate => new() { Fate = true, Kings = true };

    public static IEnumerable<TestScenario> All() =>
    [
        new("fate-card-orcs-erebor-held", FateCardScenarios.Group, "Orcs Multiplying Again · Erebor held",
            "Muster and Event dice; Orcs Multiplying Again is in hand. The Free Peoples hold Erebor, so Mount Gundabad is " +
            "the only second region, and the full reserve leaves one plan: the card places three Sauron Regulars in Dol " +
            "Guldur and three in Mount Gundabad without another click.",
            Side.Shadow, OrcsEreborHeld) { Modules = [ExpansionModules.Fate] },
        new("fate-kings-dale-cards", KingsGroup, "Bardings and Dwarves with Kings",
            "Kings of Middle-earth with Fate of Erebor: Brand and Dáin are not sovereigns, and Bardings and Dwarves of the " +
            "Iron Hills take the places of King Brand's Men and Dáin Ironfoot's Guard. Both are in hand with two Muster " +
            "dice; a Free Peoples army holds Dale, so Dale itself and the regions next to it can recruit.",
            Side.FreePeoples, KingsDaleCards) { Modules = [ExpansionModules.Fate, ExpansionModules.Kings] }
    ];

    // The Orcs' event scenario with Erebor captured by a North army.
    private static MatchState OrcsEreborHeld(string id, IRandomSource random)
    {
        var card = CardCatalog.All[FateCards.Orcs];
        var setup = new CardEventSetup("The Free Peoples hold Erebor.",
            board => board.Conquer(Side.FreePeoples, "north", "erebor"));
        return CardEventSetups.Build(ScenarioBoard.Setup(id, random, Fate), card, setup);
    }

    // Both Fate cards of the Dale slots in a Kings and Fate game, paid by two Muster dice.
    private static MatchState KingsDaleCards(string id, IRandomSource random) => ScenarioBoard.Setup(id, random, KingsAndFate)
        .Dice(Side.FreePeoples, DieFace.Muster, DieFace.Muster, DieFace.Character, DieFace.Event)
        .Hand(Side.FreePeoples, FateCards.Bardings, FateCards.Dwarves)
        .Place("north", UnitKind.Regular, "dale")
        .Act(Side.FreePeoples);
}
