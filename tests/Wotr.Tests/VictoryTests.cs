using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class VictoryTests
{
    [Theory]
    [InlineData(10, Side.FreePeoples, VictoryCondition.RingDestroyed, 5)]
    [InlineData(11, Side.Shadow, VictoryCondition.RingCorrupted, 4)]
    public void V11LastStepWaitsForDamageAndCorruptionPrecedesDestruction(int corruption, Side winner, VictoryCondition condition, int step)
    {
        var state = MordorTests.OnTrack(4, corruption);
        HuntTests.Pool(state, "base.hunt.standard.1.0.1");
        SetPoints(state, Side.Shadow, 10);
        SetPoints(state, Side.FreePeoples, 4);
        state = HuntTests.Move(state, new SequenceRandom(0));
        Assert.Equal(4, state.Fellowship.MordorStep);
        Assert.Null(state.Winner);
        Assert.Null(HuntTests.Views().For(state, Side.Shadow).Victory);
        state = HuntTests.Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(winner, state.Winner);
        Assert.Equal(condition, state.Victory);
        Assert.Equal(step, state.Fellowship.MordorStep);
        Assert.Equal(GamePhase.Finished, state.Phase);
        Assert.Null(state.Pending);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Equal(condition, HuntTests.Views().For(state, Side.FreePeoples).Victory);
    }

    [Theory]
    [InlineData(11, 4, null)]
    [InlineData(11, 5, Side.FreePeoples)]
    [InlineData(12, 4, Side.Shadow)]
    [InlineData(12, 5, Side.Shadow)]
    public void RingThresholdsUseCorruptionBeforeTrackPosition(int corruption, int step, Side? winner)
    {
        var state = MordorTests.OnTrack(step, corruption);
        Assert.Equal(winner.HasValue, RingVictory.Check(state));
        Assert.Equal(winner, state.Winner);
    }

    [Theory]
    [InlineData(9, 3, null, null)]
    [InlineData(10, 3, Side.Shadow, VictoryCondition.ShadowMilitary)]
    [InlineData(9, 4, Side.FreePeoples, VictoryCondition.FreePeoplesMilitary)]
    [InlineData(10, 4, Side.Shadow, VictoryCondition.ShadowMilitary)]
    public void MilitaryWinsOnlyInPhaseSixAndShadowWinsASimultaneousThreshold(int shadow, int free, Side? winner, VictoryCondition? condition)
    {
        var state = HuntTests.State(0);
        SetPoints(state, Side.Shadow, shadow);
        SetPoints(state, Side.FreePeoples, free);
        Assert.False(MilitaryVictory.Check(state));
        Assert.Null(state.Winner);
        state.Phase = GamePhase.VictoryCheck;
        Assert.Equal(winner.HasValue, MilitaryVictory.Check(state));
        Assert.Equal(winner, state.Winner);
        Assert.Equal(condition, state.Victory);
    }

    [Fact]
    public void BesiegersPresenceDoesNotAwardPointsAndRetakingHomeDoesNotScore()
    {
        var state = HuntTests.State(0);
        var enemy = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular);
        enemy.Location = "minas-tirith";
        foreach (var unit in state.Units.Where(u => u.Location == "minas-tirith" && u.Nation == "gondor"))
            unit.Garrison = true;
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        state.Control["minas-tirith"] = Side.Shadow;
        Assert.Equal(2, MilitaryVictory.Points(state, Side.Shadow));
        state.Control["minas-tirith"] = Side.FreePeoples;
        Assert.Equal(0, MilitaryVictory.Points(state, Side.FreePeoples));
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
    }

    [Fact]
    public void StopOnLastAttemptDoesNotWinButCancellingTheTileRemovesStop()
    {
        var state = MordorTests.OnTrack(4, 10);
        var id = "base.hunt.special-sh.1.1.1";
        HuntTests.Pool(state, id);
        state = HuntTests.Move(state, new SequenceRandom(0));
        state = HuntTests.Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Null(state.Winner);
        Assert.Equal(4, state.Fellowship.MordorStep);
        Assert.Equal(11, state.Fellowship.Corruption);
        Assert.False(state.Fellowship.Hidden);

        var catalog = new EffectCatalog([new("test.cancel", [new AdjustHuntStep(CancelTile: true)])], [],
            [new("test.cancel", "hunt.table", "test.cancel", new UnconditionalEvent(), "test.cancel", ReactionCost.DiscardTableCard)]);
        state = MordorTests.OnTrack(4, 11);
        state.Player(Side.FreePeoples).Table.Add("test.cancel");
        HuntTests.Pool(state, id);
        state = HuntTests.Move(state, new SequenceRandom(0), catalog);
        state = HuntTests.Do(state, CommandKind.RespondEffect, new() { ReactionId = "test.cancel" }, catalog: catalog);
        Assert.Equal(5, state.Fellowship.MordorStep);
        Assert.Equal(11, state.Fellowship.Corruption);
        Assert.Equal(VictoryCondition.RingDestroyed, state.Victory);
    }

    private static void SetPoints(MatchState state, Side side, int points)
    {
        foreach (var region in BoardRules.Regions.Values.Where(r => r.OriginalOwner == TurnFlow.Other(side) && r.VictoryPoints > 0)
            .OrderByDescending(r => r.VictoryPoints))
        {
            if (region.VictoryPoints > points)
                continue;
            state.Control[region.Id] = side;
            points -= region.VictoryPoints;
        }
        Assert.Equal(0, points);
    }
}
