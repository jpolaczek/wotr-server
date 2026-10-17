using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.ShadowStrategy01Fixture;

namespace Wotr.Tests;

public class ShadowStrategy01GreatHostTests
{
    [Theory]
    [InlineData(false, 4, 2, false, true)]
    [InlineData(true, 4, 2, false, true)]
    [InlineData(false, 3, 2, false, false)]
    [InlineData(true, 3, 2, false, false)]
    [InlineData(false, 2, 1, true, true)]
    [InlineData(true, 2, 1, true, true)]
    [InlineData(false, 1, 1, true, false)]
    [InlineData(true, 1, 1, true, false)]
    public void GreatHostUsesDoubleFigureCountAndEliteCountsOnce(bool shadowAttacks, int own, int enemy, bool elite, bool hits)
    {
        var state = Shadow01Fixture.Reveal(Combat(Stormcrow, shadowAttacks, own, enemy, elite), Stormcrow);
        Assert.Equal(own + enemy, state.RandomSamples.Count);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.Equal(hits ? DecisionKind.Losses : DecisionKind.BattleContinue, state.Pending!.Kind);
        if (hits)
        {
            Assert.Equal(Side.FreePeoples, state.Pending.Owner);
            Assert.Equal(1, state.Pending.RequiredCount);
            Assert.Equal(Stormcrow, BattleLosses.Current(state)!.SourceId);
            Assert.False(BattleLosses.Current(state)!.Voluntary);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void GreatHostRecountsSurvivorsAfterOrdinaryCasualties(bool shadowAttacks, bool shadowLoses)
    {
        var dice = new int[6];
        var scoringSide = shadowLoses ? Side.FreePeoples : Side.Shadow;
        var firstSide = shadowAttacks ? Side.Shadow : Side.FreePeoples;
        dice[scoringSide == firstSide ? 0 : shadowAttacks ? 4 : 2] = 5;
        var state = Shadow01Fixture.Reveal(Combat(Stormcrow, shadowAttacks, own: 4, enemy: 2), Stormcrow, dice: dice);
        Assert.Equal("combat", BattleLosses.Current(state)!.SourceId);
        Assert.Equal(shadowLoses ? Side.Shadow : Side.FreePeoples, state.Pending!.Owner);
        var victim = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(state.Pending.Owner)).First();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(victim.Id)] });
        if (shadowLoses)
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        else
        {
            Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
            Assert.Equal(Stormcrow, BattleLosses.Current(state)!.SourceId);
            Assert.Equal(1, state.Pending.RequiredCount);
        }
    }

    [Fact]
    public void GreatHostDoesNotCountLeadersNazgulOrCharactersAsArmyUnits()
    {
        var state = Combat(Stormcrow, own: 1, enemy: 1, elite: true);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var i = 0; i < 3; i++)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region).Id);
        force.Participants.Add(Put(state, CharacterRules.Mouth, force.Region).Id);
        var opponent = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        opponent.Participants.Add(BattleFixture.Put(state, "gondor", UnitKind.Leader, opponent.Region).Id);
        state = Shadow01Fixture.Reveal(state, Stormcrow);
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "BattleLosses");
    }

    [Fact]
    public void GreatHostDoesNotResolveAgainstArmyAlreadyEliminatedByNormalHits()
    {
        var state = Shadow01Fixture.Reveal(Combat(Stormcrow, own: 3, enemy: 1), Stormcrow, dice: [5, 0, 0, 0]);
        Assert.Equal("combat", BattleLosses.Current(state)!.SourceId);
        var victim = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Single();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(victim.Id)] });
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "BattleLosses" && e.Text.Contains(Stormcrow));
        Assert.Equal(4, state.RandomSamples.Count);
    }
}
