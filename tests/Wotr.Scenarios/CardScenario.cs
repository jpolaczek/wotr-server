using Wotr.Contracts;
using Wotr.Domain;

internal static class CardScenario
{
    public static MatchState Create(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Corruption = 2;
        // This scenario isolates card continuations; the optional Grey draw has its own character scenario.
        state.Fellowship.Guide = CharacterRules.Strider;
        foreach (var side in Enum.GetValues<Side>())
        {
            var dice = state.Dice.Where(d => d.Owner == side && d.Location == DieLocation.Unrolled).ToArray();
            for (var i = 0; i < dice.Length; i++)
            {
                dice[i].Location = DieLocation.Available;
                dice[i].Face = i < 2 ? DieFace.Event : i == 2 ? DieFace.Muster : DieFace.Character;
            }
        }
        Hand(state, Side.FreePeoples, ["base.fp.strategy.15", "base.fp.strategy.08", "base.fp.character.12",
            "base.fp.character.19", "base.fp.character.22", "base.fp.strategy.24"]);
        Hand(state, Side.Shadow, ["base.sh.strategy.05", "base.sh.character.24"]);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    private static void Hand(MatchState state, Side side, string[] cards)
    {
        var player = state.Player(side);
        foreach (var id in player.Hand)
            player.Decks[id.Split('.')[2]].Add(id);
        player.Hand.Clear();
        foreach (var id in cards)
        {
            if (!player.Decks[id.Split('.')[2]].Remove(id))
                throw new InvalidOperationException("Missing scenario card " + id);
            player.Hand.Add(id);
        }
    }
}
