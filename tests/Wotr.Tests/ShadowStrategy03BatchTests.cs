using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy03Fixture;

namespace Wotr.Tests;

public class ShadowStrategy03BatchTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NewPowerOffersEveryOrthancRegularEliteMixInOnePlan(int elites)
    {
        var state = Play(State(NewPower), NewPower);
        Assert.Equal("RecruitmentPlan", state.Pending!.Presentation);
        Assert.Equal(6, Option(state).Minimum);
        var plan = Plan(state, ("isengard", UnitKind.Regular, "north-dunland", 2),
            ("isengard", UnitKind.Regular, "south-dunland", 2),
            ("isengard", UnitKind.Regular, "orthanc", 2 - elites), ("isengard", UnitKind.Elite, "orthanc", elites));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal(6, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Equal(elites, state.Units.Count(u => u.Location == "orthanc" && u.Kind == UnitKind.Elite));
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("north-dunland", "north-dunland")]
    [InlineData("south-dunland", "south-dunland")]
    [InlineData("orthanc", "orthanc")]
    [InlineData("north-dunland", "south-dunland")]
    [InlineData("north-dunland", "orthanc")]
    public void SharedReserveShortageDoesNotPrioritizePrintedRegionOrder(string first, string second)
    {
        var state = State(NewPower);
        Reserve(state, "isengard", UnitKind.Regular, 2);
        Reserve(state, "isengard", UnitKind.Elite, 0);
        state = Play(state, NewPower);
        Assert.Equal(2, Option(state).Maximum);
        state = Do(state, CommandKind.ChooseEffect, Plan(state,
            ("isengard", UnitKind.Regular, first, 1), ("isengard", UnitKind.Regular, second, 1)));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Fact]
    public void SharedRegularEliteShortageRequiresMaximumPossibleWholeClause()
    {
        var state = State(NewPower);
        Reserve(state, "isengard", UnitKind.Regular, 2);
        Reserve(state, "isengard", UnitKind.Elite, 2);
        state = Play(state, NewPower);
        Assert.Equal(4, Option(state).Minimum);
        Reject(state, CommandKind.ChooseEffect, Plan(state, ("isengard", UnitKind.Regular, "orthanc", 2)));
        state = Do(state, CommandKind.ChooseEffect, Plan(state,
            ("isengard", UnitKind.Regular, "south-dunland", 2), ("isengard", UnitKind.Elite, "orthanc", 2)));
        Assert.Equal(4, state.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Theory]
    [InlineData("north-dunland")]
    [InlineData("south-dunland")]
    [InlineData("orthanc")]
    public void NewPowerOmitsUnavailableRegionButRequiresAllOtherRecruits(string captured)
    {
        var state = State(NewPower);
        state.Control[captured] = Side.FreePeoples;
        state = Play(state, NewPower);
        if (state.Pending!.Kind == DecisionKind.EffectChoice)
        {
            var option = Option(state);
            Assert.Equal(4, option.Maximum);
            Assert.DoesNotContain(captured, option.Targets);
            var plan = Plan(state, option.Targets.Select(region => ("isengard", UnitKind.Regular, region, 2)).ToArray());
            state = Do(state, CommandKind.ChooseEffect, plan);
        }
        Assert.Equal(4, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.DoesNotContain(state.Units, u => u.Location == captured);
    }

    [Fact]
    public void NewPowerRequiresSarumanAndStillAllowsEmptyRecruitment()
    {
        var state = State(NewPower);
        state.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman).Location = "reserve";
        Reject(state, CommandKind.PlayEvent, EventCost(state, NewPower));
        Put(state, CharacterRules.Saruman, "orthanc");
        Reserve(state, "isengard", UnitKind.Regular, 0);
        Reserve(state, "isengard", UnitKind.Elite, 0);
        state = Play(state, NewPower);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ManyKingsUsesOnlyAvailableRegularsAcrossAtMostThreeSettlementsWithoutPolitics(int count)
    {
        var state = State(ManyKings);
        var nation = state.Nations.Single(n => n.Id == "southrons");
        nation.Active = false;
        nation.StepsToWar = 3;
        Reserve(state, "southrons", UnitKind.Regular, count);
        state = Play(state, ManyKings);
        if (count > 0)
        {
            var option = Option(state);
            Assert.Equal(new[] { "far-harad", "near-harad", "north-rhun", "south-rhun", "umbar" }, option.Targets.Order());
            Assert.Equal(3, option.EventRecruitment!.MaximumRegions);
            Assert.Equal(count, option.Maximum);
            state = Do(state, CommandKind.ChooseEffect, Plan(state,
                ("southrons", UnitKind.Regular, "umbar", Math.Min(count, 2)),
                ("southrons", UnitKind.Regular, "north-rhun", Math.Clamp(count - 2, 0, 2)),
                ("southrons", UnitKind.Regular, "far-harad", Math.Clamp(count - 4, 0, 2))));
        }
        Assert.Equal(count, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Equal(3, state.Nations.Single(n => n.Id == "southrons").StepsToWar);
        Assert.False(state.Nations.Single(n => n.Id == "southrons").Active);
    }

    [Theory]
    [InlineData("east-rhun")]
    [InlineData("khand")]
    [InlineData("orthanc")]
    public void ManyKingsRejectsNonsettlementsAndForeignSettlements(string invalid)
    {
        var state = Play(State(ManyKings), ManyKings);
        var plan = Plan(state, ("southrons", UnitKind.Regular, "umbar", 2),
            ("southrons", UnitKind.Regular, "north-rhun", 2), ("southrons", UnitKind.Regular, "far-harad", 2));
        plan.Placements[0].RegionId = invalid;
        Reject(state, CommandKind.ChooseEffect, plan);
    }

    [Fact]
    public void ManyKingsRejectsFourthRegionAndThirdFigureInOneRegion()
    {
        var state = Play(State(ManyKings), ManyKings);
        Reject(state, CommandKind.ChooseEffect, Plan(state, ("southrons", UnitKind.Regular, "umbar", 2),
            ("southrons", UnitKind.Regular, "north-rhun", 2), ("southrons", UnitKind.Regular, "far-harad", 1),
            ("southrons", UnitKind.Regular, "near-harad", 1)));
        Reject(state, CommandKind.ChooseEffect, Plan(state, ("southrons", UnitKind.Regular, "umbar", 3),
            ("southrons", UnitKind.Regular, "north-rhun", 2), ("southrons", UnitKind.Regular, "far-harad", 1)));
    }

    [Fact]
    public void ManyKingsShortageCanSplitTwoRecruitsBetweenTwoSettlements()
    {
        var state = State(ManyKings);
        Reserve(state, "southrons", UnitKind.Regular, 2);
        state = Play(state, ManyKings);
        state = Do(state, CommandKind.ChooseEffect, Plan(state, ("southrons", UnitKind.Regular, "umbar", 1),
            ("southrons", UnitKind.Regular, "far-harad", 1)));
        Assert.Single(state.Units, u => u.Location == "umbar");
        Assert.Single(state.Units, u => u.Location == "far-harad");
    }

    [Fact]
    public void TooFewFreeSettlementsPerformOnlyPossiblePartWithoutEmptyChoices()
    {
        var state = State(ManyKings);
        foreach (var region in new[] { "near-harad", "north-rhun", "south-rhun" })
            state.Control[region] = Side.FreePeoples;
        state = Play(state, ManyKings);
        Assert.Equal(4, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(2, state.Units.Count(u => u.Location == "umbar"));
        Assert.Equal(2, state.Units.Count(u => u.Location == "far-harad"));
    }

    [Fact]
    public void PlanRejectsDuplicateWrongNationWrongTypeOffboardAndMixedPayload()
    {
        var state = Play(State(NewPower), NewPower);
        var valid = Plan(state, ("isengard", UnitKind.Regular, "north-dunland", 2),
            ("isengard", UnitKind.Regular, "south-dunland", 2), ("isengard", UnitKind.Elite, "orthanc", 2));
        var first = valid.Placements[0].UnitId;
        foreach (var id in new[] { valid.Placements[1].UnitId, "forged-id",
            state.Units.First(u => u.Nation == "gondor").Id, state.Units.First(u => u.Kind == UnitKind.Nazgul).Id })
        {
            valid.Placements[0].UnitId = id;
            Reject(state, CommandKind.ChooseEffect, valid);
        }
        valid.Placements[0].UnitId = first;
        state.Units.Single(u => u.Id == first).Location = "orthanc";
        Reject(state, CommandKind.ChooseEffect, valid);
        state.Units.Single(u => u.Id == first).Location = "reserve";
        valid.Ids.Add(first);
        Reject(state, CommandKind.ChooseEffect, valid);
    }
}
