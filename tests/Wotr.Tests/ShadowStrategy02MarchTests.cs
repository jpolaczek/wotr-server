using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy02Fixture;
using static Wotr.Domain.ShadowStrategyCards02;

namespace Wotr.Tests;

public class ShadowStrategy02MarchTests
{
    [Fact]
    public void GatherOffersAReentryRouteWhenAChosenRearguardCanKeepTheSiege()
    {
        var state = BattleFixture.State(Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, Gather);
        var rear = BattleFixture.Put(state, "sauron", UnitKind.Regular, "dol-amroth");
        var moving = BattleFixture.Put(state, "sauron", UnitKind.Regular, "dol-amroth");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "anfalas");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "dol-amroth").Garrison = true;
        state.Control["dol-amroth"] = Side.FreePeoples;
        state = Play(state, Gather);
        var route = new[] { "lamedon", "dol-amroth", "anfalas" };
        var army = new MovementOptions().GetOptions(state, Side.Shadow).Single().Armies.Single(a => a.RegionId == "dol-amroth");
        var offered = Assert.Single(army.Targets, t => t.Route.SequenceEqual(route));
        Assert.Contains(rear.Id, offered.EligibleIds);
        Assert.Contains(moving.Id, offered.EligibleIds);
        var move = Move(state, "dol-amroth", route);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [move] });
        move.UnitIds.Remove(rear.Id);
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [move] });
        Assert.Equal("dol-amroth", state.Units.Single(u => u.Id == rear.Id).Location);
        Assert.Equal("anfalas", state.Units.Single(u => u.Id == moving.Id).Location);
        Assert.Equal(Side.Shadow, state.Control["lamedon"]);
        Assert.True(BoardRules.Besieged(state, "dol-amroth", Side.FreePeoples));
    }

    [Fact]
    public void GatherMovesFixedGroupThreeRegionsAndCapturesEveryTraversedSettlement()
    {
        var state = State(Gather);
        state.Control["north-dunland"] = Side.FreePeoples;
        var extra = BattleFixture.Put(state, "sauron", UnitKind.Elite, "moria");
        var move = Move(state, "moria", "hollin", "north-dunland", "south-dunland");
        move.UnitIds.Remove(extra.Id);
        state = Play(state, Gather);
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [move] });
        Assert.Equal("moria", state.Units.Single(u => u.Id == extra.Id).Location);
        Assert.All(move.UnitIds, id => Assert.Equal("south-dunland", state.Units.Single(u => u.Id == id).Location));
        Assert.Equal(Side.Shadow, state.Control["north-dunland"]);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    [Fact]
    public void GatherRejectsEmptyDestinationReturnToOwnArmyEnemyAndExcessStepsWithoutMutation()
    {
        var state = Play(State(Gather), Gather);
        foreach (var route in new[] { new[] { "hollin" }, new[] { "hollin", "moria" },
            new[] { "hollin", "north-dunland", "hollin", "north-dunland", "south-dunland" } })
            Reject(state, CommandKind.MoveArmies, new() { Moves = [Move(state, "moria", route)] });
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "north-dunland");
        Reject(state, CommandKind.MoveArmies, new() { Moves = [Move(state, "moria", "north-dunland", "south-dunland")] });
    }

    [Fact]
    public void LengthensMovesTwoOriginalArmiesEvenWhenTheDestinationArmyHasLeft()
    {
        var state = State(Lengthens);
        var a = Move(state, "moria", "north-dunland", "south-dunland");
        var b = Move(state, "south-dunland", "north-dunland", "moria");
        state = Play(state, Lengthens);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [a] });
        var reversed = Do(StateCodec.Read(StateCodec.Write(state)), CommandKind.MoveArmies, new() { Moves = [b, a] });
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [a, b] });
        Assert.Equal(state.Units.Select(u => (u.Id, u.Location, u.Garrison)), reversed.Units.Select(u => (u.Id, u.Location, u.Garrison)));
        Assert.All(a.UnitIds, id => Assert.Equal("south-dunland", state.Units.Single(u => u.Id == id).Location));
        Assert.All(b.UnitIds, id => Assert.Equal("moria", state.Units.Single(u => u.Id == id).Location));
    }

    [Fact]
    public void LengthensAllowsOneOnlyWhenSecondArmyCannotCrossItsNationalBorder()
    {
        var state = State(Lengthens);
        var destination = state.Units.Single(u => u.Location == "south-dunland");
        BoardRules.ReturnToReserve(destination);
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "south-dunland");
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 1;
        state = Play(state, Lengthens);
        var option = new MovementOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Equal(1, option.Minimum);
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [Move(state, "moria", "north-dunland", "south-dunland")] });
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    [Fact]
    public void MovingAcceptsFourDifferentArmiesAndRejectsReuseOrAFifthArmy()
    {
        var state = State(Moving);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "hollin");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "north-dunland");
        var moves = new List<ArmyMoveChoice> { Move(state, "moria", "north-dunland"), Move(state, "north-dunland", "hollin"),
            Move(state, "hollin", "moria"), Move(state, "south-dunland", "north-dunland") };
        state = Play(state, Moving);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [.. moves, moves[0]] });
        Reject(state, CommandKind.MoveArmies, new() { Moves = [moves[0], moves[0]] });
        state = Do(state, CommandKind.MoveArmies, new() { Moves = moves });
        Assert.Equal(2, state.Units.Count(u => u.Location == "north-dunland"));
    }

    [Fact]
    public void MovingCanFinishWithoutMovementButGatherAndLengthensCannotSkipLegalMoves()
    {
        foreach (var card in new[] { Gather, Lengthens, Moving })
        {
            var state = Play(State(card), card);
            if (card == Moving)
            {
                state = Do(state, CommandKind.FinishArmyEvent);
                Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
            }
            else
                Reject(state, CommandKind.FinishArmyEvent, new());
        }
    }

    [Fact]
    public void MovementOrderMustPreserveAnExistingSiegeBeforeBesiegerLeaves()
    {
        var state = State(Lengthens);
        state.Control["moria"] = Side.FreePeoples;
        BattleFixture.Put(state, "dwarves", UnitKind.Regular, "moria").Garrison = true;
        var moves = new[] { Move(state, "moria", "north-dunland", "south-dunland"), Move(state, "south-dunland", "north-dunland", "moria") };
        state = Play(state, Lengthens);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [.. moves] });
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [moves[1], moves[0]] });
        Assert.True(state.Units.Single(u => u.Nation == "dwarves" && u.Location == "moria").Garrison);
        Assert.True(BoardRules.Besieged(state, "moria", Side.FreePeoples));
    }

    [Fact]
    public void LengthensCannotSwapTwoBesiegersThroughFreedEnemyArmies()
    {
        var state = BattleFixture.State(Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, Lengthens);
        foreach (var region in new[] { "moria", "lorien" })
        {
            state.Control[region] = Side.FreePeoples;
            BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
            BattleFixture.Put(state, "elves", UnitKind.Regular, region).Garrison = true;
        }
        var first = Move(state, "moria", "dimrill-dale", "lorien");
        var second = Move(state, "lorien", "dimrill-dale", "moria");
        state = Play(state, Lengthens);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [first, second] });
        Reject(state, CommandKind.MoveArmies, new() { Moves = [second, first] });
        Assert.Equal(1, new MovementOptions().GetOptions(state, Side.Shadow).Single().Minimum);
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, new() { Moves = [first] });
        Assert.True(new PreviewService(Engine()).Preview(state, Side.Shadow, command).Legal);
        state = Do(state, CommandKind.MoveArmies, command.Payload);
        Assert.False(state.Units.Single(u => u.Location == "moria" && u.Nation == "elves").Garrison);
        Assert.True(state.Units.Single(u => u.Location == "lorien" && u.Nation == "elves").Garrison);
        Assert.Equal(2, state.Units.Count(u => u.Location == "lorien" && u.Nation == "sauron"));
    }

    [Fact]
    public void LengthensPreviewRequiresAnotherLegalArmyWithoutChangingTheFirstDraft()
    {
        var state = Play(State(Lengthens), Lengthens);
        var first = Move(state, "moria", "north-dunland", "south-dunland");
        var before = StateCodec.Write(state);
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, new() { Moves = [first] });
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.False(preview.Legal);
        Assert.Equal("MoreArmyMovesRequired", preview.Error);
        Assert.Equal(before, StateCodec.Write(state));
        var result = Engine().Execute(state, Side.Shadow, command, new SequenceRandom());
        Assert.False(result.Receipt.Accepted);
        Assert.Equal("MoreArmyMovesRequired", result.Receipt.Error);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Fact]
    public void LeavingARearguardMakesTheSecondBesiegingArmyMoveRequiredAndLegal()
    {
        var state = BattleFixture.State(Side.Shadow);
        EventTests.GiveCard(state, Side.Shadow, Lengthens);
        foreach (var region in new[] { "moria", "lorien" })
        {
            state.Control[region] = Side.FreePeoples;
            BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
            BattleFixture.Put(state, "elves", UnitKind.Regular, region).Garrison = true;
        }
        var rear = BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        var first = Move(state, "moria", "dimrill-dale", "lorien");
        first.UnitIds.Remove(rear.Id);
        var second = Move(state, "lorien", "dimrill-dale", "moria");
        state = Play(state, Lengthens);
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, new() { Moves = [first] });
        Assert.Equal("MoreArmyMovesRequired", new PreviewService(Engine()).Preview(state, Side.Shadow, command).Error);
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [first, second] });
        Assert.All(state.Units.Where(u => u.Nation == "elves" && u.Location is "moria" or "lorien"), u => Assert.True(u.Garrison));
        Assert.Equal(2, state.Units.Count(u => u.Location == "moria" && u.Nation == "sauron"));
        Assert.Single(state.Units, u => u.Location == "lorien" && u.Nation == "sauron");
    }

    [Fact]
    public void MarchCannotMoveAnArrivingFigureAgainWithTheDestinationArmy()
    {
        var state = State(Lengthens);
        var first = Move(state, "moria", "north-dunland", "south-dunland");
        var second = Move(state, "south-dunland", "north-dunland", "moria");
        second.UnitIds.AddRange(first.UnitIds);
        state = Play(state, Lengthens);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [first, second] });
    }

    [Fact]
    public void StackingIsCheckedAfterAllMarchesAndNeverDuringAnIntermediateMerge()
    {
        var state = State(Lengthens);
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "south-dunland");
        var first = Move(state, "moria", "north-dunland", "south-dunland");
        var second = Move(state, "south-dunland", "north-dunland", "moria");
        state = Play(state, Lengthens);
        state = Do(state, CommandKind.MoveArmies, new() { Moves = [first, second] });
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(10, state.Units.Count(u => u.Location == "moria"));
    }

    [Fact]
    public void MarchRejectsAnotherDieGlobalRouteDifferentFinalTargetAndFiguresFromOutsideTheSource()
    {
        var state = Play(State(Gather), Gather);
        var move = Move(state, "moria", "north-dunland", "south-dunland");
        Reject(state, CommandKind.MoveArmies, new() { DieId = state.Dice[0].Id, Moves = [move] });
        Reject(state, CommandKind.MoveArmies, new() { Route = [.. move.Route], Moves = [move] });
        move.To = "hollin";
        Reject(state, CommandKind.MoveArmies, new() { Moves = [move] });
        move.To = "south-dunland";
        move.UnitIds.Add(state.Units.First(u => u.Location == "reserve").Id);
        Reject(state, CommandKind.MoveArmies, new() { Moves = [move] });
    }
}
