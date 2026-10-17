using Wotr.Contracts;

namespace Wotr.Domain.Content;

public static class DeckComposition
{
    public static ILookup<(Side Side, string Deck), CardDefinition> Build(MatchConfiguration configuration)
    {
        var cards = CardCatalog.ConfiguredDeck(configuration).ToArray();
        Validate(configuration, cards);
        return cards.ToLookup(card => (card.Side, card.Deck));
    }

    // Only public definitions enter diagnostics. No match, hand or shuffled deck is accepted here.
    public static void Validate(MatchConfiguration configuration, IReadOnlyCollection<CardDefinition> cards)
    {
        var slots = new HashSet<string>(StringComparer.Ordinal);
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in cards)
        {
            var slot = card.Replaces.Length == 0 ? card.Id : card.Replaces;
            if (card.Deck == "special" || !CardCatalog.Enabled(configuration, card.Id))
                throw Conflict(configuration, slot, "card is not part of the configured decks");
            if (!identities.Add(card.Id) || !slots.Add(slot))
                throw Conflict(configuration, slot, "more than one card occupies this slot");
            if (card.Replaces.Length > 0 && !CardCatalog.Base.ContainsKey(slot))
                throw Conflict(configuration, slot, "replacement has no base slot");
        }
        foreach (var slot in CardCatalog.Base.Keys)
            if (!slots.Contains(slot))
                throw Conflict(configuration, slot, "base slot has no card");
        foreach (var side in Enum.GetValues<Side>())
            foreach (var deck in new[] { "character", "strategy" })
            {
                var expected = 24 + (configuration.Kings ? 2 : 0) +
                    (configuration.Lords ? deck == "character" ? 4 : 2 : 0);
                if (cards.Count(card => card.Side == side && card.Deck == deck) != expected)
                    throw Conflict(configuration, $"{side}.{deck}", "deck has an incorrect number of cards");
            }
    }

    private static ContentConflictException Conflict(MatchConfiguration config, string slot, string reason)
    {
        if (config.Kings && config.Fate && slot is "base.fp.strategy.19" or "base.fp.strategy.22")
            return new(slot, "X03", "KINGS pp.18–19", reason);
        if (config.Fate && CardCatalog.Fate.Values.Any(card => card.Replaces == slot))
            return new(slot, "F01", "FATE-WOTR018: Setup", reason);
        if (slot.StartsWith("lords.", StringComparison.Ordinal) || slot == "base.sh.character.17" && config.Lords)
            return new(slot, "L01", "LORDS p.3", reason);
        if (config.Kings)
            return new(slot, "K03", "KINGS p.19", reason);
        return new(slot, "B06", "BASE-2.4.1 pp.14,22–23", reason);
    }
}
