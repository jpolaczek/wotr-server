using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsStormcrowTests
{
    internal static MatchState Choice()
    {
        var state = Ready(Stormcrow);
        BattleFixture.Put(state, "north", UnitKind.Regular, "dale");
        BattleFixture.Put(state, "north", UnitKind.Elite, "bree");
        return Do(state, CommandKind.PlayEvent, EventCost(state, Stormcrow), 0);
    }

    [Fact]
    public void RetreatsThenAddsPrivateTileThenLetsFpRemoveWholeUnitAnywhere()
    {
        var state = Choice();
        Assert.Equal(3, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.Equal(new[] { One }, Sovereign(state, SovereignCatalog.Brand).Tiles);
        Assert.Equal(-1, Sovereign(state, SovereignCatalog.Brand).LastAttemptTurn);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(2, state.Pending.PrivateChoices.Count);
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        Assert.Contains(One, Json(state, Side.Shadow));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Brand] });
        var elite = state.Units.Single(u => u.Kind == UnitKind.Elite && u.Location == "bree");
        var regularReserve = state.Units.Count(u => u.Nation == "north" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        state = Choose(state, elite.Id);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal(regularReserve, state.Units.Count(u => u.Nation == "north" && u.Kind == UnitKind.Regular && u.Location == "reserve"));
    }

    [Fact]
    public void AwakenedRulerPreventsBothTileAndLossButNotPoliticalRetreat()
    {
        var state = Ready(Stormcrow);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Brand), "dale");
        var unit = BattleFixture.Put(state, "north", UnitKind.Regular, "dale");
        state.Nations.Single(n => n.Id == "north").StepsToWar = 1;
        state = Do(state, CommandKind.PlayEvent, EventCost(state, Stormcrow));
        Assert.Equal(2, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.Empty(state.RandomSamples);
        Assert.Equal("dale", state.Units.Single(u => u.Id == unit.Id).Location);
    }

    [Theory]
    [InlineData(SovereignStatus.Corrupted)]
    [InlineData(SovereignStatus.Removed)]
    public void ImpossibleTileDoesNotPreventArmyLoss(SovereignStatus status)
    {
        var state = Ready(Stormcrow);
        Corrupt(state, SovereignCatalog.Brand);
        if (status == SovereignStatus.Removed)
            SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Brand));
        var unit = BattleFixture.Put(state, "north", UnitKind.Regular, "bree");
        state = Do(state, CommandKind.PlayEvent, EventCost(state, Stormcrow));
        Assert.Equal("casualties", state.Units.Single(u => u.Id == unit.Id).Location);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void ReplacesEyeAndSpecialDrawsAndKingsTileHasZeroCorruption()
    {
        var state = Ready(Stormcrow);
        var eye = state.HuntTiles.First(t => HuntPool.Definitions[t.Id].Value == "eye").Id;
        var special = state.HuntTiles.First(t => HuntPool.Definitions[t.Id].Pool != "standard").Id;
        Pool(state, eye, special, KingsTiles.One);
        var indices = new List<int>();
        var pool = state.HuntTiles.Where(t => t.Location == "pool").Select(t => t.Id).ToList();
        foreach (var id in new[] { eye, special, KingsTiles.One })
        {
            indices.Add(pool.IndexOf(id));
            pool.Remove(id);
        }
        state = Do(state, CommandKind.PlayEvent, EventCost(state, Stormcrow), indices.ToArray());
        Assert.Equal(new[] { KingsTiles.One }, Sovereign(state, SovereignCatalog.Brand).Tiles);
        Assert.Equal(SovereignStatus.Initial, Sovereign(state, SovereignCatalog.Brand).Status);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == eye).Location);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == special).Location);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Contains(KingsTiles.One, Json(state, Side.FreePeoples));
    }

    [Fact]
    public void RequiresCompanionOrFellowshipInsideChosenNonWarNation()
    {
        var state = Ready(Stormcrow);
        var payload = EventCost(state, Stormcrow);
        payload.NationId = "rohan";
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        Companion(state, Gimli, "edoras");
        Assert.Contains("rohan", new StormcrowCondition().NationTargets(state, Side.Shadow));
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 0;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }
}
