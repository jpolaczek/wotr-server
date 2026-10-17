using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// DEMO-01 (DM01–DM03): a member of a waiting room starts one demo of the base game against the CPU; a new demo
// replaces the previous one with everything stored for it; the room, other matches and other demos stay.
public sealed class DemoSessionTests
{
    [PostgresFact]
    public async Task ADemoStartsFromTheWaitingRoomOnTheSeatsSideAndTheRoomKeepsWaiting()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var alice = await MenuAccounts.Register(host);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(alice.AccountId, Side.Shadow);
        using var client = MenuAccounts.Client(host, alice);

        var demo = await Start(client, room.Id);

        Assert.Equal(Side.Shadow, demo.You);
        var view = (await client.GetFromJsonAsync<PlayerView>(Route(demo.MatchId), BoardCatalog.Json))!;
        Assert.Equal(Side.Shadow, view.You);
        Assert.True(ConfigurationRules.IsBase(view.Configuration), "A demo is always the base game.");
        Assert.Equal("demo-cpu", await Text("SELECT fp_account FROM matches WHERE id=$1", demo.MatchId));
        Assert.Equal(alice.AccountId, await Text("SELECT sh_account FROM matches WHERE id=$1", demo.MatchId));
        Assert.Equal("FreePeoples", await Text("SELECT cpu_side FROM demo_matches WHERE match_id=$1", demo.MatchId));
        Assert.Equal(Side.FreePeoples, await host.Services.GetRequiredService<DemoSessionStore>().CpuSide(demo.MatchId));
        var list = (await client.GetFromJsonAsync<MatchListView>("/api/matches", BoardCatalog.Json))!;
        var listed = Assert.Single(list.Matches, match => match.MatchId == demo.MatchId);
        Assert.Equal("CPU", listed.Opponent);
        Assert.Equal("Demo · Shadow vs CPU", listed.Name);
        var after = (await client.GetFromJsonAsync<RoomView>("/api/rooms/" + room.Id, BoardCatalog.Json))!;
        Assert.Equal(RoomStatus.Waiting, after.Status);
        Assert.Equal(room.Version, after.Version);
        Assert.Equal("", after.MatchId);
    }

    [PostgresFact]
    public async Task ANewDemoReplacesThePreviousOneAndNothingElse()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var aliceClient = MenuAccounts.Client(host, alice);
        using var bobClient = MenuAccounts.Client(host, bob);
        var aliceRoom = await rooms.Create(alice.AccountId, Side.FreePeoples);
        var bobDemo = await Start(bobClient, (await rooms.Create(bob.AccountId, Side.FreePeoples)).Id);
        var regular = await RegularMatch(rooms, alice, bob);
        var first = await Start(aliceClient, aliceRoom.Id);
        await Reject(aliceClient, first.MatchId);

        var second = await Start(aliceClient, aliceRoom.Id);

        Assert.NotEqual(first.MatchId, second.MatchId);
        foreach (var sql in new[]
                 {
                     "SELECT count(*) FROM matches WHERE id=$1",
                     "SELECT count(*) FROM command_receipts WHERE match_id=$1",
                     "SELECT count(*) FROM match_events WHERE match_id=$1",
                     "SELECT count(*) FROM demo_matches WHERE match_id=$1"
                 })
            Assert.Equal(0, await Count(sql, first.MatchId));
        using var gone = await aliceClient.GetAsync(Route(first.MatchId));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM demo_matches WHERE owner_account=$1", alice.AccountId));
        Assert.Equal(1, await Count("SELECT count(*) FROM matches WHERE id=$1", regular));
        Assert.Equal(1, await Count("SELECT count(*) FROM matches WHERE id=$1", bobDemo.MatchId));
    }

    [PostgresFact]
    public async Task OnlyAMemberOfAWaitingRoomStartsADemo()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        var outsider = await MenuAccounts.Register(host);
        using var outsiderClient = MenuAccounts.Client(host, outsider);
        using var aliceClient = MenuAccounts.Client(host, alice);
        var waiting = await rooms.Create(alice.AccountId, Side.FreePeoples);
        using var foreign = await outsiderClient.PostAsync("/api/rooms/" + waiting.Id + "/demo", null);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        var closed = await rooms.Create(alice.AccountId, Side.FreePeoples);
        await rooms.Leave(closed.Id, alice.AccountId);
        using var closedAnswer = await aliceClient.PostAsync("/api/rooms/" + closed.Id + "/demo", null);
        Assert.Equal(HttpStatusCode.NotFound, closedAnswer.StatusCode);

        var started = await StartedRoom(rooms, alice, bob);
        using var startedAnswer = await aliceClient.PostAsync("/api/rooms/" + started + "/demo", null);
        Assert.Equal(HttpStatusCode.Conflict, startedAnswer.StatusCode);
        Assert.Equal("MatchAlreadyStarted", (await startedAnswer.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        Assert.Equal(0, await Count("SELECT count(*) FROM demo_matches WHERE owner_account=$1", alice.AccountId));
    }

    [PostgresFact]
    public async Task TwoStartsAtOnceLeaveOneDemo()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var alice = await MenuAccounts.Register(host);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(alice.AccountId, Side.FreePeoples);
        var demos = host.Services.GetRequiredService<DemoSessionStore>();

        var both = await Task.WhenAll(demos.Start(alice.AccountId, room.Id), demos.Start(alice.AccountId, room.Id));

        Assert.All(both, start => Assert.NotNull(start.Demo));
        Assert.Equal(1, await Count("SELECT count(*) FROM demo_matches WHERE owner_account=$1", alice.AccountId));
        Assert.Equal(1, await Count("SELECT count(*) FROM matches WHERE fp_account=$1", alice.AccountId));
    }

    private static async Task<DemoMatchView> Start(HttpClient client, string room)
    {
        using var answer = await client.PostAsync("/api/rooms/" + room + "/demo", null);
        answer.EnsureSuccessStatusCode();
        return (await answer.Content.ReadFromJsonAsync<DemoMatchView>(BoardCatalog.Json))!;
    }

    // A rejected command keeps a receipt, so the removal has something to remove.
    private static async Task Reject(HttpClient client, string match)
    {
        var view = (await client.GetFromJsonAsync<PlayerView>(Route(match), BoardCatalog.Json))!;
        var command = new GameCommand
        {
            MatchId = match, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
            DecisionId = "expired", Kind = CommandKind.ContinueFellowship
        };
        using var sent = await client.PostAsJsonAsync(Route(match) + "/commands", command, BoardCatalog.Json);
        Assert.False((await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
    }

    private static async Task<string> RegularMatch(RoomStore rooms, SessionView host, SessionView guest)
    {
        var room = await StartedRoom(rooms, host, guest);
        return (await rooms.Read(room, host.AccountId))!.MatchId;
    }

    private static async Task<string> StartedRoom(RoomStore rooms, SessionView host, SessionView guest)
    {
        var room = await rooms.Create(host.AccountId, Side.FreePeoples);
        await rooms.Join(room.JoinCode, guest.AccountId);
        await rooms.Confirm(room.Id, host.AccountId, 1);
        await rooms.Confirm(room.Id, guest.AccountId, 1);
        await rooms.Start(room.Id, host.AccountId, 1);
        return room.Id;
    }

    private static string Route(string match) => "/api/matches/" + match;

    private static async Task<long> Count(string sql, string key)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.AddWithValue(key);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string?> Text(string sql, string key)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.AddWithValue(key);
        return await command.ExecuteScalarAsync() as string;
    }
}
