using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

public sealed class ActionBoundaryContinuationTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData(CommandKind.MoveArmies)]
    [InlineData(CommandKind.Recruit)]
    [InlineData(CommandKind.MoveCompanions)]
    [InlineData(CommandKind.MoveShadowCharacters)]
    public void PaidStepsExposeOnlyPublicObligationsToBothSeats(CommandKind kind)
    {
        var (root, first, _) = ActionSequencePersistenceTests.Scenario(kind, false);
        var state = Do(root, first.Kind, first.Payload);
        var before = StateCodec.Write(state);
        var owner = state.Pending!.Owner;
        var own = Projection.For(state, owner);
        var other = Projection.For(state, TurnFlow.Other(owner));
        Assert.Equal(32, own.ActionBoundary!.Version);
        Assert.Equal([first.Payload.DieId], own.ActionBoundary.PaidDieIds);
        Assert.Equal(own.ActionBoundary.PaidDieIds, other.ActionBoundary!.PaidDieIds);
        Assert.Equal(state.Resolution.ActionSequence!.InitialFigures, own.ActionSequence!.InitialFigureIds);
        Assert.Equal(own.ActionSequence.InitialFigureIds, other.ActionSequence!.InitialFigureIds);
        Assert.All(own.ActionSequence.InitialFigureIds, id =>
            Assert.True(own.Units.Any(u => u.Id == id) || own.Characters.Any(c => c.Id == id)));
        Assert.Null(other.Decision);
        Assert.Empty(other.Options);
        own.ActionSequence.InitialFigureIds.Clear();
        own.ActionBoundary.PaidDieIds.Clear();
        Assert.Equal(before, StateCodec.Write(state));

        var opponent = state.Player(TurnFlow.Other(owner));
        CardFlow.Draw(state, opponent.Side, "character");
        var hand = opponent.Hand[0];
        CardFlow.Draw(state, opponent.Side, "character");
        // Compare worlds with identical counts, but different opposing hidden cards.
        var visible = JsonSerializer.Serialize(Projection.For(state, owner));
        (opponent.Hand[0], opponent.Decks["character"][0]) =
            (opponent.Decks["character"][0], hand);
        state.RandomSamples.Add(new() { Value = 987654 });
        Assert.Equal(visible, JsonSerializer.Serialize(Projection.For(state, owner)));
    }

    [Fact]
    public void CapturedSettlementKeepsItsDeferredPoliticalCausesInOrder()
    {
        var state = Board(Side.Shadow);
        var north = state.Nations.Single(n => n.Id == "north");
        north.Active = false;
        north.StepsToWar = 3;
        var army = Put(state, "sauron", "woodland-realm").Id;
        state = Do(state, CommandKind.MoveArmies, Move(state, "woodland-realm", "dale", army));
        var facts = Projection.For(state, Side.FreePeoples).ActionBoundary!;
        Assert.NotEmpty(facts.PendingPolitics);
        Assert.Equal(state.PoliticalReactions.Select(p => (p.Trigger.ToString(), p.NationId, p.RegionId)),
            facts.PendingPolitics.Select(p => (p.Cause, p.NationId, p.RegionId)));
        var restored = StateCodec.Copy(state);
        Assert.Equal(JsonSerializer.Serialize(facts),
            JsonSerializer.Serialize(Projection.For(restored, Side.FreePeoples).ActionBoundary));
        facts.PendingPolitics.Clear();
        Assert.NotEmpty(state.PoliticalReactions);
        restored = Do(restored, CommandKind.FinishActionSequence);
        Assert.Empty(restored.PoliticalReactions);
        Assert.Equal(2, restored.Nations.Single(n => n.Id == "north").StepsToWar);
    }

    [Fact]
    public void ExcessAfterEarlyFinishRetainsThePublicUncompletedPayment()
    {
        var state = Board();
        var arrivals = Enumerable.Range(0, 10).Select(_ => Put(state, "gondor", "minas-tirith").Id).ToArray();
        Put(state, "rohan", "osgiliath");
        state = Do(state, CommandKind.MoveArmies, Move(state, "minas-tirith", "osgiliath", arrivals));
        state = Do(state, CommandKind.FinishActionSequence);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Null(state.Resolution.ActionSequence);
        var own = Projection.For(state, Side.FreePeoples);
        Assert.Equal(state.Resolution.PaidDieIds, own.ActionBoundary!.PaidDieIds);
        Assert.Single(own.ActionBoundary.PaidDieIds);
        state.Resolution.Suspend(state, "unsupported-nested-effect", 0);
        Assert.Null(new Projection().For(state, Side.FreePeoples).ActionBoundary);
    }
}
