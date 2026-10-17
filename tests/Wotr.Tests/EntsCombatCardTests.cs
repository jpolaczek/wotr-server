using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards04;
using static Wotr.Tests.CardPackBattleFixture;

namespace Wotr.Tests;

public class EntsCombatCardTests
{
    [Theory]
    [InlineData(Treebeard)]
    [InlineData(Huorns)]
    [InlineData(Entmoot)]
    public void DefendingRegionConditionIsIndependentOfWhichSideAttacks(string card)
    {
        foreach (var fpAttacks in new[] { false, true })
        {
            var state = BeforeCards(card, fpAttacks: fpAttacks);
            var battle = BattleRuntime.Active(state)!;
            var condition = EffectCatalog.Base.Combats[card].Condition;
            foreach (var region in new[] { "edoras", "helms-deep", "fangorn", "orthanc" })
            {
                battle.Region = region;
                Assert.True(condition.CanPlay(state, battle, Side.FreePeoples));
            }
            battle.Region = "minas-tirith";
            Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        }
    }

    internal static MatchState InRohan(string card, bool fpAttacks = true)
    {
        var state = BeforeCards(card, fpAttacks: fpAttacks);
        BattleRuntime.Active(state)!.Region = "edoras";
        return state;
    }

    [Fact]
    public void EntsRageModifiesOnlyCombatRollWithNaturalOneAlwaysMissing()
    {
        var state = Reveal(InRohan(Treebeard), Treebeard, dice: [0, 1, 2, 3, 5, 0, 0, 0]);
        var fp = BattleRuntime.Active(state)!.Attacker;
        Assert.Equal(2, fp.Round.CombatModifier);
        Assert.Equal(0, fp.Round.RerollModifier);
        Assert.Equal(new[] { false, false, false, true, true }, fp.Round.Dice.Select(d => d.Hit));
        state = BattleFixture.Execute(state, CommandKind.RerollBattle, new() { Ids = [fp.Round.Dice[0].Id, fp.Round.Dice[1].Id] });
        state = BattleFixture.Execute(state, CommandKind.RerollBattle, random: new SequenceRandom(2, 5));
        Assert.Equal(new[] { false, true }, BattleRuntime.Active(state)!.Attacker.Round.Dice.Take(2).Select(d => d.RerollHit));
    }

    [Fact]
    public void HuornDarkCapsShadowRollAtTwoAndRerollsUseOnlyItsFailures()
    {
        var state = Reveal(InRohan(Huorns), Huorns, dice: new int[7]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Defender.Round.Dice.Count);
        Assert.Equal(5, battle.Attacker.Round.Dice.Count);
        Assert.Equal(2, BattleRoll.RerollMaximum(state, battle.Defender));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 7)]
    public void NamelessWoodAddsTwoOnlyAfterACombatOrRerollHitAndHasNoFiveHitCap(bool hit, int expected)
    {
        var state = Reveal(InRohan(Entmoot), Entmoot, dice: (hit ? Enumerable.Repeat(5, 5).Concat(new int[3]) : new int[8]).ToArray());
        state = NoRerolls(state);
        Assert.Equal(expected, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
    }

    [Fact]
    public void EarlierSpecialAttackHitsDoNotEnableNamelessWood()
    {
        var state = InRohan(Entmoot);
        BattleRuntime.Active(state)!.Attacker.Round.SpecialHits = 3;
        state = NoRerolls(Reveal(state, Entmoot, dice: new int[8]));
        Assert.Equal(0, BattleRuntime.Active(state)!.Attacker.Round.ExtraHits);
    }

    [Theory]
    [InlineData(DeadMen, 2)]
    [InlineData(Stewards, 3)]
    [InlineData(GreyCompany, 3)]
    public void ReusedCombatProgramsExecuteForEveryNewCard(string card, int initiative)
    {
        var state = Reveal(BeforeCards(card), card, dice: new int[12]);
        Assert.Equal(initiative, EffectCatalog.Base.Combats[card].Effects.Single().Initiative);
        if (card == DeadMen)
            Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
        else
            Assert.Single(BattleRuntime.Active(state)!.Defender.Round.Dice);
    }
}
