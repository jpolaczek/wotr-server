using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// UI-348: Brand's Combat card replacement of the owner's playtest (EXP-15 D17) can be started from the test menu; each
// scenario waits after the reveal for Brand's owner, with the replacement and Keep card effects.
public class KingsDecisionScenarioTests
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData("kings-brand-heir", Side.FreePeoples)]
    [InlineData("kings-brand-shadows", Side.Shadow)]
    public void BrandsReplacementWaitsForItsOwnerAfterTheReveal(string id, Side owner)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Equal([ExpansionModules.Kings], scenario.Modules);
        var state = scenario.Create("ui348-" + id);
        Assert.Equal((DecisionKind.EffectChoice, owner, "CombatReplacement"),
            (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        Assert.True(BattleRuntime.Active(state)!.CardsRevealed);
        var view = Views.For(state, owner);
        Assert.Equal(["replace", "keep"], view.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds);
        Assert.All([view.Battle!.Attacker, view.Battle.Defender], force => Assert.NotEqual("", force.CardId));
    }

    [Fact]
    public void ThePalantirOfAnarionOffersEitherDeckOrNoExtraCard()
    {
        var scenario = TestScenarioCatalog.Find("kings-anarion")!;
        var state = scenario.Create("ui348-kings-anarion");
        Assert.Equal((DecisionKind.EffectChoice, Side.FreePeoples, "Deck"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        var choices = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds;
        Assert.Contains("decline", choices);
        Assert.Contains("character", choices);
    }
}
