using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01CombatTests
{
    public static IEnumerable<object[]> EveryCard() => Cards.Select(card => new object[] { card });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCombatRequiresOwnedCardAndCorrectDecision(string card)
    {
        var state = card is Denethor or Threats ? Siege(card, true) : Combat(card);
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
    [InlineData(false)]
    [InlineData(true)]
    public void DeadlyStrifeBenefitsBothArmiesInCombatAndRerollButNaturalOneMisses(bool shadowAttacks)
    {
        var state = Combat(Valinor, shadowAttacks);
        AddLeader(state, Side.Shadow);
        AddLeader(state, Side.FreePeoples);
        state = Shadow01Fixture.Reveal(state, Valinor, dice: [0, 2, 1, 0, 2, 1]);
        var battle = BattleRuntime.Active(state)!;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            Assert.Equal(2, force.Round.CombatModifier);
            Assert.Equal(2, force.Round.RerollModifier);
            Assert.False(force.Round.Dice[0].Hit);
            Assert.True(force.Round.Dice[1].Hit);
            Assert.False(force.Round.Dice[2].Hit);
        }
        while (state.Pending!.Kind == DecisionKind.BattleReroll)
        {
            var force = BattleRuntime.Active(state)!.Force(state.Pending.Owner);
            state = Do(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[2].Id] }, new SequenceRandom(2, 2));
        }
        battle = BattleRuntime.Active(state)!;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            Assert.Equal(2, force.Round.Dice[2].RerollModifier);
            Assert.True(force.Round.Dice[2].RerollHit);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeadlyStrifeCombinesWithAdvantageousPositionOnlyAgainstShadowCombat(bool shadowAttacks)
    {
        const string position = FreePeoplesStrategyCards01.TomBombadil;
        var state = Shadow01Fixture.BeforeCombat(Valinor, shadowAttacks, fpCard: position);
        state = Shadow01Fixture.Reveal(state, Valinor, position);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(2, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(2, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(2, battle.Force(Side.FreePeoples).Round.RerollModifier);
    }

    [Fact]
    public void DeadlyStrifeBonusesExpireBeforeNextRound()
    {
        var state = Shadow01Fixture.Reveal(Combat(Valinor), Valinor);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Round);
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            Assert.Equal(0, force.Round.CombatModifier);
            Assert.Equal(0, force.Round.RerollModifier);
        }
        Assert.Contains(Valinor, state.Player(Side.Shadow).Discard);
    }

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void DaringDefianceCancelsBonusSacrificeAndAdditionalAttack(string card)
    {
        const string daring = FreePeoplesCharacterCards02.Alone;
        var state = card is Denethor or Threats ? Siege(card, true) : Shadow01Fixture.BeforeCombat(card, true);
        EventTests.GiveCard(state, Side.FreePeoples, daring);
        state = Shadow01Fixture.Reveal(state, card, daring, new int[12]);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.All(new[] { battle.Attacker, battle.Defender }, force => Assert.Equal(0, force.Round.CombatModifier));
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.NotEqual(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.DoesNotContain(state.Events, e => e.Kind == "BattleLosses");
    }

    [Theory]
    [InlineData(Denethor, false)]
    [InlineData(Denethor, true)]
    [InlineData(Threats, false)]
    [InlineData(Threats, true)]
    public void BothDevilryCardsRequireParticipatingIsengardUnitAndSiege(string card, bool shadowAttacks)
    {
        var state = Siege(card, shadowAttacks);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[card].Condition;
        foreach (var mode in Enum.GetValues<BattleMode>())
        {
            battle.Mode = mode;
            Assert.Equal(mode == BattleMode.Siege, condition.CanPlay(state, battle, Side.Shadow));
        }
        battle.Mode = BattleMode.Siege;
        var force = battle.Force(Side.Shadow);
        var isengard = BattleRules.Units(state, force).Single(u => u.Nation == "isengard");
        force.Participants.Remove(isengard.Id);
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
    }

    [Theory]
    [InlineData(Denethor, false)]
    [InlineData(Denethor, true)]
    [InlineData(Threats, false)]
    [InlineData(Threats, true)]
    public void BothDevilryCardsAddOneToCombatWithoutChangingRerolls(string card, bool shadowAttacks)
    {
        var state = Shadow01Fixture.Reveal(Siege(card, shadowAttacks), card, dice: new int[9]);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(0, force.Round.RerollModifier);
        Assert.All(force.Round.Dice, die => { Assert.Equal(1, die.Modifier); Assert.False(die.Hit); });
        while (state.Pending!.Kind == DecisionKind.BattleReroll)
        {
            force = BattleRuntime.Active(state)!.Force(state.Pending.Owner);
            state = Do(state, CommandKind.RerollBattle,
                new() { Ids = force.Side == Side.Shadow ? [force.Round.Dice[0].Id] : [] }, new SequenceRandom(3));
        }
        var rerolled = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice[0];
        Assert.Equal(0, rerolled.RerollModifier);
        Assert.False(rerolled.RerollHit);
    }

    [Fact]
    public void BothDevilryCardsAcceptAnEliteIsengardParticipant()
    {
        foreach (var card in new[] { Denethor, Threats })
        {
            var state = Siege(card, true);
            var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
            var regular = BattleRules.Units(state, force).Single(u => u.Nation == "isengard");
            force.Participants.Remove(regular.Id);
            force.Participants.Add(BattleFixture.Put(state, "isengard", UnitKind.Elite, force.Region).Id);
            state = Shadow01Fixture.Reveal(state, card, dice: new int[9]);
            Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CombatModifier);
        }
    }

    private static MatchState Siege(string card, bool shadowAttacks)
    {
        var state = Shadow04CombatTests.Combat(ShadowCharacterCards04.Spells, shadowAttacks);
        EventTests.GiveCard(state, Side.Shadow, card);
        return state;
    }

    private static void AddLeader(MatchState state, Side side)
    {
        var force = BattleRuntime.Active(state)!.Force(side);
        var unit = BattleFixture.Put(state, side == Side.Shadow ? "sauron" : "gondor",
            side == Side.Shadow ? UnitKind.Nazgul : UnitKind.Leader, force.Region);
        force.Participants.Add(unit.Id);
    }
}
