using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public sealed class CompanionJourneyProjectionTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData(Gwaihir, 4, 0)]
    [InlineData(Swifter, null, 2)]
    public void PaidJourneyChoiceExposesTheSamePublicTravelRulesToBothSeats(
        string card, int? fixedLevel, int bonus)
    {
        var state = FellowshipEventCardTests.Play(TurnTests.Actions(), card);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Projection.For(state, side);
            var boundary = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            var journey = Assert.IsType<CompanionJourneyView>(boundary.CompanionJourney);
            Assert.Equal(18, boundary.Version);
            Assert.Equal(card, journey.CardId);
            Assert.Equal(nameof(FellowshipProcedureKind.Separation), journey.Stage);
            Assert.True(journey.AllowSeparation);
            Assert.False(journey.AllowNoMovement);
            Assert.Equal(1, journey.MaximumGroups);
            Assert.Equal(fixedLevel, journey.FixedLevel);
            Assert.Equal(bonus, journey.Bonus);
            Assert.True(journey.AllowBesiegedDestination);
            Assert.Equal(JsonSerializer.Serialize(Projection.For(StateCodec.Copy(state), side).ActionBoundary),
                JsonSerializer.Serialize(boundary));
            if (side != Side.FreePeoples)
            {
                Assert.Null(view.Decision);
                Assert.Empty(view.Options);
            }
        }
    }

    [Theory]
    [InlineData(Gwaihir)]
    [InlineData(Swifter)]
    public void GuideChoiceAfterJourneyRetainsOnlyItsPublicCardAndJourneyContext(string card)
    {
        var state = FellowshipEventCardTests.Play(TurnTests.Actions(), card);
        state = HuntTests.Do(state, CommandKind.SeparateCompanions,
            new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin"] },
            catalog: EffectCatalog.All);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        state.PoliticalReactions.Add(new(PoliticalTrigger.CompanionArrival, "gondor", "minas-tirith"));

        var fp = Projection.For(state, Side.FreePeoples);
        var shadow = Projection.For(state, Side.Shadow);
        Assert.True(fp.ActionBoundary is not null, $"{card}/{state.Pending!.Kind} must project the supported journey root.");
        var journey = Assert.IsType<CompanionJourneyView>(fp.ActionBoundary!.CompanionJourney);
        Assert.Equal(nameof(FellowshipProcedureKind.Guide), journey.Stage);
        Assert.Equal(card, journey.CardId);
        Assert.Equal("gondor", Assert.Single(fp.ActionBoundary.PendingPolitics).NationId);
        Assert.Equal(JsonSerializer.Serialize(fp.ActionBoundary), JsonSerializer.Serialize(shadow.ActionBoundary));
        Assert.Null(shadow.Decision);
        Assert.Empty(shadow.Options);
    }

    [Theory]
    [InlineData("wrong-stage")]
    [InlineData("wrong-source")]
    [InlineData("wrong-range")]
    [InlineData("wrong-payment")]
    public void UnreviewedJourneyShapesDoNotReceiveTheBoundary(string change)
    {
        var state = FellowshipEventCardTests.Play(TurnTests.Actions(), Swifter);
        var fellowship = state.Resolution.Frames[1].Fellowship!;
        switch (change)
        {
            case "wrong-stage": state.Pending!.Kind = DecisionKind.Guide; break;
            case "wrong-source": fellowship.SourceId = Gwaihir; break;
            case "wrong-range": fellowship.Journey!.Bonus++; break;
            case "wrong-payment": state.Resolution.PaidDieIds.Clear(); break;
        }

        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary);
    }

    [Fact]
    public void OtherFellowshipCardProgramsDoNotClaimTheJourneyMarker()
    {
        var state = FellowshipEventCardTests.Play(TurnTests.Actions(), ThereAndBack);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary);
    }

    [Theory]
    [InlineData(DecisionKind.CompanionSeparation)]
    [InlineData(DecisionKind.Guide)]
    public void OpposingHiddenAssignmentsDoNotChangeTheJourneyFact(DecisionKind stage)
    {
        var state = FellowshipEventCardTests.Play(TurnTests.Actions(), Swifter);
        if (stage == DecisionKind.Guide)
            state = HuntTests.Do(state, CommandKind.SeparateCompanions,
                new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin"] },
                catalog: EffectCatalog.All);

        var first = JsonSerializer.Serialize(Projection.For(state, Side.FreePeoples).ActionBoundary);
        var shadow = state.Player(Side.Shadow);
        shadow.Hand.Reverse();
        foreach (var deck in shadow.Decks.Values)
            deck.Reverse();
        state.RandomSamples.Add(new() { Value = 613 });
        Assert.Equal(first, JsonSerializer.Serialize(Projection.For(state, Side.FreePeoples).ActionBoundary));
    }
}
