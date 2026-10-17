using Wotr.Contracts;
using Wotr.Domain;

internal static class CardGestureScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var state = BattleScenario.Create(random)["field"];
        Give(state, Side.FreePeoples, "base.fp.strategy.01");
        Give(state, Side.Shadow, "base.sh.strategy.01");
        var scenarios = new Dictionary<string, MatchState>
        {
            ["ui18-combat"] = state,
            ["ui31-discard"] = Discard(random)
        };
        foreach (var source in new[] { "miniature", "preview" })
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 100, 125 })
                    scenarios[$"ui82-discard-{source}-{width}-{scale}"] = Discard(random);
        return scenarios;
    }

    private static MatchState Discard(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        foreach (var side in Enum.GetValues<Side>())
            while (state.Player(side).Hand.Count < 8)
                CardFlow.Draw(state, side, "character");
        state.Phase = GamePhase.RecoverDraw;
        TurnFlow.ContinueRecovery(state);
        return state;
    }

    private static void Give(MatchState state, Side side, string card)
    {
        var player = state.Player(side);
        foreach (var deck in player.Decks.Values)
            deck.Remove(card);
        if (!player.Hand.Contains(card))
            player.Hand.Add(card);
    }
}
