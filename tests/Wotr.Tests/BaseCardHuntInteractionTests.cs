using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class BaseCardHuntInteractionTests
{
    private const string Gimli = "base.character.gimli";
    private const string Merry = "base.character.merry";
    private const string Pippin = "base.character.pippin";

    [PostgresFact]
    public async Task PalantirWaitsForFoulThingWornNewGuideAndHobbitPlacementAcrossRestarts()
    {
        var state = WithPalantir(ShadowCharacterCards02.FoulThing);
        Table(state, ShadowCharacterCards03.Worn);
        Table(state, FreePeoplesCharacterCards01.Axe);
        var axe = state.ActiveEffects.Single(e => e.SourceId == FreePeoplesCharacterCards01.Axe);
        axe.ExpiresWhen = EffectExpiry.FellowshipCompanionsAbsent;
        axe.RequiredCompanions = [Gimli, "base.character.legolas"];
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Bilbo);
        state.Fellowship.Companions = [Gimli, Merry, Pippin];
        state.Fellowship.Guide = Gimli;
        Pool(state, HuntTests.Three, HuntTests.Eye);
        var cost = EventFaceCost(state, ShadowCharacterCards02.FoulThing);

        state = Do(state, CommandKind.PlayEvent, cost, new SequenceRandom(0));
        Assert.Equal("hunt.table", state.Pending!.Window);
        state = Do(state, CommandKind.DeclineReaction, random: new SequenceRandom(0));
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.DoesNotContain(FreePeoplesCharacterCards01.Axe, state.Player(Side.FreePeoples).Table);
        AssertAwaitingPalantir(state, cost.DieId);
        state = await RestartDecision(state, CommandKind.SelectGuide,
            new() { GuideId = Merry }, new SequenceRandom());
        Assert.Equal("hunt.casualty", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ShadowCharacterCards03.WornReaction });
        Assert.DoesNotContain(FreePeoplesCharacterCards01.Axe, Choices(state).EligibleIds);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [WornDiscardStep.RandomHand] }, new SequenceRandom(0));
        Assert.DoesNotContain(new Projection().For(state, Side.Shadow).History, e => e.Kind == "CardDiscarded");
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        AssertAwaitingPalantir(state, cost.DieId);

        state = await RestartDecision(state, CommandKind.SeparateCompanions,
            new() { Ids = [Merry], Route = ["hollin"] }, new SequenceRandom());
        Assert.Equal(Pippin, state.Fellowship.Guide);
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == Merry).Location);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal("Deck", state.Pending!.Presentation);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        AssertAwaitingPalantir(state, cost.DieId);
        var expected = state.Player(Side.Shadow).Decks["strategy"][0];
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            new() { Ids = ["strategy"] }, new SequenceRandom());
        Assert.Equal(expected, Assert.Single(state.Player(Side.Shadow).Hand));
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void WornRemovesStaffBeforeAdditionalStrongholdHuntAndPalantirStillWaits()
    {
        var state = WithPalantir(ShadowCharacterCards02.FoulThing);
        state.Fellowship.Progress = 1;
        Table(state, ShadowCharacterCards03.Worn);
        Table(state, FreePeoplesCharacterCards02.Staff);
        Pool(state, RevealOne, HuntTests.Three, HuntTests.Eye);
        var casualty = state.Fellowship.Companions.IndexOf(Merry);
        var cost = EventFaceCost(state, ShadowCharacterCards02.FoulThing);
        state = Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal("hunt.draw", state.Pending!.Window);
        state = Do(state, CommandKind.DeclineReaction, random: new SequenceRandom(0, casualty));
        state = ResolveGuide(state);
        Assert.Equal("hunt.casualty", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ShadowCharacterCards03.WornReaction });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [FreePeoplesCharacterCards02.Staff] });
        Assert.Contains(FreePeoplesCharacterCards02.Staff, state.Player(Side.FreePeoples).Discard);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [Merry], Route = ["hollin"] });
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        AssertAwaitingPalantir(state, cost.DieId);

        state = Do(StateCodec.Copy(state), CommandKind.RevealFellowship,
            new() { Route = ["hollin", "moria"] }, new SequenceRandom(0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal("moria", HuntRuntime.Active(state)!.SourceId);
        Assert.Equal(HuntDamageRule.Normal, HuntRuntime.Active(state)!.DamageRule);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(3, state.RandomSamples.Count);
        AssertAwaitingPalantir(state, cost.DieId);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal("Deck", state.Pending!.Presentation);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["character"] });
        Assert.Single(state.Player(Side.Shadow).Hand);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.HuntTiles, t => t.Location is "drawn" or "return-after-action");
    }

    [Fact]
    public void BaneReplacementVictoryCancelsPendingPalantirDrawAndReturnsHeldTile()
    {
        var state = WithPalantir(ShadowCharacterCards01.IsildursBane);
        state.Fellowship.Corruption = 9;
        Table(state, FreePeoplesCharacterCards01.Mithril);
        Pool(state, HuntTests.Eye, HuntTests.Three);
        var cost = EventFaceCost(state, ShadowCharacterCards01.IsildursBane);
        var deckSize = state.Player(Side.Shadow).Decks.Values.Sum(deck => deck.Count);
        state = Do(state, CommandKind.PlayEvent, cost, new SequenceRandom(0));
        Assert.Equal("hunt.replace", state.Pending!.Window);
        AssertAwaitingPalantir(state, cost.DieId);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(12, state.Fellowship.Corruption);
        Assert.Equal(Side.Shadow, state.Winner);
        Assert.Equal(VictoryCondition.RingCorrupted, state.Victory);
        Assert.Null(state.Pending);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        Assert.Equal(deckSize, state.Player(Side.Shadow).Decks.Values.Sum(deck => deck.Count));
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.DoesNotContain(state.HuntTiles, t => t.Location is "drawn" or "return-after-action");
    }

    [Theory]
    [InlineData(ShadowCharacterCards01.OrcPatrol)]
    [InlineData(ShadowCharacterCards01.IsildursBane)]
    [InlineData(ShadowCharacterCards02.FoulThing)]
    [InlineData(ShadowCharacterCards03.Breaking)]
    public void StaffCancelsTileRatherThanEventAndPalantirDrawsOnceWithoutRandomness(string card)
    {
        var state = WithPalantir(card);
        state.Fellowship.Hidden = false;
        Table(state, FreePeoplesCharacterCards02.Staff);
        Pool(state, HuntTests.Three);
        var cost = EventFaceCost(state, card);
        var companions = state.Fellowship.Companions.ToArray();
        state = Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal("hunt.draw", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards02.StaffReaction });
        Assert.Empty(state.RandomSamples);
        Assert.Equal(companions, state.Fellowship.Companions);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal("Deck", state.Pending!.Presentation);
        Assert.Contains(FreePeoplesCharacterCards02.Staff, state.Player(Side.FreePeoples).Discard);
        AssertAwaitingPalantir(state, cost.DieId);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        Assert.Single(state.Player(Side.Shadow).Hand);
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
    }

    private static MatchState WithPalantir(string card)
    {
        var state = BeforeEvent(card);
        Put(state, CharacterRules.Saruman, "orthanc");
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Palantir);
        return state;
    }

    private static CommandPayload EventFaceCost(MatchState state, string card)
    {
        var cost = Cost(state, DieFace.Event);
        cost.CardCost = DieFace.Event;
        cost.CardId = card;
        return cost;
    }

    private static void AssertAwaitingPalantir(MatchState state, string die)
    {
        Assert.Empty(state.Player(Side.Shadow).Hand);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == die).Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
    }
}
