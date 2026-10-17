using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsVillagesTests
{
    internal static ActionOption Option(MatchState state) => new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
    internal static CommandPayload Plan(MatchState state, int count = 4) => new()
    {
        Placements = Option(state).Recruitment.Where(g => g.Kind == UnitKind.Regular).Take(count)
            .Select(g => new UnitPlacementChoice { UnitId = g.ReserveIds[0], RegionId = g.Targets[0] }).ToList()
    };

    [Fact]
    public void FourDistinctNationsAreChosenTogetherWithoutWarOrActivation()
    {
        var state = Play(State(), Villages);
        var option = Option(state);
        Assert.Equal(4, option.Maximum);
        Assert.Equal(4, option.EventRecruitment!.DistinctNations);
        Assert.Equal(5, option.Recruitment.Select(g => g.Nation).Distinct().Count());
        Assert.All(option.Recruitment, g => Assert.True(g.Kind is UnitKind.Regular or UnitKind.Leader));
        var payload = Plan(state);
        var before = state.Nations.Select(n => (n.Id, n.Active, n.StepsToWar)).ToArray();
        state = Do(state, CommandKind.ChooseEffect, payload);
        Assert.All(payload.Placements, p => Assert.Equal(p.RegionId, state.Units.Single(u => u.Id == p.UnitId).Location));
        Assert.Equal(before, state.Nations.Select(n => (n.Id, n.Active, n.StepsToWar)));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void RepeatedNationTooFewTooManyDuplicateAndWrongHomelandAreRejected()
    {
        var state = Play(State(), Villages);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, Plan(state, 3));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, Plan(state, 5));
        var duplicate = Plan(state);
        duplicate.Placements[1] = duplicate.Placements[0];
        LordsFixture.Reject(state, CommandKind.ChooseEffect, duplicate);
        var repeated = Plan(state);
        var first = state.Units.Single(u => u.Id == repeated.Placements[0].UnitId);
        var leader = Option(state).Recruitment.Single(g => g.Nation == first.Nation && g.Kind == UnitKind.Leader);
        repeated.Placements[1] = new() { UnitId = leader.ReserveIds[0], RegionId = leader.Targets.Last() };
        LordsFixture.Reject(state, CommandKind.ChooseEffect, repeated);
        var wrong = Plan(state);
        wrong.Placements[0].RegionId = "barad-dur";
        LordsFixture.Reject(state, CommandKind.ChooseEffect, wrong);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void UsesMaximumPossibleNumberOfNationsWithRemainingReserves(int available)
    {
        var state = State();
        var nations = BoardRules.Nations.Values.Where(n => n.Side == Side.FreePeoples).Take(available).Select(n => n.Id).ToHashSet();
        foreach (var unit in state.Units.Where(u => u.Location == "reserve" && BoardRules.Owner(u) == Side.FreePeoples && !nations.Contains(u.Nation)))
            unit.Location = "casualties";
        state = Play(state, Villages);
        if (available > 0)
        {
            Assert.Equal(available, Option(state).Maximum);
            state = Do(state, CommandKind.ChooseEffect, Plan(state, available));
        }
        Assert.Equal(available, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void FreeSettlementExcludesCapturedAndBesiegedRegions()
    {
        var state = State();
        state.Control["edoras"] = Side.Shadow;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "helms-deep").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "helms-deep");
        state = Play(state, Villages);
        Assert.DoesNotContain("edoras", Option(state).Targets);
        Assert.DoesNotContain("helms-deep", Option(state).Targets);
        Assert.All(Option(state).Targets, id => Assert.True(BoardRules.IsSettlement(BoardRules.Region(state, id))));
    }

    [Fact]
    public void LeaderIsSelectableAndRemovedIfPlacedWithoutAnArmy()
    {
        var state = Play(State(), Villages);
        var payload = Plan(state);
        var nation = state.Units.Single(u => u.Id == payload.Placements[0].UnitId).Nation;
        var leader = Option(state).Recruitment.Single(g => g.Nation == nation && g.Kind == UnitKind.Leader).ReserveIds[0];
        payload.Placements[0].UnitId = leader;
        state = Do(state, CommandKind.ChooseEffect, payload);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Equal(4, state.Events.Count(e => e.Kind == "EventRecruited"));
    }
}
