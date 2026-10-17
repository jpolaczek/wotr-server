using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleReportTests
{
    [Fact]
    public void FinishedRoundSurvivesSaveAndFurtherMovementWithoutPrivateChoicesOrSharedReferences()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "gondor", UnitKind.Leader, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = Begin(state, state.Units.Where(u => u.Location == Origin).Select(u => u.Id));
        state = NoCards(state, 1, 1);
        var roll = BattleRuntime.Current(state)!.Attacker.Round.Dice[0].Id;
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = [roll] }, new SequenceRandom(0));
        state = Execute(state, CommandKind.StopBattle);
        state = StateCodec.Copy(state);
        Assert.Null(BattleRuntime.Active(state));
        foreach (var side in Enum.GetValues<Side>())
        {
            var report = new Projection().For(state, side).LastBattleRound!;
            Assert.Equal(1, report.Attacker.Dice[0].Result);
            Assert.Equal(1, report.Attacker.Dice[0].Reroll);
            Assert.Empty(report.Attacker.OwnRerollSelection);
            Assert.Empty(report.Defender.OwnRerollSelection);
            Assert.Contains(report.Attacker.Units, u => u.Id == attacker.Id && u.Location == Origin);
            report.Attacker.Dice.Clear();
            Assert.Single(state.LastBattleRound!.Attacker.Dice);
        }
        state.Units.Single(u => u.Id == attacker.Id).Location = "pelargir";
        Assert.Equal(Origin, state.LastBattleRound!.Attacker.Units.Single(u => u.Id == attacker.Id).Location);
    }

    [Fact]
    public void UnrevealedCommitNeverEntersPublicReport()
    {
        var catalog = Catalog([new(3, "test.report")], [new(3, "test.report")], new EffectDefinition("test.report", []));
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state.Player(Side.FreePeoples).Hand.Add(FpCard);
        state = Begin(state, [attacker.Id], catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard }, catalog: catalog);
        BattleProjection.RememberRound(state, BattleRuntime.Active(state)!);
        Assert.Null(state.LastBattleRound);
        Assert.Null(new Projection().For(state, Side.Shadow).Battle!.Attacker.CardId);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0), catalog: catalog);
        Assert.Null(state.LastBattleRound); // Step 5 still belongs to this round.
        state = Execute(state, CommandKind.StopBattle, catalog: catalog);
        Assert.Equal(FpCard, state.LastBattleRound!.Attacker.CardId);
    }
}
