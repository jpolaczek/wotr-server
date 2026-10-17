using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.HuntTests;

namespace Wotr.Tests;

// UI-141: the public composition of the Hunt pool, grouped by face, without the drawing order or hidden tiles.
public class HuntPoolProjectionTests
{
    private const string SpecialDie = "base.hunt.special-sh.die.0.1";

    [Fact]
    public void BasePoolListsTheSixteenStandardTilesByFaceForBothSides()
    {
        var state = TurnTests.Actions();
        var view = Views().For(state, Side.FreePeoples).HuntPool;
        Assert.Equal(16, view.Count);
        Assert.Equal(0, view.Unseen);
        Assert.Equal(16, view.Tiles.Sum(t => t.Count));
        Assert.Equal(("eye", 4), (view.Tiles[0].Value, view.Tiles[0].Count));
        Assert.All(view.Tiles, tile => Assert.Equal(("standard", "base"), (tile.Pool, tile.Source)));
        Assert.Equal(7, view.Tiles.Count);
        Assert.Empty(view.Prepared);
        Assert.Equal(Json(view), Json(Views().For(state, Side.Shadow).HuntPool));
    }

    [Fact]
    public void ADrawnTileLeavesItsFaceAndTheListKeepsNoDrawingOrder()
    {
        var state = TurnTests.Actions();
        var before = Views().For(state, Side.Shadow).HuntPool;
        state.HuntTiles.Single(t => t.Id == Three).Location = "drawn";
        var after = Views().For(state, Side.Shadow).HuntPool;
        Assert.Equal(15, after.Count);
        Assert.Equal(before.Tiles.Single(t => t.Value == "3").Count - 1, after.Tiles.Single(t => t.Value == "3").Count);
        state.HuntTiles.Reverse();
        Assert.Equal(Json(after), Json(Views().For(state, Side.Shadow).HuntPool));
    }

    [Fact]
    public void PreparedSpecialTilesWaitForMordorThenJoinThePool()
    {
        var state = TurnTests.Actions();
        HuntPool.PrepareSpecial(state, SpecialDie);
        var view = Views().For(state, Side.FreePeoples).HuntPool;
        var prepared = Assert.Single(view.Prepared);
        Assert.Equal(("special-sh", "die", true), (prepared.Pool, prepared.Value, prepared.Stop));
        Assert.Equal(16, view.Count);
        state.HuntTiles.Single(t => t.Id == SpecialDie).Location = "pool";
        view = Views().For(state, Side.FreePeoples).HuntPool;
        Assert.Empty(view.Prepared);
        Assert.Equal("special-sh", view.Tiles[^1].Pool);
    }

    [Fact]
    public void AHiddenCorruptionTileStaysInTheListOfTheSideThatCannotReadIt()
    {
        var state = KingsFixture.Setup();
        var before = LordsFixture.Views().For(state, Side.FreePeoples).HuntPool;
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Dain);
        sovereign.Tiles.Add(Three);
        state.HuntTiles.Single(t => t.Id == Three).Location = sovereign.Id;
        var free = LordsFixture.Views().For(state, Side.FreePeoples).HuntPool;
        Assert.Equal(Json(before.Tiles), Json(free.Tiles));
        Assert.Equal((before.Count - 1, 1), (free.Count, free.Unseen));
        var shadow = LordsFixture.Views().For(state, Side.Shadow).HuntPool;
        Assert.Equal((before.Count - 1, 0), (shadow.Count, shadow.Unseen));
        Assert.Equal(before.Tiles.Single(t => t.Value == "3").Count - 1, shadow.Tiles.Single(t => t.Value == "3").Count);
        state.Configuration.VisibleCorruption = true;
        Assert.Equal(0, LordsFixture.Views().For(state, Side.FreePeoples).HuntPool.Unseen);
    }

    [Fact]
    public void AnUnrevealedCorruptionDrawIsUnseenOnlyByTheFreePeoples()
    {
        var state = KingsFixture.Attempt(2, SovereignCatalog.Dain, Three, KingsCorruptionTests.One);
        var drawn = CorruptionRuntime.Current(state)!.Drawn.Count;
        Assert.True(drawn > 0);
        var free = LordsFixture.Views().For(state, Side.FreePeoples).HuntPool;
        Assert.Equal(drawn, free.Unseen);
        Assert.Equal(2, free.Tiles.Sum(t => t.Count));
        Assert.Equal(0, LordsFixture.Views().For(state, Side.Shadow).HuntPool.Unseen);
    }

    [Fact]
    public void TheDrawnTileCarriesItsPrintedFace()
    {
        var state = MordorTests.OnTrack(4, 0);
        Pool(state, Three);
        state = Move(state, new SequenceRandom(0));
        var face = Views().For(state, Side.Shadow).Hunt!.Face!;
        Assert.Equal(("standard", "3", false, false, "base"), (face.Pool, face.Value, face.Reveal, face.Stop, face.Source));
        Assert.Equal("3", Assert.Single(state.HuntReport!.Entries, e => e.Kind == HuntReportKind.TileDrawn).Hunt!.Face!.Value);
    }

    private static string Json(object value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
}
