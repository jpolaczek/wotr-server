using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsShadowCardsFixture;

namespace Wotr.Tests;

public class LordsShadowCombatTests
{
    [Theory]
    [InlineData(LordsShadowCards.WeWontGoBack)]
    [InlineData(LordsShadowCards.Lore)]
    public void FearForfeitsAllLeadershipAndNaturalSixesCountInBothRolls(string card)
    {
        var state = Reveal(Battle(card), card, 5, 0, 0, 0, 0, 2, 2, 2);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
        Assert.Equal(2, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(3, BattleRoll.HitCount(battle.Force(Side.Shadow)));
        var failed = BattleRoll.Failed(battle.Force(Side.FreePeoples)).Take(2).ToList();
        state = LordsFixture.Do(state, CommandKind.RerollBattle, new() { Ids = failed }, new SequenceRandom(5, 4));
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(5, BattleRoll.HitCount(battle.Force(Side.FreePeoples)));
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
    }

    [Fact]
    public void WhiteRiderPreventsEntireLeadershipCostButNotFearPenalty()
    {
        var state = Battle(LordsShadowCards.Lore, companion: CharacterRules.White);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        state = Reveal(state, LordsShadowCards.Lore, 5, 0, 0, 0, 0, 2, 2, 2);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Empty(battle.Force(Side.Shadow).Round.ForfeitedLeadership);
        Assert.Equal(2, BattleRoll.HitCount(battle.Force(Side.FreePeoples)));
        Assert.True(BattleLeadership.Total(state, battle.Force(Side.Shadow)) > 0);
    }

    [Fact]
    public void ServantChoosesOneMinionAndAppliesPenaltyAfterFiveDieCap()
    {
        var state = Reveal(Battle(LordsShadowCards.Captain), LordsShadowCards.Captain);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        var battle = BattleRuntime.Active(state)!;
        var king = BattleRules.Characters(state, battle.Force(Side.Shadow)).Single(CharacterRules.IsWitchKing);
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [king.Id] }, new SequenceRandom(new int[6]));
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(3, battle.Force(Side.FreePeoples).Round.Dice.Count);
        Assert.Equal(2, battle.Force(Side.Shadow).Round.ForfeitedLeadership[king.Id]);
        Assert.True(BattleLeadership.Total(state, battle.Force(Side.Shadow)) > 0);
    }

    [Fact]
    public void DurinsBaneAllowsParticipatingBalrogAwayFromMoriaAndAppliesImmediateLosses()
    {
        var state = Battle(LordsShadowCards.Balrog);
        var battle = BattleRuntime.Active(state)!;
        Assert.False(new NearMoriaBattle().CanPlay(state, battle, Side.Shadow));
        Assert.False(new BattleCards(EffectCatalog.All).CanCommit(state, battle, Side.Shadow, LordsShadowCards.Balrog));
        var balrog = LordsCharacterFixture.Put(state, LordsCharacters.Balrog, battle.Force(Side.Shadow).Region);
        battle.Force(Side.Shadow).Participants.Add(balrog.Id);
        state = Reveal(state, LordsShadowCards.Balrog, 3, 2, 5);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        Assert.Empty(BattleRuntime.Active(state)!.Attacker.Round.Dice);
    }
}
