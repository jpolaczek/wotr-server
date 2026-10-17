using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class ShadowStrategy01PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresPaidCostAndTheCorrectNextOwner()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            var cost = EventCost(state, card);
            state = await RestartDecision(state, CommandKind.PlayEvent, cost, new SequenceRandom(new int[20]));
            Assert.Equal(card is UrukHai or Denethor or Stormcrow ? DieLocation.Resolving : DieLocation.Spent,
                state.Dice.Single(d => d.Id == cost.DieId).Location);
            Assert.Single(state.Events, e => e.Kind == "EventPlayed" && e.Text.Contains(Wotr.Domain.Content.CardCatalog.Title(card), StringComparison.Ordinal));
            if (card == Stormcrow)
                Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        }
    }

    [PostgresFact]
    public async Task ValinorRestoresRegionCursorOpponentLossesAndEachRollOnlyOnce()
    {
        var state = State(Valinor);
        var rivendell = BattleFixture.Put(state, "elves", UnitKind.Regular, "rivendell");
        state = Play(state, Valinor);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["lorien"] }, new SequenceRandom(5, 0));
        Assert.Equal("lorien", EventLosses.Current(state)!.Target.Region);
        var lorien = state.Units.Single(u => u.Location == "lorien" && u.Kind == UnitKind.Regular);
        state = await RestartDecision(state, CommandKind.TakeEventLosses,
            new() { Losses = [BattleFixture.Loss(lorien.Id)] }, new SequenceRandom(5));
        Assert.Equal("rivendell", EventLosses.Current(state)!.Target.Region);
        state = await RestartDecision(state, CommandKind.TakeEventLosses,
            new() { Losses = [BattleFixture.Loss(rivendell.Id)] }, new SequenceRandom());
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "ReturnToValinor"));
    }

    [PostgresFact]
    public async Task DenethorRestoresShadowLeaderChoiceAndPaidRemovalWithoutRepeatedLoss()
    {
        var state = Play(State(Denethor), Denethor);
        var leader = Shadow01Fixture.Choices(state).EligibleIds[0];
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [leader] }, new SequenceRandom());
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Contains(Denethor, state.Player(Side.Shadow).Table);
        var cost = Cost(state, DieFace.WillOfTheWest);
        state = await RestartDecision(state, CommandKind.DiscardDenethor, cost, new SequenceRandom());
        Assert.DoesNotContain(Denethor, state.Player(Side.Shadow).Table);
        Assert.Single(state.Player(Side.Shadow).Discard, id => id == Denethor);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
    }

    [PostgresFact]
    public async Task StormcrowRestoresEnemySelectionAndDoesNotRetreatPoliticsAgain()
    {
        var state = Play(State(Stormcrow), Stormcrow);
        var unit = Shadow01Fixture.Choices(state).EligibleIds[0];
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [unit] }, new SequenceRandom());
        Assert.Equal("casualties", state.Units.Single(u => u.Id == unit).Location);
        Assert.Single(state.Events, e => e.Kind == "PoliticsRetreated");
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task OnslaughtRestoresVoluntarySelfLossesAndRollEvenWithNoSurvivors()
    {
        var state = Shadow01Fixture.Reveal(Combat(UrukHai, elite: true), UrukHai);
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        state = await RestartDecision(state, CommandKind.TakeBattleLosses, Sacrifice(state, 3), new SequenceRandom(3, 3, 3, 0));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.False(BattleRules.Alive(state, BattleRuntime.Active(state)!.Force(Side.Shadow)));
        var enemy = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = await RestartDecision(state, CommandKind.TakeBattleLosses, new() { Losses = enemy }, new SequenceRandom());
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(Side.FreePeoples, state.Control[BattleFixture.Target]);
        Assert.Equal(10, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task VoluntaryZeroAndRelentlessSacrificeRestoreBeforeCombatAndGreatHostAfterCombat()
    {
        foreach (var card in new[] { Dawn, UrukHai })
        {
            var state = Shadow01Fixture.Reveal(Combat(card), card);
            state = await RestartDecision(state, CommandKind.TakeBattleLosses, new(), new SequenceRandom(new int[6]));
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            Assert.Equal(6, state.RandomSamples.Count);
        }
        var relentless = Shadow01Fixture.Reveal(Combat(Dawn, own: 1), Dawn);
        relentless = await RestartDecision(relentless, CommandKind.TakeBattleLosses, Sacrifice(relentless, 1), new SequenceRandom());
        Assert.Empty(relentless.RandomSamples);
        Assert.Empty(relentless.Resolution.Frames);
        var host = Shadow01Fixture.Reveal(Combat(Stormcrow, own: 4, enemy: 2), Stormcrow);
        var unit = BattleRules.Units(host, BattleRuntime.Active(host)!.Force(Side.FreePeoples)).First();
        host = await RestartDecision(host, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(unit.Id)] }, new SequenceRandom());
        Assert.Equal(DecisionKind.BattleContinue, host.Pending!.Kind);
        Assert.Equal(6, host.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task UrukHaiRestoresAllThreeSiegeRoundsWithNoRepeatedPayment()
    {
        var state = Play(State(UrukHai), UrukHai);
        var attack = Shadow04Fixture.Army(state, "minas-tirith", "minas-tirith");
        attack.Moves[0].CharacterIds.Clear();
        attack.Moves[0].UnitIds.RemoveAll(id => state.Units.Single(u => u.Id == id) is { Kind: UnitKind.Nazgul } or { Nation: "isengard" });
        state = await RestartDecision(state, CommandKind.AttackArmy, attack, new SequenceRandom());
        for (var round = 1; round <= 3; round++)
        {
            Assert.Equal(round, BattleRuntime.Active(state)!.Round);
            state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[7]));
            if (state.Pending?.Kind == DecisionKind.CombatCard)
                state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[7]));
        }
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(18, state.RandomSamples.Count);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
