using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow01ValidationTests
{
    [Theory]
    [InlineData("base.sh.character.01")]
    [InlineData("base.sh.character.02")]
    [InlineData("base.sh.character.03")]
    [InlineData("base.sh.character.04")]
    [InlineData("base.sh.character.05")]
    [InlineData("base.sh.character.06")]
    public void EveryEventRequiresOwnedCardCorrectFaceAvailableCostAndActionWindow(string card)
    {
        var state = BeforeEvent(card);
        var payload = EventCost(state, card);
        payload.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Character;
        var die = state.Dice.Single(d => d.Id == payload.DieId);
        die.Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        die.Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        Assert.Null(Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.PlayEvent, payload)));
        die.Face = DieFace.Event;
        payload.CardCost = DieFace.Event;
        Assert.Null(Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.PlayEvent, payload)));
        state.Pending!.Kind = DecisionKind.HuntDamage;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData("base.sh.character.01")]
    [InlineData("base.sh.character.02")]
    [InlineData("base.sh.character.03")]
    [InlineData("base.sh.character.04")]
    [InlineData("base.sh.character.05")]
    [InlineData("base.sh.character.06")]
    public void EveryCombatRequiresOwnCardAndCombatWindow(string card)
    {
        var state = BeforeCombat(card);
        state = Do(state, CommandKind.CommitCombatCard);
        var payload = new CommandPayload { CardId = card };
        Assert.Null(Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.CommitCombatCard, payload)));
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }
}
