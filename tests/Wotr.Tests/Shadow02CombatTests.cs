using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow02CombatTests
{
    [Theory]
    [InlineData(FoulThing)]
    [InlineData(Candles)]
    [InlineData(Search)]
    [InlineData(Weather)]
    [InlineData(Strike)]
    [InlineData(Wound)]
    public void EveryCombatRequiresOwnershipAndItsWindow(string card)
    {
        var state = Do(BeforeCombat(card), CommandKind.CommitCombatCard);
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
    [InlineData(FoulThing)]
    [InlineData(Candles)]
    [InlineData(Search)]
    [InlineData(Weather)]
    public void LeadershipCardsExcludeOtherMinionsAndAreBlockedByWhiteRider(string card)
    {
        var state = BeforeCombat(card);
        var battle = BattleRuntime.Active(state)!;
        var sh = battle.Force(Side.Shadow);
        sh.Participants.RemoveAll(id => state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Nazgul) || id == CharacterRules.WitchKing);
        Assert.True(BattleLeadership.Total(state, sh) > 0);
        Assert.False(EffectCatalog.Base.Combats[card].Condition.CanPlay(state, battle, Side.Shadow));
        state = BeforeCombat(card, companion: CharacterRules.White);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        state = Do(state, CommandKind.CommitCombatCard);
        Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
    }

    [Fact]
    public void TerriblePaysOneSelectedPointAfterCombatAndBonusesOnlyTheLeaderReroll()
    {
        var state = Reveal(BeforeCombat(FoulThing), FoulThing);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(3, battle.Force(Side.Shadow).Round.Dice.Count);
        Assert.All(battle.Force(Side.Shadow).Round.Dice, die => Assert.Equal(0, die.Modifier));
        var choice = Choices(state).EffectChoices.First(c => c.SourceId == CharacterRules.WitchKing).Id;
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = Choices(state).EligibleIds.Take(2).ToList() });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [choice] });
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(1, force.Round.ForfeitedLeadership[CharacterRules.WitchKing]);
        Assert.Equal(1, force.Round.RerollModifier);
        state = RerollShadow(state, 1, 3);
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.True(force.Round.Dice[0].RerollHit);
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 3)]
    [InlineData(4, 1)]
    [InlineData(10, 1)]
    public void DreadSubtractsChosenPointsAfterFiveDiceCapAndCanForfeitMoreThanNeeded(int cost, int expected)
    {
        var state = BeforeCombat(Candles);
        AddArmy(state, Side.FreePeoples, 5);
        var sh = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var i = 0; i < 7; i++)
            sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, sh.Region).Id);
        state = Reveal(state, Candles);
        var options = Choices(state);
        Assert.Equal(1, options.Minimum);
        Assert.Equal(10, options.Maximum);
        Assert.All(options.EffectChoices, c => { Assert.Equal(1, c.GroupMinimum); Assert.Equal(10, c.GroupMaximum); });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [options.EligibleIds[0], options.EligibleIds[0]] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = options.EligibleIds.Take(cost).ToList() }, new SequenceRandom(new int[8]));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(8, BattleRules.Strength(state, battle.Force(Side.FreePeoples)));
        Assert.True(BattleRules.CaptainBonus(state, battle.Force(Side.FreePeoples)) > 0);
        Assert.Equal(expected, battle.Force(Side.FreePeoples).Round.Dice.Count);
        Assert.Equal(cost, battle.Force(Side.Shadow).Round.ForfeitedLeadership.Values.Sum());
        Assert.Equal(12 - cost, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
        Assert.Equal(3, battle.Force(Side.Shadow).Round.Dice.Count);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(5, 1)]
    public void BraveStandAlsoReducesAnArmyLargerThanFiveAndCombinesWithBattleReduction(int battleReduction, int expected)
    {
        const string brave = "base.fp.character.17";
        var state = BeforeCombat(Search, fpCard: brave);
        AddArmy(state, Side.Shadow, 5);
        BattleRuntime.Active(state)!.Force(Side.Shadow).CombatDiceReduction = battleReduction;
        state = Reveal(state, Search, brave);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(8, BattleRules.Strength(state, force));
        Assert.Equal(2, force.Round.CombatDicePenalty);
        Assert.Equal(expected, force.Round.Dice.Count);
        force.Round.CombatDiceLimit = 1;
        Assert.Equal(1, BattleRoll.DiceCount(state, force));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void StenchComparesOnlyNazgulAgainstAllFpLeadershipAndCancelsOnlyReroll(int addedNazgul, bool cancelled)
    {
        var state = BeforeCombat(Search);
        var battle = BattleRuntime.Active(state)!;
        for (var i = 0; i < addedNazgul; i++)
            battle.Force(Side.Shadow).Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, battle.Force(Side.Shadow).Region).Id);
        var leadership = BattleLeadership.Total(state, battle.Force(Side.FreePeoples));
        Assert.Equal(4, leadership);
        state = Reveal(state, Search);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(cancelled ? 0 : 5, battle.Force(Side.FreePeoples).Round.RerollLimit);
        Assert.Equal(leadership, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Equal(5, battle.Force(Side.FreePeoples).Round.Dice.Count);
        Assert.Equal(cancelled ? Side.Shadow : Side.FreePeoples, state.Pending!.Owner);
    }

    [Theory]
    [InlineData("base.fp.character.09")]
    [InlineData("base.fp.character.05")]
    [InlineData("base.fp.character.06")]
    [InlineData("base.fp.character.12")]
    public void StenchDisablesRerollDependentCardsWithoutCancellingTheirLeadership(string fpCard)
    {
        var state = BeforeCombat(Search, fpCard: fpCard);
        var fp = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        fp.Participants.Add(Put(state, "base.character.merry", fp.Region).Id);
        var sh = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var index = 0; index < 2; index++)
            sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, sh.Region).Id);
        state = Reveal(state, Search, fpCard);
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(5, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Empty(battle.Force(Side.FreePeoples).Round.ForfeitedLeadership);
        Assert.Equal(0, BattleRoll.HitCount(battle.Force(Side.FreePeoples)));
        Assert.Equal(BattleFixture.Target, state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
    }

    [Theory]
    [InlineData(FoulThing)]
    [InlineData(Candles)]
    public void DaringDefianceCancelsEffectAndItsCost(string card)
    {
        const string daring = FreePeoplesCharacterCards02.Alone;
        var state = Reveal(BeforeCombat(card, fpCard: daring), card, daring);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Empty(battle.Force(Side.Shadow).Round.ForfeitedLeadership);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(5, battle.Force(Side.FreePeoples).Round.Dice.Count);
    }

    [Fact]
    public void TerribleKeepsRerollBonusAgainstAdvantageousPositionAndItsCostExpiresNextRound()
    {
        const string position = "base.fp.strategy.03";
        var state = Reveal(BeforeCombat(FoulThing, fpCard: position), FoulThing, position);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [Choices(state).EligibleIds[0]] });
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ForfeitedLeadership.Values.Sum());
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.RerollModifier);
        state = CardPackBattleFixture.NoRerolls(state);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.Empty(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ForfeitedLeadership);
        Assert.Equal(3, BattleLeadership.Nazgul(state, BattleRuntime.Active(state)!.Force(Side.Shadow)).Values.Sum());
    }

    [Fact]
    public void SuddenStrikeUsesLeadershipBeforeFoulStenchCancelsTheReroll()
    {
        const string sudden = "base.fp.character.14";
        var state = BeforeCombat(Search, fpCard: sudden);
        var sh = BattleRuntime.Active(state)!.Force(Side.Shadow);
        sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, sh.Region).Id);
        state = Reveal(state, Search, sudden, new int[12]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(4, Assert.Single(battle.SpecialAttacks).Dice.Count);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.RerollLimit);
        Assert.Equal(4, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
    }

    [Fact]
    public void WeatherUsesTheCompleteCruelAsDeathEffect()
    {
        var state = Reveal(BeforeCombat(Weather), Weather);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = Choices(state).EligibleIds.Take(2).ToList() }, new SequenceRandom(new int[8]));
        var round = BattleRuntime.Active(state)!.Force(Side.Shadow).Round;
        Assert.Equal(2, round.ForfeitedLeadership.Values.Sum());
        Assert.Equal(1, round.CombatModifier);
        Assert.Equal(0, round.RerollModifier);
    }

    internal static void AddArmy(MatchState state, Side side, int count)
    {
        var force = BattleRuntime.Active(state)!.Force(side);
        for (var i = 0; i < count; i++)
            force.Participants.Add(BattleFixture.Put(state, side == Side.FreePeoples ? "gondor" : "sauron", UnitKind.Regular, force.Region).Id);
    }

    internal static MatchState RerollShadow(MatchState state, int count, params int[] raw)
    {
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
        {
            var ids = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice.Take(count).Select(d => d.Id).ToList();
            state = Do(state, CommandKind.RerollBattle, new() { Ids = state.Pending.Owner == Side.Shadow ? ids : [] }, new SequenceRandom(raw));
        }
        return state;
    }
}
