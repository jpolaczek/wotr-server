using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;
using static Wotr.Tests.LordsWeShallGetItTests;

namespace Wotr.Tests;

public class LordsWeShallGetItInteractionTests
{
    internal const string SecondEye = "base.hunt.standard.eye.1.2";

    [Fact]
    public void NenyaAndMithrilAffectSuccessiveReplacementTilesAfterTheShadowDecision()
    {
        var state = Window();
        LordsCharacterFixture.Put(state, LordsCharacters.Galadriel, "lorien");
        AddTile(state, HuntTests.Eye);
        state = Use(state, 0);
        Assert.Contains(NenyaReaction.Id, state.Pending!.PrivateChoices);
        AddTile(state, SecondEye);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id }, new SequenceRandom(0));
        Assert.Equal(SecondEye, HuntRuntime.Active(state)!.TileId);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == HuntTests.Eye).Location);
        Assert.Equal(new[] { FreePeoplesCharacterCards01.ReplaceReaction }, state.Pending!.PrivateChoices);
        var zero = "base.hunt.special-fp.0.0.1";
        AddTile(state, zero);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == SecondEye).Location);
        Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner);
    }

    [Fact]
    public void GollumMayRevealToReduceTheReplacementTile()
    {
        var state = Window(HuntTests.Eye, false);
        state.Fellowship.Hidden = true;
        AddTile(state, HuntTests.Three);
        state = Use(state, 0);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.gollum-reveal" });
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["hollin"] });
        Assert.Empty(state.Resolution.Frames);
        Assert.False(state.Fellowship.Hidden);
    }

    [Fact]
    public void StaffCanPreventADrawWhileTheSpecialEventWaitsForGollum()
    {
        var state = State(false);
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards02.Staff);
        state = Draw(state, HuntTests.Three);
        Assert.Equal("hunt.draw", state.Pending!.Window);
        var protectedState = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards02.StaffReaction });
        Assert.Contains(SmeagolRules.WeShallGetIt, protectedState.Player(Side.Shadow).Table);
        Assert.Equal(0, protectedState.Fellowship.Corruption);
        Assert.DoesNotContain(protectedState.Events, e => e.Kind == "HuntTileDrawn");
    }

    [Fact]
    public void ShelobIsReplacedBeforeItsDieIsRolledAndHealingTileUsesItsOwnValue()
    {
        var shelob = HuntPool.Definitions.Values.Single(t => t.Value == "die").Id;
        var state = Window(shelob, false);
        Assert.Null(HuntRuntime.Active(state)!.TileDie);
        Assert.True(Views().For(state, Side.Shadow).Hunt!.DamagePending);
        state.Fellowship.Corruption = 4;
        AddTile(state, "base.hunt.special-fp.-2.0.1");
        state = Use(state, 0);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntDamageDetermined" && e.Text.StartsWith(shelob));
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == shelob).Location);
    }

    [Theory]
    [InlineData(LordsFreePeoplesCards.Wind)]
    [InlineData(LordsFreePeoplesCards.SafePaths)]
    public void RemovalCardsDiscardTheSpecialEventWithoutDrawingOrSpendingIt(string card)
    {
        var state = State();
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = LordsCardsFixture.Play(state, card);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Single(state.Player(Side.Shadow).Discard, c => c == SmeagolRules.WeShallGetIt);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileReplaced");
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        state = Draw(state, HuntTests.Three);
        Assert.Equal("hunt.replace", state.Pending!.Window);
    }

    [Fact]
    public void ChallengeOfTheKingDoesNotCreateAHuntResolutionOrReplacementWindow()
    {
        var state = State();
        CompanionTests.Outside(state, CharacterRules.Strider, "minas-tirith");
        HuntTests.Pool(state, HuntTests.Three, HuntTests.Eye, SecondEye);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = LordsCardsFixture.Play(state, FreePeoplesCharacterCards03.Challenge, 0, 0, 0);
        Assert.Empty(state.Resolution.Frames);
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileReplaced");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MordorUsesOnlyTheReplacementAndCorruptionVictoryPrecedesMovement(bool corrupted)
    {
        var state = MordorReplacement(corrupted);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Equal(corrupted ? 12 : 0, state.Fellowship.Corruption);
        Assert.Equal(corrupted ? 4 : 5, state.Fellowship.MordorStep);
        Assert.Equal(corrupted ? VictoryCondition.RingCorrupted : VictoryCondition.RingDestroyed, state.Victory);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == "base.hunt.special-sh.3.0.1").Location);
    }

    internal static MatchState MordorReplacement(bool corrupted)
    {
        var state = State();
        state.Fellowship.MordorStep = 4;
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.Hidden = true;
        state.Fellowship.Corruption = corrupted ? 9 : 0;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        HuntTests.Pool(state, "base.hunt.special-sh.3.0.1");
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }, new SequenceRandom(0));
        AddTile(state, corrupted ? HuntTests.Three : "base.hunt.special-fp.0.0.1");
        return Use(state, 0);
    }

    [Fact]
    public void IgnoredGateEyeMayBeReplacedBeforeItsDamageRuleIsApplied()
    {
        var state = Window(HuntTests.Eye, false);
        HuntRuntime.Active(state)!.TileRule = HuntTileRule.IgnoreEyes;
        AddTile(state, HuntTests.Three);
        state = Use(state, 0);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileIgnored");
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Discard);
    }
}
