using Wotr.Contracts;

namespace Wotr.Domain;

public static class CardFlow
{
    public static void Draw(MatchState state, Side side, string deckName)
    {
        var player = state.Player(side);
        var deck = player.Decks[deckName];
        if (deck.Count == 0)
            return;
        var card = deck[0];
        deck.RemoveAt(0);
        HandRules.Add(player, card);
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "CardDrawn",
            OnlyFor = side,
            Activity = HistoryFacts.Card(state, side, ActionCardUse.Draw, card),
            Text = $"Drew {Content.CardCatalog.Title(card)}."
        });
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "CardsDrawn", Text = $"{side} drew a {deckName} card.",
            Activity = HistoryFacts.Card(state, side, ActionCardUse.Draw, deck: deckName)
        });
    }

    public static bool RequireDiscard(MatchState state, Side actor, DiscardContinuation continuation)
    {
        var player = state.Player(actor);
        if (player.Hand.Count <= 6)
            return false;
        state.AfterDiscard = continuation;
        state.Open(DecisionKind.DiscardCards, actor, [CommandKind.DiscardCards], player.Hand.Count - 6);
        state.Pending!.PrivateChoices = [.. player.Hand];
        return true;
    }

    public static void Discard(MatchState state, Side actor, IReadOnlyCollection<string> ids)
    {
        var player = state.Player(actor);
        foreach (var id in ids)
        {
            HandRules.Remove(player, id);
            player.Discard.Add(id);
        }
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "CardsDiscarded",
            Activity = HistoryFacts.Card(state, actor, ActionCardUse.Discard, count: ids.Count),
            Text = $"{actor} discarded {ids.Count} card(s)."
        });
        foreach (var id in ids)
            state.Events.Add(new()
            {
                Revision = state.Revision, Kind = "OwnExcessDiscard", OnlyFor = actor, Text = $"Discarded {Content.CardCatalog.Title(id)}.",
                Activity = HistoryFacts.Card(state, actor, ActionCardUse.Discard, id)
            });
        if (RequireDiscard(state, actor, state.AfterDiscard))
            return;
        if (state.AfterDiscard == DiscardContinuation.RecoverDraw)
            TurnFlow.ContinueRecovery(state);
        else if (state.AfterDiscard == DiscardContinuation.FinishAction)
            TurnFlow.FinishAction(state, actor);
        else if (state.AfterDiscard == DiscardContinuation.ResumeEffect)
        {
            state.AfterDiscard = DiscardContinuation.None;
            state.Pending = null;
        }
        else
            throw new InvalidOperationException("Discard requires a persisted continuation.");
    }
}
