using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class Strategy01ValidationTests
{
    [Theory]
    [InlineData(LastBattle)]
    [InlineData(PowerTooGreat)]
    [InlineData(TomBombadil)]
    [InlineData(BookOfMazarbul)]
    [InlineData(SpiritOfMordor)]
    [InlineData(FaramirsRangers)]
    public void EveryEventRejectsWrongFaceSpentDieMissingCardAndWrongWindow(string card)
    {
        var state = card == LastBattle ? KingArmy() : MixedArmy();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, CardCatalog.Base[card].EventType);
        payload.CardId = card;
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
        state.Pending!.Kind = DecisionKind.Losses;
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Fact]
    public void ProtectionPreviewReportsPrivateCostWithoutMutatingOrDrawing()
    {
        var (state, payload) = RegionProtectionCardTests.Removal();
        var before = StateCodec.Write(state);
        var command = BattleFixture.Command(state, CommandKind.DiscardProtection, payload);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.False(preview.HasUnknownResult);
        Assert.Equal(3, preview.Costs.Count);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.False(new PreviewService(Engine()).Preview(state, Side.FreePeoples, command).Legal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualMordorTileKeepsOrRemovesLastBattleBeforeTheCost(bool reveal)
    {
        var state = Play(KingArmy(), LastBattle);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 2;
        HuntTests.Pool(state, reveal ? "base.hunt.standard.0.1.1" : "base.hunt.special-fp.0.0.1");
        var payload = Cost(state, DieFace.Character);
        state = Do(state, CommandKind.MoveFellowship, payload, new SequenceRandom(0));
        Assert.Equal(reveal ? DieLocation.Hunt : DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Equal(!reveal, state.Player(Side.FreePeoples).Table.Contains(LastBattle));
    }

    [Fact]
    public void OrdinaryRetreatIntoFriendlyCityDoesNotActivateCompanionNation()
    {
        var state = CardPackBattleFixture.BeforeCards(SpiritOfMordor, fpAttacks: false);
        var battle = BattleRuntime.Active(state)!;
        state.Nations.Single(n => n.Id == "gondor").Active = false;
        BattleMovement.Retreat(state, battle, "pelargir");
        Assert.False(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.All(state.Characters.Where(c => c.Side == Side.FreePeoples), c => Assert.Equal("pelargir", c.Location));
    }
}
