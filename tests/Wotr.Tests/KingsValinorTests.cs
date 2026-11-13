using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsValinorTests
{
    [Fact]
    public void RequiresCapturedElvenStrongholdOrAnyCorruptedSovereign()
    {
        var state = State(Side.Shadow);
        var payload = Cost(state, Valinor);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Control["lorien"] = Side.Shadow;
        Assert.True(EffectCatalog.All.Events[Valinor].Condition.CanPlay(state, Side.Shadow));
        state.Control["lorien"] = Side.FreePeoples;
        Corrupt(state, SovereignCatalog.Denethor);
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.Contains(Valinor, state.Player(Side.Shadow).Discard);
        Assert.Empty(state.RandomSamples);
    }

    [Theory]
    [InlineData(1, 4, 0)]
    [InlineData(1, 5, 5)]
    [InlineData(2, 4, 5)]
    public void CapsAtFiveElvenArmyDiceAndScoresPrintedCorruptionThreshold(int corrupted, int roll, int hits)
    {
        var state = Ready(Valinor);
        if (corrupted == 2)
            Corrupt(state, SovereignCatalog.Denethor);
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien");
        BattleFixture.Put(state, "elves", UnitKind.Elite, "lorien");
        BattleFixture.Put(state, "elves", UnitKind.Leader, "lorien");
        var north = BattleFixture.Put(state, "north", UnitKind.Elite, "lorien");
        state = Do(state, CommandKind.PlayEvent, Cost(state, Valinor), Enumerable.Repeat(roll, 5).ToArray());
        Assert.Equal(5, state.RandomSamples.Count);
        Assert.Equal(hits, EventLosses.Current(state)?.Hits ?? 0);
        if (hits > 0)
        {
            Assert.Contains(north.Id, new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds);
            Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        }
    }

    // Three Elven Strongholds hold one Regular each; every die hits. The last stronghold is the
    // lone option, struck without a choice in the trail.
    internal static IReadOnlyList<(MatchState State, string[] Targets)> RepeatedStrikes()
    {
        var state = Ready(Valinor);
        foreach (var region in new[] { "lorien", "rivendell", "woodland-realm" })
            BattleFixture.Put(state, "elves", UnitKind.Regular, region);
        state = Play(state, Valinor);
        var positions = new List<(MatchState State, string[] Targets)> { (state, []) };
        foreach (var region in new[] { "rivendell", "lorien" })
        {
            state = Choose(state, region, 5);
            positions.Add((state, [.. state.Resolution.Frames[0].Effect!.TargetIds]));
            var id = EventLosses.Current(state)!.Target.Units(state).First().Id;
            state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(id)] }, 5);
            positions.Add((state, [.. state.Resolution.Frames[0].Effect!.TargetIds]));
        }
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(new[] { "rivendell", "lorien", "woodland-realm" }, state.Resolution.Frames[0].Effect!.TargetIds);
        Assert.Equal(3, state.Resolution.Frames[0].Effect!.Committed.Count);
        return positions;
    }

    [Fact]
    public void EveryRepeatedChoiceAndItsLossesAreMarkedWithTheStruckRegions()
    {
        foreach (var (state, targets) in RepeatedStrikes())
            Assert.All(Enum.GetValues<Side>(), side =>
            {
                var choice = LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice;
                Assert.Equal(Valinor, choice?.CardId);
                Assert.Equal(targets, choice!.TargetIds);
            });
    }

    [Theory]
    [InlineData("target")]
    [InlineData("trail")]
    [InlineData("cursor")]
    public void RepeatedStrikesRefuseInconsistentRegions(string change)
    {
        var positions = RepeatedStrikes();
        var state = change == "cursor" ? positions[2].State : positions[^1].State;
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "target": execution.TargetIds[^1] = "grey-havens"; break;
            case "trail": execution.Committed.Add(new(Side.Shadow, Valinor, "grey-havens", "grey-havens")); break;
            case "cursor": state.Resolution.Frames[0].NextStep = 0; break;
        }

        Assert.All(Enum.GetValues<Side>(), side =>
            Assert.Null(LordsFixture.Views().For(state, side).ActionBoundary?.DirectEventChoice));
    }

    [Fact]
    public void SkipsConqueredAndBesiegedStrongholdsAndOffersOrderOfRemainingRegions()
    {
        var state = Ready(Valinor);
        foreach (var region in new[] { "lorien", "rivendell", "woodland-realm", "grey-havens" })
            BattleFixture.Put(state, "elves", UnitKind.Regular, region);
        state.Control["grey-havens"] = Side.Shadow;
        state.Units.First(u => u.Location == "woodland-realm" && BoardRules.IsArmyUnit(u)).Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "woodland-realm");
        state = Play(state, Valinor);
        Assert.Equal(new[] { "lorien", "rivendell" }, state.Pending!.PrivateChoices.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["woodland-realm"] });
        state = Choose(state, "rivendell", 5);
        Assert.Equal("rivendell", EventLosses.Current(state)!.Target.Region);
        var id = EventLosses.Current(state)!.Target.Units(state).First().Id;
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(id)] }, 0);
        Assert.Null(EventLosses.Current(state));
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }
}
