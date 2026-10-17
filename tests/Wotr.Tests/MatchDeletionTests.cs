using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// MENU-21: either player deletes a match for both, with everything stored for it; nobody else can.
public sealed class MatchDeletionTests
{
    [PostgresFact]
    public async Task EitherPlayerDeletesTheMatchWithEverythingStoredForIt()
    {
        foreach (var deletingShadow in new[] { false, true })
        {
            using var host = new AccountHost(extra: TestBudgets.Stress);
            var table = await Table.Start(host);
            await table.LeaveTraces();
            Assert.Equal(1, await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1", table.Match));
            Assert.Equal(1, await Count("SELECT count(*) FROM match_reads WHERE match_id=$1", table.Match));
            Assert.Equal(1, await Count("SELECT count(*) FROM rooms WHERE match_id=$1", table.Match));
            var deleting = deletingShadow ? table.Shadow : table.FreePeoples;
            var other = deletingShadow ? table.FreePeoples : table.Shadow;
            using var deleted = await deleting.DeleteAsync(Route(table.Match));
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            foreach (var sql in new[]
                     {
                         "SELECT count(*) FROM matches WHERE id=$1",
                         "SELECT count(*) FROM command_receipts WHERE match_id=$1",
                         "SELECT count(*) FROM match_events WHERE match_id=$1",
                         "SELECT count(*) FROM match_reads WHERE match_id=$1",
                         "SELECT count(*) FROM rooms WHERE match_id=$1"
                     })
                Assert.Equal(0, await Count(sql, table.Match));
            foreach (var player in new[] { deleting, other })
            {
                using var read = await player.GetAsync(Route(table.Match));
                Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
                var list = await player.GetFromJsonAsync<MatchListView>("/api/matches", BoardCatalog.Json);
                Assert.DoesNotContain(list!.Matches, match => match.MatchId == table.Match);
            }
            using var again = await other.DeleteAsync(Route(table.Match));
            Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        }
    }

    [PostgresFact]
    public async Task AnOutsiderCannotDeleteTheMatch()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await Table.Start(host);
        var outsider = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, outsider);
        using var refused = await client.DeleteAsync(Route(table.Match));
        Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
        using var read = await table.FreePeoples.GetAsync(Route(table.Match));
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM rooms WHERE match_id=$1", table.Match));
    }

    // The opponent's open socket wakes up at once and closes; the client then reads 404 and leaves the board.
    [PostgresFact]
    public async Task TheOpponentsOpenSocketClosesWhenTheMatchIsDeleted()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await Table.Start(host);
        var client = host.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = "Bearer " + table.ShadowSession.Token;
        using var socket = await client.ConnectAsync(new Uri($"wss://localhost/api/matches/{table.Match}/updates"), CancellationToken.None);
        var buffer = new byte[1024];
        var first = await socket.ReceiveAsync(buffer, CancellationToken.None);
        Assert.Equal(WebSocketMessageType.Text, first.MessageType);
        using var deleted = await table.FreePeoples.DeleteAsync(Route(table.Match));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        // Well inside the five-second refresh: only the published wake-up closes it this fast.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        Assert.True(await Closes(socket, timeout.Token), "The socket closes right after the deletion.");
    }

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
        // The server aborts the socket; the test host reports it as a closed connection.
        catch (Exception error) when (error is WebSocketException or IOException)
        {
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static string Route(string match) => "/api/matches/" + match;

    private static async Task<long> Count(string sql, string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.AddWithValue(match);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    // A match started from a room by two real accounts, as in the menu.
    private sealed class Table
    {
        public required string Match { get; init; }
        public required HttpClient FreePeoples { get; init; }
        public required HttpClient Shadow { get; init; }
        public required SessionView ShadowSession { get; init; }
        public required MatchStore Store { get; init; }
        public required string FreePeoplesAccount { get; init; }

        public static async Task<Table> Start(AccountHost host)
        {
            var alice = await MenuAccounts.Register(host);
            var bob = await MenuAccounts.Register(host);
            var rooms = host.Services.GetRequiredService<RoomStore>();
            var room = await rooms.Create(alice.AccountId, Side.FreePeoples);
            await rooms.Join(room.JoinCode, bob.AccountId);
            await rooms.Confirm(room.Id, alice.AccountId, 1);
            await rooms.Confirm(room.Id, bob.AccountId, 1);
            var started = await rooms.Start(room.Id, alice.AccountId, 1);
            return new()
            {
                Match = started.Room!.MatchId,
                FreePeoples = MenuAccounts.Client(host, alice),
                Shadow = MenuAccounts.Client(host, bob),
                ShadowSession = bob,
                Store = host.Services.GetRequiredService<MatchStore>(),
                FreePeoplesAccount = alice.AccountId
            };
        }

        // A rejected command keeps a receipt and a seen revision keeps a read position.
        public async Task LeaveTraces()
        {
            var state = (await Store.Read(Match, FreePeoplesAccount))!.State;
            var command = KernelTests.Command(state, CommandKind.ContinueFellowship);
            command.Payload = new CommandPayload();
            command.DecisionId = "expired";
            using var sent = await FreePeoples.PostAsJsonAsync(Route(Match) + "/commands", command, BoardCatalog.Json);
            var receipt = (await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
            Assert.False(receipt.Accepted);
            using var seen = await FreePeoples.PutAsJsonAsync(Route(Match) + "/seen", new SeenRevisionRequest { Revision = 0 }, BoardCatalog.Json);
            seen.EnsureSuccessStatusCode();
        }
    }
}
