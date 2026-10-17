using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionLogFamilyTests;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class ActionLogBattleFactsTests
{
    [Fact]
    public void CommittedCardRemainsHiddenInItsOriginalStepAfterRevealAndRoundCompletion()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        EventTests.GiveCard(state, Side.FreePeoples, FpCard);
        state = Begin(state, [attacker.Id]);
        var battleId = BattleRuntime.Active(state)!.Id;
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard });
        var committed = Steps(state, Side.Shadow).Single(s => s.Command == CommandKind.CommitCombatCard);
        var original = Json(committed);
        Assert.Null(committed.Facts.Battle!.Attacker.CardId);
        Assert.Equal("strategy", Assert.Single(committed.Facts.Cards).Deck);
        Assert.DoesNotContain(FpCard, original);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0));
        state = Execute(state, CommandKind.StopBattle);
        var steps = Steps(StateCodec.Copy(state), Side.Shadow);
        Assert.Equal(original, Json(steps.Single(s => s.Id == committed.Id)));
        Assert.Contains(steps, s => s.Facts.Cards.Any(c => c.Use == ActionCardUse.CombatReveal && c.Id == FpCard));
        var round = Assert.Single(steps, s => s.Kind == "BattleRoundCompleted");
        Assert.Equal(battleId, round.Context.BattleId);
        Assert.True(round.Facts.Battle!.CardsRevealed);
        Assert.Equal(FpCard, round.Facts.Battle.Attacker.CardId);
        Assert.Equal(1, round.Facts.Battle.Attacker.Dice[0].Result);
        Assert.All(steps.Where(s => s.Context.BattleId == battleId).SelectMany(s => s.Costs),
            c => Assert.Equal(ActionCostKind.Die, c.Kind));
        Assert.Single(steps.SelectMany(s => s.Costs), c => c.Kind == ActionCostKind.Die);
        Assert.Equal(ActionLogStatus.Completed, steps.Last().Status);
    }

    [Fact]
    public void RerollChoicesArePrivateButResolvedNaturalAndRerollResultsArePublic()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Nazgul, Target);
        state = NoCards(Begin(state, [unit.Id, leader.Id]), 1, 2);
        var choice = BattleRuntime.Current(state)!.Attacker.Round.Dice[0].Id;
        var publicBefore = Json(Steps(state, Side.Shadow));
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = [choice] });
        Assert.Equal(publicBefore, Json(Steps(state, Side.Shadow)));
        Assert.Equal(choice, Assert.Single(Steps(state).Single(s => s.Command == CommandKind.RerollBattle).Facts.Targets));
        var other = BattleRuntime.Current(state)!.Defender.Round.Dice[0].Id;
        state = Execute(StateCodec.Copy(state), CommandKind.RerollBattle, new() { Ids = [other] }, new SequenceRandom(5, 0));
        foreach (var side in Enum.GetValues<Side>())
        {
            var rolled = Steps(state, side).Last(s => s.Kind == "LeaderReroll").Facts.Battle!;
            Assert.Equal(1, rolled.Attacker.Dice[0].Result);
            Assert.Equal(6, rolled.Attacker.Dice[0].Reroll);
            Assert.True(rolled.Attacker.Dice[0].RerollHit);
            Assert.Equal(2, rolled.Defender.Dice[0].Result);
            Assert.Equal(1, rolled.Defender.Dice[0].Reroll);
            Assert.Empty(rolled.Attacker.OwnRerollSelection);
            Assert.Empty(rolled.Defender.OwnRerollSelection);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LossFactsRetainOrderAndReplacementOriginEvenWhenTheSameRegularReturns(bool downgrade)
    {
        var state = State();
        var elite = Put(state, "gondor", UnitKind.Elite, Origin);
        var regular = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Regular, Target);
        foreach (var reserve in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve"))
            reserve.Location = "removed";
        state = NoCards(Begin(state, [elite.Id, regular.Id]), 1, 1, 6, 6);
        state = downgrade ? Losses(state, Loss(regular.Id), Loss(elite.Id, BattleLossKind.Downgrade)) : Losses(state, Loss(elite.Id));
        foreach (var side in Enum.GetValues<Side>())
        {
            var step = Steps(state, side).Single(s => s.Kind == "BattleLosses");
            var facts = step.Facts.LossDetails;
            Assert.Equal(2, facts.Sum(f => f.Hits));
            Assert.Equal(UnitKind.Elite, facts.Last().Figure.UnitKind);
            Assert.Equal(Origin, facts.Last().RegionId);
            if (downgrade)
            {
                Assert.Equal(new[] { regular.Id, elite.Id }, facts.Select(f => f.Figure.Id));
                Assert.Equal(regular.Id, facts[1].ReplacementId);
                Assert.Equal("casualties", facts[1].ReplacementFrom);
            }
            else
                Assert.Equal("", Assert.Single(facts).ReplacementId);
        }
        var history = Json(Steps(state));
        state.Units.Single(u => u.Id == regular.Id).Location = "casualties";
        Assert.Equal(history, Json(Steps(state)));
    }

    [Fact]
    public void RetreatAndPartialAdvanceKeepActualParticipantsAndActionOpenUntilAdvance()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        var rearguard = Put(state, "gondor", UnitKind.Elite, Origin);
        var enemy = Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [unit.Id, rearguard.Id]), 1, 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        var destination = BattleMovement.Retreats(state, BattleRuntime.Current(state)!).First();
        state = Execute(state, CommandKind.RetreatBattle, new() { RegionId = destination });
        var retreat = Steps(state).Single(s => s.Kind == "BattleRetreat");
        Assert.Equal(ActionLogStatus.InProgress, retreat.Status);
        Assert.Equal(enemy.Id, Assert.Single(Assert.Single(retreat.Movements).Participants).Id);
        Assert.Equal(new[] { Target, destination }, retreat.Movements[0].Route);
        state = Execute(state, CommandKind.AdvanceBattle, new() { Ids = [unit.Id] });
        var advance = Steps(state).Single(s => s.Kind == "BattleAdvance");
        Assert.Equal(unit.Id, Assert.Single(Assert.Single(advance.Movements).Participants).Id);
        Assert.Equal(ActionLogStatus.Completed, advance.Status);
        Assert.Equal(Origin, state.Units.Single(u => u.Id == rearguard.Id).Location);
        Assert.Equal(retreat.ActionId, advance.ActionId);
    }
}
