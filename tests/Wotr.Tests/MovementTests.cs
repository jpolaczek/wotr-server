using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class MovementTests
{
    private static readonly GameEngine Engine = new(new TurnCommands(), new MusterCommands(), new MovementCommands());

    [Fact]
    public void TwoDistantArmiesMoveWithOneDieAndPreviewMatchesTheResult()
    {
        var state = Arrange();
        var first = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var second = Put(state, "north", UnitKind.Regular, "dale");
        var command = Command(state, Move("minas-tirith", "osgiliath", first), Move("dale", "erebor", second));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.DoesNotContain("osgiliath", preview.ControlChanges.Keys);
        Assert.Equal(before, StateCodec.Write(state));
        var result = Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        foreach (var placement in preview.Placements)
            Assert.Equal(placement.RegionId, result.State.Units.Single(u => u.Id == placement.UnitId).Location);
        Assert.Equal(1, result.State.Dice.Count(d => d.Location == DieLocation.Spent));
        Assert.Equal(state.Units.Count, result.State.Units.Select(u => u.Id).Distinct().Count());
    }

    [Fact]
    public void MergedArrivalsCannotMoveAgainButTheOriginalDestinationArmyCan()
    {
        var state = Arrange();
        var first = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var second = Put(state, "gondor", UnitKind.Regular, "osgiliath");
        var illegal = Command(state, Move("minas-tirith", "osgiliath", first), Move("osgiliath", "south-ithilien", first, second));
        AssertRejected(state, illegal);
        var legal = Command(state, Move("minas-tirith", "osgiliath", first), Move("osgiliath", "south-ithilien", second));
        var result = Engine.Execute(state, Side.FreePeoples, legal, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal("osgiliath", result.State.Units.Single(u => u.Id == first.Id).Location);
        Assert.Equal("south-ithilien", result.State.Units.Single(u => u.Id == second.Id).Location);
    }

    [Fact]
    public void OneArmyCanSplitIntoTwoDisjointMovingGroups()
    {
        var state = Arrange();
        var first = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var second = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var result = Engine.Execute(state, Side.FreePeoples,
            Command(state, Move("minas-tirith", "osgiliath", first), Move("minas-tirith", "lossarnach", second)), new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
    }

    [Fact]
    public void TemporaryExcessIsEvaluatedOnlyAfterBothMovements()
    {
        var state = Arrange();
        var arrivals = Enumerable.Range(0, 10).Select(_ => Put(state, "gondor", UnitKind.Regular, "minas-tirith")).ToArray();
        var residents = Enumerable.Range(0, 2).Select(_ => Put(state, "rohan", UnitKind.Regular, "osgiliath")).ToArray();
        var command = Command(state, Move("minas-tirith", "osgiliath", arrivals), Move("osgiliath", "south-ithilien", residents));
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Empty(preview.Excess);
        var result = Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal(DecisionKind.Action, result.State.Pending!.Kind);
        Assert.Equal(10, result.State.Units.Count(u => u.Location == "osgiliath"));
    }

    [Fact]
    public void ExcessDecisionAndItsLegalChoicesSurviveRoundTrip()
    {
        var state = Arrange();
        var arrivals = Enumerable.Range(0, 10).Select(_ => Put(state, "gondor", UnitKind.Regular, "minas-tirith")).ToArray();
        var resident = Put(state, "rohan", UnitKind.Elite, "osgiliath");
        var command = Command(state, Move("minas-tirith", "osgiliath", arrivals));
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, command);
        Assert.Equal(1, Assert.Single(preview.Excess).Count);
        var result = Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal(DecisionKind.RemoveExcess, result.State.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, result.State.Dice[0].Location);
        var restored = StateCodec.Read(StateCodec.Write(result.State));
        var options = new MusterOptions();
        Assert.Equal(options.GetOptions(result.State, Side.FreePeoples).Single().EligibleIds,
            options.GetOptions(restored, Side.FreePeoples).Single().EligibleIds);
        var remove = KernelTests.Command(restored, CommandKind.RemoveExcess);
        remove.Payload.Ids = [resident.Id];
        var finished = Engine.Execute(restored, Side.FreePeoples, remove, new SequenceRandom());
        Assert.True(finished.Receipt.Accepted);
        Assert.Equal("reserve", finished.State.Units.Single(u => u.Id == resident.Id).Location);
        Assert.Equal(DieLocation.Spent, finished.State.Dice[0].Location);
    }

    [Fact]
    public void MixedNationsCheckEveryFigureAndNeutralRegionsRemainAvailable()
    {
        var state = Arrange();
        var north = state.Nations.Single(n => n.Id == "north");
        north.StepsToWar = 1;
        var peaceful = Put(state, "north", UnitKind.Regular, "dale");
        var ready = Put(state, "gondor", UnitKind.Regular, "dale");
        AssertRejected(state, Command(state, Move("dale", "erebor", peaceful, ready)));
        Assert.True(Engine.Execute(state, Side.FreePeoples, Command(state, Move("dale", "erebor", ready)), new SequenceRandom()).Receipt.Accepted);
        peaceful.Location = "bree";
        Assert.True(Engine.Execute(state, Side.FreePeoples, Command(state, Move("bree", "south-downs", peaceful)), new SequenceRandom()).Receipt.Accepted);
        Assert.True(Engine.Execute(state, Side.FreePeoples, Command(state, Move("bree", "north-downs", peaceful)), new SequenceRandom()).Receipt.Accepted);
    }

    [Fact]
    public void CharacterCostNeedsTheCommanderInTheMovingGroupAndAllowsOnlyOneMove()
    {
        var state = Arrange();
        state.Dice[0].Face = DieFace.Character;
        var regular = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var left = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var leader = Put(state, "gondor", UnitKind.Leader, "minas-tirith");
        var missing = Command(state, Move("minas-tirith", "osgiliath", regular));
        missing.Payload.MovementCost = DieFace.Character;
        AssertRejected(state, missing);
        var command = Command(state, Move("minas-tirith", "osgiliath", regular, leader));
        command.Payload.MovementCost = DieFace.Character;
        Assert.True(Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom()).Receipt.Accepted);
        command.Payload.Moves.Add(Move("minas-tirith", "lossarnach", left));
        AssertRejected(state, command);
    }

    [Fact]
    public void FreePeoplesLeadersCannotBeStrandedButNazgulAndCharactersCanStay()
    {
        var state = Arrange();
        var regular = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var leader = Put(state, "gondor", UnitKind.Leader, "minas-tirith");
        AssertRejected(state, Command(state, Move("minas-tirith", "osgiliath", regular)));
        BoardRules.ReturnToReserve(leader);
        state.Characters.Add(Character("fixed", Side.FreePeoples, "minas-tirith", 0));
        Assert.True(Engine.Execute(state, Side.FreePeoples, Command(state, Move("minas-tirith", "osgiliath", regular)), new SequenceRandom()).Receipt.Accepted);
        var shadow = Arrange(Side.Shadow);
        var orc = Put(shadow, "sauron", UnitKind.Regular, "minas-tirith");
        Put(shadow, "sauron", UnitKind.Nazgul, "minas-tirith");
        Assert.True(Engine.Execute(shadow, Side.Shadow, Command(shadow, Move("minas-tirith", "osgiliath", orc)), new SequenceRandom()).Receipt.Accepted);
    }

    [Fact]
    public void CharacterMovementWithArmyUsesLevelAndActivatesItsNationAtDestination()
    {
        var state = Arrange();
        var unit = Put(state, "rohan", UnitKind.Regular, "lossarnach");
        state.Nations.Single(n => n.Id == "gondor").Active = false;
        var character = Character("fixture-guide", Side.FreePeoples, "lossarnach", 2);
        character.NationIcon = "gondor";
        state.Characters.Add(character);
        state.Dice[0].Face = DieFace.Character;
        var command = Command(state, Move("lossarnach", "minas-tirith", unit));
        command.Payload.MovementCost = DieFace.Character;
        command.Payload.Moves[0].CharacterIds.Add(character.Id);
        var result = Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal("minas-tirith", Assert.Single(result.State.Characters).Location);
        Assert.True(result.State.Nations.Single(n => n.Id == "gondor").Active);
        character.Level = 0;
        AssertRejected(state, command);
    }

    [Theory]
    [InlineData("black-border")]
    [InlineData("enemy-army")]
    [InlineData("garrison")]
    [InlineData("foreign-unit")]
    [InlineData("duplicate")]
    [InlineData("nonadjacent")]
    [InlineData("no-army")]
    [InlineData("wrong-cost")]
    public void IllegalMovementDoesNotChangePiecesCostOrRandomness(string fault)
    {
        var state = Arrange();
        var unit = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        var move = Move("minas-tirith", "osgiliath", unit);
        switch (fault)
        {
            case "black-border":
                unit.Location = move.From = "rivendell";
                move.To = "high-pass";
                break;
            case "enemy-army":
                Put(state, "sauron", UnitKind.Regular, "osgiliath");
                break;
            case "garrison":
                unit.Garrison = true;
                break;
            case "foreign-unit":
                unit.Nation = "sauron";
                break;
            case "duplicate":
                move.UnitIds.Add(unit.Id);
                break;
            case "nonadjacent":
                move.To = "erebor";
                break;
            case "no-army":
                unit.Kind = UnitKind.Leader;
                break;
            case "wrong-cost":
                state.Dice[0].Face = DieFace.Event;
                break;
        }
        AssertRejected(state, Command(state, move));
    }

    [Fact]
    public void EmptySettlementCaptureActivatesAndAdvancesOnlyOnceAndPreviewShowsIt()
    {
        var state = Arrange(Side.Shadow);
        var north = state.Nations.Single(n => n.Id == "north");
        north.Active = false;
        north.StepsToWar = 3;
        var orc = Put(state, "sauron", UnitKind.Regular, "erebor");
        var command = Command(state, Move("erebor", "dale", orc));
        var preview = new PreviewService(Engine).Preview(state, Side.Shadow, command);
        Assert.Equal(Side.Shadow, preview.ControlChanges["dale"]);
        Assert.True(Assert.Single(preview.PoliticsChanges).Active);
        Assert.Equal(2, preview.PoliticsChanges[0].StepsToWar);
        var result = Engine.Execute(state, Side.Shadow, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.Equal(Side.Shadow, result.State.Control["dale"]);
        Assert.Equal(1, MilitaryVictory.Points(result.State, Side.Shadow));
    }

    [Fact]
    public void EnteringAFortificationActivatesWithoutSettlementCaptureOrPoliticalAdvance()
    {
        var state = Arrange(Side.Shadow);
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        rohan.Active = false;
        rohan.StepsToWar = 3;
        var orc = Put(state, "sauron", UnitKind.Regular, "orthanc");
        var result = Engine.Execute(state, Side.Shadow, Command(state, Move("orthanc", "fords-of-isen", orc)), new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        var after = result.State.Nations.Single(n => n.Id == "rohan");
        Assert.True(after.Active);
        Assert.Equal(3, after.StepsToWar);
        Assert.Equal(state.Control["fords-of-isen"], result.State.Control["fords-of-isen"]);
        Assert.DoesNotContain(result.State.Events, e => e.Kind == "SettlementCaptured");
    }

    [Fact]
    public void ReinforceAndLeaveSiegesWithoutCapturingTheOccupiedStronghold()
    {
        var state = Arrange(Side.Shadow);
        var garrison = Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        garrison.Garrison = true;
        var besieger = Put(state, "sauron", UnitKind.Regular, "minas-tirith");
        var incoming = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        var reinforced = Engine.Execute(state, Side.Shadow, Command(state, Move("osgiliath", "minas-tirith", incoming)), new SequenceRandom());
        Assert.True(reinforced.Receipt.Accepted);
        Assert.Equal(Side.FreePeoples, reinforced.State.Control["minas-tirith"]);
        var left = Engine.Execute(state, Side.Shadow, Command(state, Move("minas-tirith", "lossarnach", besieger)), new SequenceRandom());
        Assert.True(left.Receipt.Accepted);
        Assert.False(left.State.Units.Single(u => u.Id == garrison.Id).Garrison);
        var tooLate = Command(state, Move("minas-tirith", "lossarnach", besieger), Move("osgiliath", "minas-tirith", incoming));
        AssertRejected(state, tooLate);
    }

    [Fact]
    public void OptionsExposeCandidatesWithoutSubsetsAndDoNotPublishOffBoardCharacterDefinitions()
    {
        var state = Arrange();
        Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        state.Characters.Add(Character("on-board", Side.FreePeoples, "minas-tirith", 2));
        state.Characters.Add(Character("secret-version", Side.FreePeoples, "fellowship", 2));
        var option = new MovementOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Single(option.Armies);
        Assert.Contains(option.Armies[0].Targets, t => t.RegionId == "osgiliath");
        var view = new Projection(new MovementOptions()).For(state, Side.Shadow);
        Assert.Equal("on-board", Assert.Single(view.Characters).Id);
        Assert.Empty(view.Options);
    }

    private static MatchState Arrange(Side side = Side.FreePeoples)
    {
        var state = TurnTests.Actions(actor: side);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        { nation.Active = true; nation.StepsToWar = 0; }
        foreach (var die in state.Dice.Where(d => d.Owner == side))
            die.Face = DieFace.Army;
        return state;
    }
    private static UnitState Put(MatchState state, string nation, UnitKind kind, string region)
    {
        var unit = state.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
        unit.Location = region;
        return unit;
    }
    private static CharacterState Character(string id, Side side, string region, int level) =>
        new() { Id = id, DefinitionId = id, Side = side, Location = region, Level = level, Leadership = 1 };
    private static ArmyMoveChoice Move(string from, string to, params UnitState[] units) =>
        new() { From = from, To = to, UnitIds = units.Select(u => u.Id).ToList() };
    private static GameCommand Command(MatchState state, params ArmyMoveChoice[] moves)
    {
        var command = KernelTests.Command(state, CommandKind.MoveArmies);
        command.Payload = new() { DieId = state.Dice.First(d => d.Owner == state.Pending!.Owner).Id, Moves = [.. moves] };
        return command;
    }
    private static void AssertRejected(MatchState state, GameCommand command)
    {
        var before = StateCodec.Write(state);
        var result = Engine.Execute(state, state.Pending!.Owner, command, new SequenceRandom());
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(result.State));
    }
}
