using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsRingRecoveryTests
{
    internal static MatchState Choice()
    {
        var state = State();
        state.Rings[0].Owner = null;
        state.Rings[1].Owner = Side.Shadow;
        state.Rings[2].Owner = Side.Shadow;
        return Play(state, LordsFreePeoplesCards.ThreeRings, 4, 5, 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ExactlyThreeDiceAreRolledAndEachFiveOrSixRecoversOneRing(int successes)
    {
        var state = State();
        foreach (var ring in state.Rings)
            ring.Owner = null;
        state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        state = Play(state, LordsFreePeoplesCards.ThreeRings, Enumerable.Range(0, 3).Select(i => i < successes ? 4 : 3).ToArray());
        if (successes is 1 or 2)
            state = Choose(state, state.Rings.Take(successes).Select(r => r.Id).ToArray());
        Assert.Equal(successes, state.Rings.Count(r => r.Owner == Side.FreePeoples));
        Assert.True(state.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void RemovedRingsMustBeTakenBeforeChoosingShadowOwnedIdentities()
    {
        var state = Choice();
        Assert.Equal(2, state.Pending!.RequiredCount);
        var rings = state.Rings;
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [rings[1].Id, rings[2].Id] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [rings[0].Id, rings[0].Id] });
        state = Choose(state, rings[0].Id, rings[2].Id);
        Assert.Equal(Side.Shadow, state.Rings[1].Owner);
        Assert.Equal(Side.FreePeoples, state.Rings[2].Owner);
        Assert.Equal(3, state.RandomSamples.Count);
    }

    [Fact]
    public void TooManySuccessesRecoverOnlyAvailableRingsAndNoRingsStillRolls()
    {
        var state = State();
        state.Rings[1].Owner = Side.Shadow;
        state = Play(state, LordsFreePeoplesCards.ThreeRings, 5, 5, 5);
        Assert.All(state.Rings, r => Assert.Equal(Side.FreePeoples, r.Owner));
        Assert.Empty(state.Resolution.Frames);
        state = Play(State(), LordsFreePeoplesCards.ThreeRings, 5, 5, 5);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }
}
