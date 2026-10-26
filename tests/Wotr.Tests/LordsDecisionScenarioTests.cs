using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// UI-347: the Lords decisions of the owner's playtest (EXP-15 D7, D10-D12) can be started from the test menu; each
// scenario waits on the decision it names, for the side it names, with the choices the client puts on their objects.
public class LordsDecisionScenarioTests
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    [Fact]
    public void WindFromTheWestOffersInspectAndFinishWithoutTableCards()
    {
        var state = Create("lords-card-wind-finish");
        Assert.Equal((DecisionKind.EffectChoice, Side.FreePeoples, "TableCards"),
            (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        Assert.Equal([WindFromWestStep.Inspect, WindFromWestStep.Decline], Choices(state, Side.FreePeoples));
    }

    [Fact]
    public void OrderlyWithdrawalWaitsWithTheShadowsHitsOnTheTable()
    {
        var state = Create("lords-card-orderly-withdrawal");
        Assert.Equal((DecisionKind.EffectChoice, Side.FreePeoples, "CombatEffect"),
            (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        Assert.Equal([OrderlyWithdrawalStep.Cancel, OrderlyWithdrawalStep.Decline], Choices(state, Side.FreePeoples));
        var battle = BattleRuntime.Active(state)!;
        Assert.True(BattleRoll.HitCount(battle.Attacker) > 0);
    }

    [Fact]
    public void TheBalrogCardOffersHimAloneHisArmyAndFinish()
    {
        var state = Create("lords-card-balrog");
        Assert.Equal((DecisionKind.EffectChoice, Side.Shadow, "BalrogOrder"),
            (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        Assert.Equal([BalrogEventStep.Decline, BalrogEventStep.Alone, BalrogEventStep.Army], Choices(state, Side.Shadow));
        Assert.Equal("moria", state.Characters.Single(c => c.DefinitionId == LordsCharacters.Balrog).Location);
        Assert.True(state.Units.Count(u => u.Location == "moria" && u.Nation == "sauron") >= 2);
    }

    [Fact]
    public void EvilKnowledgeWaitsAtTheShadowsSiegeContinuation()
    {
        var state = Create("lords-evil-knowledge");
        Assert.Equal((DecisionKind.SiegeContinuation, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
        var view = Views.For(state, Side.Shadow);
        Assert.Contains(view.Options, o => o.Kind == CommandKind.UseEvilKnowledge && o.Enabled);
        Assert.Contains(view.Battle!.Attacker.Characters, c => c.DefinitionId == LordsCharacters.Mouth);
        Assert.Single(state.Characters, c => CharacterCatalog.All[c.DefinitionId].Identity == "mouth-of-sauron" && CharacterRules.OnBoard(c));
    }

    private static List<string> Choices(MatchState state, Side side) =>
        Views.For(state, side).Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds;

    private static MatchState Create(string id)
    {
        var scenario = TestScenarioCatalog.Find(id)!;
        Assert.Equal([ExpansionModules.Lords], scenario.Modules);
        var state = scenario.Create("ui347-" + id);
        Assert.True(state.Configuration.Lords);
        return state;
    }
}
