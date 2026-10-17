using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;
using static Wotr.Tests.CardPackBattleFixture;
using static Wotr.Domain.FreePeoplesCharacterCards02;

namespace Wotr.Tests;

public class LeadershipCombatCardTests
{
    [Fact]
    public void HornUsesTheSharedMightyAttackProgramForItsActualCombatCard()
    {
        var state = Reveal(BeforeCards(Horn), Horn);
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice[0].Id;
        state = Execute(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider, die] });
        Assert.Equal(2, BattleRuntime.Active(state)!.Attacker.Round.ForfeitedLeadership[CharacterRules.Strider]);
        Assert.Equal(1, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
    }

    [Theory]
    [InlineData(Horn, CharacterRules.Strider)]
    [InlineData(Staff, CharacterRules.Grey)]
    [InlineData(Athelas, CharacterRules.Strider)]
    [InlineData(AnotherWay, CharacterRules.Strider)]
    [InlineData(Alone, CharacterRules.Strider)]
    [InlineData(Bilbo, CharacterRules.Strider)]
    public void EveryCombatRequiresThePrintedParticipantAndOwnedCard(string card, string character)
    {
        var state = BeforeCards(card, character);
        var battle = BattleRuntime.Active(state)!;
        var cards = new BattleCards(EffectCatalog.Base);
        Assert.True(cards.CanCommit(state, battle, Side.FreePeoples, card));
        Assert.False(cards.CanCommit(state, battle, Side.Shadow, card));
        battle.Attacker.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id) || state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Leader));
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, card));
    }

    [Theory]
    [InlineData(CharacterRules.Grey)]
    [InlineData(CharacterRules.White)]
    public void ServantAddsOnlyToCombatDiceEvenWithCancelledGandalfLeadership(string character)
    {
        var state = BeforeCards(Staff, character);
        BattleRuntime.Active(state)!.WhiteRiderUsed = character == CharacterRules.White;
        state = Reveal(state, Staff, dice: [0, 3, 3, 0, 0, 0, 0, 0]);
        var force = BattleRuntime.Active(state)!.Attacker;
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(0, force.Round.RerollModifier);
        Assert.False(force.Round.Dice[0].Hit);
        Assert.True(force.Round.Dice[1].Hit);
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[0].Id] });
        state = Execute(state, CommandKind.RerollBattle, random: new SequenceRandom(3));
        Assert.False(BattleRuntime.Active(state)!.Attacker.Round.Dice[0].RerollHit);
    }

    internal static MatchState AndurilChoice(bool aragorn = true) =>
        Reveal(BeforeCards(Athelas, aragorn ? CharacterRules.Aragorn : CharacterRules.Strider), Athelas);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AndurilLetsAragornChooseUpToTwoSpecificMissesForHisWholeLeadership(int count)
    {
        var state = AndurilChoice();
        var round = BattleRuntime.Active(state)!.Attacker.Round;
        round.Dice[0].CannotReroll = true;
        var ids = count == 0 ? new List<string> { AndurilStep.KeepMisses } : round.Dice.Take(count).Select(d => d.Id).ToList();
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = round.Dice.Take(3).Select(d => d.Id).ToList() }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [AndurilStep.KeepMisses, round.Dice[0].Id] }));
        state = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ids });
        round = BattleRuntime.Active(state)!.Attacker.Round;
        Assert.Equal(2, round.ForfeitedLeadership[CharacterRules.Strider]);
        Assert.Equal(count, round.Dice.Count(d => d.ConvertedHit));
        Assert.All(round.Dice, d => Assert.False(d.Hit));
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
    }

    [Fact]
    public void StriderConvertsExactlyOneAndCancelledRerollOrLeadershipCannotPayAnduril()
    {
        var state = AndurilChoice(false);
        var round = BattleRuntime.Active(state)!.Attacker.Round;
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = round.Dice.Take(2).Select(d => d.Id).ToList() }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [AndurilStep.KeepMisses] }));
        var step = new AndurilStep();
        var execution = EffectRuntime.Current(state)!.Effect!;
        round.RerollLimit = 0;
        Assert.Empty(step.Candidates(state, execution));
        round.RerollLimit = 5;
        round.CancelledLeadership.Add(CharacterRules.Strider);
        Assert.Empty(step.Candidates(state, execution));
        round.CancelledLeadership.Clear();
        round.Dice.ForEach(d => d.Hit = true);
        Assert.Empty(step.Candidates(state, execution));
        round.Dice.ForEach(d => d.Hit = false);
        state = Execute(state, CommandKind.ChooseEffect, new() { Ids = [round.Dice[0].Id] });
        Assert.Equal(1, BattleRuntime.Active(state)!.Attacker.Round.ForfeitedLeadership[CharacterRules.Strider]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DaringAndOpposingCancellationResolveDefenderFirst(bool fpAttacks)
    {
        var catalog = Opponent(new CancelOpposingCardStep(), 0);
        var state = NoRerolls(Reveal(BeforeCards(Alone, fpAttacks: fpAttacks, catalog: catalog), Alone, catalog), catalog);
        var battle = BattleRuntime.Active(state)!;
        var fp = battle.Force(Side.FreePeoples);
        Assert.Equal(fpAttacks, fp.Round.CardCancelled);
        Assert.Equal(!fpAttacks, battle.Opponent(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(fpAttacks ? 0 : 3, fp.Round.ForfeitedLeadership.Values.Sum());
        Assert.Equal(fpAttacks ? 4 : 1, BattleLeadership.Total(state, fp));
    }

    [Fact]
    public void DaringDoesNotForfeitWithoutEnemyCardAndWhiteRiderCannotPayEvenWithGimli()
    {
        var state = Reveal(BeforeCards(Alone), Alone);
        Assert.Empty(BattleRuntime.Active(state)!.Attacker.Round.ForfeitedLeadership);
        var catalog = Opponent(new CombatModifierStep(1, 0), 3);
        state = BeforeCards(Alone, CharacterRules.White, catalog: catalog);
        BattleRuntime.Active(state)!.WhiteRiderUsed = true;
        state = Reveal(state, Alone, catalog);
        var battle = BattleRuntime.Active(state)!;
        Assert.False(battle.Defender.Round.CardCancelled);
        Assert.Empty(battle.Attacker.Round.ForfeitedLeadership);
        Assert.Equal(1, battle.Defender.Round.CombatModifier);
        Assert.Equal(2, BattleLeadership.Total(state, battle.Attacker));
    }

    private sealed record CancelOpposingCardStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round.CardCancelled = true;
    }
}
