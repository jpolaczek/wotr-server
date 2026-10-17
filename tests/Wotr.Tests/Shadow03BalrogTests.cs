using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class Shadow03BalrogTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeclarationResolvesHealingAndExpiryBeforeBalrogAndIgnoresSettlementReveal(bool captured)
    {
        var state = Declaration(captured: captured);
        Table(state, Worn);
        Table(state, Crebain);
        Pool(state, RevealOne, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria", "dimrill-dale", "lorien"] });
        Assert.Equal(captured ? 3 : 2, state.Fellowship.Corruption);
        Assert.Equal(captured, state.Player(Side.Shadow).Table.Contains(Worn));
        Assert.Equal(captured, state.Player(Side.Shadow).Table.Contains(Crebain));
        Assert.Equal("fellowship.declared", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = BalrogReaction + ".declaration" }, new SequenceRandom(0));
        Assert.False(HuntRuntime.Active(state)!.Reveal);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(captured ? 4 : 3, state.Fellowship.Corruption);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Table);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GollumCannotRevealInUnconqueredDeclaredSanctuary(bool captured)
    {
        var state = Declaration(captured: captured);
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        Pool(state, HuntTests.Three, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria", "dimrill-dale", "lorien"] });
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = BalrogReaction + ".declaration" }, new SequenceRandom(0));
        if (captured)
        {
            Assert.Equal("hunt.guide", state.Pending!.Window);
            state = Do(state, CommandKind.DeclineReaction);
        }
        Assert.Equal(captured ? 6 : 5, state.Fellowship.Corruption);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BalrogRevealUsesWholeDeclarationRouteBeforeStrongholdTiles(bool capturedMoria)
    {
        var state = Declaration("hollin");
        if (capturedMoria)
            state.Control["moria"] = Side.FreePeoples;
        Pool(state, RevealOne, HuntTests.Eye);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria", "hollin"] });
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = BalrogReaction + ".declaration" }, new SequenceRandom(0));
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(capturedMoria ? [] : [0]));
        Assert.False(state.Fellowship.Hidden);
        Assert.Equal("hollin", state.Fellowship.RegionId);
        Assert.Equal(0, state.Fellowship.Progress);
        Assert.Equal(capturedMoria ? 1 : 2, state.RandomSamples.Count);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.HuntTiles, t => t.Location is "drawn" or "return-after-action");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StationaryMoriaTriggersBalrogBeforeAnyStrongholdTile(bool declaration)
    {
        var state = Declaration("moria");
        Pool(state, HuntTests.Eye, HuntTests.Three);
        if (declaration)
            state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] });
        else
        {
            FellowshipFlow.BeginReveal(state, "test", FellowshipReturn.Decision);
            state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] });
        }
        Assert.Equal(declaration ? "fellowship.declared" : "fellowship.revealed", state.Pending!.Window);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.DeclineReaction, new(), new SequenceRandom(declaration ? [] : [0]));
        Assert.Contains(Balrog, state.Player(Side.Shadow).Table);
        Assert.Equal(declaration ? 0 : 1, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("morannon", "dagorlad")]
    [InlineData("minas-morgul", "north-ithilien")]
    public void GatewayDeclarationFinishesBalrogBeforeOptionalMordorEntryAndPoolRefill(string gateway, string approach)
    {
        var state = Declaration(gateway);
        state.Fellowship.Progress = 7;
        Pool(state, HuntTests.Three, HuntTests.Eye);
        const string special = "base.hunt.special-sh.die.0.1";
        state.HuntTiles.Single(t => t.Id == special).Location = "set-aside";
        HuntPool.PrepareSpecial(state, special);
        state = Do(state, CommandKind.DeclareFellowship, new()
        {
            Route = ["moria", "dimrill-dale", "north-anduin-vale", "dol-guldur", "eastern-brown-lands", "eastern-emyn-muil", approach, gateway]
        });
        Assert.Equal("fellowship.declared", state.Pending!.Window);
        Assert.Null(state.Fellowship.MordorStep);
        Assert.Equal("prepared", state.HuntTiles.Single(t => t.Id == special).Location);
        Assert.DoesNotContain(HuntTests.Views().For(state, Side.FreePeoples).Options, o => o.Kind == CommandKind.EnterMordor);
        Reject(state, CommandKind.EnterMordor, new());
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = BalrogReaction + ".declaration" }, new SequenceRandom(0));
        Assert.False(HuntRuntime.Active(state)!.Mordor);
        Reject(state, CommandKind.EnterMordor, new());
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(6, state.Fellowship.Corruption);
        Assert.Equal("prepared", state.HuntTiles.Single(t => t.Id == special).Location);
        Assert.True(MordorRules.CanEnter(state));
        state = Do(state, CommandKind.EnterMordor);
        Assert.Equal(0, state.Fellowship.MordorStep);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == special).Location);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
    }

    [Fact]
    public void IgnoredEyeNeverRevealsButIndependentStrongholdHuntStillFollowsRevelation()
    {
        var state = Declaration("moria");
        FellowshipFlow.BeginReveal(state, "test", FellowshipReturn.Decision);
        Pool(state, HuntTests.Eye, HuntTests.Three);
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] });
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = BalrogReaction }, new SequenceRandom(0, 0));
        Assert.Equal("moria", HuntRuntime.Active(state)!.SourceId);
        Assert.Equal(3, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Table);
    }
}
