using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class BattleTableCards
{
    public static bool Available(MatchState state, BattleState battle, Side side, string card) => side == Side.Shadow &&
        card == ShadowCharacterCards03.Balrog && state.Player(side).Table.Contains(card) &&
        new NearMoriaBattle().CanPlay(state, battle, side);

    public static IEnumerable<string> Candidates(MatchState state, BattleState battle, Side side) =>
        Available(state, battle, side, ShadowCharacterCards03.Balrog) && !battle.Force(side).Round.CardFromTable
            ? [ShadowCharacterCards03.Balrog] : [];

    public static int PublicHandCount(MatchState state, PlayerState player) => PublicHand(state, player).Count();

    // UI-361: the public hand by deck; the decks' backs differ (BASE 2.4.1 pp. 14, 29), so only a card's deck is public.
    // A card id the catalog does not know has no deck to show. The decks follow the order of the player's own decks, as
    // DeckCounts does, so equal hands give equal views whatever the order of their cards, also after a stored round trip.
    public static Dictionary<string, int> PublicHandDecks(MatchState state, PlayerState player)
    {
        var counts = PublicHand(state, player)
            .Select(card => CardCatalog.All.TryGetValue(card, out var definition) ? definition.Deck : "")
            .Where(deck => deck.Length > 0)
            .GroupBy(deck => deck, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var others = counts.Keys.Where(deck => !player.Decks.ContainsKey(deck)).OrderBy(deck => deck, StringComparer.Ordinal);
        return player.Decks.Keys.Where(counts.ContainsKey).Concat(others)
            .ToDictionary(deck => deck, deck => counts[deck], StringComparer.Ordinal);
    }

    // The hand as the other side sees it: its cards and, in a battle, a table card that may be played as a combat card.
    private static IEnumerable<string> PublicHand(MatchState state, PlayerState player)
    {
        var battle = BattleRuntime.Active(state);
        return battle is null ? player.Hand : player.Hand.Concat(Candidates(state, battle, player.Side));
    }

    public static void Reveal(MatchState state, BattleState battle)
    {
        foreach (var force in new[] { battle.Attacker, battle.Defender }.Where(f => f.Round.CardFromTable))
            TableCardRules.TakeForCombat(state, force.Side, force.Round.CardId);
    }
}
