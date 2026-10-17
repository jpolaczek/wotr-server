using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class MordorTests
{
    internal static MatchState OnTrack(int step = 2, int corruption = 4, int hunt = 2)
    {
        var state = HuntTests.State(hunt);
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = step;
        state.Fellowship.Corruption = corruption;
        return state;
    }

    [Theory]
    [InlineData("morannon", true)]
    [InlineData("morannon", false)]
    [InlineData("minas-morgul", true)]
    [InlineData("minas-morgul", false)]
    public void EntryIsOptionalInTheFellowshipPhaseAndPreservesHiddenState(string region, bool hidden)
    {
        var state = FellowshipTests.Phase(region);
        state.Fellowship.Hidden = hidden;
        var option = HuntTests.Views().For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.EnterMordor);
        Assert.Empty(option.EligibleDice);
        var stayed = FellowshipTests.Do(StateCodec.Copy(state), CommandKind.ContinueFellowship, new());
        Assert.Null(stayed.Fellowship.MordorStep);
        Assert.Equal(region, stayed.Fellowship.RegionId);
        state = FellowshipTests.Do(state, CommandKind.EnterMordor, new());
        Assert.Equal(0, state.Fellowship.MordorStep);
        Assert.Equal(MordorRules.Track, state.Fellowship.RegionId);
        Assert.False(BoardRules.Regions.ContainsKey(state.Fellowship.RegionId));
        Assert.Equal(hidden, state.Fellowship.Hidden);
        MordorProjectionTests.AssertActivity(state, false);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        HuntTests.Reject(state, CommandKind.EnterMordor, new());
        HuntTests.Reject(state, CommandKind.DeclareFellowship, new() { Route = ["gorgoroth"] });
    }

    [Fact]
    public void EntryFollowsDeclarationButRejectsOtherRegionsAndActionPhase()
    {
        var state = FellowshipTests.Phase("north-ithilien", 1);
        HuntTests.Reject(state, CommandKind.EnterMordor, new());
        state = FellowshipTests.Do(state, CommandKind.DeclareFellowship, new() { Route = ["north-ithilien", "minas-morgul"] });
        state = FellowshipTests.Do(state, CommandKind.EnterMordor, new());
        Assert.Equal(0, state.Fellowship.MordorStep);
        state = HuntTests.State(0, "minas-morgul");
        HuntTests.Reject(state, CommandKind.EnterMordor, new());
    }

    [Fact]
    public void EntryReturnsDiscardedEyesAndPreparedSpecialsButNotNumberedOrRemovedTiles()
    {
        var state = FellowshipTests.Phase("morannon");
        var returnedEye = state.HuntTiles.Single(t => t.Id == HuntTests.Eye);
        returnedEye.Location = "discard";
        var removedEye = state.HuntTiles.Single(t => t.Id == "base.hunt.standard.eye.1.2");
        removedEye.Location = "removed";
        var number = state.HuntTiles.Single(t => t.Id == HuntTests.Three);
        number.Location = "discard";
        var specialId = "base.hunt.special-sh.die.0.1";
        HuntPool.PrepareSpecial(state, specialId);
        state = FellowshipTests.Do(state, CommandKind.EnterMordor, new());
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == returnedEye.Id).Location);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == removedEye.Id).Location);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == number.Id).Location);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == specialId).Location);
        Assert.Equal("set-aside", state.HuntTiles.Single(t => t.Id == "base.hunt.special-fp.-2.0.1").Location);
    }

    public static IEnumerable<object[]> Tiles()
    {
        // Expected printed inventory: independent of the production definition parser.
        for (var i = 1; i <= 4; i++)
            yield return [$"base.hunt.standard.eye.1.{i}", 3, true, false];
        for (var i = 1; i <= 3; i++)
            yield return [$"base.hunt.standard.3.0.{i}", 3, false, false];
        for (var i = 1; i <= 2; i++)
        {
            yield return [$"base.hunt.standard.2.0.{i}", 2, false, false];
            yield return [$"base.hunt.standard.1.0.{i}", 1, false, false];
            yield return [$"base.hunt.standard.1.1.{i}", 1, true, false];
            yield return [$"base.hunt.standard.0.1.{i}", 0, true, false];
            yield return [$"base.hunt.special-fp.0.0.{i}", 0, false, false];
        }
        yield return ["base.hunt.standard.2.1.1", 2, true, false];
        yield return ["base.hunt.special-fp.-2.0.1", -2, false, false];
        yield return ["base.hunt.special-fp.-1.0.1", -1, false, false];
        yield return ["base.hunt.special-sh.die.0.1", 5, false, true];
        yield return ["base.hunt.special-sh.eye.1.1", 3, true, true];
        yield return ["base.hunt.special-sh.3.0.1", 3, false, true];
        yield return ["base.hunt.special-sh.1.1.1", 1, true, true];
    }

    [Theory]
    [MemberData(nameof(Tiles))]
    public void EveryPrintedTileResolvesBeforeMordorProgress(string id, int value, bool reveal, bool stop)
    {
        var state = OnTrack();
        state.Dice[1].Location = DieLocation.Hunt;
        state.Dice[1].HuntOrigin = DiceRules.FellowshipMove;
        HuntTests.Pool(state, id);
        var command = FellowshipTests.Command(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
        var preview = new PreviewService(HuntTests.Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(2, preview.Fellowship!.MordorStep);
        var random = id.Contains(".die.") ? new SequenceRandom(0, 4) : new SequenceRandom(0);
        state = HuntTests.Move(state, random);
        Assert.Equal(id.Contains(".die.") ? 2 : 1, random.Calls);
        if (value > 0)
        {
            Assert.Equal(2, state.Fellowship.MordorStep);
            Assert.Null(state.Winner);
            Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
            Assert.Empty(HuntTests.Views().For(state, Side.FreePeoples).Hunt!.Dice);
            state = HuntTests.Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        }
        Assert.Equal(stop ? 2 : 3, state.Fellowship.MordorStep);
        Assert.Equal(4 + value, state.Fellowship.Corruption);
        Assert.Equal(!reveal, state.Fellowship.Hidden);
        Assert.Equal(0, state.Fellowship.Progress);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.Equal(state.Turn, state.Fellowship.LastMordorAttemptTurn);
        MordorProjectionTests.AssertActivity(state, true);
        Assert.DoesNotContain(state.Events, e => e.Kind is "HuntRolled" or "HuntRerolled");
        Assert.Single(state.Events, e => e.Kind == "HuntTileDrawn");
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void ZeroHuntDiceStillDrawsAnEyeAndRevealsWithoutStoppingProgress()
    {
        var state = OnTrack(hunt: 0);
        HuntTests.Pool(state, HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(0));
        Assert.Equal(3, state.Fellowship.MordorStep);
        Assert.Equal(4, state.Fellowship.Corruption);
        Assert.False(state.Fellowship.Hidden);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        HuntTests.Reject(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[1].Id });
    }

    [Fact]
    public void StopAndHidingBothCountAsAttemptsButHidingDoesNotAdvanceOrUseHunt()
    {
        var state = OnTrack();
        HuntTests.Pool(state, "base.hunt.special-sh.1.1.1");
        state = HuntTests.Move(state, new SequenceRandom(0));
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(2, state.Fellowship.MordorStep);
        EndTurn(state);
        Assert.Equal(5, state.Fellowship.Corruption);
        Assert.Equal(2, state.Turn);
        MordorProjectionTests.AssertActivity(state, false);

        state = OnTrack();
        state.Fellowship.Hidden = false;
        state = HuntTests.Do(state, CommandKind.HideFellowship, new() { DieId = state.Dice[0].Id });
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(2, state.Fellowship.MordorStep);
        MordorProjectionTests.AssertActivity(state, true);
        EndTurn(state);
        Assert.Equal(4, state.Fellowship.Corruption);
    }

    [Fact]
    public void AnInactiveMordorTurnAddsOneCorruptionBeforeMilitaryVictory()
    {
        var state = OnTrack(corruption: 10);
        EndTurn(state);
        Assert.Equal(11, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "MordorInactivity");
        state = OnTrack(corruption: 11);
        state.Control["moria"] = Side.FreePeoples;
        state.Control["dol-guldur"] = Side.FreePeoples;
        EndTurn(state);
        Assert.Equal(Side.Shadow, state.Winner);
        Assert.Equal(VictoryCondition.RingCorrupted, state.Victory);
        Assert.Equal(1, state.Turn);
    }

    [Fact]
    public void ForcedSeparationRemovesTheGroupWithoutAMapRouteButNormalSeparationIsForbidden()
    {
        var state = OnTrack();
        var ids = state.Fellowship.Companions.ToList();
        HuntTests.Reject(state, CommandKind.SeparateCompanions, new() { DieId = state.Dice[0].Id, Ids = ids });
        FellowshipFlow.BeginSeparation(state, "test-effect", FellowshipReturn.Decision);
        var option = Assert.Single(HuntTests.Views().For(state, Side.FreePeoples).Options);
        Assert.Empty(option.Travel);
        HuntTests.Reject(state, CommandKind.SeparateCompanions, new() { Ids = ids, Route = ["gorgoroth"] });
        var preview = new PreviewService(HuntTests.Engine()).Preview(state, Side.FreePeoples,
            FellowshipTests.Command(state, CommandKind.SeparateCompanions, new() { Ids = ids }));
        Assert.Equal(ids, preview.RemovedIds);
        Assert.Empty(preview.Placements);
        state = HuntTests.Do(StateCodec.Copy(state), CommandKind.SeparateCompanions, new() { Ids = ids });
        Assert.Equal("base.character.gollum", state.Fellowship.Guide);
        Assert.Empty(state.Fellowship.Companions);
        Assert.All(state.Characters, c => Assert.Equal("casualties", c.Location));
        Assert.Empty(HuntTests.Views().For(state, Side.Shadow).Characters);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Fact]
    public void EventMovementCountsAsAnAttemptAndSpendsItsCostOutsideHunt()
    {
        var state = OnTrack(hunt: 0);
        HuntTests.Pool(state, "base.hunt.special-fp.0.0.1");
        DiceRules.Pay(state, Side.FreePeoples, state.Dice[0].Id, DieFace.Character);
        state.Resolution.Frames.Add(new() { EffectId = "test.move", Effect = new() { Owner = Side.FreePeoples, Return = EffectReturn.FinishAction } });
        state.Pending = null;
        var catalog = new EffectCatalog([new("test.move", [new MoveFellowshipStep()])], []);
        HuntResolutionTests.Pump(state, catalog, new SequenceRandom(0));
        Assert.Equal(3, state.Fellowship.MordorStep);
        Assert.Equal(state.Turn, state.Fellowship.LastMordorAttemptTurn);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        MordorProjectionTests.AssertActivity(state, true);
        EndTurn(state);
        Assert.Equal(4, state.Fellowship.Corruption);
        MordorProjectionTests.AssertActivity(state, false);
    }

    [Fact]
    public void AnEventEyeInMordorDoesNotInheritMovementDamageOrAdvanceTheTrack()
    {
        var state = OnTrack(hunt: 7);
        HuntTests.Pool(state, HuntTests.Eye);
        HuntRuntime.BeginTile(state, "test-event");
        HuntResolutionTests.Pump(state, EffectCatalog.Base, new SequenceRandom(0));
        Assert.Equal(4, state.Fellowship.Corruption);
        Assert.Equal(2, state.Fellowship.MordorStep);
        Assert.False(state.Fellowship.Hidden);
        Assert.Null(state.Pending);
        Assert.Empty(state.Resolution.Frames);
    }

    private static void EndTurn(MatchState state)
    {
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Available))
            die.Location = DieLocation.Spent;
        TurnFlow.SelectNext(state, Side.FreePeoples);
    }
}
