using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.KingsFreePeoplesCards01;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards01EventTests
{
    public static TheoryData<string> Cards => new() { Stewards, Aid, Dark, Wisdom, Riders, Brand };

    [Theory]
    [MemberData(nameof(Cards))]
    public void EachCardValidatesModuleOwnerCostWindowAndPresenceWithoutMutation(string card)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        var payload = Cost(state, card, CardCatalog.All[card].EventType);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        var before = StateCodec.Write(state);
        Assert.True(new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, command).Legal);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, Side.Shadow, command));
        payload.CardCost = DieFace.Army;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = CardCatalog.All[card].EventType;
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Configuration.Kings = true;
        state.Player(Side.FreePeoples).Hand.Remove(card);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Player(Side.FreePeoples).Hand.Add(card);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Available;
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void StewardsRecruitsAtBoromirThenDrawsTwoStrategyCards(UnitKind kind)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        var cards = state.Player(Side.FreePeoples).Decks["strategy"].Take(2).ToArray();
        state = Choose(Play(state, Stewards), "recruit");
        Assert.Equal("ReserveFigures", state.Pending!.Presentation);
        var unit = state.Units.First(u => u.Nation == "gondor" && u.Kind == kind && u.Location == "reserve");
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor] });
        state = Choose(state, unit.Id);
        Assert.Equal("pelargir", state.Units.Single(u => u.Id == unit.Id).Location);
        Assert.Equal(cards, state.Player(Side.FreePeoples).Hand);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData("fellowship")]
    [InlineData("edoras")]
    [InlineData("casualties")]
    public void BothStewardsBranchesRequireBoromirInGondor(string region)
    {
        var state = State();
        if (region != "fellowship")
            Companion(state, Boromir, region);
        Tile(state, SovereignCatalog.Denethor);
        LordsFixture.Reject(state, CommandKind.PlayEvent, Cost(state, Stewards));
    }

    [Theory]
    [InlineData(Stewards, SovereignCatalog.Denethor, 2)]
    [InlineData(Riders, SovereignCatalog.Theoden, 1)]
    [InlineData(Brand, SovereignCatalog.Brand, 1)]
    public void RandomRemovalExcludesKingsTileAndDrawsOnlyAfterTheServerChoice(string card, string sovereign, int draw)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        Tile(state, sovereign);
        Tile(state, sovereign, KingsTiles.One);
        var expected = state.Player(Side.FreePeoples).Decks["strategy"].Take(draw).ToArray();
        state = Play(state, card);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = ["corruption"] }));
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Empty(state.RandomSamples);
        state = Choose(StateCodec.Copy(state), "corruption", 0);
        Assert.Equal(new[] { KingsTiles.One }, Sovereign(state, sovereign).Tiles);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == One).Location);
        Assert.Equal(expected, state.Player(Side.FreePeoples).Hand);
        Assert.Single(state.RandomSamples);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData(Stewards, SovereignCatalog.Denethor, 2)]
    [InlineData(Riders, SovereignCatalog.Theoden, 1)]
    [InlineData(Brand, SovereignCatalog.Brand, 1)]
    public void NoHiddenTileStillCompletesTheChosenBranchWithoutRandomness(string card, string sovereign, int draw)
    {
        var state = State();
        Companion(state, Boromir, "pelargir");
        Tile(state, sovereign, KingsTiles.One);
        state = Choose(Play(state, card), "corruption");
        Assert.Equal(draw, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(new[] { KingsTiles.One }, Sovereign(state, sovereign).Tiles);
        Assert.Empty(state.RandomSamples);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(Riders, SovereignCatalog.Theoden)]
    [InlineData(Brand, SovereignCatalog.Brand)]
    public void RemovingTheCorruptedSovereignDoesNotRecruitOrDraw(string card, string sovereign)
    {
        var state = State();
        Tile(state, sovereign, HuntTests.Three);
        var payload = Cost(state, card);
        var deck = state.Player(Side.FreePeoples).Decks["strategy"].ToArray();
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.Equal(SovereignStatus.Removed, Sovereign(state, sovereign).Status);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        Assert.Equal(deck, state.Player(Side.FreePeoples).Decks["strategy"]);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }
}
