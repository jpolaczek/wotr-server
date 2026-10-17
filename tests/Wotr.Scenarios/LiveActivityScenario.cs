using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

// Starts before the first command; the clients play every recorded step against the real server.
internal static class LiveActivityScenario
{
    internal static MatchState Create(IRandomSource random)
    {
        var state = BattleScenario.Field(random);
        Put(state, "gondor", UnitKind.Elite, "minas-tirith", 2);
        Put(state, "rohan", UnitKind.Regular, "edoras", 2);
        foreach (var unit in state.Units.Where(u => BoardRules.Owner(u) == Side.Shadow))
            BoardRules.ReturnToReserve(unit);
        Put(state, "sauron", UnitKind.Elite, "north-ithilien", 6);
        Put(state, "isengard", UnitKind.Elite, "north-ithilien", 2);
        Put(state, "sauron", UnitKind.Nazgul, "north-ithilien", 1);
        foreach (var side in Enum.GetValues<Side>())
        {
            var player = state.Player(side);
            foreach (var id in player.Hand)
                player.Decks[CardCatalog.All[id].Deck].Add(id);
            player.Hand.Clear();
            var card = side == Side.FreePeoples ? "base.fp.strategy.01" : "base.sh.strategy.01";
            foreach (var deck in player.Decks.Values)
                deck.Remove(card);
            player.Hand.Add(card);
        }
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static void Put(MatchState state, string nation, UnitKind kind, string region, int count)
    {
        var figures = state.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count).ToArray();
        if (figures.Length != count)
            throw new InvalidOperationException("Not enough figures for the activity acceptance scenario.");
        foreach (var figure in figures)
            figure.Location = region;
    }
}
