using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Domain.FreePeoplesCharacterCards02;

namespace Wotr.Tests;

public class CardPack02PersistenceTests
{
    [PostgresFact]
    public async Task StaffRestoresBeforeAnyTileEntropyAndConsumesOnlyOneTableCard()
    {
        var state = await RestartDecision(FellowshipEventCardTests.StaffWindow(), CommandKind.RespondEffect,
            new() { ReactionId = StaffReaction }, new SequenceRandom());
        Assert.Equal(1, state.Fellowship.MordorStep);
        Assert.Empty(state.RandomSamples);
        Assert.Single(state.Player(Side.FreePeoples).Discard, id => id == Staff);
    }

    [PostgresFact]
    public async Task AthelasRetriesDoNotRepeatDiceHealingOrCost()
    {
        var state = TurnTests.Actions();
        state.RandomSamples.Clear();
        state.Fellowship.Corruption = 5;
        EventTests.GiveCard(state, Side.FreePeoples, Athelas);
        state = await RestartDecision(state, CommandKind.PlayEvent, EventTests.Play(state, Athelas, DieFace.Character).Payload, new SequenceRandom(5, 5, 5));
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [PostgresFact]
    public async Task AnotherWayRestoresOptionalMoveWithoutTurningTheCardCostIntoAHuntDie()
    {
        var state = await RestartDecision(FellowshipTravelCardTests.AnotherWayChoice(), CommandKind.ChooseEffect,
            new() { Ids = [AnotherWayStep.Move] }, new SequenceRandom());
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }

    [PostgresFact]
    public async Task AloneRestoresBonusSeparationAndGuideChoiceBeforeHealing()
    {
        var state = await RestartDecision(FellowshipTravelCardTests.AloneChoice(), CommandKind.SeparateCompanions,
            new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin"] }, new SequenceRandom());
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(2, state.Fellowship.Corruption);
        state = await RestartDecision(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider }, new SequenceRandom());
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Equal("hollin", state.Characters.Single().Location);
    }

    [PostgresFact]
    public async Task AndurilRestoresTwoChosenMissesAndForfeitsLeadershipOnce()
    {
        var state = LeadershipCombatCardTests.AndurilChoice();
        var ids = BattleRuntime.Active(state)!.Attacker.Round.Dice.Take(2).Select(d => d.Id).ToList();
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ids }, new SequenceRandom());
        var round = BattleRuntime.Active(state)!.Attacker.Round;
        Assert.Equal(2, round.Dice.Count(d => d.ConvertedHit));
        Assert.Equal(2, round.ForfeitedLeadership[CharacterRules.Strider]);
    }

    [PostgresFact]
    public async Task HeroicRestoresDependentChoicesAndEliminatesExactlyOneFigure()
    {
        var state = await RestartDecision(CasualtyCombatCardTests.HeroicChoice(), CommandKind.ChooseEffect,
            new() { Ids = [CharacterRules.Strider, HeroicDeathStep.HitPrefix + 2] }, new SequenceRandom());
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        Assert.Single(state.Characters, c => c.Location == "casualties");
        Assert.Single(state.Events, e => e.Kind == "HeroicDeath");
    }

    [PostgresFact]
    public async Task FatefulRestoresTargetChoiceWithoutSpendingDamage()
    {
        var state = await RestartDecision(CasualtyCombatCardTests.FatefulChoice(), CommandKind.ChooseEffect,
            new() { Ids = [CharacterRules.WitchKing] }, new SequenceRandom());
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        Assert.Single(state.Events, e => e.Kind == "FatefulStrike");
    }
}
