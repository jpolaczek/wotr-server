using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class Strategy01PersistenceTests
{
    [PostgresFact]
    public async Task ProtectionAndHiddenCardCostSurviveRestartAndRetry()
    {
        var (state, payload) = RegionProtectionCardTests.Removal();
        state = await RestartDecision(state, CommandKind.DiscardProtection, payload, new SequenceRandom());
        Assert.Empty(state.ActiveEffects);
        Assert.Single(state.Events, e => e.Kind == "ProtectionRemoved");
        Assert.Equal(2, state.Player(Side.Shadow).Discard.Count);
    }

    [PostgresFact]
    public async Task LastBattlePersistsAcrossHuntAndDoesNotPayMovementTwice()
    {
        var state = Play(KingArmy(), LastBattle);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = CharacterFixture.Cost(state, DieFace.Character);
        state = await RestartDecision(state, CommandKind.MoveFellowship, payload, new SequenceRandom());
        Assert.Contains(LastBattle, state.Player(Side.FreePeoples).Table);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [PostgresFact]
    public async Task MazarbulKeepsMultipleGroupsAndItsRemainingPoliticalStep()
    {
        var state = Play(MazarbulCardTests.BeforeJourney(), BookOfMazarbul);
        state = await RestartDecision(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [
            new() { Ids = ["base.character.gimli"], Route = ["iron-hills", "erebor"] },
            new() { Ids = ["base.character.legolas"], Route = ["dale"] }
        ]
        }, new SequenceRandom());
        Assert.Equal(0, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "CompanionsMoved"));
    }

    [PostgresFact]
    public async Task SpiritRestoresTargetChoiceAndOrdinaryLossCause()
    {
        var state = MixedArmy();
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "morannon");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "morannon");
        state = Play(state, SpiritOfMordor);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["orthanc"] }, new SequenceRandom(5, 5, 5, 5, 5));
        Assert.Equal(CasualtyCause.Event, EventLosses.Current(state)!.Cause);
        state = await RestartDecision(state, CommandKind.TakeEventLosses, new()
        {
            Losses = EventLosses.Current(state)!.Target.Units(state)
            .Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList()
        }, new SequenceRandom());
        Assert.NotNull(CharacterRules.InPlay(state, CharacterRules.WitchKing));
        Assert.Equal(5, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task RangersRestoresLossesThenRecruitmentForOtherPlayer()
    {
        var state = MixedArmy("north-ithilien");
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "osgiliath");
        state = Play(state, FaramirsRangers, 5, 0, 0);
        var regular = EventLosses.Current(state)!.Target.Units(state).Single(u => u.Kind == UnitKind.Regular).Id;
        state = await RestartDecision(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(regular)] }, new SequenceRandom());
        var elite = state.Pending!.PrivateChoices.Single(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [elite] }, new SequenceRandom());
        Assert.Equal("osgiliath", state.Units.Single(u => u.Id == elite).Location);
        Assert.Equal(3, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task ScoutsRestoresBeforeRollingAndNeverDrawsCombatDiceOnRetry()
    {
        var state = StrategyCombatCardTests.ScoutsChoice();
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [state.Pending!.PrivateChoices[0]] }, new SequenceRandom());
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Empty(state.RandomSamples);
    }
}
