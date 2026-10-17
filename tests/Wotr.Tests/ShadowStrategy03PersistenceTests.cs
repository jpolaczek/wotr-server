using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy03Fixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class ShadowStrategy03PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresItsPaidCostAndContinuationWithoutDoubleExecution()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            var cost = EventCost(state, card);
            state = await RestartDecision(state, CommandKind.PlayEvent, cost, new SequenceRandom());
            Assert.Single(state.Events, e => e.Kind == "EventPlayed" && e.Text.Contains(Wotr.Domain.Content.CardCatalog.Title(card), StringComparison.Ordinal));
            Assert.Equal(card is NewPower or ManyKings ? DieLocation.Resolving : DieLocation.Spent,
                state.Dice.Single(d => d.Id == cost.DieId).Location);
        }
    }

    [PostgresFact]
    public async Task ArmyRecruitRestoresRegionThenKindChoiceAndOnlyOneFigure()
    {
        var state = State(HalfOrcs);
        state.Control["westemnet"] = Side.Shadow;
        BattleFixture.Put(state, "southrons", UnitKind.Regular, "westemnet");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "eastemnet");
        state = Play(state, HalfOrcs);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["westemnet"] }, new SequenceRandom());
        Assert.Equal("ReserveFigures", state.Pending!.Presentation);
        Assert.Equal("westemnet", EffectRuntime.Current(state)!.Effect!.RegionId);
        var id = state.Units.Single(u => u.Nation == "isengard" && u.Kind == UnitKind.Elite && state.Pending.PrivateChoices.Contains(u.Id)).Id;
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [id] }, new SequenceRandom());
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
        Assert.Equal("westemnet", state.Units.Single(u => u.Id == id).Location);
    }

    [PostgresFact]
    public async Task SharedRecruitmentPlanRestoresPlacementsAndExcessCursor()
    {
        var state = State(NewPower);
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "orthanc");
        state = Play(state, NewPower);
        var plan = Plan(state, ("isengard", UnitKind.Regular, "north-dunland", 2),
            ("isengard", UnitKind.Regular, "south-dunland", 2), ("isengard", UnitKind.Elite, "orthanc", 2));
        state = await RestartDecision(state, CommandKind.ChooseEffect, plan, new SequenceRandom());
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(6, EffectRuntime.Current(state)!.Effect!.Committed.Count(c => c.TargetId.Length > 0));
        Assert.Equal(6, state.Events.Count(e => e.Kind == "EventRecruited"));
        var old = state.Units.First(u => u.Nation == "sauron" && u.Location == "orthanc").Id;
        state = await RestartDecision(state, CommandKind.RemoveExcess, new() { Ids = [old] }, new SequenceRandom());
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [PostgresFact]
    public async Task ManyKingsShortageRestoresChosenRegionsWithoutRefillingReserve()
    {
        var state = State(ManyKings);
        Reserve(state, "southrons", UnitKind.Regular, 2);
        state = Play(state, ManyKings);
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            Plan(state, ("southrons", UnitKind.Regular, "far-harad", 1), ("southrons", UnitKind.Regular, "umbar", 1)), new SequenceRandom());
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.DoesNotContain(state.Units, u => u.Nation == "southrons" && u.Kind == UnitKind.Regular && u.Location == "reserve");
    }

    [PostgresFact]
    public async Task HillTrollsRestoresBothReplacementsExactlyOnce()
    {
        var state = State(HillTrolls);
        var first = BattleFixture.Put(state, "sauron", UnitKind.Regular, "orthanc").Id;
        var second = BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria").Id;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "gorgoroth");
        state = Play(state, HillTrolls);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [first, second] }, new SequenceRandom());
        Assert.Equal(2, state.Events.Count(e => e.Kind == "EventUnitReplaced"));
        Assert.All(new[] { first, second }, id => Assert.Equal("reserve", state.Units.Single(u => u.Id == id).Location));
    }

    [PostgresFact]
    public async Task SpecialAttackRestoresLastCommitAndHitLossWithoutRerolling()
    {
        var state = ShadowStrategy01Fixture.Combat(OlogHai, elite: true);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = OlogHai });
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(0, 0, 0, 0, 0, 0, 4));
        Assert.Equal(7, state.RandomSamples.Count);
        var unit = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).First();
        state = await RestartDecision(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(unit.Id)] }, new SequenceRandom());
        Assert.Equal(7, state.RandomSamples.Count);
        Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
    }
}
