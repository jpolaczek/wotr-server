using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class MatchFactory
{
    public static MatchState Create(string id, IRandomSource entropy, MatchConfiguration? configuration = null)
    {
        var board = BoardCatalog.LoadBase();
        var state = new MatchState { Id = id };
        if (configuration is not null)
            state.Configuration = System.Text.Json.JsonSerializer.Deserialize<MatchConfiguration>(
                System.Text.Json.JsonSerializer.Serialize(configuration, BoardCatalog.Json), BoardCatalog.Json)!;
        if (ConfigurationRules.Conflict(state.Configuration) is { } conflict)
            throw new ArgumentException($"Invalid match configuration: {conflict}", nameof(configuration));
        var configuredCards = DeckComposition.Build(state.Configuration);
        var rng = new RecordingRandom(entropy, state);
        state.Units = board.Units.Select(u => new UnitState { Id = u.Id, Nation = u.Nation, Kind = u.Kind, Location = u.Location }).ToList();
        state.Nations = board.Nations.Select(n => new NationState { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }).ToList();
        state.Control = BoardRules.SetupRegions(state.Configuration).ToDictionary(r => r.Id, r => r.OriginalOwner);
        FateSetup.Apply(state);
        state.HuntTiles = board.HuntTiles.Select(t => new TileState { Id = t.Id, Location = t.Pool == "standard" ? "pool" : "set-aside" }).ToList();
        AddPlayers(state, board, configuredCards, rng);
        for (var i = 1; i <= 3; i++)
            state.Rings.Add(new() { Id = $"base.ring.{i}", Owner = Side.FreePeoples });
        return state;
    }

    private static void AddPlayers(MatchState state, BoardData board,
        ILookup<(Side Side, string Deck), CardDefinition> configuredCards, IRandomSource rng)
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            var player = new PlayerState { Side = side };
            state.Players.Add(player);
            foreach (var deck in new[] { "character", "strategy" })
            {
                var cards = configuredCards[(side, deck)].Select(c => c.Id).ToList();
                Shuffle(cards, rng);
                player.Decks[deck] = cards;
                // BASE p14 step 8 only shuffles. First cards arrive in phase 1 (p18).
            }
            var definition = board.Dice.Single(d => d.Side == side);
            for (var i = 1; i <= definition.MaximumCount; i++)
                state.Dice.Add(new()
                {
                    Id = $"{side}.standard.{i}",
                    Owner = side,
                    Location = i <= definition.InitialCount ? DieLocation.Unrolled : DieLocation.Removed
                });
        }
    }

    private static void Shuffle(List<string> cards, IRandomSource random)
    {
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
