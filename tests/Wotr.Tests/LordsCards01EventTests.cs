using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.LordsFreePeoplesCards;
using static Wotr.Tests.LordsCardsFixture;

namespace Wotr.Tests;

public class LordsCards01EventTests
{
    public static TheoryData<string> Cards => new() { YouKnowTheWay, SafePaths, Wind, ThreeRings, EveryLittle, Rangers };

    [Theory]
    [MemberData(nameof(Cards))]
    public void EachEventHasCorrectCostAndRejectsMissingCardWrongActorWindowAndModule(string card)
    {
        var state = State();
        var payload = Cost(state, card, CardCatalog.All[card].EventType);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        var preview = new PreviewService(LordsFixture.Engine());
        Assert.True(preview.Preview(state, Side.FreePeoples, command).Legal);
        Assert.False(preview.Preview(state, Side.Shadow, command).Legal);
        payload.CardCost = payload.CardCost == DieFace.Muster ? DieFace.Army : DieFace.Muster;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = CardCatalog.All[card].EventType;
        state.Configuration.Lords = false;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Configuration.Lords = true;
        state.Player(Side.FreePeoples).Hand.Remove(card);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Player(Side.FreePeoples).Hand.Add(card);
        state.Dice.First(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.First(d => d.Id == payload.DieId).Location = DieLocation.Available;
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }

    [Fact]
    public void YouKnowTheWayAddsOnlyTwoSetAsideTilesEvenAfterSmeagolLeft()
    {
        var state = State();
        state.HuntTiles.Single(t => t.Id == SmeagolTiles.Definitions[0].Id).Location = "removed";
        state.Lords!.SmeagolLeft = true;
        var result = Play(state, YouKnowTheWay);
        Assert.Equal("removed", result.HuntTiles.Single(t => t.Id == SmeagolTiles.Definitions[0].Id).Location);
        Assert.All(SmeagolTiles.Definitions.Skip(2), d => Assert.Equal("pool", result.HuntTiles.Single(t => t.Id == d.Id).Location));
        Assert.Equal(4, result.HuntTiles.Count(t => t.Id.StartsWith("lords.hunt.smeagol.")));
        Assert.Single(result.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(2, SmeagolRules.WeShallGetIt)]
    [InlineData(1, WeWontGoBack)]
    public void SafePathsHealsAndRemovesTheSmeagolEffectWithoutTriggeringIt(int corruption, string table)
    {
        var state = State();
        state.Fellowship.Corruption = corruption;
        if (table != "")
        {
            state.Player(Side.Shadow).Table.Add(table);
            state.ActiveEffects.Add(new() { SourceId = table, Owner = Side.Shadow, Duration = EffectDuration.UntilRemoved });
        }
        var result = Play(state, SafePaths);
        Assert.Equal(Math.Max(0, corruption - 1), result.Fellowship.Corruption);
        Assert.Empty(result.Player(Side.Shadow).Table);
        Assert.Empty(result.ActiveEffects);
        Assert.Empty(result.RandomSamples);
        Assert.Empty(result.Resolution.Frames);
    }

    [Fact]
    public void EveryLittleRecruitsDespitePeaceThenDrawsFromStrategy()
    {
        var state = State();
        var next = state.Player(Side.FreePeoples).Decks["strategy"][0];
        var result = Play(state, EveryLittle);
        Assert.All(new[] { "pelargir", "lossarnach", "lamedon" }, region =>
            Assert.Single(result.Units, u => u.Location == region && u.Nation == "gondor" && u.Kind == UnitKind.Regular));
        Assert.Contains(next, result.Player(Side.FreePeoples).Hand);
        Assert.Equal(3, result.Events.Count(e => e.Kind == "EventRecruited"));
    }

    [Fact]
    public void EveryLittleRequiresAChoiceWhenSharedReservesCannotFillAllRegions()
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).Skip(1))
            unit.Location = "casualties";
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lossarnach");
        state = Play(state, EveryLittle);
        Assert.Equal("RecruitmentPlan", state.Pending!.Presentation);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        var unitId = state.Pending.PrivateChoices.Single();
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Placements = [new() { UnitId = unitId, RegionId = "lossarnach" }] });
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Placements = [new() { UnitId = unitId, RegionId = "lamedon" }] });
        Assert.Equal("lamedon", state.Units.Single(u => u.Id == unitId).Location);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    [Fact]
    public void MissingRecruitsAndEmptyDeckDoNotBlockEventCompletion()
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular))
            unit.Location = "casualties";
        state.Player(Side.FreePeoples).Decks["strategy"].Clear();
        state = Play(state, EveryLittle);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        state = Play(State(), Rangers);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
    }
}
