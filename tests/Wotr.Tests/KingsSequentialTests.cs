using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsIntegrationFixture;

namespace Wotr.Tests;

public class KingsSequentialTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public void StopsWithLastTileAtEveryPositionNeverTheEarlierHigherValue(bool visible, int count)
    {
        var state = Sequential(visible);
        string[] tiles = [HuntTests.Three, KingsCorruptionTests.One, "base.hunt.standard.2.0.1"];
        for (var i = 1; i < count; i++)
            state = Draw(StateCodec.Copy(state), tiles[i]);
        if (count < 3)
        {
            var options = LordsFixture.Views().For(state, Side.Shadow).Options;
            Assert.Contains(options, o => o.Kind == CommandKind.DrawCorruptionTile);
            var stop = options.Single(o => o.Kind == CommandKind.ChooseCorruptionTile);
            Assert.Equal(new[] { tiles[count - 1] }, stop.EligibleIds);
            if (count > 1)
                LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [tiles[0]] });
            state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [tiles[count - 1]] });
        }
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(new[] { tiles[count - 1] }, KingsFixture.Sovereign(state, SovereignCatalog.Thranduil).Tiles);
        foreach (var returned in tiles.Take(count - 1))
        {
            Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == returned).Location);
            Assert.Contains(state.Events, e => e.Kind == "CorruptionTileRevealed" && e.Text.Contains(returned));
        }
        var publicTile = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns
            .Single(s => s.Id == SovereignCatalog.Thranduil).Tiles.Single();
        Assert.Equal(visible ? tiles[count - 1] : "", publicTile.TileId);
    }

    [Theory]
    [InlineData(HuntTests.Eye)]
    [InlineData("base.hunt.special-fp.0.0.1")]
    [InlineData("lords.hunt.smeagol.1")]
    public void IgnoredTilesForceRedrawWithoutAllowingFallbackOrSmeagolEntry(string ignored)
    {
        var state = Draw(Sequential(), ignored);
        var attempt = CorruptionRuntime.Current(state)!;
        Assert.Single(attempt.Drawn);
        Assert.Equal(CorruptionStage.Drawing, attempt.Stage);
        Assert.Equal(CommandKind.DrawCorruptionTile, Assert.Single(LordsFixture.Views().For(state, Side.Shadow).Options).Kind);
        Assert.Empty(LordsFixture.Views().For(state, Side.Shadow).CorruptionAttempt!.OwnChoices);
        LordsFixture.Reject(state, CommandKind.ChooseCorruptionTile, new() { Ids = [HuntTests.Three] });
        Assert.Contains(ignored, KingsFixture.Json(state, Side.FreePeoples));
        Assert.DoesNotContain(state.Characters, c => c.DefinitionId == LordsCharacters.Smeagol);
        state = Draw(StateCodec.Copy(state), KingsCorruptionTests.One);
        state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == ignored).Location);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VisibleVariantDoesNotExposeDrawnTilesBeforeAttachment(bool visible)
    {
        var state = Sequential(visible);
        // UI-141: the pool composition is public, so both worlds hold the same tiles: One waits in the pool.
        var seen = StateCodec.Copy(state);
        seen.HuntTiles.Single(t => t.Id == KingsCorruptionTests.One).Location = "pool";
        var fp = KingsFixture.Json(seen, Side.FreePeoples);
        Assert.DoesNotContain(HuntTests.Three, fp);
        Assert.Empty(LordsFixture.Views().For(state, Side.FreePeoples).Options);
        var other = StateCodec.Copy(seen);
        CorruptionRuntime.Current(other)!.Drawn = [KingsCorruptionTests.One];
        other.Pending!.PrivateChoices = [KingsCorruptionTests.One];
        other.HuntTiles.Single(t => t.Id == HuntTests.Three).Location = "pool";
        other.HuntTiles.Single(t => t.Id == KingsCorruptionTests.One).Location = "corruption-draw";
        Assert.Equal(fp, KingsFixture.Json(other, Side.FreePeoples));
        state = Draw(state, KingsTiles.Two);
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(new[] { KingsTiles.Two }, KingsFixture.Sovereign(state, SovereignCatalog.Thranduil).Tiles);
        Assert.Equal(0, KingsFixture.Sovereign(state, SovereignCatalog.Thranduil).Tiles.Sum(KingsTiles.CorruptionValue));
        Assert.DoesNotContain(state.Events, e => e.Kind == "CorruptionTileRevealed" && e.Text.Contains(HuntTests.Three));
    }

    [Fact]
    public void LastAllowedDrawAttachesImmediatelyAndPreviewNeverDraws()
    {
        var state = Sequential(eyes: 3);
        var command = BattleFixture.Command(state, CommandKind.DrawCorruptionTile);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(before, StateCodec.Write(state));
        state = Draw(state, KingsCorruptionTests.One);
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(new[] { KingsCorruptionTests.One }, KingsFixture.Sovereign(state, SovereignCatalog.Thranduil).Tiles);
        Assert.Equal(DecisionKind.Action, Sequential(eyes: 2).Pending!.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void KingsTileStopsSequentialDrawAtEveryPositionWithoutOfferingFallback(int beforeKings)
    {
        var state = Actions(new() { Kings = true, Lords = true, SequentialCorruption = true });
        KingsFixture.Eyes(state, 4);
        KingsFixture.Pool(state, beforeKings == 0 ? KingsTiles.One : HuntTests.Three);
        state = KingsFixture.Do(state, CommandKind.AttemptCorruption, new()
        {
            CharacterId = SovereignCatalog.Thranduil, DieId = CorruptionRules.Eyes(state).First().Id
        }, 0);
        if (beforeKings == 2)
            state = Draw(state, KingsCorruptionTests.One);
        if (beforeKings > 0)
            state = Draw(state, KingsTiles.One);
        Assert.Null(CorruptionRuntime.Current(state));
        Assert.Equal(new[] { KingsTiles.One }, KingsFixture.Sovereign(state, SovereignCatalog.Thranduil).Tiles);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SmeagolRedrawAlsoAppliesToOrdinaryCorruptionWithoutChangingTheFellowship(bool sequential)
    {
        var state = Actions(new() { Kings = true, Lords = true, SequentialCorruption = sequential });
        KingsFixture.Eyes(state, 2);
        KingsFixture.Pool(state, "lords.hunt.smeagol.1");
        state = KingsFixture.Do(state, CommandKind.AttemptCorruption, new()
        {
            CharacterId = SovereignCatalog.Thranduil, DieId = CorruptionRules.Eyes(state).First().Id
        }, 0);
        Assert.Equal(CorruptionStage.Drawing, CorruptionRuntime.Current(state)!.Stage);
        Assert.Empty(CorruptionRuntime.Current(state)!.Drawn);
        state = Draw(state, KingsCorruptionTests.One);
        if (!sequential)
            state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
        Assert.DoesNotContain(state.Characters, c => c.DefinitionId == LordsCharacters.Smeagol);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == "lords.hunt.smeagol.1").Location);
        Assert.Equal(CharacterRules.Strider, state.Fellowship.Guide);
    }
}
