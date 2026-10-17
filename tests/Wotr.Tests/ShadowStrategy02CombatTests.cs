using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards02;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class ShadowStrategy02CombatTests
{
    private const string Daring = FreePeoplesCharacterCards02.Alone;
    private const string Charge = FreePeoplesStrategyCards03.CirdansShips;
    private static readonly string[] AllCards = [Gather, Lengthens, Moving, Corsairs, Rage, Return];

    public static IEnumerable<object[]> EveryCard() => AllCards.Select(card => new object[] { card });

    [Theory]
    [MemberData(nameof(EveryCard))]
    public void CombatUseRequiresOwnedCardCorrectSideAndCardWindow(string card)
    {
        var state = Before(card);
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
    [InlineData(Gather, "regular")]
    [InlineData(Gather, "wrong-nation")]
    [InlineData(Gather, "rearguard")]
    [InlineData(Lengthens, "regular")]
    [InlineData(Lengthens, "wrong-nation")]
    [InlineData(Lengthens, "rearguard")]
    public void MumakilRequiresSouthronEliteAmongActualParticipants(string card, string invalid)
    {
        var state = Before(card);
        var battle = BattleRuntime.Active(state)!;
        var elite = BattleRules.Units(state, battle.Force(Side.Shadow)).Single(u => u.Kind == UnitKind.Elite);
        if (invalid == "regular")
            elite.Kind = UnitKind.Regular;
        else if (invalid == "wrong-nation")
            elite.Nation = "sauron";
        else
            battle.Force(Side.Shadow).Participants.Remove(elite.Id);
        Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
    }

    [Fact]
    public void BothMumakilCardsAddOnlyCombatBonusAndCompareHitsAfterTheLeaderReroll()
    {
        foreach (var card in new[] { Gather, Lengthens })
        {
            var state = Before(card, nazgul: true);
            state = Reveal(state, card, values: [3, 0, 0, 4, 0, 0]);
            var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
            Assert.Equal(1, shadow.Round.CombatModifier);
            Assert.Equal(0, shadow.Round.RerollModifier);
            Assert.True(shadow.Round.Dice[0].Hit);
            Assert.False(shadow.Round.Dice[1].Hit);
            Assert.Equal(0, shadow.Round.ExtraHits);
            state = Do(state, CommandKind.RerollBattle, new() { Ids = [shadow.Round.Dice[1].Id] }, new SequenceRandom(4));
            shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
            Assert.Equal(0, shadow.Round.Dice[1].RerollModifier);
            Assert.True(shadow.Round.Dice[1].RerollHit);
            Assert.Equal(1, shadow.Round.ExtraHits);
            Assert.Equal(3, BattleRoll.HitCount(shadow));
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 1, 1)]
    public void MumakilAwardsAnExtraHitOnlyForStrictlyMoreTotalHits(int shadowHits, int freeHits, int bonus)
    {
        var dice = Enumerable.Range(0, 3).Select(i => i < shadowHits ? 3 : 0)
            .Concat(Enumerable.Range(0, 3).Select(i => i < freeHits ? 4 : 0)).ToArray();
        var state = Reveal(Before(Gather), Gather, values: dice);
        Assert.Equal(bonus, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ExtraHits);
    }

    [Theory]
    [InlineData(Gather)]
    [InlineData(Lengthens)]
    public void MumakilAlsoImprovesShadowWhenDefending(string card)
    {
        var state = Reveal(Before(card, shadowAttacks: false), card, values: [0, 0, 0, 3, 0, 0]);
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(1, shadow.Round.CombatModifier);
        Assert.True(shadow.Round.Dice[0].Hit);
        Assert.Equal(1, shadow.Round.ExtraHits);
    }

    [Theory]
    [InlineData(Lengthens, 1, 0)]
    [InlineData(Lengthens, 2, 1)]
    [InlineData(Gather, 1, 0)]
    [InlineData(Gather, 2, 1)]
    public void MumakilComparisonIncludesFreePeoplesChargeHitsAlreadyTaken(string card, int shadowHits, int bonus)
    {
        var state = Reveal(Before(card, fpCard: Charge, fpElites: 1), card, Charge, 5);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        var elite = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Single(u => u.Kind == UnitKind.Elite);
        var dice = Enumerable.Range(0, 3).Select(i => i < shadowHits ? 3 : 0).Concat(new int[3]).ToArray();
        state = Do(state, CommandKind.TakeBattleLosses,
            new() { Losses = [BattleFixture.Loss(elite.Id, BattleLossKind.Downgrade)] }, new SequenceRandom(dice));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.SpecialHits);
        Assert.Equal(0, BattleRoll.HitCount(battle.Force(Side.FreePeoples)));
        Assert.Equal(bonus, battle.Force(Side.Shadow).Round.ExtraHits);
    }

    [Fact]
    public void LosingTheSouthronEliteToChargeDoesNotRemoveCommittedMumakilBonus()
    {
        var state = Before(Gather, fpCard: Charge, fpElites: 2);
        state = Reveal(state, Gather, Charge, 5, 5);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        var elite = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Single(u => u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(elite.Id)] }, new SequenceRandom(3, 0, 0, 0, 0));
        var battle = BattleRuntime.Active(state)!;
        Assert.DoesNotContain(BattleRules.Units(state, battle.Force(Side.Shadow)), u => u.Kind == UnitKind.Elite);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.True(battle.Force(Side.Shadow).Round.Dice[0].Hit);
        Assert.Equal(2, battle.Force(Side.FreePeoples).Round.SpecialHits);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.ExtraHits);
    }

    [Fact]
    public void BothSwarmCardsWithoutOpponentCardImproveOnlyLeaderRerolls()
    {
        foreach (var card in new[] { Moving, Return })
        {
            var state = Reveal(Before(card, nazgul: true), card, values: [3, 0, 0, 0, 0, 0]);
            var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
            Assert.Equal(0, shadow.Round.CombatModifier);
            Assert.Equal(1, shadow.Round.RerollModifier);
            Assert.False(shadow.Round.Dice[0].Hit);
            state = Do(state, CommandKind.RerollBattle, new() { Ids = [shadow.Round.Dice[0].Id] }, new SequenceRandom(3));
            shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
            Assert.True(shadow.Round.Dice[0].RerollHit);
            Assert.Equal(1, shadow.Round.Dice[0].RerollModifier);
        }
    }

    [Theory]
    [InlineData(Moving)]
    [InlineData(Return)]
    public void SwarmCancelsAnOpposingCardWithoutReceivingTheNoCardBonus(string card)
    {
        const string position = FreePeoplesStrategyCards01.TomBombadil;
        var state = Reveal(Before(card, fpCard: position), card, position);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
    }

    [Theory]
    [InlineData(Moving, true)]
    [InlineData(Moving, false)]
    [InlineData(Return, true)]
    [InlineData(Return, false)]
    public void SwarmAndDaringResolveTheDefendersInitiativeZeroFirst(string card, bool shadowAttacks)
    {
        var state = Reveal(Before(card, shadowAttacks, Daring, "base.character.boromir"), card, Daring);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(shadowAttacks, battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(!shadowAttacks, battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(shadowAttacks, battle.Force(Side.FreePeoples).Round.ForfeitedLeadership.Count > 0);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
    }

    [Fact]
    public void WhiteRiderMakesDaringIneffectiveButItStillBlocksTheSwarmRerollBonus()
    {
        foreach (var shadowAttacks in new[] { false, true })
        {
            var state = Before(Moving, shadowAttacks, Daring, CharacterRules.White, nazgul: true);
            Assert.Equal(DecisionKind.Reaction, state.Pending!.Kind);
            state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
            state = Reveal(state, Moving, Daring);
            var battle = BattleRuntime.Active(state)!;
            Assert.True(battle.WhiteRiderUsed);
            Assert.False(battle.Force(Side.Shadow).Round.CardCancelled);
            Assert.True(battle.Force(Side.FreePeoples).Round.CardCancelled);
            Assert.Empty(battle.Force(Side.FreePeoples).Round.ForfeitedLeadership);
            Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
            Assert.Equal(0, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
        }
    }

    [Fact]
    public void SwarmBonusExpiresBeforeTheNextRound()
    {
        var state = Reveal(Before(Return), Return);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.RerollModifier);
    }

    [Fact]
    public void CorsairsCombatUsesTheSharedDeadlyStrifeForBothArmies()
    {
        var state = Reveal(Before(Corsairs), Corsairs);
        var battle = BattleRuntime.Active(state)!;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            Assert.Equal(2, force.Round.CombatModifier);
            Assert.Equal(2, force.Round.RerollModifier);
            Assert.All(force.Round.Dice, die => Assert.False(die.Hit));
        }
    }

    [Fact]
    public void RageCombatPaysVoluntaryHitsBeforeRollingAndExcludesRearGuard()
    {
        var state = Before(Rage, nazgul: true);
        var rear = BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Origin);
        state = Reveal(state, Rage);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Empty(state.RandomSamples);
        Assert.Equal(2, state.Pending.RequiredCount);
        Reject(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(rear.Id)] });
        var elite = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Single(u => u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(elite.Id)] }, new SequenceRandom(new int[5]));
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(2, shadow.Round.CombatModifier);
        Assert.Equal(0, shadow.Round.RerollModifier);
        Assert.Equal(2, shadow.Round.Dice.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RageAcceptsEachPrintedVoluntaryCostIncludingZero(int hits)
    {
        var state = Reveal(Before(Rage), Rage);
        var losses = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.Shadow))
            .Where(u => u.Kind == UnitKind.Regular).Take(hits).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses }, new SequenceRandom(new int[6 - hits]));
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(hits, shadow.Round.CombatModifier);
        Assert.Equal(3 - hits, BattleRules.Strength(state, shadow));
        Assert.Equal(0, shadow.Round.RerollModifier);
    }

    [Fact]
    public void DaringCancelsMumakilDeadlyStrifeAndRelentlessIncludingItsCost()
    {
        foreach (var card in new[] { Gather, Lengthens, Corsairs, Rage })
        {
            var state = Reveal(Before(card, fpCard: Daring, companion: "base.character.boromir"), card, Daring);
            var battle = BattleRuntime.Active(state)!;
            Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
            Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
            Assert.Equal(0, battle.Force(Side.Shadow).Round.ExtraHits);
            Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
            Assert.Equal(3, BattleRules.Strength(state, battle.Force(Side.Shadow)));
            Assert.NotEqual(DecisionKind.Losses, state.Pending!.Kind);
        }
    }

    private static MatchState Before(string card, bool shadowAttacks = true, string fpCard = "", string companion = "", bool nazgul = false, int fpElites = 0)
    {
        var state = BattleFixture.State(shadowAttacks ? Side.Shadow : Side.FreePeoples);
        var shadowRegion = shadowAttacks ? BattleFixture.Origin : BattleFixture.Target;
        var freeRegion = shadowAttacks ? BattleFixture.Target : BattleFixture.Origin;
        for (var i = 0; i < 3; i++)
        {
            BattleFixture.Put(state, "southrons", i == 0 ? UnitKind.Elite : UnitKind.Regular, shadowRegion);
            BattleFixture.Put(state, "gondor", i < fpElites ? UnitKind.Elite : UnitKind.Regular, freeRegion);
        }
        if (nazgul)
            BattleFixture.Put(state, "sauron", UnitKind.Nazgul, shadowRegion);
        if (companion != "")
            Put(state, companion, freeRegion);
        EventTests.GiveCard(state, Side.Shadow, card);
        if (fpCard != "")
            EventTests.GiveCard(state, Side.FreePeoples, fpCard);
        var attackers = state.Units.Where(u => u.Location == BattleFixture.Origin).Select(u => u.Id)
            .Concat(state.Characters.Where(c => c.Location == BattleFixture.Origin).Select(c => c.Id));
        return BattleFixture.Begin(state, attackers);
    }

    private static MatchState Reveal(MatchState state, string card, string fpCard = "", params int[] values) =>
        Shadow01Fixture.Reveal(state, card, fpCard, values.Length == 0 ? new int[10] : values);
}
