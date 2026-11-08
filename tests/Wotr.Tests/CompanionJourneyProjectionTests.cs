using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
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
            Assert.Equal(41, boundary.Version);
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
    [InlineData(FreePeoplesStrategyCards01.BookOfMazarbul, false, "erebor")]
    [InlineData(FreePeoplesStrategyCards02.FearFireFoes, false, "the-shire")]
    [InlineData(FateCards.Book, true, "iron-hills")]
    public void OutsideCompanionJourneyExposesOnlyItsOptionalMoveAndPrintedParameters(
        string card, bool fate, string destination)
    {
        var state = fate ? FateFixture.Actions() : TurnTests.Actions();
        foreach (var die in state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available))
            die.Face = DieFace.Character;
        var companionId = state.Fellowship.Companions.First();
        state.Fellowship.Companions.Remove(companionId);
        var companion = CharacterCatalog.Companion(state, companionId);
        companion.Location = destination;
        state.Characters.Add(companion);
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var costDie = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        costDie.Face = DieFace.Muster;
        state = EventTests.Execute(state, EventTests.Play(state, card, DieFace.Muster));
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);

        var freePeoples = Projection.For(state, Side.FreePeoples);
        var shadow = Projection.For(state, Side.Shadow);
        var frameSummary = string.Join(" | ", state.Resolution.Frames.Select(frame =>
            $"{frame.EffectId}:{frame.NextStep}:{frame.Effect?.SourceId}:{frame.Fellowship?.Kind}"));
        var effect = state.Resolution.Frames[0].Effect!;
        var journeyFacts = state.Resolution.Frames[1].Fellowship!.Journey!;
        var program = EffectCatalog.All.Programs[card];
        Assert.True(freePeoples.ActionBoundary?.CompanionJourney is not null,
            $"{card}: boundary={freePeoples.ActionBoundary is not null}; frames={frameSummary}; " +
            $"paid={string.Join(',', state.Resolution.PaidDieIds)}; pending={state.Pending.Kind}/{state.Pending.SourceId}; " +
            $"face={effect.PaidFace}; owner={effect.Owner}; return={effect.Return}; wait={effect.Wait}; " +
            $"event={effect.IsEvent}; committed={effect.Committed.Count}; " +
            $"discard={state.Player(Side.FreePeoples).Discard.Contains(card)}; " +
            $"journey={journeyFacts.AllowSeparation}/{journeyFacts.AllowNoMovement}/{journeyFacts.MaximumGroups}/" +
            $"{journeyFacts.FixedLevel}/{journeyFacts.Bonus}/{journeyFacts.AllowBesiegedDestination}; " +
            $"steps={string.Join(',', program.Steps.Select(step => step.GetType().Name))}; " +
            $"commands={string.Join(',', state.Pending.Commands)}");
        var journey = Assert.IsType<CompanionJourneyView>(freePeoples.ActionBoundary!.CompanionJourney);
        Assert.Equal(41, freePeoples.ActionBoundary.Version);
        Assert.Equal(card, journey.CardId);
        Assert.Equal(nameof(FellowshipProcedureKind.Separation), journey.Stage);
        Assert.False(journey.AllowSeparation);
        Assert.True(journey.AllowNoMovement);
        Assert.Equal(0, journey.MaximumGroups);
        Assert.Null(journey.FixedLevel);
        Assert.Equal(0, journey.Bonus);
        Assert.False(journey.AllowBesiegedDestination);
        var move = Assert.Single(freePeoples.Options);
        Assert.Equal(CommandKind.MoveCompanions, move.Kind);
        Assert.True(move.AllowEmptySelection);
        Assert.Equal(0, move.MaximumGroups);
        Assert.Empty(freePeoples.ActionBoundary.PendingPolitics);
        Assert.Equal(JsonSerializer.Serialize(freePeoples.ActionBoundary),
            JsonSerializer.Serialize(shadow.ActionBoundary));
        Assert.Null(shadow.Decision);
        Assert.Empty(shadow.Options);

        var original = JsonSerializer.Serialize(freePeoples.ActionBoundary);
        var opposingPlayer = state.Player(Side.Shadow);
        opposingPlayer.Hand.Reverse();
        foreach (var deck in opposingPlayer.Decks.Values)
            deck.Reverse();
        state.RandomSamples.Add(new() { Value = 613 });
        Assert.Equal(original, JsonSerializer.Serialize(Projection.For(state, Side.FreePeoples).ActionBoundary));
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
        var boundary = Projection.For(state, Side.FreePeoples).ActionBoundary!;
        Assert.Null(boundary.CompanionJourney);
        Assert.Equal(ThereAndBack, boundary.DirectEventChoice!.CardId);
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
