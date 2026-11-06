using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public sealed class BreakingCardHuntProjectionTests
{
    private static readonly Projection Projector = GameRuntime.Projection(EffectCatalog.All);

    [Fact]
    public void PaidBreakingHuntAndCompanionChoiceArePublicForBothSidesWithoutChangingTheState()
    {
        var state = At();
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        var before = StateCodec.Write(state);
        string? factsJson = null;

        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projector.For(state, side);
            var boundary = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Equal(15, boundary.Version);
            Assert.Equal(ShadowCharacterCards03.Breaking, boundary.CardHunt!.CardId);
            Assert.Equal(DieFace.Character, boundary.CardHunt.CardCost);
            Assert.True(view.Hunt!.SeparatesCompanions);
            Assert.Equal("Done", view.Hunt.Stage);
            Assert.Equal(Assert.Single(boundary.PaidDieIds), view.Dice.Single(d =>
                d.Owner == Side.Shadow && d.Location == DieLocation.Resolving).Id);
            Assert.Equal(JsonSerializer.Serialize(view), JsonSerializer.Serialize(Projector.For(StateCodec.Copy(state), side)));

            var currentFacts = JsonSerializer.Serialize(boundary.CardHunt);
            if (factsJson is not null)
                Assert.Equal(factsJson, currentFacts);
            factsJson = currentFacts;

            if (side == Side.FreePeoples)
            {
                var option = Assert.Single(view.Options);
                Assert.Equal(CommandKind.ChooseEffect, option.Kind);
                Assert.Equal(state.Pending.RequiredCount, option.Minimum);
                Assert.Equal(option.Minimum, option.Maximum);
                Assert.Equal(state.Fellowship.Companions.Order(), option.EligibleIds.Order());
            }
            else
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
        }

        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void BreakingCardHuntFactsIgnoreOpposingPrivateCardsAndRandomSamples()
    {
        var state = At();
        var before = JsonSerializer.Serialize(Projector.For(state, Side.Shadow));
        state.Player(Side.FreePeoples).Hand.Reverse();
        foreach (var deck in state.Player(Side.FreePeoples).Decks.Values)
            deck.Reverse();
        state.RandomSamples.Add(new() { Value = 4321 });
        Assert.Equal(before, JsonSerializer.Serialize(Projector.For(state, Side.Shadow)));
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("card")]
    [InlineData("purpose")]
    [InlineData("tile-rule")]
    [InlineData("choice-source")]
    [InlineData("choice-owner")]
    [InlineData("nested")]
    public void UnsupportedOrInconsistentBreakingContinuationsDoNotClaimACardBoundary(string change)
    {
        var state = At();
        switch (change)
        {
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
            case "card": state.Resolution.Frames[0].Effect!.SourceId = ShadowCharacterCards01.OrcPatrol; break;
            case "purpose": state.Resolution.Frames[1].Hunt!.Purpose = HuntPurpose.Hunt; break;
            case "tile-rule": state.Resolution.Frames[1].Hunt!.TileRule = HuntTileRule.All; break;
            case "choice-source": state.Pending!.SourceId = "unknown-source"; break;
            case "choice-owner": state.Resolution.Frames[^1].Effect!.Owner = Side.Shadow; break;
            case "nested": EffectRuntime.BeginProgram(state, Side.FreePeoples, "unknown", "unknown"); break;
        }

        Assert.Null(Projector.For(state, Side.FreePeoples).ActionBoundary);
        Assert.Null(Projector.For(state, Side.Shadow).ActionBoundary);
    }

    private static MatchState At()
    {
        var state = RevealedEvent(ShadowCharacterCards03.Breaking);
        Pool(state, HuntTests.Three);
        return Play(state, ShadowCharacterCards03.Breaking, 0);
    }
}
