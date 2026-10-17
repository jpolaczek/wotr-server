using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FateGimliHuntTests
{
    [Theory]
    [InlineData("base.hunt.standard.0.1.1", 0)]
    [InlineData(HuntTests.Three, 2)]
    public void AlternateGuideSeparatesAndReducesDamageToAMinimumOfZero(string tile, int remaining)
    {
        var state = CouncilFixture.Move(FateGimliFixture.Hunt(tile), 5, 0);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = KingsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = FateGimli.GuideReaction });
        Assert.Equal(remaining, HuntRuntime.Active(state)!.Damage);
        Assert.Equal("separating", state.Characters.Single(c => c.Id == FateGimli.Figure).Location);
        Assert.Equal(FateGimli.Alternate, state.Lords!.RevealedCompanionVersions[FateGimli.Figure]);
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(FateGimli.Figure, Assert.Single(option.EligibleIds));
        Assert.Equal(3, Assert.Single(option.Travel).Range);
    }

    [Fact]
    public void GimliJoinsTheOtherCouncilGuidesInOnePersistedGroupWithOneMove()
    {
        var state = CouncilFixture.Move(FateGimliFixture.Hunt(HuntTests.Three, "boromir", "gimli", "legolas"), 5, 0);
        state = KingsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.boromir-guide" });
        state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.SelectGuide, new() { GuideId = FateGimli.Figure });
        state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = FateGimli.GuideReaction });
        state = KingsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.legolas-guide" });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(3, option.Minimum);
        Assert.All(option.Travel, t => Assert.Equal(3, t.Range));
        LordsFixture.Reject(state, CommandKind.SeparateCompanions, new() { Ids = [FateGimli.Figure], Route = ["hollin"] });
        state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.SeparateCompanions,
            new() { Ids = option.EligibleIds, Route = ["hollin", "north-dunland", "south-dunland", "gap-of-rohan"] });
        Assert.All(state.Characters, c => Assert.Equal("gap-of-rohan", c.Location));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(FateGimli.Alternate, state.Lords!.RevealedCompanionVersions[FateGimli.Figure]);
    }

    [Fact]
    public void DecliningTheAlternateGuideThenTakingHimAsCasualtyAbsorbsOnlyOneAndDoesNotReturn()
    {
        var state = CouncilFixture.Move(FateGimliFixture.Hunt(HuntTests.Three), 5, 0);
        state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        state = KingsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == FateGimli.Figure).Location);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void OrdinaryVersionHasNoGuideDepartureAndAbsorbsItsPrintedLevelTwoAsCasualty()
    {
        var state = FateGimliFixture.Hunt(HuntTests.Three);
        CharacterLifecycle.ApplyVersion(state.Characters.Single(c => c.Id == FateGimli.Figure), CharacterCatalog.Fate[FateGimli.Standard]);
        LordsCompanionSetup.Reveal(state, CharacterCatalog.Companion(state, FateGimli.Figure));
        state = CouncilFixture.Move(state, 5, 0);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        LordsFixture.Reject(state, CommandKind.RespondEffect, new() { ReactionId = FateGimli.GuideReaction });
        state = KingsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == FateGimli.Figure).Location);
        Assert.Equal(1, state.Fellowship.Corruption);
    }

    [Fact]
    public void AlternateGuideCannotReduceCorruptionOnlyDamage()
    {
        var state = CouncilFixture.Move(FateGimliFixture.Hunt(HuntTests.Three), 5, 0);
        HuntRuntime.Active(state)!.DamageRule = HuntDamageRule.CorruptionOnly;
        Assert.DoesNotContain(FateGimli.GuideReaction, new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds);
        LordsFixture.Reject(state, CommandKind.RespondEffect, new() { ReactionId = FateGimli.GuideReaction });
    }

    [Fact]
    public void GuideUsedInMordorStillReducesDamageButCannotReturnToTheBoard()
    {
        var state = FateGimliFixture.Hunt(HuntTests.Three);
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 1;
        state = CouncilFixture.Move(state, 0);
        state = KingsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = FateGimli.GuideReaction });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = KingsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == FateGimli.Figure).Location);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }
}
