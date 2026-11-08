using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

// Fire and Shadow: a declared route through the Lords' Balrog draws one tile for him at once.
public sealed class FireAndShadowBoundaryTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    private static MatchState Declared(string tile)
    {
        var state = LordsFireAndShadowTests.BeforeRoute(true, "moria");
        state.Fellowship.Companions.Remove(CharacterRules.Strider);
        HuntTests.Pool(state, tile);
        return Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
    }

    // An Eye eliminates the Balrog and Gandalf as Guide; the Free Peoples choose a new one.
    [Fact]
    public void TheGuideAfterAnEyeIsMarkedWithTheDeclaration()
    {
        var state = Declared(HuntTests.Eye);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(HuntStage.Corruption, HuntRuntime.Active(state)!.Stage);

        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var view = Projection.For(state, side);
            Assert.Equal(["moria"], view.ActionBoundary?.FellowshipDeclaration?.Route);
            Assert.Equal(LordsCharacters.Balrog, view.Hunt?.SourceId);
        }
    }

    [Fact]
    public void TheDamageOfANumberedTileIsMarkedWithTheDeclaration()
    {
        var state = Declared(HuntTests.Three);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);

        Assert.NotNull(Projection.For(state, Side.FreePeoples).ActionBoundary?.FellowshipDeclaration);
    }

    [Theory]
    [InlineData("revealed")]
    [InlineData("tile")]
    [InlineData("balrog")]
    [InlineData("source")]
    public void AnInconsistentFireAndShadowHuntIsNotMarked(string change)
    {
        var state = Declared(HuntTests.Eye);
        var hunt = HuntRuntime.Active(state)!;
        switch (change)
        {
            case "revealed": state.Fellowship.Hidden = false; break;
            case "tile": hunt.TileId = HuntTests.Three; break;
            case "balrog": state.Characters.Single(c => c.Id == LordsCharacters.Balrog).Location = "moria"; break;
            case "source": hunt.SourceId = "fellowship"; break;
        }

        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.FellowshipDeclaration);
    }
}
