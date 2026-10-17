using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleRollTests
{
    [Theory]
    [InlineData(1, 20, 5, false)]
    [InlineData(6, -20, 6, true)]
    [InlineData(4, 1, 5, true)]
    [InlineData(5, 0, 6, false)]
    [InlineData(2, 3, 5, true)]
    [InlineData(5, -1, 5, false)]
    public void NaturalResultsOverrideModifiers(int raw, int modifier, int target, bool hit) =>
        Assert.Equal(hit, BattleRoll.Hits(raw, modifier, target));

    [Fact]
    public void CombatAndRerollsAreCappedAtFiveAndSelectionsStayPrivateUntilBothCommit()
    {
        var state = State();
        var attackers = Enumerable.Range(0, 7).Select(_ => Put(state, "gondor", UnitKind.Regular, Origin).Id).ToList();
        attackers.Add(Character(state, "test.captain", Side.FreePeoples, Origin, 9).Id);
        for (var i = 0; i < 6; i++)
            Put(state, "sauron", UnitKind.Regular, Target);
        Character(state, "test.commander", Side.Shadow, Target, 8);
        state = NoCards(Begin(state, attackers), 1, 2, 3, 4, 1, 1, 2, 3, 4, 1);
        var battle = BattleRuntime.Current(state)!;
        Assert.Equal(5, battle.Attacker.Round.Dice.Count);
        Assert.Equal(5, battle.Defender.Round.Dice.Count);
        var option = Assert.Single(new BattleOptions().GetOptions(state, Side.FreePeoples));
        Assert.Equal(5, option.Maximum);
        Rejected(state, Command(state, CommandKind.RerollBattle, new() { Ids = [option.EligibleIds[0], option.EligibleIds[0]] }));
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = option.EligibleIds });
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(10, state.RandomSamples.Count);
        Assert.Empty(new Projection().For(state, Side.Shadow).Battle!.Attacker.OwnRerollSelection);
        Assert.Equal(5, new Projection().For(state, Side.FreePeoples).Battle!.Attacker.OwnRerollSelection.Count);
        Assert.All(BattleRuntime.Current(state)!.Attacker.Round.Dice, die => Assert.Null(die.Reroll));
        var restored = StateCodec.Copy(state);
        var defender = Assert.Single(new BattleOptions().GetOptions(restored, Side.Shadow));
        state = Execute(restored, CommandKind.RerollBattle, new() { Ids = defender.EligibleIds }, new SequenceRandom(5, 5, 0, 0, 0, 5, 0, 0, 0, 0));
        Assert.Equal(20, state.RandomSamples.Count);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(1, state.Pending.RequiredCount);
        Assert.Equal(2, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
        Assert.Equal(1, BattleRoll.HitCount(BattleRuntime.Active(state)!.Defender));
    }

    [Fact]
    public void LeadershipCancellationAndForfeitCannotBeSpentTwice()
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        var character = Character(state, "test.captain", Side.FreePeoples, Origin, 2);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = Begin(state, [unit.Id, character.Id]);
        var force = BattleRuntime.Current(state)!.Attacker;
        BattleLeadership.Forfeit(state, force, new Dictionary<string, int> { [character.Id] = 1 });
        Assert.Equal(1, BattleLeadership.Total(state, force));
        Assert.False(BattleLeadership.CanForfeit(state, force, new Dictionary<string, int> { [character.Id] = 2 }));
        force.Round.CancelledLeadership.Add(character.Id);
        Assert.Equal(0, BattleLeadership.Total(state, force));
        Assert.False(BattleLeadership.CanForfeit(state, force, new Dictionary<string, int> { [character.Id] = 1 }));
        Assert.Throws<InvalidOperationException>(() => BattleLeadership.Forfeit(state, force, new Dictionary<string, int> { [character.Id] = 1 }));
        BattleRuntime.NextRound(BattleRuntime.Current(state)!);
        Assert.Equal(2, BattleLeadership.Total(state, force));
    }

    [Fact]
    public void CityAndFortificationOnlyProtectTheFirstRound()
    {
        foreach (var (from, to) in new[] { ("minas-tirith", "osgiliath"), ("osgiliath", "pelargir") })
        {
            var state = State();
            var unit = Put(state, "gondor", UnitKind.Regular, from);
            Put(state, "sauron", UnitKind.Regular, to);
            state = NoCards(Begin(state, [unit.Id], from: from, to: to), 5, 1);
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            Assert.Equal(6, BattleRuntime.Current(state)!.Attacker.Round.Dice[0].HitTarget);
            state = Execute(state, CommandKind.ContinueBattle);
            state = Execute(state, CommandKind.HoldBattle);
            state = NoCards(state, 5, 1);
            Assert.Equal(Side.Shadow, state.Pending!.Owner);
            Assert.Equal(1, state.Pending.RequiredCount);
            Assert.Equal(5, BattleRuntime.Active(state)!.Attacker.Round.Dice[0].HitTarget);
        }
    }
}
