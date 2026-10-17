using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionLogFamilyTests;
using static Wotr.Tests.HuntTests;

namespace Wotr.Tests;

public class ActionLogHuntFactsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CompletedChainRemainsAvailableAfterTheNextHiddenMove(int level)
    {
        var state = Move(State(level), level == 0 ? new SequenceRandom() : new SequenceRandom(0));
        AssertReport(state);
        var chain = state.HuntReport!.Id;
        var before = Json(Steps(state).Where(s => s.Context.HuntChainId == chain).ToList());
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Move(state, level == 0 ? new SequenceRandom() : new SequenceRandom(0));
        AssertReport(state);
        Assert.NotEqual(chain, state.HuntReport!.Id);
        Assert.Equal(before, Json(Steps(StateCodec.Copy(state)).Where(s => s.Context.HuntChainId == chain).ToList()));
        Assert.All(Steps(state).Where(s => s.Command == CommandKind.MoveFellowship), s =>
        {
            Assert.Empty(s.Movements);
            Assert.Empty(s.Facts.FellowshipRoute);
        });
    }

    [Fact]
    public void NestedRealProtectionCardsHaveSeparateCostsAndKeepTheSameChain()
    {
        var state = HuntProtectionCardTests.ShelobReplacement();
        var root = Steps(state).Single(s => s.Command == CommandKind.MoveFellowship);
        state = Do(state, CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ProtectReaction });
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        AssertReport(state);
        var steps = Steps(state).Where(s => s.ActionId == root.ActionId).ToList();
        Assert.Single(steps.SelectMany(s => s.Costs), c => c.Kind == ActionCostKind.Die);
        Assert.Equal(2, steps.SelectMany(s => s.Costs).Count(c => c.Kind == ActionCostKind.Card));
        Assert.Equal(ActionLogStatus.Completed, steps.Last().Status);
        Assert.Equal(ActionLogStatus.InProgress, steps.Single(s => s.Id == root.Id).Status);
        Assert.Equal(2, steps.Last(s => s.Facts.Hunt is not null).Facts.Hunt!.Entry.Outcome!.Corruption);
    }

    [Theory]
    [InlineData(8, Side.FreePeoples)]
    [InlineData(9, Side.Shadow)]
    public void MordorVictoryIsRecordedOnlyAfterThePendingDamage(int corruption, Side winner)
    {
        var state = MordorTests.OnTrack(4, corruption);
        Pool(state, Three);
        state = Move(state, new SequenceRandom(0));
        Assert.DoesNotContain(Steps(state), s => s.Facts.Outcome?.Winner.HasValue == true);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        AssertReport(state);
        Assert.Equal(winner, Assert.Single(Steps(state), s => s.Facts.Outcome?.Winner.HasValue == true).Facts.Outcome!.Winner);
        Assert.Equal(ActionLogStatus.Completed, Steps(state).Last().Status);
    }

    internal static void AssertReport(MatchState state)
    {
        var report = state.HuntReport!;
        foreach (var side in Enum.GetValues<Side>())
        {
            var facts = Steps(state, side).Where(s => s.Facts.Hunt?.ChainId == report.Id).Select(s => s.Facts.Hunt!).ToList();
            Assert.Equal(Json(report.Entries), Json(facts.Where(f => !f.Completed).Select(f => f.Entry).ToList()));
            Assert.Equal(report.Completed ? 1 : 0, facts.Count(f => f.Completed));
        }
    }
}
