using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// OBS-06 (OB09): a full room under the production budgets — two players and sixteen observers, each observer with
// an open update socket and reading both sides in turn. The seventeenth observer is refused without a change, the
// players keep deciding at the pace of a game without an audience, every observer sees the last revision, a player
// reconnects, and the update streams and socket slots come back when the sessions close.
public sealed class ObserverAcceptanceTests(ITestOutputHelper output)
{
    private const int Moves = 12;

    [PostgresFact]
    public async Task TwoPlayersPlayOnWithSixteenObserversWatchingAndTheResourcesComeBack()
    {
        using var host = new AccountHost();
        var hub = host.Services.GetRequiredService<RevisionHub>();
        var budget = host.Services.GetRequiredService<UpdateSocketBudget>();
        var alone = await Table.Start(host, 0);
        var baseline = await alone.Play(Moves);

        var full = await Table.Start(host, RoomLimits);
        var refused = await full.JoinSeventeenth();
        Assert.Equal("ObserversFull", refused.Error);
        Assert.Equal(16, refused.Observers);
        var sockets = await Task.WhenAll(full.Observers.Select(o => Socket.Open(host, o.Session.Token, $"/api/observe/{full.Match}/updates")));
        var player = await Socket.Open(host, full.FreePeoples.Session.Token, $"/api/matches/{full.Match}/updates");
        Assert.Equal(17, hub.Watchers(full.Match));

        var watched = await full.Play(Moves, full.ReadBothSides);
        var last = await full.Revision();
        foreach (var socket in sockets)
            await socket.Until(last);
        output.WriteLine($"Command time without observers: {Summary(baseline)}; with 16 observers: {Summary(watched)}.");
        Assert.True(Percentile(watched, 0.95) < Percentile(baseline, 0.95) * 3 + 250,
            "Sixteen observers must not slow the players' commands down materially.");

        await player.DisposeAsync();
        var back = await Socket.Open(host, full.FreePeoples.Session.Token, $"/api/matches/{full.Match}/updates");
        await back.Until(last);
        await full.Play(1);
        foreach (var socket in sockets)
            await socket.DisposeAsync();
        await back.DisposeAsync();
        await Until(() => hub.Watchers(full.Match) == 0 && budget.Accounts == 0);
    }

    private static int RoomLimits => ObserverOptions.Default.Limit;

    private static string Summary(List<double> times) =>
        $"median {Percentile(times, 0.5):F0} ms, p95 {Percentile(times, 0.95):F0} ms over {times.Count} commands";

    private static double Percentile(List<double> times, double share)
    {
        var sorted = times.Order().ToList();
        return sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(share * sorted.Count) - 1)];
    }

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Resources did not come back in time.");
            await Task.Delay(50);
        }
    }

    private sealed record Member(SessionView Session, HttpClient Client);

    // A started match of two players with the given audience; moves come from the demo bot for whichever side
    // decides and go through the players' HTTP endpoint.
    private sealed class Table
    {
        public required string Room { get; init; }
        public required string Match { get; init; }
        public required Member FreePeoples { get; init; }
        public required Member Shadow { get; init; }
        public required List<Member> Observers { get; init; }
        public required AccountHost Host { get; init; }

        public static async Task<Table> Start(AccountHost host, int observers)
        {
            var rooms = host.Services.GetRequiredService<RoomStore>();
            var (fp, sh) = (await Member(host), await Member(host));
            var room = await rooms.Create(fp.Session.AccountId, new CreateRoomRequest { Name = "Full house", Visibility = RoomVisibility.Public });
            await rooms.JoinPublic(room.Id, sh.Session.AccountId);
            var audience = new List<Member>();
            for (var i = 0; i < observers; i++)
                audience.Add(await Member(host));
            await Task.WhenAll(audience.Select(o => rooms.JoinPublic(room.Id, o.Session.AccountId, RoomRole.Observer)));
            var version = (await rooms.Read(room.Id, fp.Session.AccountId))!.Version;
            await rooms.Confirm(room.Id, fp.Session.AccountId, version);
            await rooms.Confirm(room.Id, sh.Session.AccountId, version);
            var started = await rooms.Start(room.Id, fp.Session.AccountId, version);
            return new Table
            {
                Room = room.Id, Match = started.Room!.MatchId, FreePeoples = fp, Shadow = sh, Observers = audience, Host = host
            };
        }

        private static async Task<Member> Member(AccountHost host)
        {
            var session = await MenuAccounts.Register(host);
            return new Member(session, MenuAccounts.Client(host, session));
        }

        // The refused seventeenth observer and the audience the room still has afterwards.
        public async Task<(string Error, int Observers)> JoinSeventeenth()
        {
            var late = await Member(Host);
            using var response = await late.Client.PostAsync($"/api/rooms/{Room}/join?role=Observer", null);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;
            var room = await FreePeoples.Client.GetFromJsonAsync<RoomView>($"/api/rooms/{Room}", BoardCatalog.Json);
            return (error, room!.Observers.Count);
        }

        // Plays the moves and returns how long each command took; between moves the audience may read.
        public async Task<List<double>> Play(int moves, Func<Task>? between = null)
        {
            var store = Host.Services.GetRequiredService<MatchStore>();
            var bot = Host.Services.GetRequiredService<DemoBot>();
            var times = new List<double>();
            for (var i = 0; i < moves; i++)
            {
                var state = (await store.Read(Match, FreePeoples.Session.AccountId))!.State;
                Assert.Null(state.Winner);
                var side = state.Pending!.Owner;
                var command = bot.Next(state, side)!;
                command.CommandId = Guid.NewGuid().ToString("N");
                var client = side == Side.FreePeoples ? FreePeoples.Client : Shadow.Client;
                var clock = Stopwatch.StartNew();
                using var response = await client.PostAsJsonAsync($"/api/matches/{Match}/commands", command, BoardCatalog.Json);
                times.Add(clock.Elapsed.TotalMilliseconds);
                var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
                Assert.True(receipt.Accepted, receipt.Error);
                if (between is not null)
                    await between();
            }
            return times;
        }

        // Every observer reads the board of one side and then the other, as a change of perspective does.
        public async Task ReadBothSides()
        {
            await Task.WhenAll(Observers.Select(async (observer, index) =>
            {
                foreach (var side in index % 2 == 0 ? new[] { Side.FreePeoples, Side.Shadow } : new[] { Side.Shadow, Side.FreePeoples })
                {
                    var view = await observer.Client.GetFromJsonAsync<PlayerView>($"/api/observe/{Match}?perspective={side}", BoardCatalog.Json);
                    Assert.Equal(side, view!.You);
                    Assert.Empty(view.Options);
                }
            }));
        }

        public async Task<long> Revision() =>
            (await FreePeoples.Client.GetFromJsonAsync<PlayerView>($"/api/matches/{Match}", BoardCatalog.Json))!.Revision;
    }

    // One update socket that remembers the newest revision it was told about.
    private sealed class Socket : IAsyncDisposable
    {
        private readonly WebSocket socket;
        private readonly CancellationTokenSource lifetime = new();
        private readonly Task reading;
        private long revision = -1;

        private Socket(WebSocket socket)
        {
            this.socket = socket;
            reading = Read();
        }

        public static async Task<Socket> Open(AccountHost host, string token, string route)
        {
            var client = host.Server.CreateWebSocketClient();
            client.ConfigureRequest = request => request.Headers.Authorization = "Bearer " + token;
            return new Socket(await client.ConnectAsync(new Uri("wss://localhost" + route), CancellationToken.None));
        }

        public async Task Until(long expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (Interlocked.Read(ref revision) < expected)
            {
                Assert.True(DateTime.UtcNow < deadline, $"The socket did not reach revision {expected}.");
                await Task.Delay(50);
            }
        }

        private async Task Read()
        {
            var buffer = new byte[1024];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var message = await socket.ReceiveAsync(buffer, lifetime.Token);
                    if (message.MessageType == WebSocketMessageType.Close)
                        return;
                    using var update = JsonDocument.Parse(Encoding.UTF8.GetString(buffer, 0, message.Count));
                    Interlocked.Exchange(ref revision, update.RootElement.GetProperty("revision").GetInt64());
                }
            }
            catch (Exception error) when (error is OperationCanceledException or WebSocketException or IOException or ObjectDisposedException) { }
        }

        public async ValueTask DisposeAsync()
        {
            await lifetime.CancelAsync();
            socket.Abort();
            await reading;
            socket.Dispose();
            lifetime.Dispose();
        }
    }
}
