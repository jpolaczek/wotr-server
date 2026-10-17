using Wotr.Contracts;
using Wotr.Domain;

internal static class MultiUseDiceScenario
{
    public static Dictionary<string, MatchState> Create(MatchState movement)
    {
        var basis = BaseBoardScenario.Create(movement)["board-politics"];
        var result = new Dictionary<string, MatchState>();
        foreach (var face in new[] { DieFace.ArmyMuster, DieFace.WillOfTheWest })
        {
            foreach (var action in new[] { "march", "recruit", "politics", "card", "card-alternate", "draw", "travel" })
            {
                if (face == DieFace.ArmyMuster && action is "draw" or "travel" or "card-alternate")
                    continue;
                var state = StateCodec.Copy(basis);
                state.Id = Guid.NewGuid().ToString("N");
                state.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army).Face = face;
                const string card = "base.fp.strategy.08";
                var player = state.Player(Side.FreePeoples);
                foreach (var deck in player.Decks.Values)
                    deck.Remove(card);
                if (!player.Hand.Contains(card))
                    player.Hand.Add(card);
                TurnFlow.SelectNext(state, Side.FreePeoples);
                result.Add("ui28-" + face + "-" + action, state);
            }
        }
        return result;
    }
}
