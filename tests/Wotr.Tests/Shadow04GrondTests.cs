using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04GrondTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThreeRoundsAreMandatoryThenOrdinaryEliteExtensionResumes(bool killKing)
    {
        var state = BeginGrond(elite: true);
        for (var round = 1; round <= 3; round++)
        {
            Assert.Equal(round, BattleRuntime.Active(state)!.Round);
            Reject(state, CommandKind.StopBattle, new());
            Reject(state, CommandKind.FinishArmyEvent, new());
            if (killKing && round == 2)
                CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.WitchKing)!);
            state = BlankRound(StateCodec.Copy(state));
            Assert.Single(state.Dice, d => d.Location == DieLocation.Resolving);
        }
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        var elite = state.Units.Single(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        Assert.Equal(4, BattleRuntime.Active(state)!.Round);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == elite.Id).Location);
        state = BlankRound(state);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstRoundRequiresCompanionButSecondRoundAlwaysAllowsCards(bool companion)
    {
        var state = BeginGrond(companion: companion);
        const string card = "base.fp.strategy.01";
        EventTests.GiveCard(state, Side.FreePeoples, card);
        Assert.Equal(companion, new BattleCards(EffectCatalog.Base).CanCommit(state, BattleRuntime.Active(state)!, Side.FreePeoples, card));
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        if (companion)
            state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        else
            Assert.NotEqual(DecisionKind.CombatCard, state.Pending!.Kind);
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        state = Do(state, CommandKind.CommitCombatCard);
        Assert.Contains(card, new BattleOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds);
        Assert.Equal(3, BattleProjection.View(state, Side.FreePeoples)!.MandatorySiegeRounds);
        Assert.False(BattleProjection.View(state, Side.FreePeoples)!.CompanionRequiredForCard);
    }

    [Fact]
    public void KingMayStayWithLegalRearButNotOutsideTheBesiegingArmy()
    {
        var state = State(Grond, true);
        var king = CharacterRules.InPlay(state, CharacterRules.WitchKing)!;
        king.Location = "osgiliath";
        Reject(state, CommandKind.PlayEvent, EventCost(state, Grond));
        king.Location = "minas-tirith";
        state = Play(state, Grond);
        var payload = Army(state, "minas-tirith", "minas-tirith");
        payload.Moves[0].CharacterIds.Clear();
        Reject(state, CommandKind.AttackArmy, payload);
        payload.Moves[0].UnitIds.Remove(state.Units.First(u => u.Location == "minas-tirith" && u.Nation == "sauron" && u.Kind == UnitKind.Regular).Id);
        state = Do(state, CommandKind.AttackArmy, payload);
        Assert.DoesNotContain(king.Id, BattleRuntime.Active(state)!.Attacker.Participants);
        Assert.Equal(3, BattleRuntime.Active(state)!.MandatorySiegeRounds);
    }

    [Fact]
    public void EliminatedArmyEndsGrondBeforeThreeRounds()
    {
        var state = BeginGrond();
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(5, 5, 5, 0, 0, 0));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        var ids = state.Units.Where(u => u.Location == "minas-tirith" && u.Garrison && u.Kind == UnitKind.Regular).Select(u => u.Id).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = ids.Select(id => new BattleLossChoice { UnitId = id }).ToList() });
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal(Side.Shadow, state.Control["minas-tirith"]);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
