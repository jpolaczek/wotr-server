using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;
using static Wotr.Tests.KingsChieftainBattleFixture;

namespace Wotr.Tests;

public class KingsMirkwoodTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LordOfTheBatsForfeitsOnlyItsOwnLeadershipAndDoesNotCreateRerolls(bool otherLeader)
    {
        var state = Ready(KingsChieftains.Mirkwood);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        if (otherLeader)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region).Id);
        state = Choose(Cards(state), KingsChieftains.Mirkwood);
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(1, force.Round.ForfeitedLeadership[KingsChieftains.Mirkwood]);
        Assert.Equal(otherLeader ? 1 : 0, BattleLeadership.Total(state, force));
        Assert.Equal(otherLeader ? 1 : 0, BattleRoll.RerollMaximum(state, force));
        Assert.Equal(0, force.Round.CombatModifier);
        Assert.Equal(1, force.Round.RerollModifier);
        Assert.Equal(4, Assert.Single(BattleRuntime.Active(state)!.Work).Initiative);
    }

    [Theory]
    [InlineData(ShadowStrategyCards02.Moving)]
    [InlineData(ShadowStrategyCards02.Return)]
    public void SwarmOfBatsUsesSeparateZeroAndFourInitiativesWhileBothCardsRemainPlayed(string card)
    {
        var state = Choose(Cards(Ready(KingsChieftains.Mirkwood), card, FreePeoplesStrategyCards01.LastBattle), KingsChieftains.Mirkwood);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(new[] { 0, 4 }, battle.Work.Where(w => w.Owner == Side.Shadow).Select(w => w.Initiative));
        Assert.True(battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.True(state.Player(Side.FreePeoples).CombatCardCommitted);
        Assert.Equal(FreePeoplesStrategyCards01.LastBattle, battle.Force(Side.FreePeoples).Round.CardId);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.RerollModifier);
    }

    [Theory]
    [InlineData(Side.FreePeoples, true)]
    [InlineData(Side.Shadow, false)]
    public void DefenderWinsTheInitiativeZeroTieBetweenSwarmAndDaring(Side attacker, bool shadowWins)
    {
        var state = Choose(Cards(Ready(KingsChieftains.Mirkwood, attacker), ShadowStrategyCards02.Moving, Daring), KingsChieftains.Mirkwood);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(shadowWins, battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(!shadowWins, battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(shadowWins ? 1 : 0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.ForfeitedLeadership[KingsChieftains.Mirkwood]);
        Assert.Equal(shadowWins ? 1 : 0, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("forfeited")]
    public void PreviouslyUnavailableLeadershipCannotBePaidAgain(string reason)
    {
        var state = Ready(KingsChieftains.Mirkwood);
        var round = BattleRuntime.Active(state)!.Force(Side.Shadow).Round;
        if (reason == "cancelled")
            round.CancelledLeadership.Add(KingsChieftains.Mirkwood);
        else
            round.ForfeitedLeadership[KingsChieftains.Mirkwood] = 1;
        state = Cards(state);
        Assert.NotEqual("CombatReplacement", state.Pending!.Presentation);
        Assert.True(BattleRuntime.Active(state)!.CardsRevealed);
    }

    [Fact]
    public void BrandsLaterReplacementCanCancelTheChieftainWithoutReturningItsLeadership()
    {
        var state = Ready(KingsChieftains.Mirkwood);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Brand), "lossarnach");
        BattleRuntime.Active(state)!.Force(Side.FreePeoples).Participants.Add(SovereignCatalog.Brand);
        state = Cards(state, fpCard: Daring);
        Assert.False(BattleRuntime.Active(state)!.CardsRevealed);
        state = Choose(state, KingsChieftains.Mirkwood);
        Assert.True(BattleRuntime.Active(state)!.CardsRevealed);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
        state = Choose(state, "replace");
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.ForfeitedLeadership[KingsChieftains.Mirkwood]);
    }

    [Fact]
    public void SorcererDrawsForTheReplacedAndCancelledCombatCardAfterTheRoundCloses()
    {
        var state = Ready(KingsChieftains.Mirkwood);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        BattleFixture.Character(state, CharacterRules.WitchKing, Side.Shadow, force.Region, 2, 3);
        force.Participants.Add(CharacterRules.WitchKing);
        state = SkipRerolls(Choose(Cards(state, fpCard: Daring), KingsChieftains.Mirkwood));
        Assert.True(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        var top = state.Player(Side.Shadow).Decks["strategy"][0];
        state = Do(state, CommandKind.StopBattle);
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        Assert.Contains(KingsCards03.Valinor, state.Player(Side.Shadow).Discard);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "character.sorcerer" });
        Assert.Contains(top, state.Player(Side.Shadow).Hand);
        Assert.Equal(KingsFixture.SetupSamples + 4, state.RandomSamples.Count);
        Assert.Null(BattleRuntime.Active(state));
    }

    [PostgresFact]
    public Task LeadershipCostAndBothSwarmInitiativesSurviveRestartWithoutRepayment() =>
        LordsPersistenceTests.Restart(Cards(Ready(KingsChieftains.Mirkwood), ShadowStrategyCards02.Moving, Daring),
            CommandKind.ChooseEffect, new() { Ids = [KingsChieftains.Mirkwood] }, 0, 0, 0, 0);
}
