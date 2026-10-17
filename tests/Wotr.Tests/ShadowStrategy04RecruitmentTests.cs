using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy04Fixture;

namespace Wotr.Tests;

public class ShadowStrategy04RecruitmentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void PitsRequiresAvailableMaximumWithoutEmptyRegionSelections(int reserve)
    {
        var state = State(Pits);
        Reserve(state, "sauron", UnitKind.Regular, reserve);
        state = Play(state, Pits);
        if (reserve > 0)
        {
            var option = Option(state);
            Assert.Equal(new[] { "barad-dur", "dol-guldur", "minas-morgul", "morannon", "moria", "mount-gundabad" }, option.Targets.Order());
            Assert.Equal(reserve, option.Minimum);
            Assert.Equal(3, option.EventRecruitment!.MaximumRegions);
            state = Do(state, CommandKind.ChooseEffect, Plan(state,
                ("sauron", UnitKind.Regular, "moria", Math.Min(reserve, 2)),
                ("sauron", UnitKind.Regular, "morannon", Math.Clamp(reserve - 2, 0, 2)),
                ("sauron", UnitKind.Regular, "barad-dur", Math.Clamp(reserve - 4, 0, 2))));
        }
        Assert.Equal(reserve, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void PitsRejectsFourthRegionThirdUnitForeignStrongholdAndForgedFigures()
    {
        var state = Play(State(Pits), Pits);
        var plan = Plan(state, ("sauron", UnitKind.Regular, "moria", 2),
            ("sauron", UnitKind.Regular, "morannon", 2), ("sauron", UnitKind.Regular, "barad-dur", 2));
        foreach (var region in new[] { "orthanc", "mount-gundabad", "morannon", "forged-region" })
        {
            plan.Placements[0].RegionId = region;
            Reject(state, CommandKind.ChooseEffect, plan);
        }
        plan.Placements[0].RegionId = "moria";
        var original = plan.Placements[0].UnitId;
        foreach (var id in new[] { plan.Placements[1].UnitId, "forged-unit", state.Units.First(u => u.Kind == UnitKind.Elite).Id,
            state.Units.First(u => u.Nation == "southrons").Id })
        {
            plan.Placements[0].UnitId = id;
            Reject(state, CommandKind.ChooseEffect, plan);
        }
        plan.Placements[0].UnitId = original;
        plan.Ids.Add(original);
        Reject(state, CommandKind.ChooseEffect, plan);
        plan.Ids.Clear();
        state.Units.Single(u => u.Id == original).Location = "moria";
        Reject(state, CommandKind.ChooseEffect, plan);
    }

    [Fact]
    public void PitsCanSplitScarceFiguresAndOmitsCapturedEmptyStrongholds()
    {
        var state = State(Pits);
        Reserve(state, "sauron", UnitKind.Regular, 2);
        state.Control["barad-dur"] = Side.FreePeoples;
        state = Play(state, Pits);
        Assert.DoesNotContain("barad-dur", Option(state).Targets);
        state = Do(state, CommandKind.ChooseEffect, Plan(state,
            ("sauron", UnitKind.Regular, "moria", 1), ("sauron", UnitKind.Regular, "morannon", 1)));
        Assert.Single(state.Units, u => u.Location == "moria");
        Assert.Single(state.Units, u => u.Location == "morannon");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MistyMountainsRecruitsOnEitherSideOfSiegeThenRemovesChosenExcess(bool garrison)
    {
        var state = State(MistyMountains);
        state.Control["moria"] = garrison ? Side.Shadow : Side.FreePeoples;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "moria").Garrison = !garrison;
        for (var i = 0; i < (garrison ? 5 : 10); i++)
            BattleFixture.Put(state, "southrons", UnitKind.Regular, "moria").Garrison = garrison;
        state = Play(state, MistyMountains);
        var plan = Plan(state, ("sauron", UnitKind.Elite, "moria", 2), ("sauron", UnitKind.Nazgul, "moria", 1));
        state = Do(state, CommandKind.ChooseEffect, plan);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.All(plan.Placements, placement => Assert.Equal(garrison, state.Units.Single(u => u.Id == placement.UnitId).Garrison));
        var old = state.Units.Where(u => u.Nation == "southrons" && u.Location == "moria").Take(2).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.RemoveExcess, new() { Ids = old });
        Assert.All(old, id => Assert.Equal("reserve", state.Units.Single(u => u.Id == id).Location));
        Assert.All(plan.Placements, placement => Assert.Equal("moria", state.Units.Single(u => u.Id == placement.UnitId).Location));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void MusteringsTenRecruitsFinishBeforeIndependentExcessDecisions()
    {
        var state = State(Musterings);
        for (var i = 0; i < 8; i++)
        {
            BattleFixture.Put(state, "southrons", UnitKind.Regular, "nurn");
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "gorgoroth");
        }
        state = Play(state, Musterings);
        Assert.Equal(10, state.Events.Count(e => e.Kind == "EventRecruited"));
        foreach (var unused in new[] { 0, 1 })
        {
            Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
            var eligible = new MusterOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.RemoveExcess);
            state = Do(state, CommandKind.RemoveExcess, new() { Ids = eligible.EligibleIds.Take(eligible.Minimum).ToList() });
        }
        Assert.Equal(10, state.Units.Count(u => u.Location == "nurn"));
        Assert.Equal(10, state.Units.Count(u => u.Location == "gorgoroth"));
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void PlanPreviewAndPrivateProjectionExposeOnlyLegalPublicPlacements()
    {
        var state = Play(State(MistyMountains), MistyMountains);
        var plan = Plan(state, ("sauron", UnitKind.Regular, "moria", 1), ("sauron", UnitKind.Elite, "moria", 1), ("sauron", UnitKind.Nazgul, "moria", 1));
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, plan);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.False(preview.HasUnknownResult);
        Assert.Equal(plan.Placements.Select(p => p.UnitId), preview.Placements.Select(p => p.UnitId));
        Assert.Equal(before, StateCodec.Write(state));
        var projection = new Projection(new CoreOptions(), new EffectOptions());
        var opponent = projection.For(state, Side.FreePeoples);
        Assert.Null(opponent.Decision);
        Assert.Empty(opponent.Options);
        Assert.DoesNotContain(MistyMountains, opponent.Hand);
    }
}
