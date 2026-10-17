using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsPoliticsTests
{
    internal static MatchState Ready(Side actor = Side.FreePeoples)
    {
        var state = Actions(actor);
        const string gimli = "base.character.gimli";
        var companion = new CharacterState { Id = gimli, Location = "erebor" };
        CharacterLifecycle.ApplyVersion(companion, CharacterCatalog.All[gimli]);
        state.Characters.Add(companion);
        CorruptionRules.Attach(state, Sovereign(state, SovereignCatalog.Dain), HuntTests.Three);
        CorruptionRules.Attach(state, Sovereign(state, SovereignCatalog.Dain), KingsTiles.One);
        return state;
    }

    [Theory]
    [InlineData("remove", 0)]
    [InlineData("decline", 3)]
    public void PoliticalActionOffersOptionalRandomRemovalBeforeSpendingItsDie(string choice, int corruption)
    {
        var state = Ready();
        var cost = Die(state).Id;
        state = Do(state, CommandKind.AdvancePolitics, new() { DieId = cost, NationId = "dwarves" });
        Assert.Equal("CorruptionRemoval", state.Pending!.Presentation);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost).Location);
        var before = state.RandomSamples.Count;
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [choice] }, choice == "remove" ? [0] : []);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Equal(corruption, Sovereign(state, SovereignCatalog.Dain).Tiles.Sum(KingsTiles.CorruptionValue));
        Assert.Contains(KingsTiles.One, Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal(before + (choice == "remove" ? 1 : 0), state.RandomSamples.Count);
        Assert.DoesNotContain(HuntTests.Three, Json(state, Side.FreePeoples));
        Assert.Empty(state.Kings!.PendingCorruptionRemoval);
    }

    [Fact]
    public void ShadowTriggeredActivationAndAdvancePreserveShadowCostAndOfferEachChangeOnlyOnce()
    {
        var state = Ready(Side.Shadow);
        var cost = Die(state, Side.Shadow).Id;
        DiceRules.Pay(state, Side.Shadow, cost, DieFace.Muster);
        PoliticalReactions.Add(state, PoliticalTrigger.ArmyAttack, "dwarves", "erebor");
        TurnFlow.FinishAction(state, Side.Shadow);
        new EffectRuntime(EffectCatalog.All).Continue(state, new SequenceRandom());
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(Side.Shadow, EffectRuntime.Current(state)!.Effect!.Owner);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["decline"] });
        Assert.Equal("CorruptionRemoval", state.Pending!.Presentation);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost).Location);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["remove"] }, 0);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Empty(state.Resolution.PaidDieIds);
    }

    [Theory]
    [InlineData("no-change")]
    [InlineData("wrong-icon")]
    [InlineData("elsewhere")]
    [InlineData("only-kings")]
    [InlineData("corrupted")]
    public void NoOfferWithoutAnActualEligibleTrigger(string scenario)
    {
        var state = Ready();
        var sovereign = Sovereign(state, SovereignCatalog.Dain);
        if (scenario == "no-change")
            state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 1;
        if (scenario == "wrong-icon")
            state.Characters[0].NationIcon = "";
        if (scenario == "elsewhere")
            state.Characters[0].Location = "dale";
        if (scenario == "only-kings")
            sovereign.Tiles.Remove(HuntTests.Three);
        if (scenario == "corrupted")
            sovereign.Status = SovereignStatus.Corrupted;
        PoliticalRules.Advance(state, "dwarves");
        Assert.Empty(state.Kings!.PendingCorruptionRemoval);
        Assert.False(KingsPolitics.BeginPending(state));
    }

    [Fact]
    public void InterruptingAnExistingDecisionRestoresItsIdentityAndNeverExposesRandomTile()
    {
        var state = Ready();
        var pending = state.Pending!;
        PoliticalRules.Activate(state, "dwarves");
        Assert.True(KingsPolitics.BeginPending(state));
        new EffectRuntime(EffectCatalog.All).Continue(state, new SequenceRandom());
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["remove"] }, 0);
        Assert.Equal(pending.Id, state.Pending!.Id);
        Assert.Equal(pending.Commands, state.Pending.Commands);
        Assert.DoesNotContain(HuntTests.Three, Json(state, Side.FreePeoples));
    }
}
