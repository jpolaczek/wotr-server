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
    public void AChieftainsSubstitutionWaitsForTheShadowBeforeTheReveal()
    {
        var state = TestScenarioCatalog.Find("kings-chieftain-substitution")!.Create("ui277-kings-chieftain");
        Assert.Equal((DecisionKind.EffectChoice, Side.Shadow, "CombatReplacement"),
            (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        Assert.False(BattleRuntime.Active(state)!.CardsRevealed);
        var view = Views.For(state, Side.Shadow);
        Assert.Equal([KingsChieftains.Serpent, "keep"], view.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds);
        Assert.Null(Views.For(state, Side.FreePeoples).Battle!.Defender.CardId);
    }

    [Fact]
    public void DismayWaitsForTheShadowAfterTheRollWithTheFreePeoplesRulerDie()
    {
        var state = TestScenarioCatalog.Find("kings-dismay")!.Create("ui276-kings-dismay");
        Assert.Equal((DecisionKind.Dismay, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
        var dismay = Views.For(state, Side.Shadow).Options.Single(o => o.Kind == CommandKind.UseDismay);
        Assert.NotEmpty(dismay.EligibleIds);
        Assert.Contains(dismay.EligibleDice, id => state.Dice.Single(d => d.Id == id).Owner == Side.FreePeoples);
        Assert.Contains(Views.For(state, Side.Shadow).Options, o => o.Kind == CommandKind.DeclineDismay && o.Enabled);
    }

    [Fact]
    public void FarSightedIsOfferedWithTheRulerDie()
    {
        var state = TestScenarioCatalog.Find("kings-far-sighted")!.Create("ui276-kings-far-sighted");
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        var options = Views.For(state, Side.FreePeoples).Options;
        Assert.Contains(options, o => o.RulerTravel?.AbilityId == KingsFarSighted.Ability);
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
