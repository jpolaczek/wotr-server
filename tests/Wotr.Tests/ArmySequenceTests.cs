using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

public class ArmySequenceTests
{
    [Fact]
    public void SavedFirstMoveCannotMoveAgainAfterMergeAndSecondCostsNothing()
    {
        var state = Board();
        var incoming = Put(state, "gondor", "minas-tirith").Id;
        var resident = Put(state, "gondor", "osgiliath").Id;
        state = Do(state, CommandKind.MoveArmies, Move(state, "minas-tirith", "osgiliath", incoming));
        state = StateCodec.Copy(state);
        Assert.Equal(DecisionKind.ActionSequence, state.Pending!.Kind);
        Assert.Equal(1, state.Resolution.ActionSequence!.Remaining);
        Assert.Single(state.Resolution.PaidDieIds);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        var projection = new Projection(new TurnOptions());
        var own = projection.For(state, Side.FreePeoples);
        var other = projection.For(state, Side.Shadow);
        Assert.Equal(1, other.ActionSequence!.CompletedSteps);
        Assert.Null(other.Decision);
        Assert.Empty(other.Options);
        Assert.DoesNotContain(incoming, own.Options.Single(o => o.Kind == CommandKind.MoveArmies).EligibleIds);
        Reject(state, CommandKind.MoveArmies, Move(state, "osgiliath", "south-ithilien", incoming, resident));
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, Move(state, "osgiliath", "south-ithilien", resident));
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Empty(preview.Costs);
        Assert.Equal("south-ithilien", Assert.Single(preview.Placements).RegionId);
        state = Do(state, command.Kind, command.Payload);
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal("osgiliath", state.Units.Single(u => u.Id == incoming).Location);
        Assert.Equal("south-ithilien", state.Units.Single(u => u.Id == resident).Location);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TemporaryExcessWaitsForSecondMoveOrExplicitFinish(bool finish)
    {
        var state = Board();
        var arrivals = Enumerable.Range(0, 10).Select(_ => Put(state, "gondor", "minas-tirith").Id).ToArray();
        var resident = Put(state, "rohan", "osgiliath").Id;
        state = Do(state, CommandKind.MoveArmies, Move(state, "minas-tirith", "osgiliath", arrivals));
        Assert.Equal(DecisionKind.ActionSequence, state.Pending!.Kind);
        Assert.Equal(11, state.Units.Count(u => u.Location == "osgiliath"));
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.FinishActionSequence));
        Assert.Equal(1, Assert.Single(preview.Excess).Count);
        Assert.Empty(preview.Costs);
        state = StateCodec.Copy(state);
        state = finish ? Do(state, CommandKind.FinishActionSequence)
            : Do(state, CommandKind.MoveArmies, Move(state, "osgiliath", "south-ithilien", resident));
        Assert.Null(state.Resolution.ActionSequence);
        if (finish)
        {
            Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
            Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
            state = Do(StateCodec.Copy(state), CommandKind.RemoveExcess, new() { Ids = [resident] });
            Assert.Equal("reserve", state.Units.Single(u => u.Id == resident).Location);
        }
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void PoliticalConsequencesWaitForFinishAndDoNotAdvanceTwice()
    {
        var state = Board(Side.Shadow);
        var north = state.Nations.Single(n => n.Id == "north");
        north.Active = false;
        north.StepsToWar = 3;
        var army = Put(state, "sauron", "woodland-realm").Id;
        state = Do(state, CommandKind.MoveArmies, Move(state, "woodland-realm", "dale", army));
        Assert.Equal(Side.Shadow, state.Control["dale"]);
        Assert.False(state.Nations.Single(n => n.Id == "north").Active);
        Assert.NotEmpty(state.PoliticalReactions);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.FinishActionSequence));
        Assert.Equal(2, Assert.Single(preview.PoliticsChanges).StepsToWar);
        Assert.False(state.Nations.Single(n => n.Id == "north").Active);
        state = Do(StateCodec.Copy(state), CommandKind.FinishActionSequence);
        Assert.True(state.Nations.Single(n => n.Id == "north").Active);
        Assert.Equal(2, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.Empty(state.PoliticalReactions);
    }

    [Theory]
    [InlineData("die")]
    [InlineData("face")]
    [InlineData("ability")]
    [InlineData("legacy")]
    [InlineData("batch")]
    [InlineData("kind")]
    public void ContinuationCannotChangeCostKindOrReuseLegacyBatch(string change)
    {
        var state = Board();
        var first = Put(state, "gondor", "minas-tirith").Id;
        var next = Put(state, "north", "dale").Id;
        state = Do(state, CommandKind.MoveArmies, Move(state, "minas-tirith", "osgiliath", first));
        var payload = Move(state, "dale", "erebor", next);
        var kind = CommandKind.MoveArmies;
        switch (change)
        {
            case "die":
                payload.DieId = state.Dice[1].Id;
                break;
            case "face":
                payload.MovementCost = DieFace.Character;
                break;
            case "ability":
                payload.AbilityId = "invented";
                break;
            case "legacy":
                payload.Stepwise = false;
                break;
            case "batch":
                payload.Moves.Add(payload.Moves[0]);
                break;
            case "kind":
                kind = CommandKind.Recruit;
                break;
        }
        Reject(state, kind, payload);
    }

    [Fact]
    public void CharacterCostFinishesAfterOneAndStillRequiresTheMovingCommander()
    {
        var state = Board();
        state.Dice[0].Face = DieFace.Character;
        var army = Put(state, "gondor", "minas-tirith").Id;
        var leader = Put(state, "gondor", "minas-tirith", UnitKind.Leader).Id;
        var payload = Move(state, "minas-tirith", "osgiliath", army);
        payload.MovementCost = DieFace.Character;
        Reject(state, CommandKind.MoveArmies, payload);
        payload.Moves[0].UnitIds.Add(leader);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }
}
