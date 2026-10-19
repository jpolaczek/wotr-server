using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

// EXP-03: a copy of a state in which only what the viewer must not know has changed: the opponent's hand card against
// its deck, every deck's order, the stored random samples and the opponent's hidden corruption tiles (while the viewer
// may not read them). The viewer's projection of the copy must equal the original's. EXP-10: while an effect lets the
// viewer look at the opponent's hand (Wind from the West), that hand is not a secret to them and stays as it is.
internal static class HiddenInformation
{
    public static MatchState Perturbed(MatchState state, Side viewer)
    {
        var changed = StateCodec.Copy(state);
        var opponent = changed.Player(TurnFlow.Other(viewer));
        var card = opponent.Hand.FindIndex(id => id != opponent.CommittedCombatCard);
        var inspected = changed.Resolution.Frames.Any(frame => frame.Effect?.InspectedHand == opponent.Side);
        if (!inspected && card >= 0 && opponent.Decks.TryGetValue(CardCatalog.All[opponent.Hand[card]].Deck, out var deck) &&
            deck.Count > 0)
            (opponent.Hand[card], deck[0]) = (deck[0], opponent.Hand[card]);
        foreach (var player in changed.Players)
            foreach (var hidden in player.Decks.Values)
                hidden.Reverse();
        foreach (var sample in changed.RandomSamples)
            sample.Value = (sample.Value + 1) % sample.ExclusiveMaximum;
        if (!Visibility.CanReadCorruption(changed, viewer, opponent.Side))
            opponent.HiddenCorruptionTiles.Reverse();
        return changed;
    }
}
