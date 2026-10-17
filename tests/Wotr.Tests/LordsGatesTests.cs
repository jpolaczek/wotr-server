using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class LordsGatesTests
{
    [Fact]
    public void DeclarationMustResolveGatesBeforeEnteringMordor()
    {
        var state = Declaration("dagorlad");
        HuntTests.Pool(state, HuntTests.Three);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["dagorlad", "morannon"] });
        LordsFixture.Reject(state, CommandKind.EnterMordor, new());
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = LordsShadowCards.GatesReaction + ".declaration" }, new SequenceRandom(0));
        LordsFixture.Reject(state, CommandKind.EnterMordor, new());
        state = LordsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        state = LordsFixture.Do(state, CommandKind.EnterMordor);
        Assert.Equal(0, state.Fellowship.MordorStep);
        Assert.Equal(3, state.Fellowship.Corruption);
    }

    [Fact]
    public void GatesAndFireAndShadowKeepSeparateHuntsBeforeOrdinaryStrongholdHunt()
    {
        var state = Declaration();
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria");
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three);
        FellowshipFlow.BeginReveal(state, "test", FellowshipReturn.Decision);
        state = LordsFixture.Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] });
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = LordsShadowCards.GatesReaction }, new SequenceRandom(0, 0));
        Assert.True(HuntRuntime.Active(state)!.FireAndShadow);
        Assert.Equal(LordsCharacters.Balrog, HuntRuntime.Active(state)!.SourceId);
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
        state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location = "pool";
        state = LordsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(0));
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
    }

    internal static MatchState Declaration(string origin = "moria")
    {
        var state = LordsShadowCardsFixture.Play(LordsShadowCardsFixture.State(), LordsShadowCards.Gates);
        state.Fellowship.RegionId = origin;
        state.Fellowship.Progress = 2;
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        return state;
    }

    [Theory]
    [InlineData("hollin", "moria")]
    [InlineData("moria", "hollin")]
    [InlineData("moria", "moria")]
    public void EnterLeaveAndStationaryDeclarationOfferOptionalAdditionalHunt(string from, string to)
    {
        var state = Declaration(from);
        var route = from == to ? new List<string> { from } : [from, to];
        HuntTests.Pool(state, HuntTests.Three);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = route });
        Assert.Equal("fellowship.declared", state.Pending!.Window);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = LordsShadowCards.GatesReaction + ".declaration" }, new SequenceRandom(0));
        Assert.False(HuntRuntime.Active(state)!.Reveal);
        state = LordsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.DoesNotContain(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConqueredStrongholdDoesNotTriggerAndDeclineKeepsTheCard(bool conquered)
    {
        var state = Declaration("hollin");
        if (conquered)
            state.Control["moria"] = Side.FreePeoples;
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["hollin", "moria", "dimrill-dale"] });
        if (!conquered)
            state = LordsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Contains(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void RevealedRouteIgnoresGatesEyeButStillDrawsTheStrongholdTileLast()
    {
        var state = Declaration("moria");
        HuntTests.Pool(state, HuntTests.Eye, HuntTests.Three);
        FellowshipFlow.BeginReveal(state, "test", FellowshipReturn.Decision);
        state = LordsFixture.Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] });
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = LordsShadowCards.GatesReaction }, new SequenceRandom(0, 0));
        Assert.Equal("moria", HuntRuntime.Active(state)!.SourceId);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GatesAndChiefCanBothRespondOnceInEitherOrder(bool chiefFirst)
    {
        var state = Declaration();
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "dol-guldur");
        HuntTests.Pool(state, HuntTests.Eye);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria", "hollin"] });
        var gates = LordsShadowCards.GatesReaction + ".declaration";
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = chiefFirst ? ChiefReactions.Follow : gates }, new SequenceRandom(chiefFirst ? [] : [0]));
        Assert.Equal("fellowship.declared", state.Pending!.Window);
        var options = LordsFixture.Views().For(state, Side.Shadow).Options.Single(o => o.Kind == CommandKind.RespondEffect);
        Assert.DoesNotContain(chiefFirst ? ChiefReactions.Follow : gates, options.EligibleIds);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = chiefFirst ? gates : ChiefReactions.Follow }, new SequenceRandom(chiefFirst ? [0] : []));
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Equal("hollin", CharacterRules.WitchKingInPlay(state)!.Location);
        Assert.DoesNotContain(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
    }
}
