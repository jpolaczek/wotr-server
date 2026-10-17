using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.LordsFreePeoplesCards;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsCards01CombatTests
{
    [Theory]
    [MemberData(nameof(LordsCards01EventTests.Cards), MemberType = typeof(LordsCards01EventTests))]
    public void AllCombatHalvesRequireOwnershipCorrectWindowAndLords(string card)
    {
        var state = Battle(card, companion: "base.character.merry");
        state.Fellowship.RegionId = BattleFixture.Target;
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, new() { CardId = card });
        Assert.Null(LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        Assert.NotNull(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        state.Configuration.Lords = false;
        LordsFixture.Reject(state, command.Kind, command.Payload);
        state.Configuration.Lords = true;
        state.Player(Side.FreePeoples).Hand.Remove(card);
        LordsFixture.Reject(state, command.Kind, command.Payload);
        state.Player(Side.FreePeoples).Hand.Add(card);
        state.Pending!.Kind = DecisionKind.Action;
        LordsFixture.Reject(state, command.Kind, command.Payload);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GiftRequiresTheFigureRegionAndModifiesBothRolls(bool fpAttacks)
    {
        var state = Battle(SafePaths, fpAttacks);
        var battle = BattleRuntime.Active(state)!;
        var cards = new BattleCards(EffectCatalog.All);
        state.Fellowship.RegionId = BattleFixture.Origin;
        state.Fellowship.Progress = 5;
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, SafePaths));
        state.Fellowship.RegionId = battle.Region;
        Assert.True(cards.CanCommit(state, battle, Side.FreePeoples, SafePaths));
        state.Fellowship.MordorStep = 0;
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, SafePaths));
        state.Fellowship.MordorStep = null;
        state = CardPackBattleFixture.Reveal(state, SafePaths, dice: new int[8]);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(1, force.Round.RerollModifier);
        Assert.All(force.Round.Dice, d => Assert.False(d.Hit));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
        {
            var own = state.Pending.Owner == Side.FreePeoples;
            var ids = own ? new List<string> { force.Round.Dice[0].Id } : [];
            state = LordsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = ids }, new SequenceRandom(3));
        }
        Assert.True(BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.Dice[0].RerollHit);
    }

    [Fact]
    public void BladeAcceptsCouncilHobbitAndSpendsOneRealRerollHitOnAParticipatingMinion()
    {
        var state = Battle(YouKnowTheWay, companion: "base.character.merry");
        state.Configuration.Council = true;
        var merry = state.Characters.Single(c => c.Id == "base.character.merry");
        CharacterLifecycle.ApplyVersion(merry, CharacterCatalog.Lords["lords.character.merry"]);
        state = CardPackBattleFixture.Reveal(state, YouKnowTheWay, dice: new int[7]);
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice[0].Id;
        state = LordsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = [die] });
        state = LordsFixture.Do(state, CommandKind.RerollBattle, new(), new SequenceRandom(5));
        Assert.Equal(new[] { CharacterRules.Mouth, CharacterRules.WitchKing }.Order(), state.Pending!.PrivateChoices.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Saruman] });
        state = Choose(state, CharacterRules.Mouth);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Mouth).Location);
        Assert.Equal(1, BattleRuntime.Active(state)!.Attacker.Round.SpentHits);
        Assert.Equal(0, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void ShieldWallCancelsOnlyWhenAtLeastTwoNormalHitsAreScored(int hits, int cancelled)
    {
        var state = LordsWithdrawalTests.RevealHits(Battle(EveryLittle), EveryLittle, 0, hits);
        Assert.Equal(cancelled, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelHits);
        Assert.Equal(hits - cancelled, state.Pending!.RequiredCount);
    }

    [Fact]
    public void BladeCannotUseCombatRollHitsAsItsRerollCost()
    {
        var state = LordsWithdrawalTests.RevealHits(Battle(YouKnowTheWay, companion: "base.character.merry"), YouKnowTheWay, 1, 0);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(0, BattleRuntime.Active(state)!.Attacker.Round.SpentHits);
        Assert.DoesNotContain(state.Characters, c => c.Side == Side.Shadow && c.Location == "casualties");
    }

    [Fact]
    public void AdvantageousPositionUsesNationBordersAndOnlyModifiesCombatRoll()
    {
        var state = Battle(Rangers);
        var battle = BattleRuntime.Active(state)!;
        var cards = new BattleCards(EffectCatalog.All);
        battle.Region = "morannon";
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, Rangers));
        battle.Region = BattleFixture.Target;
        state = CardPackBattleFixture.Reveal(state, Rangers, dice: [0, 0, 0, 0, 0, 4, 5, 0]);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.False(force.Round.Dice[0].Hit);
        Assert.True(force.Round.Dice[1].Hit);
        Assert.Equal(-1, force.Round.CombatModifier);
        Assert.Equal(0, force.Round.RerollModifier);
        state = LordsFixture.Do(state, CommandKind.RerollBattle);
        state = LordsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[0].Id] }, new SequenceRandom(4));
        Assert.True(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice[0].RerollHit);
    }
}
