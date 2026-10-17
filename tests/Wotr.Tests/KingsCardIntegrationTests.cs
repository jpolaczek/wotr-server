using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCardIntegrationTests
{
    public static IEnumerable<object[]> Cards => CardCatalog.Kings.Keys.Select(id => new object[] { id });

    private static EffectCatalog ReversedCatalog()
    {
        var catalog = EffectCatalog.All;
        return new(catalog.Programs.Values.Where(p => p.Id != EffectCatalog.ReactionProgram).Reverse(),
            catalog.Events.Values.Reverse(), catalog.Reactions.Values.Reverse(), catalog.Combats.Values.Reverse());
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void EveryKingsEventPaysItsRulerDieAndResolvesIndependentlyOfCatalogRegistrationOrder(string card)
    {
        var side = CardCatalog.Kings[card].Side;
        var state = side == Side.Shadow ? KingsCards03Fixture.Ready(card) : State();
        if (side == Side.FreePeoples)
            Companion(state, Boromir, "pelargir");
        var payload = KingsCards03Fixture.EventCost(state, card, CardCatalog.Kings[card].EventType);
        var ruler = KingsDice.Ruler(state, side)!;
        ruler.Location = DieLocation.Available;
        ruler.Face = payload.CardCost;
        payload.DieId = ruler.Id;
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        var normal = LordsFixture.Engine().Execute(state, side, command, new SequenceRandom(0));
        var reversed = LordsFixture.Engine(ReversedCatalog()).Execute(state, side, command, new SequenceRandom(0));
        Assert.True(normal.Receipt.Accepted, normal.Receipt.Error);
        Assert.True(reversed.Receipt.Accepted, reversed.Receipt.Error);
        Assert.Equal(StateCodec.Write(normal.State), StateCodec.Write(reversed.State));
        Assert.Contains(normal.State.Dice.Single(d => d.Id == ruler.Id).Location,
            new[] { DieLocation.Resolving, DieLocation.Spent });
        Assert.DoesNotContain(card, normal.State.Player(side).Hand);
    }

    public static IEnumerable<object[]> HiddenRemovalCards
    {
        get
        {
            var cards = new[]
            {
                (KingsFreePeoplesCards01.Stewards, SovereignCatalog.Denethor),
                (KingsFreePeoplesCards01.Riders, SovereignCatalog.Theoden),
                (KingsFreePeoplesCards01.Brand, SovereignCatalog.Brand),
                (KingsCards02.Dain, SovereignCatalog.Dain),
                (KingsCards02.Thranduil, SovereignCatalog.Thranduil),
                (KingsCards02.Service, SovereignCatalog.Dain)
            };
            foreach (var (card, sovereign) in cards)
                foreach (var visible in new[] { false, true })
                    yield return new object[] { card, sovereign, visible };
        }
    }

    [Theory]
    [MemberData(nameof(HiddenRemovalCards))]
    public void HiddenTileReferencesRemainRandomAndExcludeKingsInBothVisibilityModes(string card, string sovereignId, bool visible)
    {
        var state = State();
        state.Configuration.VisibleCorruption = visible;
        Companion(state, Boromir, "pelargir");
        Tile(state, sovereignId);
        Tile(state, sovereignId, KingsTiles.One);
        var fp = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == sovereignId);
        Assert.Equal(visible, fp.Tiles.Any(t => t.TileId == One));
        if (card == KingsCards02.Service)
            foreach (var unit in state.Units.Where(u => u.Nation == "dwarves" && BoardRules.IsArmyUnit(u)))
                unit.Location = "casualties";
        state = Play(state, card);
        state = Choose(state, card == KingsCards02.Service ? sovereignId : "corruption", 0);
        Assert.Equal(new[] { KingsTiles.One }, Sovereign(state, sovereignId).Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == One).Location);
        Assert.Single(state.RandomSamples);
    }
}
