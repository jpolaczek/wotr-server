using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class KingsDeckTests
{
    private static readonly string[] Replaced =
    [
        "fp.character.23", "fp.strategy.08", "fp.strategy.16", "fp.strategy.19", "fp.strategy.22", "fp.strategy.24",
        "sh.strategy.01", "sh.strategy.03", "sh.strategy.05", "sh.strategy.06", "sh.strategy.18"
    ];

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void FactoryShufflesCompleteDecksWithElevenReplacementsEightAdditionsAndFateExclusions(bool kings, bool lords, bool fate)
    {
        var configuration = new MatchConfiguration { Kings = kings, Lords = lords, Fate = fate };
        var total = 96 + (kings ? 8 : 0) + (lords ? 12 : 0);
        var state = MatchFactory.Create("deck-test", new SequenceRandom(new int[total - 4]), configuration);
        var ids = state.Players.SelectMany(p => p.Decks.Values).SelectMany(c => c).ToArray();
        Assert.Equal(total, ids.Length);
        Assert.Equal(total, ids.Distinct().Count());
        Assert.Equal(total - 4, state.RandomSamples.Count);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, ids);
        Assert.Equal(kings ? fate ? 17 : 19 : 0, ids.Count(id => id.StartsWith("kings.")));
        foreach (var slot in Replaced)
        {
            var replaced = kings && !(fate && slot is "fp.strategy.19" or "fp.strategy.22");
            Assert.Equal(replaced, ids.Contains("kings." + slot));
            var fateReplacement = fate && slot is "fp.strategy.19" or "fp.strategy.22";
            Assert.Equal(!replaced && !fateReplacement, ids.Contains("base." + slot));
        }
        foreach (var player in state.Players)
        {
            Assert.Empty(player.Hand);
            Assert.Equal(24 + (kings ? 2 : 0) + (lords ? 4 : 0), player.Decks["character"].Count);
            Assert.Equal(24 + (kings ? 2 : 0) + (lords ? 2 : 0), player.Decks["strategy"].Count);
            foreach (var card in player.Decks.Values.SelectMany(c => c))
            {
                Assert.Equal(player.Side, CardCatalog.All[card].Side);
                Assert.Contains(card, EffectCatalog.All.Events.Keys);
                Assert.Contains(card, EffectCatalog.All.Combats.Keys);
            }
        }
        Assert.Equal(!lords, ids.Contains(ShadowCharacterCards03.Balrog));
        configuration.Kings = !kings;
        Assert.Equal(kings, state.Configuration.Kings);
    }

    [Fact]
    public void DisabledKingsPreservesPreviousBaseAndLordsShuffleOrder()
    {
        var board = BoardCatalog.LoadBase();
        foreach (var lords in new[] { false, true })
        {
            var state = MatchFactory.Create("unchanged", new LastIndexRandom(), new() { Lords = lords });
            var oldCards = lords ? CardCatalog.Base.Values.Where(c => c.Id != ShadowCharacterCards03.Balrog)
                .Concat(CardCatalog.Lords.Values.Where(c => c.Deck != "special")).Select(c => c.Id).ToArray() : board.Cards.ToArray();
            foreach (var player in state.Players)
                foreach (var deck in player.Decks)
                    Assert.Equal(oldCards.Where(id => CardCatalog.All[id].Side == player.Side && CardCatalog.All[id].Deck == deck.Key), deck.Value);
        }
    }

    [Fact]
    public void CompleteDeckPersistsItsOrderAndDrawDoesNotReshuffleOrExposeTheDeck()
    {
        var state = KingsFixture.Actions();
        var original = StateCodec.Write(state);
        state = StateCodec.Copy(state);
        Assert.Equal(original, StateCodec.Write(state));
        var deck = state.Player(Side.FreePeoples).Decks["strategy"].ToArray();
        var die = KingsFixture.Die(state);
        die.Face = DieFace.Event;
        state = KingsFixture.Do(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "strategy" });
        Assert.Equal(new[] { deck[0] }, state.Player(Side.FreePeoples).Hand);
        Assert.Equal(deck.Skip(1), state.Player(Side.FreePeoples).Decks["strategy"]);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
        Assert.DoesNotContain(deck[0], KingsFixture.Json(state, Side.Shadow));
        foreach (var id in deck.Skip(1))
            Assert.DoesNotContain(id, KingsFixture.Json(state, Side.FreePeoples));
    }

    private sealed class LastIndexRandom : IRandomSource
    {
        public int Next(int upperBound) => upperBound - 1;
    }
}
