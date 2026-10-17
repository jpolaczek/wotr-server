using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class BattleWindowScenario
{
    internal static MatchState Create(IRandomSource random, bool relief = false)
    {
        var state = BattleScenario.Field(random);
        var source = relief ? "lossarnach" : "minas-tirith";
        var target = relief ? "minas-tirith" : "lossarnach";
        if (relief)
            foreach (var unit in state.Units.Where(u => u.Location is "minas-tirith" or "lossarnach"))
                unit.Location = unit.Location == "minas-tirith" ? source : target;
        var rear = state.Units.First(u => u.Nation == "rohan" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        rear.Location = source;
        var companion = CharacterCatalog.Companion(state, "base.character.gimli");
        state.Fellowship.Companions.Remove(companion.Id);
        companion.Location = source;
        state.Characters.Add(companion);
        if (relief)
            foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(5))
            {
                unit.Location = target;
                unit.Garrison = true;
            }
        foreach (var side in Enum.GetValues<Side>())
        {
            var player = state.Player(side);
            foreach (var id in player.Hand.ToArray())
                player.Decks[CardCatalog.All[id].Deck].Add(id);
            player.Hand.Clear();
            for (var number = 1; number <= 6; number++)
            {
                var id = $"base.{(side == Side.FreePeoples ? "fp" : "sh")}.strategy.{number:00}";
                foreach (var deck in player.Decks.Values) deck.Remove(id);
                player.Hand.Add(id);
            }
        }
        return state;
    }
}
