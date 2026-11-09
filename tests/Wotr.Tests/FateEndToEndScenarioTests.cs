using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-14: every Fate scenario stands in the Fate tab (the client's tab is its first module) with a unique id and title
// in a "Fate · " group, declares the modules its game plays with and builds the same state every time.
public class FateEndToEndScenarioTests
{
    private static readonly string[] Added =
    [
        "fate-card-there-and-back-again-combat", "fate-card-book-of-mazarbul-combat",
        "fate-card-dwarves-of-the-iron-hills-combat", "fate-card-orcs-multiplying-again-combat",
        "fate-card-monsters-roused-combat", "fate-card-bardings-shield-wall", "fate-card-orcs-erebor-held",
        "fate-kings-dale-cards", "fate-gimli-captain", "fate-gimli-champion", "fate-gimli-journey", "fate-erebor-victory",
        "fate-erebor-muster", "fate-erebor-revealed", "fate-dale-fortification", "fate-dwarves-to-war",
        "fate-iron-hills-captured", "fate-iron-hills-sanctuary", "fate-new-cities-ered-luin", "fate-new-cities-defence",
        "fate-new-cities-south-rhun"
    ];

    public static TheoryData<string> FateScenarios =>
        new(TestScenarioCatalog.All.Where(s => s.Id.StartsWith("fate-", StringComparison.Ordinal)).Select(s => s.Id));

    [Fact]
    public void EveryFateScenarioStandsInTheFateTabWithAUniqueIdAndTitle()
    {
        var all = TestScenarioCatalog.All;
        Assert.Equal(all.Count, all.Select(s => s.Id).Distinct().Count());
        var fate = all.Where(s => s.Modules.FirstOrDefault() == ExpansionModules.Fate).ToList();
        Assert.All(Added, id => Assert.Contains(id, fate.Select(s => s.Id)));
        Assert.All(fate, scenario =>
        {
            Assert.StartsWith("fate-", scenario.Id);
            Assert.StartsWith("Fate · ", scenario.Group);
        });
        Assert.Equal(fate.Count, fate.Select(s => s.Title).Distinct().Count());
        Assert.Equal(fate.Count, all.Count(s => s.Id.StartsWith("fate-", StringComparison.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(FateScenarios))]
    public void TheScenarioDeclaresItsModulesAndBuildsTheSameState(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        var state = scenario.Create("modules-" + id);
        Assert.Equal(ExpansionModules.Of(state.Configuration), scenario.Modules);
        Assert.Equal(scenario.Modules, scenario.View().Modules);
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(scenario.Create("modules-" + id)));
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
    }
}
