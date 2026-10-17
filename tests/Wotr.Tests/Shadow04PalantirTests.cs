using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04PalantirTests
{
    [Fact]
    public void PlayingPalantirWithEventDoesNotTriggerItselfAndSarumanDeathDiscardsIt()
    {
        var state = State(Palantir);
        var cost = EventCost(state, Palantir);
        cost.CardCost = DieFace.Event;
        state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, cost);
        Assert.Contains(Palantir, state.Player(Side.Shadow).Table);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        Assert.Empty(state.Resolution.Frames);
        CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
        ActiveEffects.CheckConditions(state);
        Assert.DoesNotContain(Palantir, state.Player(Side.Shadow).Table);
        Assert.Contains(Palantir, state.Player(Side.Shadow).Discard);
    }

    [Theory]
    [InlineData(DieFace.Character, false)]
    [InlineData(DieFace.Event, false)]
    [InlineData(DieFace.Event, true)]
    public void BonusWaitsForWholeEventUsesPrintedFaceAndRequiresSurvivingSource(DieFace face, bool killSaruman)
    {
        var state = State(ShadowCharacterCards03.Lidless);
        Table(state, Palantir);
        var cost = EventCost(state, ShadowCharacterCards03.Lidless);
        cost.CardCost = face;
        state.Dice.Single(d => d.Id == cost.DieId).Face = face;
        var next = state.Player(Side.Shadow).Decks["strategy"][0];
        state = Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal("ActionDie", state.Pending!.Presentation);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        if (killSaruman)
            CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
        state = Do(state, CommandKind.ChooseEffect, new());
        if (face == DieFace.Event && !killSaruman)
        {
            Assert.Equal("Deck", state.Pending!.Presentation);
            Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
            Reject(state, CommandKind.ChooseEffect, new() { Ids = [next] });
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
            Assert.Equal(next, Assert.Single(state.Player(Side.Shadow).Hand));
            Assert.DoesNotContain(new Projection().For(state, Side.FreePeoples).History, e => e.Text.Contains(next, StringComparison.Ordinal));
        }
        else
            Assert.Empty(state.Player(Side.Shadow).Hand);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovalPaysChosenDieAndOptionalRingAtomically(bool useRing)
    {
        var state = State();
        Table(state, Palantir);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, useRing ? DieFace.Army : DieFace.WillOfTheWest);
        payload.RingId = useRing ? state.Rings.First(r => r.Owner == Side.FreePeoples).Id : "";
        var option = PalantirRemoval.Option(state, Side.FreePeoples)!;
        Assert.Equal(!useRing, option.DiceWithoutRing.Contains(payload.DieId));
        Assert.Null(PalantirRemoval.Option(state, Side.Shadow));
        state = Do(state, CommandKind.DiscardPalantir, payload);
        Assert.DoesNotContain(Palantir, state.Player(Side.Shadow).Table);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Equal(useRing, state.Player(Side.FreePeoples).RingUsedThisTurn);
        if (useRing)
            Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == payload.RingId).Owner);
    }

    [Theory]
    [InlineData("no-ring")]
    [InlineData("foreign-ring")]
    [InlineData("used-ring")]
    [InlineData("spent-die")]
    [InlineData("foreign-die")]
    [InlineData("not-on-table")]
    public void IllegalPalantirRemovalDoesNotSpendAnything(string reason)
    {
        var state = State();
        Table(state, Palantir);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, DieFace.Army);
        payload.RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
        switch (reason)
        {
            case "no-ring":
                payload.RingId = "";
                break;
            case "foreign-ring":
                state.Rings.Single(r => r.Id == payload.RingId).Owner = Side.Shadow;
                break;
            case "used-ring":
                state.Player(Side.FreePeoples).RingUsedThisTurn = true;
                break;
            case "spent-die":
                state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
                break;
            case "foreign-die":
                payload.DieId = state.Dice.First(d => d.Owner == Side.Shadow).Id;
                break;
            case "not-on-table":
                TableCardRules.Discard(state, Side.Shadow, Palantir);
                break;
        }
        Reject(state, CommandKind.DiscardPalantir, payload);
    }

    [Fact]
    public void EmptyDecksAndHandLimitUseOrdinaryDrawRules()
    {
        var state = State(ShadowCharacterCards03.Lidless);
        Table(state, Palantir);
        foreach (var card in state.Player(Side.Shadow).Decks["character"].Take(6).ToList())
            EventTests.GiveCard(state, Side.Shadow, card);
        var cost = EventCost(state, ShadowCharacterCards03.Lidless);
        cost.CardCost = DieFace.Event;
        state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, cost);
        state = Do(state, CommandKind.ChooseEffect, new());
        state.Player(Side.Shadow).Decks["strategy"].Clear();
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["character"] });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(7, state.Player(Side.Shadow).Hand.Count);
        state = Do(state, CommandKind.DiscardCards, new() { Ids = [state.Player(Side.Shadow).Hand[0]] });
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(6, state.Player(Side.Shadow).Hand.Count);
    }
}
