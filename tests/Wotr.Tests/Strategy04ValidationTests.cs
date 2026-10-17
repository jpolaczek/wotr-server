using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards04;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class Strategy04ValidationTests
{
    [Theory]
    [InlineData(KingBrandsMen)]
    [InlineData(SwordsInEriador)]
    [InlineData(KindredOfGlorfindel)]
    [InlineData(DainIronfoot)]
    [InlineData(Eomer)]
    [InlineData(ThranduilsArchers)]
    public void EveryEventRejectsWrongFaceSpentDieMissingCardAndWrongWindow(string card)
    {
        var state = BattleFixture.State();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, DieFace.Muster);
        payload.CardId = card;
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Muster;
        var die = state.Dice.Single(d => d.Id == payload.DieId);
        die.Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        die.Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.PlayEvent, payload);
        HandRules.Add(state.Player(Side.FreePeoples), card);
        Assert.Null(Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.PlayEvent, payload)));
        state.Pending!.Kind = DecisionKind.Losses;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(KingBrandsMen)]
    [InlineData(SwordsInEriador)]
    [InlineData(KindredOfGlorfindel)]
    [InlineData(DainIronfoot)]
    [InlineData(Eomer)]
    [InlineData(ThranduilsArchers)]
    public void EveryCombatRequiresOwnCardAndCorrectWindow(string card)
    {
        var state = Strategy03Fixture.Charge(card, fpAttacks: false);
        state = Do(state, CommandKind.CommitCombatCard);
        var payload = new CommandPayload { CardId = card };
        Assert.Null(Engine().Validate(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.CommitCombatCard, payload)));
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.FreePeoples), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }
}
