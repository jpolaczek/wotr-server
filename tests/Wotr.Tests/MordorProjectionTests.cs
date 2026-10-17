using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class MordorProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActivityIsPublicSurvivesSavedStateAndResetsWithTheTurn(bool attempted)
    {
        var state = MordorTests.OnTrack();
        if (attempted)
            MordorRules.Attempt(state);
        state = StateCodec.Copy(state);
        AssertActivity(state, attempted);
        state.Turn++;
        AssertActivity(state, false);
        state.Fellowship.MordorStep = null;
        state.Fellowship.RegionId = "morannon";
        AssertActivity(state, null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicDtoRoundTripsWithoutInferringMissingActivity(bool? attempted)
    {
        var view = new FellowshipView { MordorStep = 2, MordorAttemptedThisTurn = attempted };
        var json = JsonSerializer.Serialize(view, BoardCatalog.Json);
        Assert.Equal(attempted, JsonSerializer.Deserialize<FellowshipView>(json, BoardCatalog.Json)!.MordorAttemptedThisTurn);
        var legacy = JsonSerializer.Deserialize<FellowshipView>("{\"mordorStep\":2}", BoardCatalog.Json)!;
        Assert.Equal(2, legacy.MordorStep);
        Assert.Null(legacy.MordorAttemptedThisTurn);
    }

    internal static void AssertActivity(MatchState state, bool? expected)
    {
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var view = HuntTests.Views().For(state, side);
            Assert.Equal(expected, view.Fellowship.MordorAttemptedThisTurn);
        }
    }
}
