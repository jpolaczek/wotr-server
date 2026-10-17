using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsAnarionTests
{
    internal static MatchState Ready()
    {
        var state = Actions();
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        Ruler(state, DieFace.Event);
        return state;
    }

    [Theory]
    [InlineData("character", 2)]
    [InlineData("strategy", 2)]
    [InlineData("decline", 1)]
    public void RulerDrawOffersAnOptionalExtraDeckBeforeFinishingTheCost(string choice, int drawn)
    {
        var state = Ready();
        var before = state.Player(Side.FreePeoples).Hand.Count;
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        state = Do(state, CommandKind.DrawCard, new() { DieId = cost, Deck = "strategy" });
        Assert.Equal(SovereignCatalog.Denethor, state.Pending!.SourceId);
        Assert.Contains("character", state.Pending.PrivateChoices);
        Assert.Contains("strategy", state.Pending.PrivateChoices);
        Assert.Contains("decline", state.Pending.PrivateChoices);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost).Location);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [choice] });
        Assert.Equal(before + drawn, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    internal static MatchState EventWindow()
    {
        var state = Ready();
        state.Player(Side.FreePeoples).Hand = ["base.fp.strategy.15"];
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        state = Do(state, CommandKind.PlayEvent, new() { CardId = "base.fp.strategy.15", CardCost = DieFace.Event, DieId = cost });
        Assert.NotEqual(SovereignCatalog.Denethor, state.Pending!.SourceId);
        Assert.True(EffectRuntime.Current(state)!.Effect!.AnarionDrawPending);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [state.Pending.PrivateChoices[0]] });
        Assert.Equal(SovereignCatalog.Denethor, state.Pending!.SourceId);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        return state;
    }

    [Fact]
    public void RulerEventCompletesItsOwnNestedChoicesBeforeOfferingAnarion()
    {
        var state = EventWindow();
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["character"] });
        Assert.Equal(2, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("away")]
    [InlineData("corrupted")]
    public void OrdinaryEventDiceOrMissingDenethorDoNotGrantAnExtraDraw(string reason)
    {
        var state = Ready();
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource);
        if (reason == "standard")
            cost.Source = "standard";
        if (reason == "away")
            state.Characters.Single(c => c.Id == SovereignCatalog.Denethor).Location = "osgiliath";
        if (reason == "corrupted")
            Sovereign(state, SovereignCatalog.Denethor).Status = SovereignStatus.Corrupted;
        state = Do(state, CommandKind.DrawCard, new() { DieId = cost.Id, Deck = "character" });
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
    }

    [PostgresFact]
    public Task AnarionExtraDrawRestartsAfterNestedEventAndRetriesWithoutDrawingAgain() =>
        LordsPersistenceTests.Restart(EventWindow(), CommandKind.ChooseEffect, new() { Ids = ["strategy"] });

    [Fact]
    public void ExtraDrawAndHandLimitKeepTheRulerCostUntilTheDiscardCompletes()
    {
        var state = Ready();
        while (state.Player(Side.FreePeoples).Hand.Count < 6)
            CardFlow.Draw(state, Side.FreePeoples, "strategy");
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        state = Do(state, CommandKind.DrawCard, new() { DieId = cost, Deck = "character" });
        Assert.Equal(SovereignCatalog.Denethor, state.Pending!.SourceId);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(2, state.Pending.RequiredCount);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == cost).Location);
        state = Do(StateCodec.Copy(state), CommandKind.DiscardCards, new() { Ids = state.Pending.PrivateChoices.Take(2).ToList() });
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost).Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }
}
