using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy04Fixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class ShadowStrategy04PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresItsCostAndMandatoryContinuationExactlyOnce()
    {
        foreach (var card in Cards)
        {
            var state = State(card);
            var cost = EventCost(state, card);
            state = await RestartDecision(state, CommandKind.PlayEvent, cost, new SequenceRandom());
            Assert.Single(state.Events, e => e.Kind == "EventPlayed" && e.Text.Contains(Wotr.Domain.Content.CardCatalog.Title(card), StringComparison.Ordinal));
            Assert.Equal(card is MistyMountains or Horde or Pits ? DieLocation.Resolving : DieLocation.Spent,
                state.Dice.Single(d => d.Id == cost.DieId).Location);
        }
    }

    [PostgresFact]
    public async Task EveryRecruitmentChoiceRestoresPlacementsWithoutDoubleRecruitment()
    {
        foreach (var card in new[] { MistyMountains, OrcsMultiplying, Horde, Monsters, Pits })
        {
            var state = State(card);
            if (card is OrcsMultiplying or Monsters)
                Reserve(state, "sauron", UnitKind.Regular, 1);
            state = Play(state, card);
            var plan = card switch
            {
                MistyMountains => Plan(state, ("sauron", UnitKind.Regular, "moria", 1), ("sauron", UnitKind.Elite, "moria", 1), ("sauron", UnitKind.Nazgul, "moria", 1)),
                OrcsMultiplying => Plan(state, ("sauron", UnitKind.Regular, "mount-gundabad", 1)),
                Horde => Plan(state, ("southrons", UnitKind.Regular, "khand", 5)),
                Monsters => Plan(state, ("sauron", UnitKind.Regular, "ettenmoors", 1), ("sauron", UnitKind.Elite, "trollshaws", 1)),
                _ => Plan(state, ("sauron", UnitKind.Regular, "moria", 2), ("sauron", UnitKind.Regular, "morannon", 2), ("sauron", UnitKind.Regular, "barad-dur", 2))
            };
            state = await RestartDecision(state, CommandKind.ChooseEffect, plan, new SequenceRandom());
            Assert.Equal(plan.Placements.Count, state.Events.Count(e => e.Kind == "EventRecruited"));
            Assert.All(plan.Placements, placement => Assert.Equal(placement.RegionId, state.Units.Single(u => u.Id == placement.UnitId).Location));
            Assert.Empty(state.Resolution.Frames);
        }
    }

    [PostgresFact]
    public async Task ExcessAndPalantirResumeAfterTheWholePaidPlanWithoutRepeatingIt()
    {
        var state = State(MistyMountains);
        Put(state, CharacterRules.Saruman, "orthanc");
        Shadow04Fixture.Table(state, ShadowCharacterCards04.Palantir);
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "southrons", UnitKind.Regular, "moria");
        state = Do(state, CommandKind.PlayEvent, EventCost(state, MistyMountains, DieFace.Event));
        var plan = Plan(state, ("sauron", UnitKind.Elite, "moria", 2), ("sauron", UnitKind.Nazgul, "moria", 1));
        state = await RestartDecision(state, CommandKind.ChooseEffect, plan, new SequenceRandom());
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CardDrawn");
        var excess = state.Units.First(u => u.Nation == "southrons" && u.Location == "moria").Id;
        state = await RestartDecision(state, CommandKind.RemoveExcess, new() { Ids = [excess] }, new SequenceRandom());
        Assert.Equal("Deck", state.Pending!.Presentation);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] }, new SequenceRandom());
        Assert.Single(state.Events, e => e.Kind == "CardDrawn");
        Assert.Equal(3, state.Events.Count(e => e.Kind == "EventRecruited"));
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task OnslaughtAndDesperateBattleRestoreResolutionAndEntropy()
    {
        foreach (var card in new[] { MistyMountains, OrcsMultiplying, Monsters, Musterings, Pits, Horde })
        {
            var state = ShadowStrategy01Fixture.Combat(card);
            state = Do(state, CommandKind.CommitCombatCard, new() { CardId = card });
            state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[6]));
            Assert.Equal(6, state.RandomSamples.Count);
            if (card is MistyMountains or OrcsMultiplying)
                state = await RestartDecision(state, CommandKind.TakeBattleLosses, new(), new SequenceRandom());
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            Assert.Equal(6, state.RandomSamples.Count);
        }
    }
}
