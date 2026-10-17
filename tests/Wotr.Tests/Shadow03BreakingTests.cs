using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class Shadow03BreakingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BreakingSeparatesChosenCompanionsWithoutDamageOrHuntReactions(bool mordor)
    {
        var state = RevealedEvent(Breaking, mordor);
        Table(state, Worn);
        Table(state, FreePeoplesCharacterCards01.Axe);
        state.Fellowship.Guide = "base.character.merry";
        Pool(state, HuntTests.Three, HuntTests.Eye);
        state = Play(state, Breaking, 0);
        Assert.Equal("FellowshipCompanions", state.Pending!.Presentation);
        Assert.True(new Projection().For(state, Side.FreePeoples).Hunt!.SeparatesCompanions);
        Assert.Equal(3, Choices(state).Minimum);
        var ids = new List<string> { "base.character.merry", "base.character.pippin", "base.character.boromir" };
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ids.Take(2).ToList() });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ids });
        state = ResolveGuide(state);
        Assert.All(ids, id => Assert.Equal(mordor ? "casualties" : "hollin", state.Characters.Single(c => c.Id == id).Location));
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.False(state.Fellowship.Hidden);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IgnoredTilesDoNotSeparateCompanions(bool special)
    {
        var state = RevealedEvent(Breaking, true);
        var tile = special ? HuntPool.Definitions.Values.First(t => t.Pool == "special-fp").Id : HuntTests.Eye;
        Pool(state, tile, HuntTests.Three);
        var before = state.Fellowship.Companions.ToArray();
        state = Play(state, Breaking, 0);
        Assert.Equal(before, state.Fellowship.Companions);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Fact]
    public void StaffPreventsBreakingAndMithrilReplacementPrecedesTheSeparationValue()
    {
        var state = RevealedEvent(Breaking);
        Table(state, FreePeoplesCharacterCards02.Staff);
        Pool(state, HuntTests.Three);
        state = Play(state, Breaking);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards02.StaffReaction });
        Assert.Empty(state.RandomSamples);
        Assert.Equal(7, state.Fellowship.Companions.Count);
        state = RevealedEvent(Breaking);
        Table(state, FreePeoplesCharacterCards01.Mithril);
        Pool(state, HuntTests.Eye, HuntTests.Three);
        state = Play(state, Breaking, 0);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal(3, Choices(state).Minimum);
        Assert.Equal(2, state.RandomSamples.Count);
    }

    [Fact]
    public void ShelobSeparatesOnlyExistingCompanionsAndNeverAddsExcessCorruption()
    {
        var state = RevealedEvent(Breaking, true);
        state.Fellowship.Companions = ["base.character.merry", "base.character.pippin"];
        state.Fellowship.Guide = state.Fellowship.Companions[0];
        Pool(state, ShadowCharacterCards01.SpecialTiles[0].Tile, HuntTests.Eye);
        state = Play(state, Breaking, 0, 5);
        Assert.Empty(state.Fellowship.Companions);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }
}
