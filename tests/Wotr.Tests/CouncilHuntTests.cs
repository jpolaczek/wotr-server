using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class CouncilHuntTests
{
    [Fact]
    public void AlternateGuidesMaySeparateAtZeroDamageUnlikeBaseHobbits()
    {
        var state = CouncilFixture.Move(CouncilFixture.Hunt("base.hunt.standard.0.1.1", "boromir", "gimli"), 5, 0);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.boromir-guide" });
        Assert.Equal(0, HuntRuntime.Active(state)!.Damage);
        Assert.Equal("base.character.gimli", state.Fellowship.Guide);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.gimli-guide" });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = LordsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        Assert.Equal(2, FellowshipFlow.Current(state)!.Fellowship!.FixedCompanions.Count);
    }

    [Fact]
    public void ThreeSeparatingGuidesFormOneGroupUsingMovementLevelTwoAndPersistEachGuide()
    {
        var state = CouncilFixture.Move(CouncilFixture.Hunt(HuntTests.Three, "boromir", "gimli", "legolas"), 5, 0);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.boromir-guide" });
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(2, HuntRuntime.Active(state)!.Damage);
        state = LordsFixture.Do(StateCodec.Copy(state), CommandKind.SelectGuide, new() { GuideId = "base.character.gimli" });
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.gimli-guide" });
        Assert.Equal("base.character.legolas", state.Fellowship.Guide);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "council.legolas-guide" });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = LordsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(3, option.Minimum);
        Assert.All(option.Travel, t => { Assert.Equal(3, t.Range); Assert.Equal(3, t.RequiredGroup.Count); });
        LordsFixture.Reject(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.boromir"], Route = ["hollin"] });
        state = LordsFixture.Do(StateCodec.Copy(state), CommandKind.SeparateCompanions,
            new() { Ids = option.EligibleIds, Route = ["hollin", "north-dunland", "south-dunland", "gap-of-rohan"] });
        Assert.All(state.Characters, c => Assert.Equal("gap-of-rohan", c.Location));
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(3, state.Lords!.RevealedCompanionVersions.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("boromir")]
    [InlineData("gimli")]
    [InlineData("legolas")]
    public void AlternateCasualtyAbsorbsOneDamageAndDoesNotReturn(string name)
    {
        var state = CouncilFixture.Move(CouncilFixture.Hunt(HuntTests.Three, name), 5, 0);
        state = LordsFixture.Do(state, CommandKind.DeclineReaction);
        state = LordsFixture.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = LordsFixture.Do(state, CommandKind.DeclineReaction);
        Assert.Equal("casualties", Assert.Single(state.Characters).Location);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(HuntTests.Eye, "merry")]
    [InlineData("base.hunt.standard.0.1.1", "merry")]
    [InlineData("base.hunt.standard.2.1.1", "merry")]
    [InlineData(HuntTests.Eye, "pippin")]
    [InlineData("base.hunt.standard.0.1.1", "pippin")]
    [InlineData("base.hunt.standard.2.1.1", "pippin")]
    public void BothAlternateHobbitsCancelStandardEyeOrRevealWithoutBattleReturn(string tile, string guide)
    {
        var state = CouncilFixture.Move(CouncilFixture.Hunt(tile, guide, guide == "merry" ? "pippin" : "merry"), 5, 0);
        Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
        state = LordsFixture.Do(StateCodec.Copy(state), CommandKind.RespondEffect,
            new() { ReactionId = CouncilGuideReactions.Cancel + ".lords.character." + guide });
        Assert.Empty(state.Fellowship.Companions);
        Assert.All(state.Characters, c => Assert.Equal("casualties", c.Location));
        Assert.Empty(state.Lords!.Council!.BattleReturns);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.True(state.Fellowship.Hidden);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.Equal(2, state.Lords.RevealedCompanionVersions.Count);
    }

    [Fact]
    public void UnknownPartnerVersionDoesNotLeakThroughReactionWindowOrOpponentOptions()
    {
        var original = CouncilFixture.Hunt(HuntTests.Three, "merry", "pippin");
        var alternate = StateCodec.Copy(original);
        CharacterLifecycle.ApplyVersion(original.Characters.Single(c => c.Id == "base.character.pippin"), CharacterCatalog.Base["base.character.pippin"]);
        original = CouncilFixture.Move(original, 5, 0);
        alternate = CouncilFixture.Move(alternate, 5, 0);
        Assert.Equal(JsonSerializer.Serialize(LordsFixture.Views().For(original, Side.Shadow)), JsonSerializer.Serialize(LordsFixture.Views().For(alternate, Side.Shadow)));
        Assert.Empty(new EffectOptions().GetOptions(original, Side.FreePeoples).Single().EligibleIds);
        Assert.Single(new EffectOptions().GetOptions(alternate, Side.FreePeoples).Single().EligibleIds);
        LordsFixture.Reject(original, CommandKind.RespondEffect, new() { ReactionId = CouncilGuideReactions.Cancel + "." + CouncilCompanions.Merry });
    }

    [Fact]
    public void HobbitGuideDoesNotCancelSpecialHuntTile()
    {
        var special = HuntPool.Definitions.Values.First(t => t.Pool == "special-sh" && t.Value == "3").Id;
        var state = CouncilFixture.Hunt(special, "pippin", "merry");
        state.Fellowship.MordorStep = 1;
        state.Fellowship.RegionId = MordorRules.Track;
        state = CouncilFixture.Move(state, 0);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        LordsFixture.Reject(state, CommandKind.RespondEffect, new() { ReactionId = CouncilGuideReactions.Cancel + "." + CouncilCompanions.Pippin });
    }
}
