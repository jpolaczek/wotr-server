using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// OBS-01 (OB01, OB02, OB07): a room takes two players and up to sixteen observers. Joins and role changes run under
// the room lock, so the limit holds under concurrency and a refused change leaves the room as it was; an observer's
// place is a lease renewed by reading the room and lost when it lapses or the observer leaves.
public sealed class RoomObserverTests
{
    [PostgresFact]
    public async Task TheAudienceTakesExactlySixteenEvenWhenEighteenJoinAtOnce()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var (room, hostAccount, guest) = await FullTable(host, rooms, RoomVisibility.Public);
        var audience = await Accounts(host, 18);

        var joins = await Task.WhenAll(audience.Select(a => rooms.JoinPublic(room.Id, a, RoomRole.Observer)));

        Assert.Equal(16, joins.Count(j => j.Error.Length == 0));
        Assert.Equal(2, joins.Count(j => j.Error == "ObserversFull"));
        var admitted = audience.Where((_, i) => joins[i].Error.Length == 0).ToList();
        var retry = await rooms.JoinPublic(room.Id, admitted[0], RoomRole.Observer);
        Assert.Equal(16, retry.Room!.Observers.Count);
        var player = await rooms.JoinPublic(room.Id, guest, RoomRole.Observer);
        Assert.Equal(RoomRole.Player, player.Room!.Role);
        Assert.Equal(16, await ObserverRows(room.Id));
        var view = (await rooms.Read(room.Id, admitted[3]))!;
        Assert.Equal(RoomRole.Observer, view.Role);
        Assert.Equal(16, view.ObserverLimit);
        Assert.Empty(view.JoinCode);
        Assert.Equal(16, view.Observers.Select(o => o.Name).Distinct().Count());
        Assert.NotNull(await rooms.Read(room.Id, hostAccount));
    }

    [PostgresFact]
    public async Task TheCodeAdmitsObserversAfterTheSeatsAndTheStartButNotAfterTheEnd()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var (room, hostAccount, guest) = await FullTable(host, rooms, RoomVisibility.Private);
        var code = (await rooms.Invitation(room.Id, hostAccount)).Room!.JoinCode;
        var (early, late, after, stranger) = (await Account(host), await Account(host), await Account(host), await Account(host));

        Assert.Equal("RoomFull", (await rooms.Join(code, stranger)).Error);
        Assert.Equal(RoomRole.Observer, (await rooms.Join(code, early, RoomRole.Observer)).Room!.Role);
        Assert.Equal("RoomNotFound", (await rooms.JoinPublic(room.Id, stranger, RoomRole.Observer)).Error);
        var started = await Start(rooms, room.Id, hostAccount, hostAccount, guest);
        Assert.Equal(RoomStatus.Started, (await rooms.Join(code, late, RoomRole.Observer)).Room!.Status);
        Assert.Equal("MatchAlreadyStarted", (await rooms.Join(code, stranger)).Error);

        await Finish(started.MatchId);

        Assert.Equal("MatchFinished", (await rooms.Join(code, after, RoomRole.Observer)).Error);
        Assert.Equal(2, (await rooms.Read(room.Id, early))!.Observers.Count);
        Assert.Null(await rooms.Read(room.Id, after));
    }

    [PostgresFact]
    public async Task RolesChangeOnlyBeforeTheStartAndARefusedChangeLeavesTheRoomAsItWas()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = Store(host, new ObserverOptions(1, TimeSpan.FromSeconds(60)));
        var (room, hostAccount, guest) = await FullTable(host, rooms, RoomVisibility.Public);
        var watcher = await Account(host);
        var ready = await Ready(rooms, room.Id, hostAccount, guest);

        var watching = (await rooms.JoinPublic(room.Id, watcher, RoomRole.Observer)).Room!;
        Assert.Equal(ready.Version, watching.Version);
        Assert.True(watching.FreePeoplesAccepted && watching.ShadowAccepted);
        Assert.Equal("SeatTaken", await Refused(rooms, room.Id, watcher, RoomRole.Player, Side.Shadow, watching));
        Assert.Equal("ObserversFull", await Refused(rooms, room.Id, guest, RoomRole.Observer, Side.Shadow, watching));
        Assert.Equal("PlayerRequired", (await rooms.SetReady(room.Id, watcher, new() { ExpectedVersion = watching.Version, Ready = true })).Error);
        Assert.Equal("PlayerRequired", (await rooms.Confirm(room.Id, watcher, watching.Version)).Error);

        await rooms.Leave(room.Id, watcher);
        var moved = (await rooms.ChangeRole(room.Id, guest, new() { Role = RoomRole.Observer })).Room!;
        Assert.Equal("", moved.Shadow);
        Assert.Equal(RoomRole.Observer, moved.Role);
        Assert.Equal(watching.Version + 1, moved.Version);
        Assert.False(moved.FreePeoplesAccepted || moved.ShadowAccepted);
        var back = (await rooms.ChangeRole(room.Id, guest, new() { Role = RoomRole.Player, Side = Side.Shadow })).Room!;
        Assert.Equal(RoomRole.Player, back.Role);
        Assert.Equal(Side.Shadow, back.You);
        Assert.Empty(back.Observers);

        await Start(rooms, room.Id, hostAccount, hostAccount, guest);
        var started = (await rooms.Read(room.Id, guest))!;
        Assert.Equal("RolesLocked", await Refused(rooms, room.Id, guest, RoomRole.Observer, Side.Shadow, started));
    }

    [PostgresFact]
    public async Task AHostInTheAudienceStillRunsTheRoomAndRemovesAnyParticipant()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var hostAccount = await Account(host);
        var room = await rooms.Create(hostAccount, new CreateRoomRequest { Name = "Hosted from the audience", Visibility = RoomVisibility.Public });
        var (first, second, watcher) = (await Account(host), await Account(host), await Account(host));

        var hosting = (await rooms.ChangeRole(room.Id, hostAccount, new() { Role = RoomRole.Observer })).Room!;
        Assert.True(hosting.IsHost);
        Assert.Equal("", hosting.FreePeoples);
        Assert.True(Assert.Single(hosting.Observers).IsHost);
        Assert.Equal("ParticipantNotFound", (await rooms.Remove(room.Id, hostAccount)).Error);
        await rooms.JoinPublic(room.Id, first);
        await rooms.JoinPublic(room.Id, second);
        var watching = (await rooms.JoinPublic(room.Id, watcher, RoomRole.Observer)).Room!;
        var name = watching.Observers.Single(o => !o.IsHost).Name;

        Assert.Equal("HostRequired", (await rooms.Remove(room.Id, first, new RemoveParticipantRequest { Name = name })).Error);
        var removed = (await rooms.Remove(room.Id, hostAccount, new RemoveParticipantRequest { Name = name })).Room!;
        Assert.Single(removed.Observers);
        Assert.Equal(watching.Version, removed.Version);
        Assert.Null(await rooms.Read(room.Id, watcher));
        Assert.True(await rooms.WasRemoved(room.Id, watcher));
        Assert.Equal("RemovedFromRoom", (await rooms.JoinPublic(room.Id, watcher, RoomRole.Observer)).Error);
        Assert.Equal("ParticipantNotFound", (await rooms.Remove(room.Id, hostAccount, new RemoveParticipantRequest { Name = "nobody here" })).Error);

        var configured = (await rooms.Configure(room.Id, hostAccount, new() { ExpectedVersion = removed.Version, Configuration = new() })).Room!;
        Assert.Equal("PlayerRequired", (await rooms.SetReady(room.Id, hostAccount, new() { ExpectedVersion = configured.Version, Ready = true })).Error);
        var started = await Start(rooms, room.Id, hostAccount, first, second);
        Assert.NotEmpty(started.MatchId);
        Assert.Equal(RoomRole.Observer, started.Role);
        await rooms.Leave(room.Id, hostAccount);
        var after = (await rooms.Read(room.Id, first))!;
        Assert.Equal(RoomStatus.Started, after.Status);
        Assert.Empty(after.Observers);
    }

    [PostgresFact]
    public async Task TheHostLeavingTheWaitingRoomClosesItAndDeletesItWhenNobodyElseIsIn()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var hostAccount = await Account(host);
        var watcher = await Account(host);
        var lonely = await rooms.Create(hostAccount, new CreateRoomRequest { Name = "Nobody comes", Visibility = RoomVisibility.Public });
        var watched = await rooms.Create(hostAccount, new CreateRoomRequest { Name = "Somebody watches", Visibility = RoomVisibility.Public });
        await rooms.ChangeRole(lonely.Id, hostAccount, new() { Role = RoomRole.Observer });
        await rooms.JoinPublic(watched.Id, watcher, RoomRole.Observer);

        await rooms.Leave(lonely.Id, hostAccount);
        await rooms.Leave(watched.Id, hostAccount);

        Assert.Equal(0, await RoomRows(lonely.Id));
        Assert.Equal(RoomStatus.Closed, (await rooms.Read(watched.Id, watcher))!.Status);
        Assert.Equal("RoomNotFound", (await rooms.JoinPublic(watched.Id, await Account(host), RoomRole.Observer)).Error);
    }

    [PostgresFact]
    public async Task ALapsedObserverLosesThePlaceWhileAReadingOneKeepsItAcrossARestart()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var lease = new ObserverOptions(2, TimeSpan.FromSeconds(2));
        var rooms = Store(host, lease);
        var (room, _, _) = await FullTable(host, rooms, RoomVisibility.Public);
        var (reading, silent, waiting) = (await Account(host), await Account(host), await Account(host));
        await Task.WhenAll(rooms.JoinPublic(room.Id, reading, RoomRole.Observer), rooms.JoinPublic(room.Id, reading, RoomRole.Observer));
        await rooms.JoinPublic(room.Id, silent, RoomRole.Observer);
        Assert.Equal("ObserversFull", (await rooms.JoinPublic(room.Id, waiting, RoomRole.Observer)).Error);

        var restarted = Store(host, lease);
        for (var i = 0; i < 10; i++)
        {
            Assert.NotNull(await (i % 2 == 0 ? rooms : restarted).Read(room.Id, reading));
            await Task.Delay(300);
        }

        Assert.True(await rooms.Presence(room.Id, reading));
        Assert.Null(await rooms.Read(room.Id, silent));
        Assert.False(await rooms.Presence(room.Id, silent));
        Assert.Equal(RoomRole.Observer, (await rooms.JoinPublic(room.Id, waiting, RoomRole.Observer)).Room!.Role);
        Assert.Equal("ObserversFull", (await rooms.JoinPublic(room.Id, silent, RoomRole.Observer)).Error);
        await rooms.Leave(room.Id, reading);
        Assert.Null(await rooms.Read(room.Id, reading));
        Assert.Equal(2, (await rooms.JoinPublic(room.Id, silent, RoomRole.Observer)).Room!.Observers.Count);
        Assert.Contains(await rooms.List(silent), r => r.Id == room.Id && r.Role == RoomRole.Observer);
    }

    [PostgresFact]
    public async Task WatchingObserversKeepAWaitingRoomFromTheSweeperUntilTheirLeaseLapses()
    {
        await using var database = await OwnDatabase.Create();
        using var host = new AccountHost(database.Connection, extra: new Dictionary<string, string?>(TestBudgets.Stress)
        {
            [EmptyRoomOptions.EmptyKey] = "1",
            [EmptyRoomOptions.IntervalKey] = "0.2",
            [ObserverOptions.LeaseKey] = "3"
        });
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var room = await rooms.Create(await Account(host), new CreateRoomRequest { Name = "Watched room", Visibility = RoomVisibility.Public });
        await rooms.JoinPublic(room.Id, await Account(host), RoomRole.Observer);

        await Task.Delay(2000);
        Assert.Equal(1, await RoomRows(room.Id, database.Connection));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (await RoomRows(room.Id, database.Connection) > 0)
        {
            Assert.True(DateTime.UtcNow < deadline, "The sweeper did not take the room after the lease lapsed.");
            await Task.Delay(100);
        }
    }

    [PostgresFact]
    public async Task ObserversChatUnderTheirNameAndRole()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var chat = host.Services.GetRequiredService<RoomChat>();
        var (room, hostAccount, _) = await FullTable(host, rooms, RoomVisibility.Public);
        var watcher = await MenuAccounts.Register(host);
        await rooms.JoinPublic(room.Id, watcher.AccountId, RoomRole.Observer);

        await chat.Post(room.Id, hostAccount, "Welcome");
        var view = (await chat.Post(room.Id, watcher.AccountId, "Just watching")).View!;

        Assert.Equal(RoomRole.Player, view.Messages[0].Role);
        Assert.Equal(Side.FreePeoples, view.Messages[0].Side);
        Assert.Equal(RoomRole.Observer, view.Messages[1].Role);
        Assert.Equal(watcher.Username, view.Messages[1].Author);
        Assert.Equal("RoomNotFound", (await chat.Post(room.Id, await Account(host), "Let me in")).Error);
    }

    [PostgresFact]
    public async Task ThePublicListShowsWaitingAndStartedRoomsWithTheirSeatsAndAudienceButNoFinishedOne()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = Store(host, new ObserverOptions(1, TimeSpan.FromSeconds(60)));
        var viewer = await Account(host);
        var open = await rooms.Create(await Account(host), new CreateRoomRequest { Name = "Open seat", Visibility = RoomVisibility.Public });
        var (started, hostAccount, guest) = await FullTable(host, rooms, RoomVisibility.Public);
        var match = await Start(rooms, started.Id, hostAccount, hostAccount, guest);
        await rooms.JoinPublic(started.Id, await Account(host), RoomRole.Observer);
        var (ended, endedHost, endedGuest) = await FullTable(host, rooms, RoomVisibility.Public);
        var endedMatch = await Start(rooms, ended.Id, endedHost, endedHost, endedGuest);
        await Finish(endedMatch.MatchId);

        var listed = await AllPublic(rooms, viewer);

        var waiting = listed.Single(r => r.Id == open.Id);
        Assert.Equal((RoomStatus.Waiting, 1, 0, true, true), (waiting.Status, waiting.Players, waiting.Observers, waiting.CanPlay, waiting.CanObserve));
        var playing = listed.Single(r => r.Id == started.Id);
        Assert.Equal((RoomStatus.Started, 2, 1, false, false), (playing.Status, playing.Players, playing.Observers, playing.CanPlay, playing.CanObserve));
        Assert.DoesNotContain(listed, r => r.Id == ended.Id);
        Assert.NotEmpty(match.MatchId);
    }

    [PostgresFact]
    public async Task TheHttpApiJoinsWatchesMovesAndRemovesObservers()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var owner = await MenuAccounts.Register(host);
        var watcher = await MenuAccounts.Register(host);
        using var ownerClient = MenuAccounts.Client(host, owner);
        using var watcherClient = MenuAccounts.Client(host, watcher);
        var room = await rooms.Create(owner.AccountId, new CreateRoomRequest { Name = "Http audience", Visibility = RoomVisibility.Public });
        var route = "/api/rooms/" + room.Id;

        using (var joined = await watcherClient.PostAsync(route + "/join?role=Observer", null))
            Assert.Equal(RoomRole.Observer, (await joined.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.Role);
        using (var invalid = await watcherClient.PostAsJsonAsync(route + "/role", new { role = 7, side = 0 }))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using (var presence = await watcherClient.PostAsync(route + "/presence", null))
            Assert.Equal(HttpStatusCode.NoContent, presence.StatusCode);
        using (var seat = await watcherClient.PostAsJsonAsync(route + "/role", new RoomRoleRequest { Role = RoomRole.Player, Side = Side.Shadow }, BoardCatalog.Json))
            Assert.Equal(Side.Shadow, (await seat.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.You);
        using (var audience = await watcherClient.PostAsJsonAsync(route + "/role", new RoomRoleRequest { Role = RoomRole.Observer }, BoardCatalog.Json))
            audience.EnsureSuccessStatusCode();
        using (var removed = await ownerClient.PostAsJsonAsync(route + "/remove", new RemoveParticipantRequest { Name = watcher.Username }, BoardCatalog.Json))
            Assert.Empty((await removed.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.Observers);
        using (var gone = await watcherClient.GetAsync(route))
            Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        using (var lapsed = await watcherClient.PostAsync(route + "/presence", null))
            Assert.Equal(HttpStatusCode.NotFound, lapsed.StatusCode);
    }

    private static RoomStore Store(AccountHost host, ObserverOptions options) =>
        new(host.Services.GetRequiredService<Database>(), new ServerRandom(), null, options);

    private static async Task<string> Account(AccountHost host) => (await MenuAccounts.Register(host)).AccountId;

    private static async Task<List<string>> Accounts(AccountHost host, int count)
    {
        var accounts = new List<string>();
        for (var i = 0; i < count; i++)
            accounts.Add(await Account(host));
        return accounts;
    }

    private static async Task<(RoomView Room, string Host, string Guest)> FullTable(AccountHost host, RoomStore rooms,
        RoomVisibility visibility)
    {
        var (owner, guest) = (await Account(host), await Account(host));
        var room = await rooms.Create(owner, new CreateRoomRequest { Name = "Observed table", Visibility = visibility });
        var code = (await rooms.Invitation(room.Id, owner)).Room!.JoinCode;
        await rooms.Join(code, guest);
        return (room, owner, guest);
    }

    private static async Task<RoomView> Ready(RoomStore rooms, string room, params string[] players)
    {
        RoomView? view = null;
        var version = (await rooms.Read(room, players[0]))!.Version;
        foreach (var player in players)
            view = (await rooms.Confirm(room, player, version)).Room!;
        return view!;
    }

    private static async Task<RoomView> Start(RoomStore rooms, string room, string host, string freePeoples, string shadow)
    {
        var ready = await Ready(rooms, room, freePeoples, shadow);
        var started = await rooms.Start(room, host, ready.Version);
        Assert.Equal("", started.Error);
        return started.Room!;
    }

    // A refused change returns its error and leaves the room exactly as it was.
    private static async Task<string> Refused(RoomStore rooms, string room, string account, RoomRole role, Side side,
        RoomView before)
    {
        var result = await rooms.ChangeRole(room, account, new() { Role = role, Side = side });
        var after = (await rooms.Read(room, account))!;
        Assert.Equal((before.Version, before.FreePeoples, before.Shadow, before.Observers.Count),
            (after.Version, after.FreePeoples, after.Shadow, after.Observers.Count));
        return result.Error;
    }

    private static async Task<List<PublicRoomSummary>> AllPublic(RoomStore rooms, string viewer)
    {
        var listed = new List<PublicRoomSummary>();
        var cursor = "";
        do
        {
            var page = await rooms.ListPublic(viewer, cursor, 50);
            listed.AddRange(page.Rooms);
            cursor = page.NextCursor;
        } while (cursor.Length > 0);
        return listed;
    }

    private static async Task Finish(string match) =>
        await Execute("UPDATE matches SET state=jsonb_set(state,'{phase}','\"Finished\"') WHERE id=$1", match);

    private static async Task<long> ObserverRows(string room) =>
        await Count("SELECT count(*) FROM room_observers WHERE room_id=$1", room);

    private static async Task<long> RoomRows(string room, string? connection = null) =>
        await Count("SELECT count(*) FROM rooms WHERE id=$1", room, connection);

    private static async Task<long> Count(string sql, string value, string? connection = null)
    {
        await using var data = NpgsqlDataSource.Create(connection ?? PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task Execute(string sql, string value)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        await command.ExecuteNonQueryAsync();
    }
}
