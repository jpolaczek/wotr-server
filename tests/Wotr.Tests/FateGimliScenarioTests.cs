using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// UI-349: Gimli's Fate mechanics and one Fate Combat card can be started from the test menu; each scenario waits on the
// decision it names, for the side it names, in a game with the modules it names.
public class FateGimliScenarioTests
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    [Fact]
    public void MessengerOfTheDwarvesWaitsInEredLuinWithTheDwarvesPassive()
    {
        var state = Create("fate-gimli-messenger", [ExpansionModules.Fate, ExpansionModules.Lords]);
        Assert.True(state.Configuration.Council);
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        var gimli = state.Characters.Single(c => c.Id == FateGimli.Figure);
        Assert.Equal((FateGimli.Alternate, "ered-luin"), (gimli.DefinitionId, gimli.Location));
        Assert.False(state.Nations.Single(n => n.Id == "dwarves").Active);
        var ability = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
            .CharacterActions.Single(c => c.CharacterId == FateGimli.Figure && c.AbilityId == "messenger-dwarves");
        Assert.Equal(["ered-luin"], ability.Targets);
    }

    [Fact]
    public void TheDwarvenLordsGuideResponseWaitsInTheHunt()
    {
        var state = Create("fate-gimli-guide", [ExpansionModules.Fate, ExpansionModules.Lords]);
        Assert.Equal((DecisionKind.Reaction, Side.FreePeoples, "hunt.guide"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Window));
        var respond = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.RespondEffect);
        Assert.Contains(FateGimli.GuideReaction, respond.EligibleIds);
    }

    [Fact]
    public void GimlisHomelandWaitsOnTheCouncilsSetup()
    {
        var state = Create("fate-gimli-homeland", [ExpansionModules.Fate, ExpansionModules.Lords]);
        Assert.Equal((DecisionKind.CouncilHomelands, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        Assert.Equal(FateGimli.Alternate, state.Characters.Single(c => c.Id == FateGimli.Figure).DefinitionId);
    }

    [Fact]
    public void TheBardingsCombatCardWaitsInItsBattle()
    {
        var id = FateCardScenarios.Id(CardCatalog.All[FateCards.Bardings]) + "-combat";
        var state = Create(id, [ExpansionModules.Fate]);
        Assert.Equal((DecisionKind.CombatCard, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        Assert.Contains(FateCards.Bardings, state.Player(Side.FreePeoples).Hand);
        Assert.Equal(FateCardScenarios.Group, TestScenarioCatalog.Find(id)!.Group);
    }

    private static MatchState Create(string id, string[] modules)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Equal(modules, scenario.Modules);
        var state = scenario.Create("ui349-" + id);
        Assert.True(state.Configuration.Fate);
        return state;
    }
}
