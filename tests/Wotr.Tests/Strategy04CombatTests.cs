using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards04;
using static Wotr.Tests.CardPackBattleFixture;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class Strategy04CombatTests
{
    [Theory]
    [InlineData(DainIronfoot)]
    [InlineData(Eomer)]
    [InlineData(ThranduilsArchers)]
    public void ValourRequiresEliteAddsToCombatButNotRerollAndNaturalOneStillMisses(string card)
    {
        var state = BeforeCards(card);
        Assert.False(EffectCatalog.Base.Combats[card].Condition.CanPlay(state, BattleRuntime.Active(state)!, Side.FreePeoples));
        state = Reveal(Strategy03Fixture.Charge(card), card, dice: [0, 3, 4, 0, 0, 0, 0, 0]);
        var force = BattleRuntime.Active(state)!.Attacker;
        Assert.False(force.Round.Dice[0].Hit);
        Assert.True(force.Round.Dice[1].Hit);
        Assert.All(force.Round.Dice, d => Assert.Equal(1, d.Modifier));
        var reroll = force.Round.Dice[0].Id;
        state = Do(state, CommandKind.RerollBattle, new() { Ids = [reroll] });
        state = Do(state, CommandKind.RerollBattle, random: new SequenceRandom(3));
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice.Single(d => d.Id == reroll);
        Assert.Equal(0, die.RerollModifier);
        Assert.False(die.RerollHit);
    }

    [Fact]
    public void ValourStillAppliesAfterAllElitesAreEliminatedByEarlierSpecialAttack()
    {
        var catalog = Opponent(new SpecialStep(), 2);
        var state = Reveal(Strategy03Fixture.Charge(DainIronfoot, elites: 1, catalog: catalog), DainIronfoot, catalog, 5, 5);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        var elite = BattleRules.Units(state, BattleRuntime.Active(state)!.Attacker).Single(u => u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(elite.Id)] }, new SequenceRandom(new int[6]), catalog);
        var force = BattleRuntime.Active(state)!.Attacker;
        Assert.DoesNotContain(BattleRules.Units(state, force), u => u.Kind == UnitKind.Elite);
        Assert.All(force.Round.Dice, d => Assert.Equal(1, d.Modifier));
    }

    [Fact]
    public void CancelledValourDoesNotModifyCombat()
    {
        var catalog = Opponent(new CancelStep(), 0);
        var state = Reveal(Strategy03Fixture.Charge(Eomer, catalog: catalog), Eomer, catalog);
        Assert.All(BattleRuntime.Active(state)!.Attacker.Round.Dice, d => Assert.Equal(0, d.Modifier));
    }

    [Theory]
    [InlineData(SwordsInEriador)]
    [InlineData(KindredOfGlorfindel)]
    public void AdvantageousPositionUsesOriginalNationAndOnlyModifiesFirstShadowRoll(string card)
    {
        var state = BeforeCards(card);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[card].Condition;
        state.Control[battle.Region] = Side.Shadow;
        Assert.True(condition.CanPlay(state, battle, Side.FreePeoples));
        var original = battle.Region;
        battle.Region = "osgiliath";
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Region = "minas-morgul";
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Region = original;
        state = Reveal(state, card, dice: [0, 0, 0, 0, 0, 4, 5, 0]);
        var dice = BattleRuntime.Active(state)!.Defender.Round.Dice;
        Assert.False(dice[0].Hit);
        Assert.True(dice[1].Hit);
        Assert.Equal(-1, dice[0].Modifier);
        var id = dice[0].Id;
        state = Do(state, CommandKind.RerollBattle);
        state = Do(state, CommandKind.RerollBattle, new() { Ids = [id] }, new SequenceRandom(4));
        var reroll = BattleRuntime.Active(state)!.Defender.Round.Dice.Single(d => d.Id == id);
        Assert.True(reroll.RerollHit);
        Assert.Equal(0, reroll.RerollModifier);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void BrandsShieldWallCancelsOneOnlyWithTwoHits(int hits, int cancelled)
    {
        var dice = new int[8];
        for (var i = 0; i < hits; i++)
            dice[5 + i] = 5;
        var state = NoRerolls(Reveal(BeforeCards(KingBrandsMen), KingBrandsMen, dice: dice));
        Assert.Equal(cancelled, BattleRuntime.Active(state)!.Attacker.Round.CancelHits);
    }

    private sealed record SpecialStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRoll.Special(context.State,
            BattleRuntime.Active(context.State)!, context.Owner, context.Execution.SourceId, 2, context.Random);
    }

    private sealed record CancelStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round.CardCancelled = true;
    }
}
