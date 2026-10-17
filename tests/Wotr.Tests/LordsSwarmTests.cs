using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsSwarmTests
{
    internal static MatchState Before(bool fpAttacks = true, string fpCard = "", string companion = CharacterRules.Aragorn)
    {
        var state = LordsShadowCardsFixture.Battle(LordsShadowCards.Madness, fpAttacks, companion);
        if (fpCard.Length > 0)
            EventTests.GiveCard(state, Side.FreePeoples, fpCard);
        return state;
    }

    internal static MatchState Reveal(MatchState state, string fpCard = "")
    {
        var fpFirst = state.Pending!.Owner == Side.FreePeoples;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = fpFirst ? fpCard : LordsShadowCards.Madness });
        return Do(state, CommandKind.CommitCombatCard, new() { CardId = fpFirst ? LordsShadowCards.Madness : fpCard }, new SequenceRandom(new int[8]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SwarmValidatesOwnershipModuleAndCombatWindowInBothRoles(bool fpAttacks)
    {
        var state = Before(fpAttacks);
        if (state.Pending!.Owner == Side.FreePeoples)
            state = Do(state, CommandKind.CommitCombatCard);
        var payload = new CommandPayload { CardId = LordsShadowCards.Madness };
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, payload);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        state.Configuration.Lords = false;
        Reject(state, command.Kind, payload);
        state.Configuration.Lords = true;
        state.Player(Side.Shadow).Hand.Clear();
        Reject(state, command.Kind, payload);
        EventTests.GiveCard(state, Side.Shadow, LordsShadowCards.Madness);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, command.Kind, payload);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoCardAddsOneOnlyToActualLeaderRerollsAndResetsNextRound(bool fpAttacks)
    {
        var state = Reveal(Before(fpAttacks));
        while (state.Pending!.Owner != Side.Shadow)
            state = Do(state, CommandKind.RerollBattle);
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(0, shadow.Round.CombatModifier);
        Assert.Equal(1, shadow.Round.RerollModifier);
        var die = shadow.Round.Dice[0].Id;
        state = Do(state, CommandKind.RerollBattle, new() { Ids = [die] }, new SequenceRandom(4));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle, random: new SequenceRandom(4));
        Assert.True(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice[0].RerollHit);
        var loss = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        state = Do(state, CommandKind.TakeBattleLosses, new()
        {
            Losses = [BattleFixture.Loss(BattleRules.Units(state, loss).First(BoardRules.IsArmyUnit).Id)]
        });
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        if (state.Pending?.Kind == DecisionKind.Reaction)
            state = Do(state, CommandKind.DeclineReaction);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.RerollModifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayedCardIsCancelledWithoutTheNoCardBonus(bool fpAttacks)
    {
        const string card = FreePeoplesStrategyCards01.TomBombadil;
        var state = Reveal(Before(fpAttacks, card), card);
        Assert.True(BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.RerollModifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefenderResolvesDaringOrSwarmFirstAtInitiativeZero(bool fpAttacks)
    {
        const string card = FreePeoplesCharacterCards02.Alone;
        var state = Reveal(Before(fpAttacks, card), card);
        Assert.Equal(!fpAttacks, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(fpAttacks, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(0, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.RerollModifier);
    }

    [Fact]
    public void IneffectiveDaringAfterWhiteRiderStillCountsAsPlayed()
    {
        const string card = FreePeoplesCharacterCards02.Alone;
        var state = Before(false, card, CharacterRules.White);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        state = Reveal(state, card);
        var battle = BattleRuntime.Active(state)!;
        Assert.False(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.True(battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Empty(battle.Force(Side.FreePeoples).Round.ForfeitedLeadership);
    }
}
