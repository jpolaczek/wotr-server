using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// OBS-02 (OB04): an observer of a match's room reads the board, history, battle reports and updates of the side
// they choose — its hand included — but no options or decision tokens; players and outsiders get nothing through
// this API, and no observer request changes the match. Losing the place closes an open update socket too.
public sealed class MatchObserverTests
{
    [PostgresFact]
    public async Task AnObserverSeesEitherSideWithItsHandButNoOptionsOrTokens()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ObservedTable.Start(host);

        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var player = await table.Player(side).GetFromJsonAsync<PlayerView>(Match(table.Match), BoardCatalog.Json);
            var observed = await table.Observer.GetFromJsonAsync<PlayerView>(Observe(table.Match, side), BoardCatalog.Json);

            Assert.Equal(side, observed!.You);
            Assert.Equal(player!.Hand, observed.Hand);
            Assert.Equal(Json(player.History), Json(observed.History));
            Assert.Equal(Json(player.Players), Json(observed.Players));
            Assert.Equal(player.DecisionOwner, observed.DecisionOwner);
            Assert.Empty(observed.Options);
            Assert.Empty(observed.Availability);
            Assert.Null(observed.AutoDecision.Policy);
            Assert.Equal(player.Decision?.Kind, observed.Decision?.Kind);
            Assert.True(observed.Decision is null or { Id: "", Choices.Count: 0 });
        }
        var freePeoples = await table.Observer.GetFromJsonAsync<PlayerView>(Observe(table.Match, Side.FreePeoples), BoardCatalog.Json);
        var shadow = await table.Observer.GetFromJsonAsync<PlayerView>(Observe(table.Match, Side.Shadow), BoardCatalog.Json);
        Assert.NotEmpty(freePeoples!.Hand);
        Assert.Empty(freePeoples.Hand.Intersect(shadow!.Hand));
        var owner = table.Player((await table.Player(Side.FreePeoples).GetFromJsonAsync<PlayerView>(Match(table.Match), BoardCatalog.Json))!.DecisionOwner!.Value);
        Assert.NotEmpty((await owner.GetFromJsonAsync<PlayerView>(Match(table.Match), BoardCatalog.Json))!.Decision!.Id);
    }

    [PostgresFact]
    public async Task TheHistoryOfAPerspectiveIsThatSidesHistory()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ObservedTable.Start(host);

        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var player = await table.Player(side).GetStringAsync($"{Match(table.Match)}/actions?limit=50");
            var observed = await table.Observer.GetStringAsync($"/api/observe/{table.Match}/actions?limit=50&perspective={side}");
            Assert.Equal(player, observed);
        }
    }

    [PostgresFact]
    public async Task PlayersAndOutsidersReadNothingThroughTheObserverApi()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ObservedTable.Start(host);
        using var outsider = MenuAccounts.Client(host, await MenuAccounts.Register(host));

        foreach (var client in new[] { table.Player(Side.FreePeoples), table.Player(Side.Shadow), outsider })
        {
            foreach (var route in ObserverRoutes(table.Match))
            {
                using var response = await client.GetAsync(route);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        using var invalid = await table.Observer.GetAsync($"/api/observe/{table.Match}?perspective=Neutral");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var socket = host.Server.CreateWebSocketClient();
        socket.ConfigureRequest = request => request.Headers.Authorization = "Bearer " + table.FreePeoplesSession.Token;
        await Assert.ThrowsAnyAsync<Exception>(() => socket.ConnectAsync(Updates(table.Match), CancellationToken.None));
    }

    [PostgresFact]
    public async Task AnObserverChangesNothingThroughThePlayersEndpoints()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ObservedTable.Start(host);
        var before = (await table.Player(Side.FreePeoples).GetFromJsonAsync<PlayerView>(Match(table.Match), BoardCatalog.Json))!;
        var command = new GameCommand { MatchId = table.Match, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = before.Revision };
        var route = Match(table.Match);

        var refused = new List<HttpResponseMessage>
        {
            await table.Observer.GetAsync(route),
            await table.Observer.PostAsJsonAsync(route + "/commands", command, BoardCatalog.Json),
            await table.Observer.PostAsJsonAsync(route + "/preview", command, BoardCatalog.Json),
            await table.Observer.GetAsync(route + "/receipts/" + command.CommandId),
            await table.Observer.PutAsJsonAsync(route + "/seen", new SeenRevisionRequest { Revision = before.Revision }, BoardCatalog.Json),
            await table.Observer.GetAsync(route + "/activity"),
            await table.Observer.GetAsync(route + "/actions"),
            await table.Observer.DeleteAsync(route)
        };

        Assert.All(refused, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        refused.ForEach(response => response.Dispose());
        var after = (await table.Player(Side.FreePeoples).GetFromJsonAsync<PlayerView>(route, BoardCatalog.Json))!;
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(Json(before), Json(after));
    }

    [PostgresFact]
    public async Task ALapsedObserverLosesTheBoardAndItsOpenSocket()
    {
        using var host = new AccountHost(extra: new Dictionary<string, string?>(TestBudgets.Stress)
        {
            [ObserverOptions.LeaseKey] = "4"
        });
        var table = await ObservedTable.Start(host);
        var client = host.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = "Bearer " + table.ObserverSession.Token;
        // The heartbeat a watching client sends: setting the table up may take longer than the lease on a busy machine.
        using (var presence = await table.Observer.PostAsync($"/api/rooms/{table.Room}/presence", null))
            Assert.Equal(HttpStatusCode.NoContent, presence.StatusCode);
        using var socket = await client.ConnectAsync(Updates(table.Match), CancellationToken.None);

        var buffer = new byte[1024];
        var first = await socket.ReceiveAsync(buffer, CancellationToken.None);
        using var update = JsonDocument.Parse(Encoding.UTF8.GetString(buffer, 0, first.Count));
        Assert.Equal(0, update.RootElement.GetProperty("privateVersion").GetInt64());

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        Assert.True(await Closes(socket, timeout.Token), "The socket closes once the lease lapsed.");
        using var gone = await table.Observer.GetAsync(Observe(table.Match, Side.FreePeoples));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [PostgresFact]
    public async Task AnObserverWhoLeftReadsNothing()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await ObservedTable.Start(host);

        using (var left = await table.Observer.PostAsync($"/api/rooms/{table.Room}/leave", null))
            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);

        using var gone = await table.Observer.GetAsync(Observe(table.Match, Side.Shadow));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    private static IEnumerable<string> ObserverRoutes(string match) =>
    [
        Observe(match, Side.FreePeoples),
        Observe(match, Side.Shadow),
        $"/api/observe/{match}/actions?perspective=Shadow",
        $"/api/observe/{match}/actions/no-such-action?perspective=Shadow",
        $"/api/observe/{match}/battles/no-such-battle?perspective=Shadow"
    ];

    private static async Task<bool> Closes(WebSocket socket, CancellationToken cancellation)
    {
        var buffer = new byte[1024];
        try
        {
            while (true)
            {
                var message = await socket.ReceiveAsync(buffer, cancellation);
                if (message.MessageType == WebSocketMessageType.Close)
                    return true;
            }
        }
        catch (Exception error) when (error is WebSocketException or IOException)
        {
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
    private static string Match(string match) => "/api/matches/" + match;
    private static string Observe(string match, Side side) => $"/api/observe/{match}?perspective={side}";
    private static Uri Updates(string match) => new($"wss://localhost/api/observe/{match}/updates");

    private sealed class ObservedTable
    {
        public required string Room { get; init; }
        public required string Match { get; init; }
        public required HttpClient FreePeoples { get; init; }
        public required HttpClient Shadow { get; init; }
        public required HttpClient Observer { get; init; }
        public required SessionView FreePeoplesSession { get; init; }
        public required SessionView ObserverSession { get; init; }

        public HttpClient Player(Side side) => side == Side.FreePeoples ? FreePeoples : Shadow;

        public static async Task<ObservedTable> Start(AccountHost host)
        {
            var (alice, bob, carol) = (await MenuAccounts.Register(host), await MenuAccounts.Register(host), await MenuAccounts.Register(host));
            var rooms = host.Services.GetRequiredService<RoomStore>();
            var room = await rooms.Create(alice.AccountId, Side.FreePeoples);
            await rooms.Join(room.JoinCode, bob.AccountId);
            await rooms.Join(room.JoinCode, carol.AccountId, RoomRole.Observer);
            await rooms.Confirm(room.Id, alice.AccountId, 1);
            await rooms.Confirm(room.Id, bob.AccountId, 1);
            var started = await rooms.Start(room.Id, alice.AccountId, 1);
            return new()
            {
                Room = room.Id,
                Match = started.Room!.MatchId,
                FreePeoples = MenuAccounts.Client(host, alice),
                Shadow = MenuAccounts.Client(host, bob),
                Observer = MenuAccounts.Client(host, carol),
                FreePeoplesSession = alice,
                ObserverSession = carol
            };
        }
    }
}
