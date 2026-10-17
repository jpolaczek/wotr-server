using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// MENU-27 (user decision): a game that has not started is not kept. A waiting room stays while one of its players
// uses it (the waiting room reads it every 2 s); left empty or closed, it is deleted; a started room never is.
// Each test has a database of its own: its quick sweeper would otherwise take the rooms of other tests.
public sealed class EmptyRoomSweeperTests
{
    private static readonly IReadOnlyDictionary<string, string?> Quick = new Dictionary<string, string?>(TestBudgets.Stress)
    {
        [EmptyRoomOptions.EmptyKey] = "1",
        [EmptyRoomOptions.IntervalKey] = "0.2"
    };

    [PostgresFact]
    public async Task AWaitingRoomLivesWhileItsPlayerIsInItAndGoesWhenItIsEmpty()
    {
        await using var database = await OwnDatabase.Create();
        using var host = new AccountHost(database.Connection, extra: Quick);
        var session = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, session);
        var room = await Create(client);

        for (var i = 0; i < 8; i++)
        {
            using var read = await client.GetAsync(Route(room));
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            await Task.Delay(300);
        }
        Assert.Equal(1, await Rows(database, room));

        await Until(async () => await Rows(database, room) == 0);
        using var gone = await client.GetAsync(Route(room));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<List<RoomView>>("/api/rooms", BoardCatalog.Json))!, r => r.Id == room);
    }

    [PostgresFact]
    public async Task AClosedRoomGoesAndAStartedOneStays()
    {
        await using var database = await OwnDatabase.Create();
        using var host = new AccountHost(database.Connection, extra: Quick);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var closed = await rooms.Create(alice.AccountId, Side.FreePeoples);
        await rooms.Leave(closed.Id, alice.AccountId);
        var started = await rooms.Create(alice.AccountId, Side.FreePeoples);
        await rooms.Join(started.JoinCode, bob.AccountId);
        await rooms.Confirm(started.Id, alice.AccountId, 1);
        await rooms.Confirm(started.Id, bob.AccountId, 1);
        await rooms.Start(started.Id, alice.AccountId, 1);

        await Until(async () => await Rows(database, closed.Id) == 0);
        await Task.Delay(1500);

        Assert.Equal(1, await Rows(database, started.Id));
    }

    // MENU-32: the host leaving a room nobody else sits in deletes it at once, without waiting for the sweeper;
    // with a guest in it the room is only closed.
    [PostgresFact]
    public async Task TheHostLeavingAnEmptyRoomDeletesItAtOnce()
    {
        await using var database = await OwnDatabase.Create();
        using var host = new AccountHost(database.Connection, extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, alice);
        var empty = await Create(client);
        var shared = await rooms.Create(alice.AccountId, Side.FreePeoples);
        await rooms.Join(shared.JoinCode, bob.AccountId);

        using (var left = await client.PostAsync(Route(empty) + "/leave", null))
            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);
        await rooms.Leave(shared.Id, alice.AccountId);

        Assert.Equal(0, await Rows(database, empty));
        using var gone = await client.GetAsync(Route(empty));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal(1, await Rows(database, shared.Id));
    }

    private static async Task<string> Create(HttpClient client)
    {
        using var created = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, BoardCatalog.Json);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.Id;
    }

    private static async Task Until(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The sweeper did not act in time.");
            await Task.Delay(100);
        }
    }

    private static string Route(string room) => "/api/rooms/" + room;

    private static async Task<long> Rows(OwnDatabase database, string room)
    {
        await using var data = NpgsqlDataSource.Create(database.Connection);
        await using var command = data.CreateCommand("SELECT count(*) FROM rooms WHERE id=$1");
        command.Parameters.AddWithValue(room);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}

// A database created for one test on the test server and dropped after it.
internal sealed class OwnDatabase : IAsyncDisposable
{
    private readonly string name;
    public string Connection { get; }

    private OwnDatabase(string name, string connection)
    {
        this.name = name;
        Connection = connection;
    }

    public static async Task<OwnDatabase> Create()
    {
        var name = "wotr_sweep_" + Guid.NewGuid().ToString("N")[..12];
        await Execute("CREATE DATABASE " + name);
        var connection = new NpgsqlConnectionStringBuilder(PostgresTests.Connection) { Database = name }.ConnectionString;
        return new OwnDatabase(name, connection);
    }

    public async ValueTask DisposeAsync()
    {
        await Execute("DROP DATABASE IF EXISTS " + name + " WITH (FORCE)");
    }

    private static async Task Execute(string sql)
    {
        await using var connection = new NpgsqlConnection(PostgresTests.Connection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
