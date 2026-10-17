using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class Shadow03InteractionTests
{
    [Theory]
    [InlineData("base.fp.character.14", 3)]
    [InlineData("base.fp.character.17", 0)]
    public void WordsUpdatesSuddenStrikeButBraveStandStillCountsTheCompanion(string fp, int specialDice)
    {
        var state = Reveal(BeforeCombat(Worn, fpCard: fp), Worn, fp);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] }, new SequenceRandom(new int[12]));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(specialDice, battle.SpecialAttacks.Sum(a => a.Dice.Count));
        if (specialDice == 0)
        {
            Assert.Equal(2, battle.Force(Side.Shadow).Round.CombatDicePenalty);
            Assert.Single(battle.Force(Side.Shadow).Round.Dice);
        }
    }

    [Fact]
    public void WordsStopsAndurilPaymentButNotHeroicDeathOfThatCompanion()
    {
        var state = Reveal(BeforeCombat(Worn, fpCard: FreePeoplesCharacterCards02.Athelas), Worn, FreePeoplesCharacterCards02.Athelas);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider] }, new SequenceRandom(new int[8]));
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
        Assert.Empty(BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.ForfeitedLeadership);
        state = Reveal(BeforeCombat(Worn, fpCard: FreePeoplesCharacterCards02.AnotherWay), Worn, FreePeoplesCharacterCards02.AnotherWay);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] }, new SequenceRandom(0, 0, 0, 0, 5, 0, 0));
        state = CardPackBattleFixture.NoRerolls(state);
        Assert.Contains("base.character.gimli", Choices(state).EligibleIds);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli", HeroicDeathStep.HitPrefix + "1"] });
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == "base.character.gimli").Location);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelHits);
    }

    [Fact]
    public void DefenderDurinsBaneCanDestroyArmyBeforeTiedSuddenStrike()
    {
        var state = MoriaBattle(fpCard: FreePeoplesCharacterCards03.Challenge);
        state = Reveal(state, Balrog, FreePeoplesCharacterCards03.Challenge, 3, 3, 3);
        var units = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Where(BoardRules.IsArmyUnit).ToArray();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = units.Select(u => new BattleLossChoice { UnitId = u.Id }).ToList() });
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.Equal(Balrog, BattleRuntime.Active(state)!.SpecialAttacks[0].SourceId);
    }

    [Fact]
    public void UnusedBalrogRemainsOnTableAfterBattleWithoutBecomingAHandDiscardCost()
    {
        var state = MoriaBattle(table: true);
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Hand);
        state = Reveal(state, "", dice: new int[8]);
        state = CardPackBattleFixture.NoRerolls(state);
        state = Do(state, CommandKind.StopBattle);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Contains(Balrog, state.Player(Side.Shadow).Table);
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Hand);
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Discard);
    }

    [Fact]
    public void TableBalrogEnablesSorcererAndExcessCardsAreDiscardedAtBattleEnd()
    {
        var state = MoriaBattle(table: true);
        foreach (var card in CardCatalog.Base.Values.Where(c => c.Side == Side.Shadow && c.Id != Balrog).Take(6))
            EventTests.GiveCard(state, Side.Shadow, card.Id);
        Assert.Equal(6, state.Player(Side.Shadow).Hand.Count);
        state = Reveal(state, Balrog, dice: new int[11]);
        state = CardPackBattleFixture.NoRerolls(state);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.sorcerer" });
        Assert.Equal(7, state.Player(Side.Shadow).Hand.Count);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = Reveal(state, "", dice: new int[8]);
        state = CardPackBattleFixture.NoRerolls(state);
        state = Do(state, CommandKind.StopBattle);
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        state = Do(state, CommandKind.DiscardCards, new() { Ids = [state.Player(Side.Shadow).Hand[0]] });
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal(6, state.Player(Side.Shadow).Hand.Count);
        Assert.Single(state.Player(Side.Shadow).Discard, id => id == Balrog);
    }
}
