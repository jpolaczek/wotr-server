using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class Strategy02ValidationTests
{
    [Theory]
    [InlineData(FearFireFoes)]
    [InlineData(WisdomOfElrond)]
    [InlineData(RedArrow)]
    [InlineData(HelpUnlookedFor)]
    [InlineData(PathsOfWoses)]
    [InlineData(ThroughDayNight)]
    public void EveryEventRejectsWrongFaceSpentDieMissingCardAndWrongWindow(string card)
    {
        var state = card == HelpUnlookedFor ? Strategy02Fixture.Relief() : Strategy02Fixture.Passage(false);
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, CardCatalog.Base[card].EventType);
        payload.CardId = card;
        payload.NationId = "north";
        payload.CardCost = DieFace.Character;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = CardCatalog.Base[card].EventType;
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

    [Fact]
    public void NewDecisionsOfferOnlyAuthorizedArmyOperationAndPreviewIsPure()
    {
        var state = Strategy01Fixture.Play(Strategy02Fixture.March(), ThroughDayNight);
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, Strategy02Fixture.Move(state, "north-ithilien", "minas-morgul"));
        var before = StateCodec.Write(state);
        var options = new MovementOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.NotNull(options.ArmyEvent);
        Assert.Empty(options.EligibleDice);
        Assert.Empty(new MovementOptions().GetOptions(state, Side.Shadow));
        var preview = new PreviewService(Engine());
        Assert.True(preview.Preview(state, Side.FreePeoples, command).Legal);
        Assert.False(preview.Preview(state, Side.Shadow, command).Legal);
        Assert.Equal(before, StateCodec.Write(state));
    }
}
