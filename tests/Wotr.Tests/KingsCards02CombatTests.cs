using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards02CombatTests
{
    internal static MatchState InitialHope()
    {
        var state = Battle();
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Participants.Remove(SovereignCatalog.Dain);
        AddInitial(state, SovereignCatalog.Theoden);
        return state;
    }

    internal static void AddInitial(MatchState state, string sovereign)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        state.Units.Single(u => u.Id == sovereign).Location = force.Region;
        force.Participants.Add(sovereign);
    }

    internal static MatchState VainReady(int nazgul = 2)
    {
        var state = Battle();
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var i = 0; i < nazgul; i++)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region).Id);
        return state;
    }

    [Theory]
    [InlineData(Dain)]
    [InlineData(Thranduil)]
    public void ValourRequiresParticipatingEliteAndModifiesOnlyCombat(string card)
    {
        var state = Battle();
        state.Player(Side.FreePeoples).Hand = [card];
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Participants.Add(BattleFixture.Put(state, "elves", UnitKind.Elite, force.Region).Id);
        state = KingsChieftainBattleFixture.Cards(state, "", card);
        var round = BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round;
        Assert.Equal(1, round.CombatModifier);
        Assert.Equal(0, round.RerollModifier);
        Assert.Equal(3, round.Dice.Count);
    }

    [Theory]
    [InlineData(Service)]
    [InlineData(Villages)]
    public void HopeAwakensParticipatingInitialRulerInPlaceBeforeReroll(string card)
    {
        var state = InitialHope();
        Tile(state, SovereignCatalog.Theoden);
        state = KingsChieftainBattleFixture.Cards(state, "", card);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Theoden).Status);
        Assert.Equal(battle.Force(Side.FreePeoples).Region, SovereignLifecycle.Region(state, Sovereign(state, SovereignCatalog.Theoden)));
        Assert.Contains(SovereignCatalog.Theoden, battle.Force(Side.FreePeoples).Participants);
        Assert.Equal(3, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Empty(Sovereign(state, SovereignCatalog.Theoden).Tiles);
        Assert.Single(state.Kings!.Tokens, t => t.SovereignId == SovereignCatalog.Theoden);
        Assert.Equal(1, battle.Force(Side.FreePeoples).Round.RerollModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(Service)]
    [InlineData(Villages)]
    public void HopeWithAwakenedSovereignAddsRerollWithoutAnotherToken(string card)
    {
        var state = KingsChieftainBattleFixture.Cards(Battle(), "", card);
        Assert.Single(state.Kings!.Tokens, t => t.SovereignId == SovereignCatalog.Dain);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.RerollModifier);
    }

    [Theory]
    [InlineData(Service)]
    [InlineData(Villages)]
    public void HopeRequiresTheSovereignToParticipateNotMerelyShareRegion(string card)
    {
        var state = InitialHope();
        BattleRuntime.Active(state)!.Force(Side.FreePeoples).Participants.Remove(SovereignCatalog.Theoden);
        state.Player(Side.FreePeoples).Hand = [card];
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WelcomeRequiresWarForAwakeningButDoesNotCancelHopeReroll(bool atWar)
    {
        var state = InitialHope();
        state.Player(Side.Shadow).Table.Add(Welcome);
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = atWar ? 0 : 1;
        state = KingsChieftainBattleFixture.Cards(state, "", Service);
        Assert.Equal(atWar ? SovereignStatus.Awakened : SovereignStatus.Initial, Sovereign(state, SovereignCatalog.Theoden).Status);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.RerollModifier);
    }

    [Fact]
    public void FollyPreventsHopeCardInDenethorsBattleRegion()
    {
        var state = Battle();
        AddInitial(state, SovereignCatalog.Denethor);
        var battle = BattleRuntime.Active(state)!;
        state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = battle.Region;
        state.Player(Side.Shadow).Table.Add(KingsCardRestrictions.Folly);
        state.Player(Side.FreePeoples).Hand = [Villages];
        Assert.False(new BattleCards(EffectCatalog.All).CanCommit(state, battle, Side.FreePeoples, Villages));
        Assert.False(KingsCardRestrictions.CanAwaken(state, SovereignCatalog.Denethor));
    }

    [Fact]
    public void HopeOffersChoiceAmongAllParticipatingSovereigns()
    {
        var state = InitialHope();
        AddInitial(state, SovereignCatalog.Denethor);
        state = KingsChieftainBattleFixture.Cards(state, "", Service);
        Assert.Equal(new[] { SovereignCatalog.Denethor, SovereignCatalog.Theoden }, state.Pending!.PrivateChoices.Order());
        state = Choose(state, SovereignCatalog.Denethor);
        Assert.Equal(SovereignStatus.Awakened, Sovereign(state, SovereignCatalog.Denethor).Status);
        Assert.Equal(SovereignStatus.Initial, Sovereign(state, SovereignCatalog.Theoden).Status);
    }

    [Fact]
    public void ServantRequiresMinionAndForfeitsAllItsLeadershipWithMinimumOneFpDie()
    {
        var state = Battle();
        state.Player(Side.Shadow).Hand = [Welcome];
        state = Do(state, CommandKind.CommitCombatCard, new());
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = Welcome });
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var minion = LordsFixture.Put(state, CharacterRules.WitchKing, force.Region);
        CharacterLifecycle.ApplyVersion(minion, Wotr.Domain.Content.CharacterCatalog.All[CharacterRules.WitchKing]);
        force.Participants.Add(minion.Id);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = Welcome }, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.Single(battle.Force(Side.FreePeoples).Round.Dice);
        Assert.Equal(2, battle.Force(Side.Shadow).Round.ForfeitedLeadership[minion.Id]);
        Assert.Equal(0, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void BattleIsVainOffersZeroThroughFiveAndReducesArmyLeadershipToAtLeastZero(int points)
    {
        var state = KingsChieftainBattleFixture.Cards(VainReady(6), West);
        var option = new EffectOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Equal(0, option.Minimum);
        Assert.Equal(5, option.Maximum);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = option.EligibleIds.ToList() });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = option.EligibleIds.Take(points).ToList() }, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(points, battle.Force(Side.FreePeoples).Round.LeadershipPenalty);
        Assert.Equal(Math.Max(0, 1 - points), BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Equal(6 - points, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
        Assert.Equal(1, BattleLeadership.Available(state, battle.Force(Side.FreePeoples))[SovereignCatalog.Dain]);
        battle.Force(Side.FreePeoples).Round = new();
        Assert.Equal(1, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
    }

    [Fact]
    public void BattleIsVainCannotBePlayedWithoutAvailableNazgulLeadership()
    {
        var state = Battle();
        state.Player(Side.Shadow).Hand = [West];
        state = Do(state, CommandKind.CommitCombatCard, new());
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = West });
    }

    [Fact]
    public void WhiteRiderPreventsVainPaymentAndCancellingVainLeavesLeadershipUntouched()
    {
        var state = VainReady();
        var battle = BattleRuntime.Active(state)!;
        var white = LordsFixture.Put(state, CharacterRules.White, battle.Force(Side.FreePeoples).Region);
        CharacterLifecycle.ApplyVersion(white, Wotr.Domain.Content.CharacterCatalog.All[CharacterRules.White]);
        battle.Force(Side.FreePeoples).Participants.Add(white.Id);
        battle.WhiteRiderUsed = true;
        state.Player(Side.Shadow).Hand = [West];
        Assert.False(new BattleCards(EffectCatalog.All).CanCommit(state, battle, Side.Shadow, West));

        state = VainReady();
        KingsCards01CombatTests.AddCompanion(state, Merry);
        state = KingsChieftainBattleFixture.Cards(state, West, FreePeoplesCharacterCards02.Alone);
        battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Empty(battle.Force(Side.Shadow).Round.ForfeitedLeadership);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.LeadershipPenalty);
    }

    [Fact]
    public void ServantOffersMinionChoiceAndCannotPayPartiallyForfeitedLeadership()
    {
        var state = KingsCards02PersistenceTests.Combat(Welcome);
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(Side.Shadow);
        var mouth = LordsFixture.Put(state, CharacterRules.Mouth, force.Region);
        CharacterLifecycle.ApplyVersion(mouth, Wotr.Domain.Content.CharacterCatalog.All[CharacterRules.Mouth]);
        force.Participants.Add(mouth.Id);
        state = KingsChieftainBattleFixture.Cards(state, Welcome);
        Assert.Equal("MinionLeadership", state.Pending!.Presentation);
        Assert.Equal(2, state.Pending.PrivateChoices.Count);
        state = Choose(state, mouth.Id, 0, 0, 0);
        Assert.Equal(2, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ForfeitedLeadership[mouth.Id]);

        state = KingsCards02PersistenceTests.Combat(Welcome);
        battle = BattleRuntime.Active(state)!;
        battle.Force(Side.Shadow).Round.ForfeitedLeadership[CharacterRules.WitchKing] = 1;
        state = KingsChieftainBattleFixture.Cards(state, Welcome);
        Assert.Equal(2, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.Dice.Count);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ForfeitedLeadership[CharacterRules.WitchKing]);
    }

    [Fact]
    public void NextRoundRestoresLeadershipAfterVain()
    {
        var state = KingsChieftainBattleFixture.Cards(VainReady(), West);
        state = Choose(state, state.Pending!.PrivateChoices[0], 0, 0, 0, 0);
        state = KingsChieftainBattleFixture.SkipRerolls(state);
        state = Do(state, CommandKind.ContinueBattle);
        if (state.Pending?.Kind == DecisionKind.BattleRetreat)
            state = Do(state, CommandKind.HoldBattle);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Round);
        Assert.Equal(1, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Equal(2, BattleLeadership.Total(state, battle.Force(Side.Shadow)));
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.LeadershipPenalty);
    }
}
