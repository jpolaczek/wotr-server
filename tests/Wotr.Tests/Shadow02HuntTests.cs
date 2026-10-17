using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Domain.FreePeoplesCharacterCards01;
using static Wotr.Domain.FreePeoplesCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow02HuntTests
{
    [Theory]
    [InlineData(HuntTests.Eye)]
    [InlineData("base.hunt.special-sh.eye.1.1")]
    [InlineData("base.hunt.special-fp.-2.0.1")]
    public void FoulThingIgnoresEyesAndFellowshipSpecialsEntirely(string tile)
    {
        var state = BeforeEvent(FoulThing, mordor: true);
        state.Fellowship.Corruption = 5;
        var companions = state.Fellowship.Companions.Count;
        Pool(state, tile);
        state = Play(state, FoulThing, 0);
        Assert.Equal(5, state.Fellowship.Corruption);
        Assert.Equal(companions, state.Fellowship.Companions.Count);
        Assert.Equal(2, state.Fellowship.MordorStep);
        Assert.True(state.Fellowship.Hidden);
        Assert.Single(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void TableProtectionPrecedesForcedRandomCasualtyAndNewGuide()
    {
        var state = BeforeEvent(FoulThing);
        state.Player(Side.FreePeoples).Table.Add(Axe);
        state.Fellowship.Guide = "base.character.legolas";
        var pick = state.Fellowship.Companions.IndexOf("base.character.legolas");
        Pool(state, HuntTests.Three);
        state = Play(state, FoulThing, 0);
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.False(HuntOptions.View(state)!.CorruptionOnly);
        Reject(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction }, new SequenceRandom(pick));
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.DoesNotContain("base.character.legolas", state.Fellowship.Companions);
        Assert.True(HuntRuntime.Active(state)!.CasualtyTaken);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void ZeroDamageAfterProtectionNeverDrawsACompanion()
    {
        var state = BeforeEvent(FoulThing);
        state.Player(Side.FreePeoples).Table.Add(Axe);
        Pool(state, RevealOne);
        var count = state.Fellowship.Companions.Count;
        state = Play(state, FoulThing, 0);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction });
        Assert.Equal(count, state.Fellowship.Companions.Count);
        Assert.Single(state.RandomSamples);
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HobbitCanSeparateBeforeTheRandomPickAndGollumCanReduceRemainder(bool mordor)
    {
        var state = BeforeEvent(FoulThing, mordor: mordor);
        state.Fellowship.Companions = ["base.character.merry"];
        state.Fellowship.Guide = "base.character.merry";
        Pool(state, HuntTests.Three);
        state = Play(state, FoulThing, 0);
        Assert.Equal("hunt.guide", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.gollum-reveal" });
        if (!mordor)
        {
            Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
            state = Do(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.merry"], Route = ["hollin"] });
            state = Do(state, CommandKind.RevealFellowship, new() { Route = ["hollin"] });
        }
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.False(state.Fellowship.Hidden);
        Assert.Single(state.RandomSamples);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Equal(mordor ? "casualties" : "hollin", state.Characters.Single(c => c.Id == "base.character.merry").Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SoleHobbitIsStillRandomCasualtyAndMayReturnOnlyOutsideMordor(bool mordor)
    {
        var state = BeforeEvent(FoulThing, mordor: mordor);
        state.Fellowship.Companions = ["base.character.merry"];
        state.Fellowship.Guide = "base.character.merry";
        Pool(state, HuntTests.Three);
        state = Play(state, FoulThing, 0);
        state = Do(state, CommandKind.DeclineReaction, random: new SequenceRandom(0));
        Assert.True(HuntRuntime.Active(state)!.CasualtyTaken);
        state = Do(state, CommandKind.DeclineReaction); // Gollum remains hidden.
        if (!mordor)
            state = Do(state, CommandKind.SeparateCompanions, new() { Ids = ["base.character.merry"], Route = ["hollin"] });
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Equal(mordor ? "casualties" : "hollin", state.Characters.Single(c => c.Id == "base.character.merry").Location);
    }

    [Fact]
    public void RevelationStartsIndependentStrongholdHuntWithoutForcedCasualtyRule()
    {
        var state = BeforeEvent(FoulThing, "moria");
        state.Fellowship.Progress = 1;
        var pick = state.Fellowship.Companions.IndexOf("base.character.legolas");
        Pool(state, RevealOne, HuntTests.Three);
        state = Play(state, FoulThing, 0, pick);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(DecisionKind.FellowshipRoute, state.Pending!.Kind);
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria", "dimrill-dale"] }, new SequenceRandom(0));
        Assert.Equal(HuntDamageRule.Normal, HuntRuntime.Active(state)!.DamageRule);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "HuntCasualty");
    }

    [Fact]
    public void StaffCanPreventTheDrawAndMithrilCanReplaceItBeforeForcedCasualty()
    {
        var state = BeforeEvent(FoulThing);
        state.Player(Side.FreePeoples).Table.AddRange([Staff, Mithril]);
        Pool(state, HuntTests.Three, HuntTests.Eye);
        state = Play(state, FoulThing);
        var saved = state;
        var prevent = new EffectOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.RespondEffect).EligibleIds.Single();
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = prevent });
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
        state = Do(saved, CommandKind.DeclineReaction, random: new SequenceRandom(0));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Equal(0, state.Fellowship.Corruption);
    }
}
