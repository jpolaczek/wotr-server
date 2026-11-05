using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// WEB-01: the website's scenes build the same state every time, wait on their side's decision and hold what the pictures
// promise: crowded regions on every front within the stacking limit, two sieges and a battle at its Combat cards.
public class ShowcaseScenarioTests
{
    private const int StackingLimit = 10;
    private const int GarrisonLimit = 5;
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static TheoryData<string> Ids => new(ShowcaseScenarios.All().Select(s => s.Id));

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheSceneStandsInTheCatalogAndRepeats(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Single(TestScenarioCatalog.All, s => s.Id == id);
        Assert.Equal(ShowcaseScenarios.Group, scenario.Group);
        var state = scenario.Create("showcase-" + id);
        Assert.Equal(ExpansionModules.Of(state.Configuration).Order(), scenario.Modules.Order());
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(scenario.Create("showcase-" + id)));
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        Assert.Contains(Views.For(state, scenario.StartingSide).Options, o => o.Enabled);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void NoRegionHoldsMoreThanTheRulesAllow(string id)
    {
        var state = TestScenarioCatalog.Find(id)!.Create("showcase-" + id);
        foreach (var region in Fighting(state).GroupBy(u => (u.Location, Owner: BoardRules.Owner(u), u.Garrison)))
            Assert.InRange(region.Count(), 1, region.Key.Garrison ? GarrisonLimit : StackingLimit);
    }

    [Fact]
    public void TheWarFillsEveryFrontWithTwoSieges()
    {
        var state = TestScenarioCatalog.Find("showcase-war")!.Create("showcase-war");
        var armies = Fighting(state).GroupBy(u => u.Location).ToDictionary(group => group.Key, group => group.Count());
        Assert.True(armies.Count(region => region.Value >= 6) >= 8, "Eight regions hold six or more units.");
        foreach (var besieged in new[] { "minas-tirith", "helms-deep" })
        {
            var units = Fighting(state).Where(u => u.Location == besieged).ToList();
            Assert.Contains(units, u => BoardRules.Owner(u) == Side.Shadow);
            Assert.All(units.Where(u => BoardRules.Owner(u) == Side.FreePeoples), u => Assert.True(u.Garrison));
        }
        Assert.Equal("dead-marshes", state.Fellowship.RegionId);
    }

    [Fact]
    public void TheSiegeWaitsOnTheShadowsCombatCard()
    {
        var state = TestScenarioCatalog.Find("showcase-siege")!.Create("showcase-siege");
        Assert.Equal(DecisionKind.CombatCard, state.Pending?.Kind);
        Assert.Equal(Side.Shadow, state.Pending?.Owner);
        Assert.Equal("minas-tirith", BattleRuntime.Active(state)?.Region);
    }

    // The units that count towards a stack: Nazgûl and leaders do not.
    private static IEnumerable<UnitState> Fighting(MatchState state) =>
        state.Units.Where(u => u.Location != "reserve" && u.Location != "casualties" && u.Kind is UnitKind.Regular or UnitKind.Elite);
}
