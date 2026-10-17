using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.FateCardsFixture;

namespace Wotr.Tests;

public class FateCombatTests
{
    [Theory]
    [InlineData(6, 2)]
    [InlineData(1, 1)]
    public void BraveStandCountsOnlyParticipatingCompanionsAfterTheFiveDiceCapAndKeepsOneDie(int shadowUnits, int expected)
    {
        var state = Combat(FateCards.ThereAndBack, shUnits: shadowUnits);
        var fp = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        foreach (var id in new[] { "base.character.gimli", CharacterRules.Aragorn })
            fp.Participants.Add(CharacterFixture.Put(state, id, fp.Region).Id);
        CharacterFixture.Put(state, "base.character.boromir", fp.Region);
        state = Reveal(state, FateCards.ThereAndBack, samples: new int[10]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(expected, battle.Force(Side.Shadow).Round.Dice.Count);
        Assert.Equal(5, battle.Force(Side.FreePeoples).Round.Dice.Count);
    }

    [Theory]
    [InlineData(FateCards.ThereAndBack)]
    [InlineData(FateCards.Dwarves)]
    public void CompanionAndEliteRequirementsExcludeNonParticipants(string card)
    {
        var state = Combat(card);
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(Side.FreePeoples);
        if (card == FateCards.ThereAndBack)
            force.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id));
        else
            force.Participants.RemoveAll(id => state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Elite));
        Assert.False(new BattleCards(EffectCatalog.All).CanCommit(state, battle, Side.FreePeoples, card));
    }

    [Theory]
    [InlineData("iron-hills", true)]
    [InlineData("ered-luin", true)]
    [InlineData("dale", false)]
    [InlineData("erebor", false)]
    public void AdvantageousPositionReadsTheDefendingRegionFromTheFateMap(string region, bool legal)
    {
        var state = Combat(FateCards.Book);
        var battle = BattleRuntime.Active(state)!;
        battle.Region = region;
        Assert.Equal(legal, new BattleCards(EffectCatalog.All).CanCommit(state, battle, Side.FreePeoples, FateCards.Book));
    }

    [Fact]
    public void AdvantageousPositionPenalizesOnlyShadowCombatAndNaturalSixStillHits()
    {
        var state = Combat(FateCards.Book);
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        shadow.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, shadow.Region).Id);
        state = Reveal(state, FateCards.Book, samples: [0, 0, 0, 4, 5, 0]);
        var battle = BattleRuntime.Active(state)!;
        shadow = battle.Force(Side.Shadow);
        Assert.Equal(-1, shadow.Round.CombatModifier);
        Assert.Equal(0, shadow.Round.RerollModifier);
        Assert.False(shadow.Round.Dice[0].Hit);
        Assert.True(shadow.Round.Dice[1].Hit);
        state = KingsFixture.Do(state, CommandKind.RerollBattle);
        shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        state = KingsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = [shadow.Round.Dice[0].Id] }, 4);
        var rerolled = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice[0];
        Assert.Equal(0, rerolled.RerollModifier);
        Assert.True(rerolled.RerollHit);
    }

    [Fact]
    public void ValourAddsOnlyToCombatAndPreservesNaturalOneAndSix()
    {
        var state = Reveal(Combat(FateCards.Dwarves), FateCards.Dwarves, samples: [0, 3, 5, 0, 0, 0]);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(0, force.Round.RerollModifier);
        Assert.False(force.Round.Dice[0].Hit);
        Assert.True(force.Round.Dice[1].Hit);
        Assert.True(force.Round.Dice[2].Hit);
        state = KingsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[0].Id] }, 3);
        var rerolled = BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.Dice[0];
        Assert.Equal(0, rerolled.RerollModifier);
        Assert.False(rerolled.RerollHit);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void ShieldWallCancelsOneOnlyWhenCombinedOrdinaryHitsReachTwo(int hits, int losses)
    {
        var samples = new[] { 0, 0, 0 }.Concat(Enumerable.Range(0, 3).Select(i => i < hits ? 5 : 0)).ToArray();
        var state = NoRerolls(Reveal(Combat(FateCards.Bardings), FateCards.Bardings, samples: samples));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(losses, BattleLosses.Current(state)!.Hits);
        Assert.Equal(hits >= 2 ? 1 : 0, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelHits);
    }

    [Fact]
    public void ShieldWallIncludesHitsFromTheOpponentsLeaderReroll()
    {
        var state = Combat(FateCards.Bardings);
        var sh = BattleRuntime.Active(state)!.Force(Side.Shadow);
        sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, sh.Region).Id);
        state = Reveal(state, FateCards.Bardings, samples: [0, 0, 0, 5, 0, 0]);
        state = KingsFixture.Do(state, CommandKind.RerollBattle);
        sh = BattleRuntime.Active(state)!.Force(Side.Shadow);
        state = KingsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = [sh.Round.Dice[1].Id] }, 5);
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelHits);
    }

    [Fact]
    public void DesperateBattleStacksWithPositionAndExpiresBeforeTheNextRound()
    {
        var state = Combat(FateCards.Monsters);
        state = Reveal(state, FateCards.Monsters, FateCards.Book, new int[6]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.RerollModifier);
        Assert.All(battle.Attacker.Round.Dice.Concat(battle.Defender.Round.Dice), d => Assert.False(d.Hit));
        state = NoRerolls(state);
        state = KingsFixture.Do(state, CommandKind.ContinueBattle);
        state = KingsFixture.Do(state, CommandKind.HoldBattle);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Round);
        Assert.Equal(0, battle.Attacker.Round.CombatModifier);
        Assert.Equal(0, battle.Defender.Round.RerollModifier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void OnslaughtTakesOptionalZeroThroughFourHitsAndMakesAnIndependentAttack(int hits)
    {
        var state = Combat(FateCards.Orcs, shUnits: 5);
        state = NoRerolls(Reveal(state, FateCards.Orcs, samples: new int[8]));
        Assert.True(BattleLosses.Current(state)!.Voluntary);
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var regulars = BattleRules.Units(state, shadow).Where(BoardRules.IsArmyUnit).ToArray();
        LordsFixture.Reject(state, CommandKind.TakeBattleLosses, new()
        {
            Losses = regulars.Select(u => BattleFixture.Loss(u.Id)).ToList()
        });
        state = KingsFixture.Do(state, CommandKind.TakeBattleLosses, new()
        {
            Losses = regulars.Take(hits).Select(u => BattleFixture.Loss(u.Id)).ToList()
        }, Enumerable.Repeat(3, hits).ToArray());
        var battle = BattleRuntime.Active(state)!;
        if (hits == 0)
        {
            Assert.Empty(battle.SpecialAttacks);
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            return;
        }
        var attack = Assert.Single(battle.SpecialAttacks);
        Assert.Equal(hits, attack.Dice.Count);
        Assert.All(attack.Dice, d =>
        {
            Assert.Equal(4, d.HitTarget);
            Assert.Equal(0, d.Modifier);
            Assert.True(d.Hit);
            Assert.Null(d.Reroll);
        });
        Assert.Equal(hits, BattleLosses.Current(state)!.Hits);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    [Theory]
    [InlineData(FateCards.Orcs)]
    [InlineData(FateCards.Monsters)]
    public void DaringDefianceCancelsFateShadowEffectsBeforeTheyCreateChoicesOrBonuses(string card)
    {
        var state = Reveal(Combat(card), card, FreePeoplesCharacterCards02.Alone, new int[6]);
        state = NoRerolls(state);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Empty(battle.SpecialAttacks);
        Assert.Null(BattleLosses.Current(state));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
    }

    [Fact]
    public void OnslaughtStillResolvesItsAttackAfterTheLastShadowUnitIsSacrificed()
    {
        var state = NoRerolls(Reveal(Combat(FateCards.Orcs), FateCards.Orcs, samples: new int[6]));
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var losses = BattleRules.Units(state, shadow).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = KingsFixture.Do(state, CommandKind.TakeBattleLosses, new() { Losses = losses }, 3, 0, 5);
        var battle = BattleRuntime.Active(state)!;
        Assert.False(BattleRules.Alive(state, battle.Force(Side.Shadow)));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        Assert.Equal(3, Assert.Single(battle.SpecialAttacks).Dice.Count);
    }
}
