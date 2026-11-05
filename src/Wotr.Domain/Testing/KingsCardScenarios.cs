using System.Globalization;
using System.Text;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-13: the Kings cards' scenarios in the Kings tab, as the base cards have theirs (TEST-09): a combat scenario for each
// of the 19 cards at its owner's Combat card choice (KingsCardBattles), and an event scenario for each card whose event
// no decision scenario shows whole yet (KingsCardEvents). The event starts on turn 1 of a Kings game with the card in
// hand, a die of its event type and an Event die, on a board where every clause acts.
public static class KingsCardScenarios
{
    public const string FreePeoplesGroup = "Kings · Free Peoples cards";
    public const string ShadowGroup = "Kings · Shadow cards";

    private static MatchConfiguration Kings => new() { Kings = true };

    public static IEnumerable<TestScenario> All() =>
        CardCatalog.Kings.Values.OrderBy(card => card.Id, StringComparer.Ordinal).SelectMany(Scenarios);

    public static string EventId(CardDefinition card) => "kings-event-" + Slug(card.Title);

    public static string CombatId(CardDefinition card) => "kings-combat-" + Slug(card.Title);

    public static string Group(CardDefinition card) => card.Side == Side.FreePeoples ? FreePeoplesGroup : ShadowGroup;

    private static IEnumerable<TestScenario> Scenarios(CardDefinition card)
    {
        if (KingsCardEvents.Setups.TryGetValue(card.Id, out var setup))
            yield return Event(card, setup);
        yield return Combat(card);
    }

    private static TestScenario Event(CardDefinition card, KingsCardEvent setup)
    {
        var title = card.Title + " · Event" + (setup.Clause.Length > 0 ? ": " + setup.Clause : "");
        var board = new CardEventSetup(setup.Description, setup.Arrange);
        return new(EventId(card), Group(card), title, $"{card.EventType} and Event dice; {card.Title} is in hand. {setup.Description}",
            card.Side, (id, random) => CardEventSetups.Build(ScenarioBoard.Setup(id, random, Kings), card, board))
        {
            Modules = [ExpansionModules.Kings]
        };
    }

    private static TestScenario Combat(CardDefinition card) =>
        new(CombatId(card), Group(card), card.Title + " · Combat: " + CardTextCatalog.CombatTitle(card.Id), KingsCardBattles.Describe(card),
            card.Side, (id, random) => KingsCardBattles.Build(id, random, card))
        {
            Modules = [ExpansionModules.Kings]
        };

    // "Dáin Ironfoot’s Guard" becomes "dain-ironfoots-guard": letters without accents, digits and single dashes.
    private static string Slug(string title)
    {
        var text = new StringBuilder();
        foreach (var character in title.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark || character is '\'' or '’')
                continue;
            if (char.IsAsciiLetterOrDigit(character))
                text.Append(character);
            else if (text.Length > 0 && text[^1] != '-')
                text.Append('-');
        }
        return text.ToString().TrimEnd('-');
    }
}
