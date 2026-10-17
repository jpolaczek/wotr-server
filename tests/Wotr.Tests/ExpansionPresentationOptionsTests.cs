using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class ExpansionPresentationOptionsTests
{
    [Theory]
    [InlineData(SovereignCatalog.Brand)]
    [InlineData(SovereignCatalog.Dain)]
    [InlineData(SovereignCatalog.Denethor)]
    [InlineData(SovereignCatalog.Theoden)]
    [InlineData(SovereignCatalog.Thranduil)]
    public void AwakeningProjectsTheExactOriginRangeDestinationsAndPublicRouteRestrictions(string id)
    {
        var state = KingsFixture.Actions();
        var definition = SovereignCatalog.All[id];
        var nation = state.Nations.Single(n => n.Id == definition.Nation);
        nation.Active = true;
        nation.StepsToWar = 0;
        var before = StateCodec.Write(state);
        var option = new SovereignCommands().GetOptions(state, Side.FreePeoples).Single(o => o.EligibleIds.Contains(id));
        var travel = Assert.Single(option.Travel);
        Assert.Equal(id, travel.Id);
        Assert.Equal(definition.Home, travel.Origin);
        Assert.Equal(definition.Level, travel.Range);
        Assert.Equal(option.Targets, travel.Targets);
        Assert.Equal(BoardRules.Regions.Keys.Where(r => TravelRules.ShadowStronghold(state, r)), travel.StopRegions);
        Assert.Equal(BoardRules.Regions.Keys.Where(r => BoardRules.Besieged(state, r, Side.FreePeoples)), travel.BlockedRegions);
        Assert.True(SovereignAwakening.Valid(state, new() { CharacterId = id, DieId = option.EligibleDice[0], Route = [travel.Origin] }));
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Empty(new SovereignCommands().GetOptions(state, Side.Shadow));
    }

    [Fact]
    public void LocalRecruitmentOutsideASettlementDoesNotInventAnExtraSettlementSlot()
    {
        var state = KingsRulerRecruitmentTests.Ready(SovereignCatalog.Theoden, "eastemnet");
        var choice = KingsRulerRecruitment.Options(state, Side.FreePeoples).Single();
        Assert.Equal("eastemnet", choice.RulerRecruitment!.LocalRegion);
        Assert.Contains("eastemnet", choice.Targets);
        Assert.DoesNotContain("eastemnet", choice.RulerRecruitment.Settlements);
        Assert.Contains("edoras", choice.RulerRecruitment.Settlements);
        state.Control["edoras"] = Side.Shadow;
        choice = KingsRulerRecruitment.Options(state, Side.FreePeoples).Single();
        Assert.DoesNotContain("edoras", choice.RulerRecruitment!.Settlements);
    }
}
