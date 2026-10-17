using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards03;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class ShadowStrategy03CombatTests
{
    public static IEnumerable<object[]> EveryCard() => ShadowStrategy03Fixture.Cards.Select(card => new object[] { card });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void EveryCombatValidatesOwnedCardAndDecision(string card)
    {
        var state = ShadowStrategy01Fixture.Combat(card, elite: true);
        var payload = new CommandPayload { CardId = card };
        Assert.Null(Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.CommitCombatCard, payload)));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.CommitCombatCard, payload)));
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Theory]
    [InlineData(HalfOrcs)]
    [InlineData(OlogHai)]
    [InlineData(HillTrolls)]
    public void WeComeToKillRequiresAnEliteParticipantNotAnEliteRearGuard(string card)
    {
        var state = ShadowStrategy01Fixture.Combat(card);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var rear = BattleFixture.Put(state, "isengard", UnitKind.Elite, force.Region);
        Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
        force.Participants.Add(rear.Id);
        Assert.Null(Engine().Validate(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.CommitCombatCard, new() { CardId = card })));
    }

    [Theory]
    [InlineData(HalfOrcs, false)]
    [InlineData(HalfOrcs, true)]
    [InlineData(OlogHai, false)]
    [InlineData(OlogHai, true)]
    [InlineData(HillTrolls, false)]
    [InlineData(HillTrolls, true)]
    public void WeComeToKillRollsAfterCombatAndImmediatelyAssignsHits(string card, bool shadowAttacks)
    {
        var state = ShadowStrategy01Fixture.Combat(card, shadowAttacks, elite: true);
        state = Shadow01Fixture.Reveal(state, card, dice: [0, 0, 0, 0, 0, 0, 4]);
        var battle = BattleRuntime.Active(state)!;
        var special = Assert.Single(battle.SpecialAttacks);
        var die = Assert.Single(special.Dice);
        Assert.Equal(5, die.HitTarget);
        Assert.True(die.Hit);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(DecisionKind.Losses, state.Pending.Kind);
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        Assert.Equal(0, BattleRoll.HitCount(battle.Force(Side.Shadow)));
        Assert.Equal(1, battle.Force(Side.Shadow).Round.SpecialHits);
    }

    [Theory]
    [InlineData(BattleMode.Field)]
    [InlineData(BattleMode.Siege)]
    [InlineData(BattleMode.Sortie)]
    [InlineData(BattleMode.Relief)]
    public void SpecialAttackAlwaysHitsOnFiveDespiteNormalBattleThreshold(BattleMode mode)
    {
        var state = ThresholdBattle(mode);
        Assert.Equal(mode, BattleRuntime.Active(state)!.Mode);
        state = Shadow01Fixture.Reveal(state, HalfOrcs, dice: [0, 0, 0, 0, 0, 0, 4]);
        Assert.True(Assert.Single(Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice).Hit);
    }

    [Theory]
    [InlineData(FreePeoplesStrategyCards01.LastBattle)]
    [InlineData(FreePeoplesStrategyCards01.TomBombadil)]
    public void SpecialAttackIgnoresDaylightAndAdvantageousPosition(string opponentCard)
    {
        var state = ShadowStrategy01Fixture.Combat(OlogHai, elite: true);
        EventTests.GiveCard(state, Side.FreePeoples, opponentCard);
        state = Shadow01Fixture.Reveal(state, OlogHai, opponentCard, 0, 0, 0, 0, 0, 0, 4);
        var die = Assert.Single(Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice);
        Assert.Equal(0, die.Modifier);
        Assert.Equal(5, die.HitTarget);
        Assert.Null(die.Reroll);
        Assert.True(die.Hit);
    }

    [Fact]
    public void DowngradedEliteDoesNotRollAfterNormalCasualties()
    {
        var state = ShadowStrategy01Fixture.Combat(HillTrolls, elite: true);
        var elite = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Single(u => u.Kind == UnitKind.Elite);
        state = Shadow01Fixture.Reveal(state, HillTrolls, dice: [0, 0, 0, 5, 0, 0]);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        var random = new SequenceRandom();
        state = Do(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(elite.Id, BattleLossKind.Downgrade)] }, random);
        Assert.Equal(0, random.Calls);
        Assert.Empty(Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice);
        Assert.Equal(6, state.RandomSamples.Count);
    }

    [Fact]
    public void SpecialAttackCountsSurvivingElitesOnceAndCapsDiceAtFive()
    {
        var state = ShadowStrategy01Fixture.Combat(HalfOrcs, own: 1, elite: true);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var i = 0; i < 5; i++)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Elite, force.Region).Id);
        state = Shadow01Fixture.Reveal(state, HalfOrcs, dice: [0, 0, 0, 0, 0, 0, 0, 0, 4, 4, 4, 4, 4]);
        Assert.Equal(5, Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice.Count);
        Assert.Equal(13, state.RandomSamples.Count);
    }

    [Fact]
    public void SarumanLeadershipAndRearGuardDoNotAddSpecialDice()
    {
        var state = ShadowStrategy01Fixture.Combat(HalfOrcs);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        force.Participants.Add(BattleFixture.Put(state, "isengard", UnitKind.Elite, force.Region).Id);
        force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region).Id);
        BattleFixture.Put(state, "isengard", UnitKind.Elite, force.Region);
        Put(state, CharacterRules.Saruman, "orthanc");
        state = Shadow01Fixture.Reveal(state, HalfOrcs, dice: new int[10]);
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle, random: new SequenceRandom(4));
        Assert.Single(Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks).Dice);
    }

    [Theory]
    [InlineData(HalfOrcs)]
    [InlineData(OlogHai)]
    [InlineData(HillTrolls)]
    [InlineData(NewPower)]
    [InlineData(ManyKings)]
    [InlineData(KingRevealed)]
    public void DaringDefianceCancelsEveryCombatApplication(string card)
    {
        var state = ShadowStrategy01Fixture.Combat(card, own: 4, enemy: 2, elite: true);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Participants.Add(Put(state, CharacterRules.Aragorn, force.Region).Id);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Alone);
        state = Shadow01Fixture.Reveal(state, card, FreePeoplesCharacterCards02.Alone, new int[10]);
        Assert.True(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardCancelled);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.NotEqual(DecisionKind.Losses, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(NewPower)]
    [InlineData(ManyKings)]
    public void BothGreatHostAliasesApplyOneAutomaticHitWithDoubleArmy(string card)
    {
        var state = Shadow01Fixture.Reveal(ShadowStrategy01Fixture.Combat(card, own: 4, enemy: 2), card);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        Assert.Equal(6, state.RandomSamples.Count);
    }

    [Fact]
    public void KingRevealedRelentlessAliasOffersOptionalLossBeforeCombat()
    {
        var state = Shadow01Fixture.Reveal(ShadowStrategy01Fixture.Combat(KingRevealed), KingRevealed);
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        Assert.Empty(state.RandomSamples);
        state = Do(state, CommandKind.TakeBattleLosses, random: new SequenceRandom(new int[6]));
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(6, state.RandomSamples.Count);
    }
    private static MatchState ThresholdBattle(BattleMode mode)
    {
        var state = ShadowStrategy03Fixture.State(HalfOrcs);
        var from = mode == BattleMode.Field ? "lossarnach" : mode == BattleMode.Relief ? "hollin" : "moria";
        var target = mode == BattleMode.Field ? "pelargir" : "moria";
        state.Control[target] = mode is BattleMode.Sortie or BattleMode.Relief ? Side.Shadow : Side.FreePeoples;
        for (var i = 0; i < 3; i++)
        {
            BattleFixture.Put(state, "sauron", i == 0 ? UnitKind.Elite : UnitKind.Regular, from).Garrison = mode == BattleMode.Sortie;
            BattleFixture.Put(state, "gondor", UnitKind.Regular, target).Garrison = mode == BattleMode.Siege;
        }
        if (mode == BattleMode.Relief)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, target).Garrison = true;
        var attackers = state.Units.Where(u => u.Location == from && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToArray();
        return BattleFixture.Begin(state, attackers, from: from, to: target);
    }

}
