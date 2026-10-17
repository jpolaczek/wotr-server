using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy04Fixture;

namespace Wotr.Tests;

public class ShadowStrategy04CombatTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(card => new object[] { card });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCombatRequiresOwnerCardAndCombatDecision(string card)
    {
        var state = ShadowStrategy01Fixture.Combat(card);
        var payload = new CommandPayload { CardId = card };
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, payload);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Theory]
    [InlineData(Horde, 2)]
    [InlineData(Monsters, 1)]
    [InlineData(Musterings, 1)]
    [InlineData(Pits, 1)]
    public void PrintedBonusesModifyBothArmiesCombatAndRerollsWithNaturalOneMissing(string card, int bonus)
    {
        var state = ShadowStrategy01Fixture.Combat(card);
        var battle = BattleRuntime.Active(state)!;
        battle.Force(Side.Shadow).Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, battle.Force(Side.Shadow).Region).Id);
        battle.Force(Side.FreePeoples).Participants.Add(BattleFixture.Put(state, "gondor", UnitKind.Leader, battle.Force(Side.FreePeoples).Region).Id);
        state = Shadow01Fixture.Reveal(state, card, dice: [0, 4 - bonus, 5, 0, 4 - bonus, 5]);
        foreach (var force in new[] { BattleRuntime.Active(state)!.Attacker, BattleRuntime.Active(state)!.Defender })
        {
            Assert.Equal(bonus, force.Round.CombatModifier);
            Assert.Equal(bonus, force.Round.RerollModifier);
            Assert.False(force.Round.Dice[0].Hit);
            Assert.True(force.Round.Dice[1].Hit);
            Assert.True(force.Round.Dice[2].Hit);
        }
        while (state.Pending!.Kind == DecisionKind.BattleReroll)
        {
            var force = BattleRuntime.Active(state)!.Force(state.Pending.Owner);
            state = Do(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[0].Id] }, new SequenceRandom(4 - bonus, 4 - bonus));
        }
        foreach (var force in new[] { BattleRuntime.Active(state)!.Attacker, BattleRuntime.Active(state)!.Defender })
        {
            Assert.Equal(bonus, force.Round.Dice[0].RerollModifier);
            Assert.True(force.Round.Dice[0].RerollHit);
        }
    }

    [Theory]
    [InlineData(Monsters)]
    [InlineData(Musterings)]
    [InlineData(Pits)]
    public void DesperateBattleStacksWithOpponentCombatPenaltyAndExpiresNextRound(string card)
    {
        const string position = FreePeoplesStrategyCards01.TomBombadil;
        var state = ShadowStrategy01Fixture.Combat(card);
        EventTests.GiveCard(state, Side.FreePeoples, position);
        state = Shadow01Fixture.Reveal(state, card, position, new int[6]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.CombatModifier);
        state = CardPackBattleFixture.NoRerolls(state);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        Assert.Equal(0, BattleRuntime.Active(state)!.Attacker.Round.CombatModifier);
        Assert.Equal(0, BattleRuntime.Active(state)!.Defender.Round.RerollModifier);
    }

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void DaringDefianceCancelsAllSixEffects(string card)
    {
        const string daring = FreePeoplesCharacterCards02.Alone;
        var state = ShadowStrategy01Fixture.Combat(card);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Participants.Add(Put(state, CharacterRules.Aragorn, force.Region).Id);
        EventTests.GiveCard(state, Side.FreePeoples, daring);
        state = Shadow01Fixture.Reveal(state, card, daring, new int[7]);
        Assert.True(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, BattleRuntime.Active(state)!.Attacker.Round.CombatModifier);
        Assert.Equal(0, BattleRuntime.Active(state)!.Defender.Round.CombatModifier);
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(MistyMountains, 0)]
    [InlineData(OrcsMultiplying, 0)]
    [InlineData(MistyMountains, 3)]
    [InlineData(OrcsMultiplying, 3)]
    public void OnslaughtKeepsOptionalZeroOrResolvesAttackEvenAfterOwnElimination(string card, int losses)
    {
        var state = Shadow01Fixture.Reveal(ShadowStrategy01Fixture.Combat(card), card, dice: new int[6]);
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        state = Do(state, CommandKind.TakeBattleLosses, ShadowStrategy01Fixture.Sacrifice(state, losses), new SequenceRandom(3, 0, 5));
        if (losses == 0)
        {
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
            return;
        }
        var battle = BattleRuntime.Active(state)!;
        Assert.False(BattleRules.Alive(state, battle.Force(Side.Shadow)));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        Assert.Single(battle.SpecialAttacks);
    }
}
