using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.KingsCards02;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards02EventTests
{
    public static IEnumerable<object[]> Cards => Ids.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(Cards))]
    public void EveryEventValidatesCostModuleOwnerHandAndWindowWithoutMutation(string card)
    {
        var owner = CardCatalog.All[card].Side;
        var state = State(owner);
        var payload = Cost(state, card, CardCatalog.All[card].EventType);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        var before = StateCodec.Write(state);
        Assert.True(new PreviewService(LordsFixture.Engine()).Preview(state, owner, command).Legal);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, TurnFlow.Other(owner), command));
        payload.CardCost = DieFace.Army;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = CardCatalog.All[card].EventType;
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Configuration.Kings = true;
        state.Player(owner).Hand.Remove(card);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Player(owner).Hand.Add(card);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Available;
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }

    [Theory]
    [InlineData(Dain, SovereignCatalog.Dain)]
    [InlineData(Thranduil, SovereignCatalog.Thranduil)]
    public void NationalCardsRemoveCorruptedRulerWithoutRecruitingOrDrawing(string card, string sovereign)
    {
        var state = State();
        state.Player(Side.Shadow).Table.Add(Welcome);
        Tile(state, sovereign, HuntTests.Three);
        Tile(state, sovereign);
        Assert.Equal(SovereignStatus.Corrupted, Sovereign(state, sovereign).Status);
        state = Play(state, card);
        Assert.Equal(SovereignStatus.Removed, Sovereign(state, sovereign).Status);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    [Theory]
    [InlineData(Dain, SovereignCatalog.Dain, false)]
    [InlineData(Thranduil, SovereignCatalog.Thranduil, false)]
    [InlineData(Dain, SovereignCatalog.Dain, true)]
    [InlineData(Thranduil, SovereignCatalog.Thranduil, true)]
    public void NationalRandomRemovalExcludesKingsAndStillDrawsUnderWelcome(string card, string sovereign, bool welcome)
    {
        var state = State();
        Tile(state, sovereign);
        Tile(state, sovereign, KingsTiles.One);
        if (welcome)
            state.Player(Side.Shadow).Table.Add(Welcome);
        state = Choose(Play(state, card), "corruption", 0);
        Assert.Equal(welcome ? 2 : 1, Sovereign(state, sovereign).Tiles.Count);
        Assert.Contains(KingsTiles.One, Sovereign(state, sovereign).Tiles);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.Equal(welcome ? 0 : 1, state.RandomSamples.Count);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData(Dain)]
    [InlineData(Thranduil)]
    public void NationalRemovalWithNoHiddenTileStillDrawsWithoutRandomness(string card)
    {
        var state = Choose(Play(State(), card), "corruption");
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.Empty(state.RandomSamples);
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void DainRecruitsUnitAndLeaderTogetherThenDraws(UnitKind kind)
    {
        var state = Choose(Play(State(), Dain), "recruit");
        var option = new EffectOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(2, option.Maximum);
        var ids = option.EligibleIds.Where(id => state.Units.Single(u => u.Id == id).Kind is UnitKind.Leader ||
            state.Units.Single(u => u.Id == id).Kind == kind).ToArray();
        state = Do(state, CommandKind.ChooseEffect, new()
        {
            Placements = ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = "erebor" }).ToList()
        });
        Assert.All(ids, id => Assert.Equal("erebor", state.Units.Single(u => u.Id == id).Location));
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void ThranduilRecruitsSelectedUnitInWoodlandRealmThenDraws(UnitKind kind)
    {
        var state = Choose(Play(State(), Thranduil), "recruit");
        var id = state.Pending!.PrivateChoices.First(id => state.Units.Single(u => u.Id == id).Kind == kind);
        state = Choose(state, id);
        Assert.Equal("woodland-realm", state.Units.Single(u => u.Id == id).Location);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData(Dain, "erebor")]
    [InlineData(Thranduil, "woodland-realm")]
    public void OccupiedRecruitDestinationDoesNotChangeChosenBranchOrPreventDraw(string card, string region)
    {
        var state = State();
        state.Control[region] = Side.Shadow;
        state.Units.Single(u => u.Id == (card == Dain ? SovereignCatalog.Dain : SovereignCatalog.Thranduil)).Location = "dale";
        BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
        state = Choose(Play(state, card), "recruit");
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DainUsesRemainingUnitOrLeaderReserve(bool onlyLeader)
    {
        var state = State();
        BattleFixture.Put(state, "north", UnitKind.Regular, "erebor");
        foreach (var unit in state.Units.Where(u => u.Nation == "dwarves" && u.Location == "reserve" &&
            (onlyLeader ? BoardRules.IsArmyUnit(u) : u.Kind != UnitKind.Regular)))
            unit.Location = "casualties";
        state = Choose(Play(state, Dain), "recruit");
        Assert.Single(state.Events, e => e.Kind == "EventRecruited");
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }
}
