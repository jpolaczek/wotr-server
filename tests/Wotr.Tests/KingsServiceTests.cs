using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsServiceTests
{
    [Theory]
    [InlineData(DieFace.Muster, DieFace.Muster)]
    [InlineData(DieFace.Event, DieFace.Event)]
    [InlineData(DieFace.Muster, DieFace.ArmyMuster)]
    [InlineData(DieFace.Muster, DieFace.WillOfTheWest)]
    public void ServiceMarksBothReviewedChoicesWithoutSharingTheSelectedRuler(DieFace cost, DieFace dieFace)
    {
        var state = PlayedServiceChoosingRuler(cost, dieFace);
        var initialViews = Views(state);
        Assert.All(initialViews, view =>
        {
            Assert.Equal(39, view.ActionBoundary?.Version);
            AssertServiceMarker(view, cost);
        });

        state = Choose(state, SovereignCatalog.Dain);
        var recruitmentViews = Views(state);
        var freePeoples = recruitmentViews.Single(view => view.You == Side.FreePeoples);
        var shadow = recruitmentViews.Single(view => view.You == Side.Shadow);
        Assert.All(recruitmentViews, view => AssertServiceMarker(view, cost));
        Assert.Equal(SovereignCatalog.Dain, freePeoples.EffectTrail[1].ChoiceId);
        Assert.Equal("erebor", freePeoples.EffectTrail[1].TargetId);
        Assert.Empty(shadow.EffectTrail);
        Assert.DoesNotContain(SovereignCatalog.Dain,
            JsonSerializer.Serialize(shadow.ActionBoundary!.DirectEventChoice));
    }

    [Fact]
    public void ServiceMarksTheRecruitmentAfterAForcedLoneRuler()
    {
        var state = StateWithTwoDwarvenReserves();
        foreach (var sovereign in state.Kings!.Sovereigns.Where(item => item.Id != SovereignCatalog.Dain))
            SovereignLifecycle.Remove(state, sovereign);

        state = Do(state, CommandKind.PlayEvent, Cost(state, Service, DieFace.Muster));

        Assert.Equal("RecruitmentPlan", state.Pending!.Presentation);
        Assert.All(Views(state), view => AssertServiceMarker(view, DieFace.Muster));
        var freePeoples = Views(state).Single(view => view.You == Side.FreePeoples);
        Assert.Equal(Service, Assert.Single(freePeoples.EffectTrail).SourceId);
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("payment")]
    [InlineData("public-play")]
    [InlineData("ruler")]
    [InlineData("region")]
    [InlineData("nation")]
    [InlineData("selection")]
    [InlineData("missing-selection")]
    public void ServiceMarkerRequiresTheExactReviewedRecruitmentWindow(string change)
    {
        var state = Choose(PlayedServiceChoosingRuler(DieFace.Muster, DieFace.Muster), SovereignCatalog.Dain);
        Assert.All(Views(state), view => AssertServiceMarker(view, DieFace.Muster));
        var execution = state.Resolution.Frames[0].Effect!;
        switch (change)
        {
            case "cursor": state.Resolution.Frames[0].NextStep++; break;
            case "payment":
                state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
                break;
            case "public-play": state.Events.RemoveAll(item => item.Kind == "EventPlayed"); break;
            case "ruler": execution.TargetIds = [SovereignCatalog.Thranduil]; break;
            case "region": execution.RegionId = "dale"; break;
            case "nation": execution.NationId = "elves"; break;
            case "selection":
                execution.Committed[1] = new(Side.FreePeoples, Service, SovereignCatalog.Thranduil, "erebor");
                break;
            case "missing-selection": execution.Committed.RemoveAt(1); break;
        }

        Assert.All(Views(state), view => Assert.Null(view.ActionBoundary?.DirectEventChoice));
    }

    private static MatchState StateWithTwoDwarvenReserves()
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain, KingsTiles.One);
        Tile(state, SovereignCatalog.Dain, HuntTests.Three);
        var dwarves = state.Units.Where(unit => unit.Nation == "dwarves" && unit.Id != SovereignCatalog.Dain &&
            unit.Kind is UnitKind.Regular or UnitKind.Elite).ToArray();
        foreach (var unit in dwarves)
            unit.Location = "casualties";
        dwarves.First(unit => unit.Kind == UnitKind.Regular).Location = "reserve";
        dwarves.First(unit => unit.Kind == UnitKind.Elite).Location = "reserve";
        return state;
    }

    private static MatchState PlayedServiceChoosingRuler(DieFace cost, DieFace dieFace)
    {
        var state = StateWithTwoDwarvenReserves();
        var payment = Cost(state, Service, cost);
        state.Dice.Single(die => die.Id == payment.DieId).Face = dieFace;
        return Do(state, CommandKind.PlayEvent, payment);
    }

    private static PlayerView[] Views(MatchState state) => Enum.GetValues<Side>()
        .Select(side => GameRuntime.Projection(EffectCatalog.All).For(state, side)).ToArray();

    private static void AssertServiceMarker(PlayerView view, DieFace cost)
    {
        var context = Assert.IsType<DirectEventChoiceView>(view.ActionBoundary?.DirectEventChoice);
        Assert.Equal(Service, context.CardId);
        Assert.Equal(cost, context.CardCost);
    }

    [Theory]
    [InlineData(UnitKind.Regular, false)]
    [InlineData(UnitKind.Elite, false)]
    [InlineData(UnitKind.Regular, true)]
    public void ServiceRecruitsAtSelectedRulerThenRandomlyCleansesUnlessWelcome(UnitKind kind, bool welcome)
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        Tile(state, SovereignCatalog.Dain, KingsTiles.One);
        if (welcome)
            state.Player(Side.Shadow).Table.Add(Welcome);
        state.Units.Single(u => u.Id == SovereignCatalog.Dain).Location = "dale";
        state = Choose(Play(state, Service), SovereignCatalog.Dain);
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(new[] { "dale" }, option.Targets);
        var id = option.Recruitment.Single(g => g.Kind == kind).ReserveIds[0];
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Placements = [new() { UnitId = id, RegionId = "erebor" }] });
        state = Do(state, CommandKind.ChooseEffect, new() { Placements = [new() { UnitId = id, RegionId = "dale" }] }, 0);
        Assert.Equal("dale", state.Units.Single(u => u.Id == id).Location);
        Assert.Equal(welcome ? 2 : 1, Sovereign(state, SovereignCatalog.Dain).Tiles.Count);
        Assert.Contains(KingsTiles.One, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void ServiceStillCleansesWithoutArmyReserves()
    {
        var state = State();
        Tile(state, SovereignCatalog.Dain);
        foreach (var unit in state.Units.Where(u => u.Nation == "dwarves" && BoardRules.IsArmyUnit(u)))
            unit.Location = "casualties";
        state = Choose(Play(state, Service), SovereignCatalog.Dain, 0);
        Assert.Empty(Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Fact]
    public void ServiceAllowsAwakenedRulerAndRejectsCorruptedOrRemovedTargets()
    {
        var state = State();
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Dain), "dale");
        Tile(state, SovereignCatalog.Denethor, HuntTests.Three);
        SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Thranduil));
        state = Play(state, Service);
        Assert.Contains(SovereignCatalog.Dain, state.Pending!.PrivateChoices);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Thranduil] });
        state = Choose(state, SovereignCatalog.Dain);
        Assert.Equal("dale", new EffectOptions().GetOptions(state, Side.FreePeoples).Single().Targets.Single());
    }

    [Fact]
    public void ServiceWithNoSovereignsCompletesWithoutAnotherChoice()
    {
        var state = State();
        foreach (var sovereign in state.Kings!.Sovereigns)
            SovereignLifecycle.Remove(state, sovereign);
        state = Play(state, Service);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
