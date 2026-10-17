using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-09: two scenarios for each base-game card, its event and its combat card, so every card can be tried from the
// test menu. The event scenario starts on turn 1 with the card in hand, dice that pay for it and the board its condition
// needs (CardEventSetups); the combat scenario starts in a battle at the card owner's Combat card choice (CardBattles).
// The client shows the groups of each side in its own tab, read from the group text before " · ".
public static class CardScenarios
{
    public const string FreePeoplesTab = "Free Peoples cards";
    public const string ShadowTab = "Shadow cards";

    public static IEnumerable<TestScenario> All() => CardCatalog.Base.Values.OrderBy(card => card.Id, StringComparer.Ordinal).SelectMany(Pair);

    public static string EventId(string cardId) => "card-" + cardId + "-event";

    public static string CombatId(string cardId) => "card-" + cardId + "-combat";

    public static string Group(CardDefinition card) =>
        (card.Side == Side.FreePeoples ? FreePeoplesTab : ShadowTab) + " · " + (card.Deck == "character" ? "Character" : "Strategy");

    private static IEnumerable<TestScenario> Pair(CardDefinition card)
    {
        var setup = CardEventSetups.For(card.Id);
        yield return new(EventId(card.Id), Group(card), card.Title + " · Event", EventDescription(card, setup), card.Side,
            (id, random) => CardEventSetups.Build(id, random, card, setup));
        yield return new(CombatId(card.Id), Group(card), card.Title + " · Combat: " + CardTextCatalog.CombatTitle(card.Id),
            CardBattles.Describe(card), card.Side, (id, random) => CardBattles.Build(id, random, card));
    }

    private static string EventDescription(CardDefinition card, CardEventSetup setup) =>
        $"{card.EventType} and Event dice; {card.Title} is in hand. {setup.Description}";
}
