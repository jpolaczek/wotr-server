using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsPalantirTests
{
    internal static MatchState WithPalantir()
    {
        var state = Table(Palantir);
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    [Theory]
    [InlineData(DieFace.Muster)]
    [InlineData(DieFace.ArmyMuster)]
    public void ConvertsMusterToEventOnceWithoutChangingPhysicalDie(DieFace face)
    {
        var state = WithPalantir();
        var payload = Cost(state, KingsCards02.Welcome, face);
        payload.CardCost = DieFace.Event;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        payload.AbilityId = KingsPalantir.Conversion;
        Assert.Contains(EventRules.Costs(state, Side.Shadow, payload.CardId), c => c.AbilityId == KingsPalantir.Conversion);
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.Equal(state.Turn, state.Kings!.PalantirUsedTurn);
        Assert.Equal(face, state.Dice.Single(d => d.Id == payload.DieId).Face);
        TurnFlow.SelectNext(state, Side.Shadow);
        payload = Cost(state, KingsCards02.West, face);
        payload.CardCost = DieFace.Event;
        payload.AbilityId = KingsPalantir.Conversion;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Turn++;
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.Equal(state.Turn, state.Kings!.PalantirUsedTurn);
    }

    [Fact]
    public void ConversionPaysDrawWithExplicitAbilityAndSharesLimitWithEventPlay()
    {
        var state = WithPalantir();
        var cost = CardDrawCost.Options(state, Side.Shadow).First(c => c.AbilityId == KingsPalantir.Conversion);
        var payload = new CommandPayload { DieId = cost.DieId, Deck = "strategy" };
        LordsFixture.Reject(state, CommandKind.DrawCard, payload);
        payload.AbilityId = cost.AbilityId;
        var expected = state.Player(Side.Shadow).Decks["strategy"][0];
        state = Do(state, CommandKind.DrawCard, payload);
        Assert.Contains(expected, state.Player(Side.Shadow).Hand);
        Assert.Equal(state.Turn, state.Kings!.PalantirUsedTurn);
        Assert.Empty(KingsPalantir.Costs(state, Side.Shadow));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChoosingNativeMusterOrConvertedEventControlsOrthancDraw(bool convert)
    {
        var state = WithPalantir();
        state.Player(Side.Shadow).Table.Add(ShadowCharacterCards04.Palantir);
        var payload = Cost(state, Threats, DieFace.Muster);
        var hand = state.Player(Side.Shadow).Hand.Count;
        var costs = EventRules.Costs(state, Side.Shadow, Threats).Where(c => c.DieId == payload.DieId).ToArray();
        Assert.Equal(2, costs.Length);
        if (convert)
        {
            payload.CardCost = DieFace.Event;
            payload.AbilityId = KingsPalantir.Conversion;
        }
        state = Do(state, CommandKind.PlayEvent, payload);
        if (convert)
        {
            Assert.Equal("Deck", state.Pending!.Presentation);
            state = Choose(state, "strategy");
        }
        Assert.Equal(hand - 1 + (convert ? 1 : 0), state.Player(Side.Shadow).Hand.Count);
        Assert.Equal(convert ? state.Turn : (int?)null, state.Kings!.PalantirUsedTurn);
    }

    [Fact]
    public void ReplayWithinSameTurnDoesNotResetConversionAndWrongSourceCannotPay()
    {
        var state = WithPalantir();
        state.Kings!.PalantirUsedTurn = state.Turn;
        TableCardRules.Discard(state, Side.Shadow, Palantir);
        state = Play(state, Palantir);
        Assert.Empty(KingsPalantir.Costs(state, Side.Shadow));
        state.Turn++;
        var die = Die(state, Side.Shadow);
        die.Face = DieFace.Character;
        Assert.False(KingsPalantir.CanConvert(state, Side.Shadow, die));
        Assert.Empty(KingsPalantir.Costs(state, Side.FreePeoples));
        state.Configuration.Kings = false;
        Assert.Empty(KingsPalantir.Costs(state, Side.Shadow));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemovesOnlyBaradPalantirWithWillOrAnyDieAndRing(bool will)
    {
        var state = Table(Palantir);
        state.Player(Side.Shadow).Table.Add(ShadowCharacterCards04.Palantir);
        var die = Die(state);
        die.Face = will ? DieFace.WillOfTheWest : DieFace.Army;
        var ring = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
        var payload = new CommandPayload { DieId = die.Id, RingId = will ? "" : ring };
        if (!will)
            LordsFixture.Reject(state, CommandKind.DiscardBaradPalantir, new() { DieId = die.Id });
        state = Do(state, CommandKind.DiscardBaradPalantir, payload);
        Assert.Contains(Palantir, state.Player(Side.Shadow).Discard);
        Assert.Contains(ShadowCharacterCards04.Palantir, state.Player(Side.Shadow).Table);
        Assert.Equal(will ? Side.FreePeoples : Side.Shadow, state.Rings.Single(r => r.Id == ring).Owner);
    }

    [Fact]
    public void RingRemovalRespectsRingAndRulerTokenLimitsButWillStillWorks()
    {
        var state = Table(Palantir);
        var payload = new CommandPayload { DieId = Die(state).Id, RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id };
        state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        LordsFixture.Reject(state, CommandKind.DiscardBaradPalantir, payload);
        state.Player(Side.FreePeoples).RingUsedThisTurn = false;
        state.Kings!.RulerTokenUsedThisTurn = true;
        LordsFixture.Reject(state, CommandKind.DiscardBaradPalantir, payload);
        payload.RingId = "";
        Die(state).Face = DieFace.WillOfTheWest;
        state = Do(state, CommandKind.DiscardBaradPalantir, payload);
        Assert.DoesNotContain(Palantir, state.Player(Side.Shadow).Table);
    }
}
