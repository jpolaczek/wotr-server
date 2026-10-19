using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// UI-272: the seeder's Lords card scenarios wait on the decision their client test makes on its object.
public class LordsCardScenarioTests
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly Dictionary<string, MatchState> Scenarios = LordsCardScenario.Create(new SeededRandom(13));

    [Fact]
    public void TheGatesAnswerADeclarationWithoutAHunt()
    {
        var state = Scenarios["ui272-gates-declaration"];
        Assert.Equal((DecisionKind.Reaction, Side.Shadow, "fellowship.declared"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Window));
        var view = Views.For(state, Side.Shadow);
        Assert.Null(view.Hunt);
        var respond = view.Options.Single(o => o.Kind == CommandKind.RespondEffect && o.Enabled);
        Assert.Contains(respond.EffectChoices, c => c.SourceId == LordsShadowCards.Gates);
    }

    [Theory]
    [InlineData("ui272-wind-table", "TableCards", new[] { LordsShadowCards.Gates, LordsShadowCards.Lore })]
    [InlineData("ui272-wind-hand", "TableCards", new[] { WindFromWestStep.Inspect, WindFromWestStep.Decline })]
    [InlineData("ui272-safe-paths", "TableCards", new[] { SmeagolRules.WeShallGetIt, LordsFreePeoplesCards.WeWontGoBack })]
    public void TheCardWaitsOnItsTableOrHandChoice(string scenario, string presentation, string[] choices)
    {
        var state = Scenarios[scenario];
        Assert.Equal((DecisionKind.EffectChoice, Side.FreePeoples, presentation), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        var option = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.ChooseEffect);
        Assert.Equal(choices.Order(), option.EffectChoices.Select(c => c.Id).Order());
    }

    [Fact]
    public void ThreeRingsRecoversTwoRingsOnTheirSockets()
    {
        var state = Scenarios["ui272-three-rings"];
        Assert.Equal("ElvenRings", state.Pending!.Presentation);
        var option = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.ChooseEffect);
        Assert.Equal(2, option.Minimum);
        Assert.Equal(state.Rings.Select(r => r.Id).Order(), option.EligibleIds.Order());
    }
}
