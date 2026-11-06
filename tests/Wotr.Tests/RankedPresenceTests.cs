using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// Sockets that must stay inside a time limit; nothing else runs beside them, so a busy machine cannot fake a silence.
[CollectionDefinition(nameof(TimedSocketTests), DisableParallelization = true)]
public sealed class TimedSocketTests;

// RANK-10 (R35, docs/RANKED_PLAY.md, Reconnecting): a seated ranked player can always get back, and the opponent can see
// what is happening. Real WebSocket clients on a server whose limits run at a quarter: 2 s of silence, a 3.75 s grace.
[Collection(nameof(TimedSocketTests))]
public sealed class RankedPresenceTests(ClockDatabase database) : IClassFixture<ClockDatabase>
{
    private static readonly TimeSpan Silence = RankedOptions.SocketSilence / 4;
    private static readonly TimeSpan Grace = RankedOptions.PresenceGrace / 4;

    [PostgresFact]
    public async Task ASilentClientGoesOfflineItsSlotIsFreeAndItsReturnShows()
    {
        using var host = Host();
        var (fp, sh, match) = await Start(host);
        var budget = host.Services.GetRequiredService<UpdateSocketBudget>();
        await using var watcher = await SeatClient.Open(host, match, fp);
        await using var silent = await SeatClient.Open(host, match, sh, heartbeat: false);
        var started = DateTimeOffset.UtcNow;

        await Until(() => budget.Open(sh.AccountId) == 0);
        var closedAfter = DateTimeOffset.UtcNow - started;
        Assert.InRange(closedAfter, Silence - TimeSpan.FromMilliseconds(200), Silence * 2);
        await Until(() => watcher.LastOnline == false);
        Assert.False((await Clock(host, fp, match)).OpponentOnline);

        await using var back = await SeatClient.Open(host, match, sh);
        await Until(() => watcher.LastOnline == true, TimeSpan.FromSeconds(1));
        Assert.True((await Clock(host, fp, match)).OpponentOnline);
    }

    [PostgresFact]
    public async Task AGapUnderTheLimitChangesNothingAnyoneCanSee()
    {
        using var host = Host();
        var (fp, sh, match) = await Start(host);
        var budget = host.Services.GetRequiredService<UpdateSocketBudget>();
        await using var watcher = await SeatClient.Open(host, match, fp);
        var player = await SeatClient.Open(host, match, sh);
        await Task.Delay(Grace + TimeSpan.FromMilliseconds(100));

        player.Mute = true;
        await Task.Delay(Silence / 2);
        player.Mute = false;
        Assert.Equal((WebSocketState.Open, 1), (player.State, budget.Open(sh.AccountId)));

        // A dropped socket that returns inside the limit does not show either.
        await player.DisposeAsync();
        await Task.Delay(Silence / 4);
        await using var back = await SeatClient.Open(host, match, sh);
        await Task.Delay(Silence + TimeSpan.FromMilliseconds(200));

        Assert.True((await Clock(host, fp, match)).OpponentOnline);
        Assert.DoesNotContain(false, watcher.Seen);
    }

    [PostgresFact]
    public async Task AReturnGetsASlotAtOnceWhileDeadSocketsHoldThemAll()
    {
        using var host = Host();
        var (fp, sh, match) = await Start(host);
        var normal = await Normal(host, fp, sh);
        var budget = host.Services.GetRequiredService<UpdateSocketBudget>();
        var dead = new List<SeatClient>();
        for (var i = 0; i < 4; i++)
            dead.Add(await SeatClient.Open(host, normal, fp, heartbeat: false));
        await Assert.ThrowsAnyAsync<Exception>(() => SeatClient.Open(host, normal, fp, heartbeat: false));

        await using var back = await SeatClient.Open(host, match, fp);

        await Until(() => dead[0].Closed);
        Assert.Equal(4, budget.Open(fp.AccountId));
        Assert.False(dead[1].Closed);
        await Assert.ThrowsAnyAsync<Exception>(() => SeatClient.Open(host, normal, fp, heartbeat: false));
        foreach (var socket in dead)
            await socket.DisposeAsync();
    }

    [PostgresFact]
    public async Task ANormalMatchsSocketNeedsNoHeartbeatAndKnowsNoPresence()
    {
        using var host = Host();
        var (fp, sh, _) = await Start(host);
        var normal = await Normal(host, fp, sh);
        await using var socket = await SeatClient.Open(host, normal, fp, heartbeat: false);
        await Until(() => socket.Notices > 0);
        await Task.Delay(Silence + TimeSpan.FromMilliseconds(300));
        Assert.False(socket.Closed);
        Assert.Equal(1, host.Services.GetRequiredService<UpdateSocketBudget>().Open(fp.AccountId));
        Assert.All(socket.Seen, online => Assert.Null(online));
    }

    [PostgresFact]
    public async Task APresenceChangeAltersNoClockRevisionOrReplay()
    {
        using var host = Host();
        var (fp, sh, match) = await Start(host);
        var before = await Snapshot(match);
        await using (await SeatClient.Open(host, match, sh, heartbeat: false))
        await using (var watcher = await SeatClient.Open(host, match, fp))
            await Until(() => watcher.LastOnline == false);
        Assert.Equal(before, await Snapshot(match));
    }

    // A second device and a server start: online while either is heard, nobody flashes offline, and the clock's shift over
    // the downtime is RANK-06's (RankedClockTests).
    [PostgresFact]
    public async Task AfterARestartNobodyFlashesOfflineDuringTheGrace()
    {
        SessionView fp;
        string match;
        using (var first = Host())
            (fp, _, match) = await Start(first);
        // Its own clock: in real time a busy machine can spend the whole grace on one request.
        var clock = new TestClock(DateTimeOffset.UtcNow);
        using var second = Host(clock);
        Assert.True((await Clock(second, fp, match)).OpponentOnline);
        clock.Advance(Grace / 2);
        Assert.True((await Clock(second, fp, match)).OpponentOnline);
        clock.Advance(Grace);
        Assert.False((await Clock(second, fp, match)).OpponentOnline);
    }

    [PostgresFact]
    public async Task StartingARankedMatchExtendsBothSessionsToAtLeastAWeek()
    {
        using var host = Host();
        var fp = await MenuAccounts.Register(host);
        var sh = await MenuAccounts.Register(host);
        await Execute("UPDATE sessions SET expires_at = now() + interval '1 hour' WHERE account_id = $1", fp.AccountId);
        await using (var data = NpgsqlDataSource.Create(database.Connection))
        await using (var connection = await data.OpenConnectionAsync())
        {
            await SessionStore.Issue(connection, null, sh.AccountId, sh.Username, SessionPurpose.Admin, default);
            await SessionStore.Issue(connection, null, sh.AccountId, sh.Username, SessionPurpose.Game, default);
        }
        await Execute("UPDATE sessions SET expires_at = now() + interval '1 hour', revoked = true WHERE account_id = $1 AND purpose = 'game' AND token_hash <> $2",
            sh.AccountId, SessionStore.Hash(sh.Token));
        var thirtyDays = await Scalar("SELECT expires_at::text FROM sessions WHERE token_hash = $1", SessionStore.Hash(sh.Token));

        var start = await host.Services.GetRequiredService<RankedMatches>().Start(fp.AccountId, sh.AccountId, new(), RankedPreset.Standard);

        Assert.Equal("", start.Error);
        Assert.Equal(true, await Scalar("SELECT expires_at > now() + interval '6 days 23 hours' FROM sessions WHERE token_hash = $1", SessionStore.Hash(fp.Token)));
        Assert.Equal(thirtyDays, await Scalar("SELECT expires_at::text FROM sessions WHERE token_hash = $1", SessionStore.Hash(sh.Token)));
        Assert.Equal(true, await Scalar("SELECT bool_and(expires_at < now() + interval '9 hours') FROM sessions WHERE account_id = $1 AND (purpose = 'admin' OR revoked)", sh.AccountId));
    }

    // A required update takes effect during the match; both players stay on the old client.
    [PostgresFact]
    public async Task AnOldClientPlaysTheActiveMatchToItsEndAndIsThenToldToUpdate()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var settings = new Dictionary<string, string?>(TestBudgets.Stress);
        foreach (var setting in releases.ServerSettings())
            settings[setting.Key] = setting.Value;
        using var host = new AccountHost(database.Connection, extra: settings);
        var (fp, sh, match) = await Start(host);
        var normal = await Normal(host, fp, sh);
        var stranger = await MenuAccounts.Register(host);
        using var old = Old(host, fp);
        using var outsider = Old(host, stranger);
        using var noProtocol = ReleaseFixture.Versioned(MenuAccounts.Client(host, fp), "0.3.0", "1");
        releases.Publisher.SetMinimum("0.4.0");

        // The match's routes, finding it, its socket and signing in.
        Assert.Equal(HttpStatusCode.OK, (await old.GetAsync($"/api/matches/{match}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await old.GetAsync($"/api/matches/{match}/receipts/unknown")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await old.PostAsync($"/api/matches/{match}/pause", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await old.PostAsync($"/api/matches/{match}/unpause", null)).StatusCode);
        using (var command = await old.PostAsJsonAsync($"/api/matches/{match}/commands",
            new GameCommand { MatchId = match, CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.Pass }, BoardCatalog.Json))
            Assert.DoesNotContain(ClientCompatibility.ClientUpdateRequired, await command.Content.ReadAsStringAsync());
        var found = await old.GetFromJsonAsync<RankedSearchView>("/api/ranked/queue", BoardCatalog.Json);
        Assert.Equal((RankedSearchStatus.Matched, match), (found!.Status, found.MatchId));
        await using (var socket = await SeatClient.Open(host, match, fp, version: "0.3.0"))
            await Until(() => socket.Notices > 0);
        Assert.Equal(HttpStatusCode.OK, await SignIn(host, fp));

        // Everything else stays as a required update leaves it.
        Assert.Equal(HttpStatusCode.Conflict, (await old.GetAsync($"/api/matches/{normal}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await old.GetAsync("/api/matches")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await old.GetAsync("/api/rooms")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, await Search(old));
        Assert.Equal(HttpStatusCode.Conflict, (await outsider.GetAsync($"/api/matches/{match}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await outsider.GetAsync("/api/ranked/queue")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, await SignIn(host, stranger));
        Assert.Equal(HttpStatusCode.Unauthorized, await SignIn(host, fp, "a wrong password long enough"));
        Assert.Equal(HttpStatusCode.Conflict, (await noProtocol.GetAsync($"/api/matches/{match}")).StatusCode);

        // The game ends: the end screen still loads, and from here on the client must update.
        using var concession = await old.PostAsJsonAsync($"/api/matches/{match}/concede",
            new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, concession.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await old.GetAsync($"/api/matches/{match}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await old.GetAsync($"/api/matches/{match}/rating")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await old.GetAsync("/api/ranked/queue")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, await Search(old));
        Assert.Equal(HttpStatusCode.Conflict, await SignIn(host, fp));
        await Execute("UPDATE rating_changes SET created_at = now() - interval '2 hours' WHERE match_id = $1", match);
        Assert.Equal(HttpStatusCode.Conflict, (await old.GetAsync($"/api/matches/{match}")).StatusCode);
    }

    private AccountHost Host(TimeProvider? clock = null)
    {
        var settings = new Dictionary<string, string?>(TestBudgets.Stress) { [RankedOptions.PresenceScaleKey] = "25" };
        return new AccountHost(database.Connection, extra: settings,
            services: clock is null ? null : services => services.AddSingleton(clock));
    }

    private static async Task<(SessionView, SessionView, string)> Start(AccountHost host)
    {
        var fp = await MenuAccounts.Register(host);
        var sh = await MenuAccounts.Register(host);
        var start = await host.Services.GetRequiredService<RankedMatches>().Start(fp.AccountId, sh.AccountId, new(), RankedPreset.Long);
        Assert.Equal("", start.Error);
        return (fp, sh, start.MatchId);
    }

    private static async Task<string> Normal(AccountHost host, SessionView fp, SessionView sh)
    {
        var state = KernelTests.NewGame();
        state.Id = Guid.NewGuid().ToString("N");
        await host.Services.GetRequiredService<MatchStore>().Create(state, fp.AccountId, sh.AccountId);
        return state.Id;
    }

    private static HttpClient Old(AccountHost host, SessionView session) =>
        ReleaseFixture.Versioned(MenuAccounts.Client(host, session), "0.3.0");

    private static async Task<HttpStatusCode> SignIn(AccountHost host, SessionView session, string password = "menu test password long enough")
    {
        using var client = ReleaseFixture.Versioned(host.CreateClient(new() { BaseAddress = new Uri("https://localhost") }), "0.3.0");
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = session.Username, Password = password });
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> Search(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/ranked/queue", new RankedSearchRequest(), BoardCatalog.Json);
        return response.StatusCode;
    }

    private static async Task<MoveClockView> Clock(AccountHost host, SessionView session, string match)
    {
        using var client = MenuAccounts.Client(host, session);
        var view = await client.GetFromJsonAsync<PlayerView>($"/api/matches/{match}", BoardCatalog.Json);
        return Assert.IsType<MoveClockView>(view!.Clock);
    }

    private Task<object?> Snapshot(string match) => Scalar(
        """
        SELECT m.state::text || '|' || m.revision || '|' || c.deadline::text || '|' || c.version || '|' ||
            (SELECT count(*) FROM match_frames f WHERE f.match_id = m.id)
        FROM matches m JOIN match_clocks c ON c.match_id = m.id WHERE m.id = $1
        """, match);

    private async Task<object?> Scalar(string sql, params object[] values)
    {
        await using var data = NpgsqlDataSource.Create(database.Connection);
        await using var command = data.CreateCommand(sql);
        foreach (var value in values)
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        return await command.ExecuteScalarAsync();
    }

    private Task Execute(string sql, params object[] values) => Scalar(sql, values);

    private static async Task Until(Func<bool> condition, TimeSpan? limit = null)
    {
        var deadline = DateTimeOffset.UtcNow + (limit ?? TimeSpan.FromSeconds(12));
        while (!condition() && DateTimeOffset.UtcNow < deadline)
            await Task.Delay(20);
        Assert.True(condition());
    }

    // A real client: it reads every notice and, unless it stopped sending, sends a frame every 200 ms. Its network going
    // silent is simply no frames, with the socket left open.
    private sealed class SeatClient : IAsyncDisposable
    {
        private readonly WebSocket socket;
        private readonly CancellationTokenSource stop = new();
        private readonly List<bool?> seen = [];
        private readonly Task reading;
        private readonly Task beating;

        private SeatClient(WebSocket socket, bool heartbeat)
        {
            this.socket = socket;
            reading = Read();
            beating = heartbeat ? Beat() : Task.CompletedTask;
        }

        public bool Mute { get; set; }
        public WebSocketState State => socket.State;
        public bool Closed => reading.IsCompleted;

        public int Notices
        {
            get
            {
                lock (seen)
                    return seen.Count;
            }
        }

        public List<bool?> Seen
        {
            get
            {
                lock (seen)
                    return [.. seen];
            }
        }

        public bool? LastOnline
        {
            get
            {
                lock (seen)
                    return seen.Count == 0 ? null : seen[^1];
            }
        }

        public static async Task<SeatClient> Open(AccountHost host, string match, SessionView session, bool heartbeat = true, string? version = null)
        {
            var client = host.Server.CreateWebSocketClient();
            client.ConfigureRequest = request =>
            {
                request.Headers.Authorization = "Bearer " + session.Token;
                if (version is not null)
                {
                    request.Headers[ClientCompatibility.ClientVersionHeader] = version;
                    request.Headers[ClientCompatibility.ProtocolVersionHeader] = "2";
                }
            };
            var socket = await client.ConnectAsync(new Uri($"wss://localhost/api/matches/{match}/updates"), CancellationToken.None);
            return new(socket, heartbeat);
        }

        public async ValueTask DisposeAsync()
        {
            await stop.CancelAsync();
            socket.Abort();
            await Task.WhenAll(reading, beating);
            socket.Dispose();
            stop.Dispose();
        }

        private async Task Read()
        {
            var buffer = new byte[4096];
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var frame = await socket.ReceiveAsync(buffer, stop.Token);
                    if (frame.MessageType == WebSocketMessageType.Close)
                        return;
                    Record(Encoding.UTF8.GetString(buffer, 0, frame.Count));
                }
            }
            catch (Exception error) when (error is OperationCanceledException or WebSocketException or IOException or ObjectDisposedException) { }
        }

        private void Record(string notice)
        {
            using var document = JsonDocument.Parse(notice);
            var online = document.RootElement.GetProperty("opponentOnline");
            lock (seen)
                seen.Add(online.ValueKind == JsonValueKind.Null ? null : online.GetBoolean());
        }

        private async Task Beat()
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    if (!Mute)
                        await socket.SendAsync("{}"u8.ToArray(), WebSocketMessageType.Text, true, stop.Token);
                    await Task.Delay(200, stop.Token);
                }
            }
            catch (Exception error) when (error is OperationCanceledException or WebSocketException or IOException or ObjectDisposedException) { }
        }
    }
}

// RANK-10: the rules of presence alone, on an injected clock.
public sealed class MatchPresenceTests
{
    private static readonly TimeSpan Grace = RankedOptions.PresenceGrace;
    private static readonly TimeSpan Silence = RankedOptions.SocketSilence;
    private readonly TestClock clock = new();

    private MatchPresence New() => new(new RevisionHub(), clock, RankedOptions.Default);

    [Fact]
    public void AServerStartCountsEverySeatAsOnlineForItsGrace()
    {
        var presence = New();
        Assert.True(presence.Online("m", Side.Shadow));
        clock.Advance(Grace - TimeSpan.FromSeconds(1));
        Assert.True(presence.Online("m", Side.Shadow));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(presence.Online("m", Side.Shadow));
    }

    [Fact]
    public void ANewMatchsSeatsCountAsOnlineWhileTheirClientsOpenTheBoard()
    {
        var presence = New();
        clock.Advance(Grace);
        presence.Expect("m");
        clock.Advance(Grace - TimeSpan.FromSeconds(1));
        Assert.True(presence.Online("m", Side.FreePeoples));
        Assert.True(presence.Online("m", Side.Shadow));
        Assert.False(presence.Online("other", Side.Shadow));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(presence.Online("m", Side.Shadow));
    }

    [Fact]
    public void ASeatIsOnlineWhileEitherDeviceHasASocketAndForItsSilenceAfterTheLast()
    {
        var presence = New();
        clock.Advance(Grace);
        var first = presence.Join("m", Side.Shadow);
        var second = presence.Join("m", Side.Shadow);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True(presence.Online("m", Side.Shadow));
        Assert.False(presence.Online("m", Side.FreePeoples));

        first.Dispose();
        second.Heard();
        clock.Advance(Silence - TimeSpan.FromSeconds(1));
        Assert.True(presence.Online("m", Side.Shadow));
        second.Heard();
        second.Dispose();
        clock.Advance(Silence - TimeSpan.FromSeconds(1));
        Assert.True(presence.Online("m", Side.Shadow));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(presence.Online("m", Side.Shadow));
    }
}
