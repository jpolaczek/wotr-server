using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.ActionLogFixture;

namespace Wotr.Tests;

public class ActionLogProjectionTests
{
    [Theory]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.ArmyMuster)]
    [InlineData(DieFace.WillOfTheWest)]
    public void RealMovementKeepsHistoricalCompositionFaceAndOneCost(DieFace face)
    {
        var (state, first, secondUnit) = Army(face);
        state = Execute(state, first);
        var id = state.Resolution.ActionLog!.Action.Id;
        var action = Action(state, id);
        Assert.Equal(ActionLogStatus.InProgress, action.Action.Status);
        Assert.Equal(face, Assert.Single(action.Action.Costs).Face);
        Assert.Equal(DieFace.Army, action.Action.Costs[0].Use);
        AssertArmy(Assert.Single(action.Steps.SelectMany(s => s.Movements)));
        var original = JsonSerializer.Serialize(action.Steps, BoardCatalog.Json);
        state = StateCodec.Read(StateCodec.Write(state));
        state = Execute(state, BattleFixture.Command(state, CommandKind.MoveArmies,
            ActionSequenceFixture.Move(state, "orthanc", "fords-of-isen", secondUnit)));
        Assert.Null(state.Resolution.ActionLog);
        Assert.Equal(ActionLogStatus.Completed, Action(state, id).Action.Status);
        Assert.Equal(2, Action(state, id).Steps.SelectMany(s => s.Movements).Count());
        Assert.Equal(new[] { "orthanc", "fords-of-isen" }, Action(state, id).Steps.Last(s => s.Command == CommandKind.MoveArmies).Context.Regions);
        Assert.Single(Action(state, id).Steps.SelectMany(s => s.Costs));
        var budget = Action(state, id).Steps.Last(s => s.Command == CommandKind.MoveArmies).Facts.Budget!;
        Assert.Equal(2, budget.Completed);
        Assert.Equal(0, budget.Remaining);
        foreach (var unit in state.Units.Where(u => u.Location == "dagorlad"))
            BoardRules.ReturnToReserve(unit);
        Assert.Equal(original, JsonSerializer.Serialize(Action(state, id).Steps.Take(action.Steps.Count), BoardCatalog.Json));
        Assert.DoesNotContain(first.CommandId, JsonSerializer.Serialize(Action(state, id), BoardCatalog.Json));
    }

    [Fact]
    public void FinishAndLegacyContinuationDoNotInventTheFirstStepOrCost()
    {
        var (state, first, _) = Army();
        state = Execute(state, first);
        state.Resolution.ActionLog = null;
        foreach (var item in state.Events)
            item.Activity = null; // A v3 save made before the action journal was installed.
        var legacy = StateCodec.Write(state);
        state = StateCodec.Read(legacy);
        Assert.Equal(legacy, StateCodec.Write(state));
        state = Execute(state, BattleFixture.Command(state, CommandKind.FinishActionSequence));
        var newStep = Records(state, Side.Shadow).Last();
        var action = Action(state, newStep.ActionId, Side.Shadow);
        Assert.True(action.Action.PartialHistory);
        Assert.Empty(action.Action.Costs);
        Assert.Empty(action.Steps.SelectMany(s => s.Movements));
        Assert.Equal(CommandKind.FinishActionSequence, Assert.Single(action.Steps).Command);
        Assert.Equal(ActionLogStatus.Completed, action.Action.Status);
        var old = Records(state, Side.Shadow).First();
        var oldAction = Action(state, old.ActionId, Side.Shadow);
        Assert.True(oldAction.Action.Legacy);
        Assert.Null(oldAction.Action.Initiator);
        Assert.Equal(state.Events[0].Text, oldAction.Steps[0].Text);
        Assert.Equal(ActionLogStatus.Unknown, oldAction.Action.Status);
    }

    [Fact]
    public void FilteringKeepsSafeContextAndExcludesPrivateStepsParentsAndCounts()
    {
        var (state, first, _) = Army();
        state = Execute(state, first);
        var root = state.Resolution.ActionLog!.Action;
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "PrivateChoice", OnlyFor = Side.Shadow, Text = "private-sentinel",
            Activity = new() { Action = root, Actor = Side.Shadow, Turn = 1, Costs = [new() { Id = "secret-cost" }] }
        });
        var privateId = EventIdentity.For(state.Id, state.Events.Count);
        state.Events.Add(new()
        {
            Revision = state.Revision, Kind = "Reaction", Text = "Public reaction",
            Activity = new() { Action = root, Actor = Side.FreePeoples, Turn = 1, ParentStepId = privateId }
        });
        var view = Action(state, root.Id, Side.FreePeoples, ActionLogFilter.You);
        Assert.Equal(1, view.Action.StepCount);
        Assert.Equal(Side.Shadow, view.Action.Initiator);
        Assert.Single(view.Action.Costs);
        Assert.Equal(Side.FreePeoples, Assert.Single(view.Steps).Actor);
        Assert.Empty(view.Steps[0].ParentStepId);
        var raw = JsonSerializer.Serialize(view, BoardCatalog.Json);
        Assert.DoesNotContain("private-sentinel", raw);
        Assert.DoesNotContain("secret-cost", raw);
        Assert.DoesNotContain(privateId, raw);
        Assert.Equal(2, Action(state, root.Id).Action.StepCount);
        Assert.Equal(3, Action(state, root.Id, Side.Shadow).Action.StepCount);
    }

    [Fact]
    public void OpponentsNestedReactionKeepsThePaidActionAndOwnActorAfterReload()
    {
        var (state, first, _) = Army();
        state = Execute(state, first);
        var root = state.Resolution.ActionLog!.Action.Id;
        state.Resolution.Suspend(state, "reaction-test", 1);
        state.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        state = StateCodec.Read(StateCodec.Write(state));
        state = Execute(state, BattleFixture.Command(state, CommandKind.DeclineReaction));
        Assert.Equal(root, state.Resolution.ActionLog!.Action.Id);
        var reply = Action(state, root, Side.FreePeoples, ActionLogFilter.You);
        Assert.Equal(Side.FreePeoples, Assert.Single(reply.Steps).Actor);
        Assert.Equal(Side.Shadow, reply.Action.Initiator);
        Assert.Empty(reply.Steps[0].Costs);
        state = Execute(state, BattleFixture.Command(state, CommandKind.FinishActionSequence));
        Assert.Equal(ActionLogStatus.Completed, Action(state, root).Action.Status);
        Assert.Single(Action(state, root).Steps.SelectMany(s => s.Costs));
    }

    [Fact]
    public void ProjectionCannotMutateFactsAndPreviewCannotPublishThem()
    {
        var (state, first, _) = Army();
        var saved = StateCodec.Write(state);
        var preview = new ActionPreview();
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, first));
        LordsFixture.Engine().DescribePreview(state, first, preview);
        Assert.Equal(saved, StateCodec.Write(state));
        state = Execute(state, first);
        saved = StateCodec.Write(state);
        var action = Action(state, state.Resolution.ActionLog!.Action.Id);
        action.Action.Costs[0].Face = DieFace.Eye;
        action.Action.Context.Regions.Clear();
        action.Steps[0].Movements[0].Participants[0].NationId = "changed";
        action.Steps[0].Movements[0].Route.Clear();
        Assert.Equal(saved, StateCodec.Write(state));
        var rejected = LordsFixture.Engine().Execute(state, Side.Shadow, first, new SequenceRandom());
        Assert.False(rejected.Receipt.Accepted);
        Assert.Equal(saved, StateCodec.Write(rejected.State));
    }
}
