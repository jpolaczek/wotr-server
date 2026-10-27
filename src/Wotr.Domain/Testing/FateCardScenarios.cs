using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// UI-266: the six Fate of Erebor cards whose choices have their own objects on the board (separation on the Fellowship
// portraits, companions on the map, recruitment regions and reserve plans, the Monsters Roused branches). Each starts
// as a base card's event scenario (TEST-09) in a Fate game: the owner acts on turn 1 with the card in hand, a die of its
// event type and an Event die, and the board meets the card's condition. The client shows them in the Fate tab.
public static class FateCardScenarios
{
    public const string Group = "Fate · Cards";

    private const string Legolas = "base.character.legolas";

    private static MatchConfiguration Fate => new() { Fate = true };

    public static IEnumerable<TestScenario> All() =>
    [
        Card(FateCards.ThereAndBack, "Legolas waits in the Carrock, so the North, the Dwarves and the Elves advance after the separation.",
            board => board.Companion(Legolas, "carrock")),
        Card(FateCards.Book, "Gimli waits in the Iron Hills and Legolas in Ered Luin: move them, then the Dwarves go to war and recruit.",
            board => board.Companion(FateGimli.Figure, "iron-hills").Companion(Legolas, "ered-luin")),
        Card(FateCards.Bardings, "Dale holds a Free Peoples army, so Dale itself and the regions next to it can recruit.", DaleArmy),
        Card(FateCards.Dwarves, "Dale holds a Free Peoples army, so Dale itself and the regions next to it can recruit.", DaleArmy),
        Card(FateCards.Orcs, "Only four Sauron Regulars are left in the reserve: choose Mount Gundabad or Erebor and split them.",
            board => board.Change(state => Reserve(state, 4))),
        Card(FateCards.Monsters, "Only one Sauron Regular is left in the reserve: choose West or East and place what it allows.",
            board => board.Change(state => Reserve(state, 1)))
    ];

    public static string Id(CardDefinition card) => "fate-card-" + card.Title.ToLowerInvariant().Replace(' ', '-');

    private static TestScenario Card(string cardId, string condition, Action<ScenarioBoard> arrange)
    {
        var card = CardCatalog.All[cardId];
        var setup = new CardEventSetup(condition, arrange);
        return new(Id(card), Group, card.Title + " · Event", $"{card.EventType} and Event dice; {card.Title} is in hand. {condition}",
            card.Side, (id, random) => CardEventSetups.Build(ScenarioBoard.Setup(id, random, Fate), card, setup))
        {
            Modules = [ExpansionModules.Fate]
        };
    }

    // TEST-14: the Fate setup leaves Dale empty, so a North Regular stands there for the cards' "Dale itself" clause.
    private static void DaleArmy(ScenarioBoard board) => board.Place("north", UnitKind.Regular, "dale");

    private static void Reserve(MatchState state, int regulars)
    {
        var reserve = state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        foreach (var unit in reserve.Skip(regulars).ToList())
            unit.Location = "removed";
    }
}
