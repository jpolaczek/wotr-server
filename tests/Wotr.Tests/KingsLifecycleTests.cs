using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsLifecycleTests
{
    [Theory]
    [InlineData(SovereignCatalog.Brand, 2)]
    [InlineData(SovereignCatalog.Dain, 4)]
    [InlineData(SovereignCatalog.Denethor, 3)]
    [InlineData(SovereignCatalog.Theoden, 3)]
    [InlineData(SovereignCatalog.Thranduil, 4)]
    public void CorruptionOccursAtThePrintedResistanceAndNeverBefore(string id, int resistance)
    {
        var state = Actions();
        var sovereign = Sovereign(state, id);
        var before = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == (resistance - 1).ToString() && !KingsTiles.IsKings(t.Id)).Id;
        var one = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == "1" && !KingsTiles.IsKings(t.Id) && t.Id != before).Id;
        CorruptionRules.Attach(state, sovereign, before);
        Assert.Equal(SovereignStatus.Initial, sovereign.Status);
        CorruptionRules.Attach(state, sovereign, one);
        Assert.Equal(SovereignStatus.Corrupted, sovereign.Status);
        Assert.Equal(SovereignCatalog.All[id].Home, sovereign.CorruptedRegion);
    }
    [Fact]
    public void CorruptingRevealsEverythingDiscardsTheHighestRevealTieAndReturnsKings()
    {
        var state = Actions();
        var sovereign = Sovereign(state, SovereignCatalog.Dain);
        var plain = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == "2" && !t.Reveal && !KingsTiles.IsKings(t.Id)).Id;
        var reveal = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == "2" && t.Reveal).Id;
        state.Units.Single(u => u.Id == sovereign.Id).Location = "dale";
        CorruptionRules.Attach(state, sovereign, KingsTiles.Two);
        CorruptionRules.Attach(state, sovereign, plain);
        Assert.Equal(SovereignStatus.Initial, sovereign.Status);
        CorruptionRules.Attach(state, sovereign, reveal);
        Assert.Equal(SovereignStatus.Corrupted, sovereign.Status);
        Assert.Equal("erebor", sovereign.CorruptedRegion);
        Assert.Equal("spent", state.HuntTiles.Single(t => t.Id == reveal).Location);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == plain).Location);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == KingsTiles.Two).Location);
        Assert.Empty(sovereign.Tiles);
        foreach (var id in new[] { plain, reveal, KingsTiles.Two })
            Assert.Contains(state.Events, e => e.Kind == "CorruptionTileRevealed" && e.Text.Contains(id));
        Assert.DoesNotContain(state.Units, u => u.Id == sovereign.Id);
        Assert.DoesNotContain(state.Characters, c => c.Id == sovereign.Id);
        Assert.Empty(SovereignAwakening.Destinations(state, sovereign));
    }

    [Theory]
    [InlineData(SovereignCatalog.Brand)]
    [InlineData(SovereignCatalog.Dain)]
    [InlineData(SovereignCatalog.Denethor)]
    [InlineData(SovereignCatalog.Theoden)]
    [InlineData(SovereignCatalog.Thranduil)]
    public void RemovingCorruptedRequiresAColocatedAppropriateCompanionAndACharacterDie(string id)
    {
        var state = Actions();
        var sovereign = Sovereign(state, id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = SovereignCatalog.All[id].Home;
        state.Units.RemoveAll(u => u.Id == id);
        var cost = Die(state);
        cost.Face = DieFace.Character;
        var payload = new CommandPayload { CharacterId = id, DieId = cost.Id };
        LordsFixture.Reject(state, CommandKind.RemoveCorruptedSovereign, payload);
        var companion = new CharacterState { Id = CharacterRules.Grey, Location = sovereign.CorruptedRegion };
        CharacterLifecycle.ApplyVersion(companion, CharacterCatalog.All[CharacterRules.Grey]);
        state.Characters.Add(companion);
        companion.NationIcon = "";
        LordsFixture.Reject(state, CommandKind.RemoveCorruptedSovereign, payload);
        companion.NationIcon = SovereignCatalog.All[id].Nation;
        cost.Face = DieFace.Muster;
        LordsFixture.Reject(state, CommandKind.RemoveCorruptedSovereign, payload);
        cost.Face = DieFace.Character;
        companion.Location = "rivendell";
        LordsFixture.Reject(state, CommandKind.RemoveCorruptedSovereign, payload);
        companion.Location = sovereign.CorruptedRegion;
        companion.NationIcon = "free-peoples";
        state = Do(StateCodec.Copy(state), CommandKind.RemoveCorruptedSovereign, payload);
        Assert.Equal(SovereignStatus.Removed, Sovereign(state, id).Status);
        Assert.False(KingsWeaknesses.Active(state, id));
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.Id).Location);
    }

    [Fact]
    public void HiddenTilesHaveIdenticalFreePeoplesJsonDespiteDifferentValues()
    {
        var one = Actions();
        var two = StateCodec.Copy(one);
        var tile = HuntPool.Definitions.Values.First(t => t.Pool == "standard" && t.Value == "1" && !KingsTiles.IsKings(t.Id)).Id;
        CorruptionRules.Attach(one, Sovereign(one, SovereignCatalog.Dain), tile);
        CorruptionRules.Attach(two, Sovereign(two, SovereignCatalog.Dain), HuntTests.Three);
        Assert.Equal(Json(one, Side.FreePeoples), Json(two, Side.FreePeoples));
        Assert.NotEqual(Json(one, Side.Shadow), Json(two, Side.Shadow));
    }

    [Fact]
    public void AwakenedDeathEndsItsCompanionStateAndFateOmitsBothNorthernSovereigns()
    {
        var state = Actions();
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Brand), "dale");
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == SovereignCatalog.Brand));
        Assert.Equal(SovereignStatus.Removed, Sovereign(state, SovereignCatalog.Brand).Status);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == SovereignCatalog.Brand).Location);
        var fate = MatchFactory.Create("fate-kings", new SequenceRandom(new int[KingsFixture.SetupSamples]), new() { Kings = true, Fate = true });
        KingsSetup.Initialize(fate);
        Assert.Equal(3, fate.Kings!.Sovereigns.Count);
        Assert.DoesNotContain(fate.Kings.Sovereigns, s => s.Id is SovereignCatalog.Dain or SovereignCatalog.Brand);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing-region")]
    public void MalformedRecruitmentTargetReturnsASafeErrorBeforeEvaluatingWeaknesses(string? region)
    {
        var state = Actions();
        var elite = state.Units.First(u => u.Location == "reserve" && u.Kind == UnitKind.Elite && BoardRules.Owner(u) == Side.FreePeoples);
        LordsFixture.Reject(state, CommandKind.Recruit, new()
        {
            DieId = Die(state).Id, Placements = [new() { UnitId = elite.Id, RegionId = region! }]
        });
    }
}
