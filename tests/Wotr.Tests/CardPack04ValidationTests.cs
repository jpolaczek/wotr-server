using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards04;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CardPack04ValidationTests
{
    [Theory]
    [InlineData(Treebeard)]
    [InlineData(Huorns)]
    [InlineData(Entmoot)]
    [InlineData(DeadMen)]
    [InlineData(Stewards)]
    [InlineData(GreyCompany)]
    public void EveryEventRejectsWrongFaceSpentDieMissingCardAndWrongWindow(string card)
    {
        var state = card switch
        {
            DeadMen => DeadMenCardTests.BeforeJourney(),
            Stewards => GondorRecruitCardTests.StewardsState(),
            GreyCompany => GondorRecruitCardTests.CompanyState(),
            _ => EntsCardTests.BeforeAttack()
        };
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = card;
        payload.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Character;
        var die = state.Dice.Single(d => d.Id == payload.DieId);
        die.Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        die.Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.PlayEvent, payload);
        HandRules.Add(state.Player(Side.FreePeoples), card);
        state.Pending!.Kind = DecisionKind.Losses;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(DeadMen)]
    [InlineData(Stewards)]
    [InlineData(GreyCompany)]
    public void ReusedCombatHalfStillRequiresItsPrintedCommander(string card)
    {
        var state = CardPackBattleFixture.BeforeCards(card);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[card].Condition;
        Assert.True(condition.CanPlay(state, battle, Side.FreePeoples));
        state.Characters.RemoveAll(c => c.Side == Side.FreePeoples);
        if (card != DeadMen)
            Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        foreach (var leader in state.Units.Where(u => u.Kind == UnitKind.Leader))
            BoardRules.RemoveCasualty(leader);
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
    }

    [Fact]
    public void ZeroEntHitsStillAdvanceEveryDefendingNationOnce()
    {
        var state = EntsCardTests.BeforeAttack();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "orthanc");
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 2;
        state = FellowshipEventCardTests.Play(state, Treebeard, 0, 1, 2);
        Assert.Null(EventLosses.Current(state));
        Assert.Equal(1, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.Equal(1, state.Nations.Single(n => n.Id == "isengard").StepsToWar);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "PoliticsAdvanced"));
    }

    [Fact]
    public void WhiteInRohanAllowsFollowUpAndPrintedConditionStillAppliesToNextCard()
    {
        var state = EntsCardTests.BeforeAttack(regulars: 0);
        state.Characters.Single(c => c.DefinitionId == CharacterRules.White).Location = "edoras";
        EventTests.GiveCard(state, Side.FreePeoples, GreyCompany);
        state = FellowshipEventCardTests.Play(state, Entmoot, 0, 0, 0);
        Assert.Equal("ents.follow-up", state.Pending!.Window);
        Reject(state, CommandKind.PlayEvent, new() { CardId = GreyCompany, CardCost = DieFace.Character });
        Assert.DoesNotContain(new EffectOptions().GetOptions(state, Side.FreePeoples).SelectMany(o => o.Events), e => e.CardId == GreyCompany);
    }

    [Fact]
    public void OrdinaryBattleEliminationAlsoReconcilesNextTurnsCharacterDice()
    {
        var state = CardPackBattleFixture.BeforeCards(FreePeoplesCharacterCards03.ThereAndBack);
        CharacterLifecycle.ReconcileDice(state);
        Assert.Equal(1, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
        state = BattleFixture.NoCards(state, 1, 1, 1, 1, 1, 6, 6, 6);
        state = CardPackBattleFixture.NoRerolls(state);
        var regulars = BattleRules.Units(state, BattleRuntime.Active(state)!.Attacker).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeBattleLosses, new() { Losses = regulars });
        Assert.Equal(0, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
    }

    [Fact]
    public void NamelessWoodCanBeEnabledByLeaderRerollAlone()
    {
        var state = CardPackBattleFixture.Reveal(EntsCombatCardTests.InRohan(Entmoot), Entmoot, dice: new int[8]);
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice[0];
        state = Do(state, CommandKind.RerollBattle, new() { Ids = [die.Id] });
        state = Do(state, CommandKind.RerollBattle, random: new SequenceRandom(5));
        Assert.Equal(3, BattleRoll.HitCount(BattleRuntime.Active(state)!.Attacker));
    }
}
