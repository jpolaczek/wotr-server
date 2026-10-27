using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-12: an event and a combat scenario for each of the thirteen Lords of Middle-earth cards of the decks, as TEST-09
// has for the base cards, in the client's Lords tab. The event scenario starts on turn 1 of a Lords game after its real
// setup, with the card in hand, a die of its event type and an Event die, on a board where every clause of the card acts
// (LordsCardEvents); the combat scenario starts at the card owner's Combat card choice in a battle where the combat
// condition holds and the effect has its target (LordsBattles). Follow-up scenarios start where a card's lasting or
// random part decides (LordsCardFollowUps): the Rings recovered, the Lore's changed die, the table cards discarded by the
// Free Peoples, the Gates at a declaration, We Shall Get It's replacement and Blade of Westernesse spending a re-roll hit.
public static class LordsCardScenarios
{
    public const string FreePeoplesGroup = "Lords · Free Peoples cards";
    public const string ShadowGroup = "Lords · Shadow cards";

    public static IEnumerable<TestScenario> All() =>
    [
        .. CardCatalog.Lords.Values.Where(c => c.Deck != "special").OrderBy(c => c.Id, StringComparer.Ordinal).SelectMany(Pair),
        .. LordsCardFollowUps.All()
    ];

    public static string EventId(CardDefinition card) => "lords-card-" + Slug(card.Title) + "-event";

    public static string CombatId(CardDefinition card) => "lords-card-" + Slug(card.Title) + "-combat";

    public static string Group(CardDefinition card) => card.Side == Side.FreePeoples ? FreePeoplesGroup : ShadowGroup;

    // A Lords scenario lists the Lords module first, so the client shows it in the Lords tab.
    public static TestScenario Lords(TestScenario scenario, params string[] others) =>
        scenario with { Modules = [ExpansionModules.Lords, .. others] };

    private static IEnumerable<TestScenario> Pair(CardDefinition card)
    {
        var setup = LordsCardEvents.For(card.Id);
        yield return Lords(new(EventId(card), Group(card), card.Title + " · Event", EventDescription(card, setup), card.Side,
            (id, random) => LordsCardEvents.Build(id, random, card, setup)));
        yield return Lords(new(CombatId(card), Group(card), card.Title + " · Combat: " + CardTextCatalog.CombatTitle(card.Id),
            LordsBattles.Describe(card), card.Side, (id, random) => LordsBattles.Build(id, random, card)));
    }

    private static string EventDescription(CardDefinition card, LordsCardSetup setup) =>
        $"{card.EventType} and Event dice; {card.Title} is in hand. {setup.Description}";

    private static string Slug(string title)
    {
        var words = new string(title.ToLowerInvariant().Where(c => c is not ('’' or '\'' or '!')).ToArray())
            .Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', words);
    }
}
