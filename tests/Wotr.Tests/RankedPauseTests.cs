using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.RankedClockTests;

namespace Wotr.Tests;

// RANK-07 (R35, docs/RANKED_PLAY.md D11): three pauses per player, either player unpauses, the same time comes back, and
// neither the game state nor the replay sees it. On a test clock and a database of their own, like the clocks.
public sealed class RankedPauseTests(ClockDatabase database) : IClassFixture<ClockDatabase>
{
    [PostgresFact]
    public async Task APauseStopsTheClockAndEitherPlayerGivesTheSameTimeBack()
    {
        await using var table = await ClockTable.Start(database.Connection);
        table.Clock.Advance(TimeSpan.FromSeconds(20));
        var left = (await Clock(table, Side.FreePeoples)).RemainingMilliseconds;
        Assert.Equal("", (await Pauses(table).Pause(table.Match, table.ShadowAccount)).Error);

        table.Clock.Advance(TimeSpan.FromMinutes(9));
        Assert.Equal(0, await table.Service.Tick());
        var paused = await Clock(table, Side.FreePeoples);
        Assert.Equal((true, Side.Shadow, left), (paused.Paused, paused.PausedBy, paused.RemainingMilliseconds));

        Assert.Equal("", (await Pauses(table).Unpause(table.Match, table.FreePeoplesAccount)).Error);
        var running = await Clock(table, Side.FreePeoples);
        Assert.Equal((false, (Side?)null, left), (running.Paused, running.PausedBy, running.RemainingMilliseconds));
        Assert.Equal(0, await table.Service.Tick());
    }

    [PostgresFact]
    public async Task AFourthPauseIsRefusedAndTheOtherPlayerKeepsTheirs()
    {
        await using var table = await ClockTable.Start(database.Connection);
        for (var pause = 0; pause < MatchPauses.PerPlayer; pause++)
        {
            Assert.Equal("", (await Pauses(table).Pause(table.Match, table.FreePeoplesAccount)).Error);
            Assert.Equal("", (await Pauses(table).Unpause(table.Match, table.FreePeoplesAccount)).Error);
        }
        Assert.Equal(MatchPauses.NoPausesLeft, (await Pauses(table).Pause(table.Match, table.FreePeoplesAccount)).Error);
        var (mine, theirs) = (await Clock(table, Side.FreePeoples), await Clock(table, Side.Shadow));
        Assert.Equal((0, 3, 3, 0), (mine.PausesLeft, mine.OpponentPausesLeft, theirs.PausesLeft, theirs.OpponentPausesLeft));
        Assert.Equal("", (await Pauses(table).Pause(table.Match, table.ShadowAccount)).Error);
    }

    [PostgresFact]
    public async Task PausingTwiceSpendsNothingAndAResumeNeedsAPause()
    {
        await using var table = await ClockTable.Start(database.Connection);
        Assert.Equal(MatchPauses.NotPaused, (await Pauses(table).Unpause(table.Match, table.FreePeoplesAccount)).Error);
        Assert.Equal("", (await Pauses(table).Pause(table.Match, table.FreePeoplesAccount)).Error);
        Assert.Equal(MatchPauses.AlreadyPaused, (await Pauses(table).Pause(table.Match, table.FreePeoplesAccount)).Error);
        Assert.Equal(MatchPauses.AlreadyPaused, (await Pauses(table).Pause(table.Match, table.ShadowAccount)).Error);
        var clock = await Clock(table, Side.FreePeoples);
        Assert.Equal((2, 3), (clock.PausesLeft, clock.OpponentPausesLeft));
    }

    // D15: a retried command that already counted keeps its receipt during a pause.
    [PostgresFact]
    public async Task DuringAPauseOnlyAConcessionGoesThrough()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var applied = await table.Command(CommandKind.ContinueFellowship);
        var first = await table.Send(applied);
        Assert.True(first.Accepted, first.Error);
        Assert.Equal("", (await Pauses(table).Pause(table.Match, table.ShadowAccount)).Error);
        var revision = (await table.State()).Revision;

        Assert.Equal(MatchClocks.PausedRefusal, (await table.Send(CommandKind.Pass)).Error);
        Assert.Equal(revision, (await table.State()).Revision);
        var retried = await table.Send(applied);
        Assert.Equal((true, first.Revision), (retried.Accepted, retried.Revision));

        var store = table.Host.Services.GetRequiredService<MatchStore>();
        var conceded = await store.Execute(RankedMatches.Concession(table.Match, Guid.NewGuid().ToString("N")), table.ShadowAccount);
        Assert.True(conceded!.Accepted, conceded.Error);
        Assert.Equal(VictoryCondition.Conceded, (await table.State()).Victory);
        Assert.Equal(MatchPauses.GameFinished, (await Pauses(table).Unpause(table.Match, table.ShadowAccount)).Error);
    }

    [PostgresFact]
    public async Task APauseEndsByItselfAfterItsLimit()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var left = (await Clock(table, Side.FreePeoples)).RemainingMilliseconds;
        Assert.Equal("", (await Pauses(table).Pause(table.Match, table.ShadowAccount)).Error);
        table.Clock.Advance(RankedOptions.Default.PauseLimit - TimeSpan.FromSeconds(1));
        await table.Service.Tick();
        Assert.True((await Clock(table, Side.FreePeoples)).Paused);
        table.Clock.Advance(TimeSpan.FromSeconds(1));
        await table.Service.Tick();
        var resumed = await Clock(table, Side.FreePeoples);
        Assert.Equal((false, left), (resumed.Paused, resumed.RemainingMilliseconds));
    }

    [PostgresFact]
    public async Task APauseReachesTheClientsWithoutARevisionAndLeavesTheReplayAlone()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var store = table.Host.Services.GetRequiredService<MatchStore>();
        var before = (await store.ReadVersion(table.Match, table.ShadowAccount, default))!;
        var frames = await table.Scalar("SELECT count(*) FROM match_frames WHERE match_id=$1");
        var state = await table.Scalar("SELECT state::text FROM matches WHERE id=$1");

        await Pauses(table).Pause(table.Match, table.ShadowAccount);
        var paused = (await store.ReadVersion(table.Match, table.ShadowAccount, default))!;
        await Pauses(table).Unpause(table.Match, table.ShadowAccount);
        var resumed = (await store.ReadVersion(table.Match, table.ShadowAccount, default))!;

        Assert.Equal((before.Revision, before.Revision), (paused.Revision, resumed.Revision));
        Assert.Equal((before.ClockVersion + 1, before.ClockVersion + 2), (paused.ClockVersion, resumed.ClockVersion));
        Assert.Equal(frames, await table.Scalar("SELECT count(*) FROM match_frames WHERE match_id=$1"));
        Assert.Equal(state, await table.Scalar("SELECT state::text FROM matches WHERE id=$1"));
    }

    // A deployment during a pause costs nobody time, and does not count towards the pause's limit.
    [PostgresFact]
    public async Task DowntimeDuringAPauseCostsNothing()
    {
        await using var table = await ClockTable.Start(database.Connection);
        await table.Service.Recover();
        var left = (await Clock(table, Side.FreePeoples)).RemainingMilliseconds;
        Assert.Equal("", (await Pauses(table).Pause(table.Match, table.ShadowAccount)).Error);
        table.Clock.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal(TimeSpan.FromMinutes(30), await table.Service.Recover());
        await table.Service.Tick();
        var clock = await Clock(table, Side.FreePeoples);
        Assert.Equal((true, left), (clock.Paused, clock.RemainingMilliseconds));
    }

    [PostgresFact]
    public async Task ThePauseEndpointsAnswerPlayersOfRankedMatchesOnly()
    {
        await using var table = await ClockTable.Start(database.Connection);
        var route = $"/api/matches/{table.Match}";
        using (var paused = await table.Client(Side.Shadow).PostAsync(route + "/pause", null))
            Assert.Equal(HttpStatusCode.NoContent, paused.StatusCode);
        Assert.Equal(MatchPauses.AlreadyPaused, await Refused(table.Client(Side.FreePeoples), route + "/pause"));
        using (var resumed = await table.Client(Side.FreePeoples).PostAsync(route + "/unpause", null))
            Assert.Equal(HttpStatusCode.NoContent, resumed.StatusCode);

        var outsider = MenuAccounts.Client(table.Host, await MenuAccounts.Register(table.Host));
        using (var refused = await outsider.PostAsync(route + "/pause", null))
            Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
        var normal = TestScenarioCatalog.Find("sh-military")!.Create(Guid.NewGuid().ToString("N"));
        await table.Host.Services.GetRequiredService<MatchStore>().Create(normal, table.FreePeoplesAccount, table.ShadowAccount);
        Assert.Equal(RankedMatches.NotRankedRefusal, await Refused(table.Client(Side.Shadow), $"/api/matches/{normal.Id}/pause"));
    }

    private static MatchPauses Pauses(ClockTable table) => table.Host.Services.GetRequiredService<MatchPauses>();

    private static async Task<MoveClockView> Clock(ClockTable table, Side viewer)
    {
        var view = await table.Client(viewer).GetFromJsonAsync<PlayerView>($"/api/matches/{table.Match}", BoardCatalog.Json);
        return Assert.IsType<MoveClockView>(view!.Clock);
    }

    private static async Task<string> Refused(HttpClient client, string route)
    {
        using var response = await client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;
    }
}
