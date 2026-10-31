using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class StrikeHuntProjectionTests
{
    [Theory]
    [InlineData("not-rolled")]
    [InlineData("movement")]
    [InlineData("mordor")]
    [InlineData("tile-rule")]
    [InlineData("damage-rule")]
    [InlineData("return")]
    [InlineData("roll-reduction")]
    [InlineData("reroll-bonus")]
    [InlineData("cursor")]
    [InlineData("before-roll-bonus")]
    public void UnreviewedStrikeContinuationsOmitTheBoundary(string change)
    {
        var state = CardHuntProjectionTests.At(ShadowCharacterCards02.Strike,
            change == "before-roll-bonus" ? "roll" : "damage");
        Assert.NotNull(GameRuntime.Projection(EffectCatalog.All).For(state, state.Pending!.Owner).ActionBoundary);
        var hunt = HuntRuntime.Active(state)!;
        switch (change)
        {
            case "not-rolled": hunt.Rolled = false; break;
            case "movement": hunt.Movement = true; break;
            case "mordor": hunt.Mordor = true; break;
            case "tile-rule": hunt.TileRule = HuntTileRule.IgnoreEyesAndFellowship; break;
            case "damage-rule": hunt.DamageRule = HuntDamageRule.CorruptionOnly; break;
            case "return": hunt.Return = HuntReturn.FinishMove; break;
            case "roll-reduction": hunt.DiceReduction = 1; break;
            case "reroll-bonus": hunt.RerollBonus = 1; break;
            case "cursor": state.Resolution.Frames[0].NextStep = 1; break;
            case "before-roll-bonus": hunt.RollBonus = 1; break;
        }
        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, state.Pending.Owner).ActionBoundary);
    }

    [Fact]
    public void TravelAndPreHuntStrikeChoiceStillRequireTheirOwnContext()
    {
        var state = Shadow02TravelTests.BeforeTravel(ShadowCharacterCards02.Strike);
        state = Shadow01Fixture.Play(state, ShadowCharacterCards02.Strike);
        Assert.Equal(DecisionKind.CharacterEvent, state.Pending!.Kind);
        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow).ActionBoundary);
        var id = NazgulEventTravel.Option(state).EligibleIds[0];
        state = CharacterFixture.Do(state, CommandKind.MoveShadowCharacters,
            Shadow02TravelTests.Flight(state, state.Fellowship.RegionId, id));
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Null(GameRuntime.Projection(EffectCatalog.All).For(state, Side.Shadow).ActionBoundary);
    }
}
