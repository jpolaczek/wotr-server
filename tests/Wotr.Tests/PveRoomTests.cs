using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(PveCollection.Name)]
public sealed class PveRoomTests(PveFixture fixture)
{
    [PostgresFact]
    public async Task EnablingEmbeddedHostingDoesNotReinterpretAnExistingLegacySeat()
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        await fixture.ReadyCpu(room);
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        var access = fixture.Host.Services.GetRequiredService<BotAccessOptions>();
        var rooms = new BotRoomOptions(true, "fixture-1");
        var host = new BotHostOptions(true, null, "0.2.0", 1, TimeSpan.FromSeconds(30),
            TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        using var missing = EmbeddedBotRuntime.Load(null);
        var mixed = new RoomStore(fixture.Database, new SeededRandom(18), ReleaseScope.BaseOnly,
            bots: access, botRooms: rooms, runtime: new(host, rooms, access, missing), botHost: host);
        Assert.Equal("", (await mixed.Start(room.Id, human.AccountId, room.Version)).Error);
    }

    [PostgresFact]
    public async Task ChangedPolicyVersionBlocksStartAndSeatCredentialCannotInviteAnotherBot()
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        var cpu = await fixture.ReadyCpu(room);
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        var changed = new RoomStore(fixture.Database, new SeededRandom(1), ReleaseScope.BaseOnly,
            bots: fixture.Host.Services.GetRequiredService<BotAccessOptions>(), botRooms: new(true, "fixture-2"));
        Assert.Equal("BotVersionUnsupported", (await changed.Start(room.Id, human.AccountId, room.Version)).Error);
        var unavailable = (await changed.Read(room.Id, human.AccountId))!;
        Assert.Equal(BotSeatStatus.Unavailable, unavailable.Bot!.Status);
        Assert.Equal("BotVersionUnsupported", unavailable.StartUnavailableReason);
        using var bot = fixture.BotClient(cpu.Session.Token);
        using var denied = await bot.PutAsJsonAsync($"/api/rooms/{room.Id}/bot", PveFixture.Request(room), BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM matches WHERE fp_account=$1", human.AccountId));
    }

    [PostgresFact]
    public async Task InvalidRequestAndHostLeavingTheSeatCannotLeaveALiveReservation()
    {
        var human = await fixture.Human();
        var room = await fixture.Room(human);
        Assert.Equal("InvalidBotRequest", (await fixture.Rooms.AddBot(room.Id, human.AccountId,
            new() { RequestId = "not-an-id", ExpectedVersion = room.Version })).Error);
        room = await fixture.Reserve(human, room);
        var cpu = await fixture.ReadyCpu(room);
        var changed = await fixture.Rooms.ChangeRole(room.Id, human.AccountId, new() { Role = RoomRole.Observer });
        Assert.Equal("", changed.Error);
        Assert.Null(changed.Room!.Bot);
        Assert.Equal(SeatOccupantKind.Empty, changed.Room.ShadowKind);
        Assert.Null(await fixture.Credentials.ResolveSeat(cpu.Session.Token, default));
        Assert.Equal("cancelled", await fixture.Scalar("SELECT status FROM bot_assignments WHERE id=$1", cpu.Grant));
    }

    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task HostReservesOppositeSeatAndRetriesCannotReplaceIt(Side side)
    {
        var human = await fixture.Human();
        var room = await fixture.Room(human, side);
        var request = PveFixture.Request(room);
        using var client = fixture.HumanClient(human);
        using var first = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/bot", request, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var reserved = (await first.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Equal(TurnFlow.Other(side), reserved.Bot!.Side);
        Assert.Equal(BotSeatStatus.Joining, reserved.Bot.Status);
        Assert.Equal(SeatOccupantKind.Cpu, side == Side.FreePeoples ? reserved.ShadowKind : reserved.FreePeoplesKind);
        Assert.Equal("fixture-1", reserved.Bot.PolicyVersion);
        Assert.False(reserved.FreePeoplesAccepted || reserved.ShadowAccepted);
        using var retry = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/bot", request, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(reserved.Version, (await retry.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.Version);
        request.ExpectedVersion++;
        using var conflict = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/bot", request, BoardCatalog.Json);
        Assert.Equal("BotRequestConflict", (await conflict.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE room_id=$1", room.Id));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_seat_grants WHERE room_id=$1", room.Id));
    }

    [PostgresFact]
    public async Task OnlySeatedHumanHostCanReserveAnEmptySeat()
    {
        var human = await fixture.Human();
        var guest = await fixture.Human();
        var room = await fixture.Room(human);
        var joined = await fixture.Rooms.JoinPublic(room.Id, guest.AccountId);
        Assert.Equal("HostRequired", (await fixture.Rooms.AddBot(room.Id, guest.AccountId, PveFixture.Request(joined.Room!))).Error);
        Assert.Equal("SeatTaken", (await fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(joined.Room!))).Error);
        var observing = await fixture.Rooms.ChangeRole(room.Id, human.AccountId, new() { Role = RoomRole.Observer });
        Assert.Equal("PlayerRequired", (await fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(observing.Room!))).Error);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE room_id=$1", room.Id));
    }

    [PostgresFact]
    public async Task VersionReleaseAndAcceptedCapabilitiesGateAdmission()
    {
        var human = await fixture.Human();
        var room = await fixture.Room(human);
        var stale = PveFixture.Request(room);
        var configured = await fixture.Rooms.Configure(room.Id, human.AccountId,
            new() { ExpectedVersion = room.Version, Configuration = new() });
        Assert.Equal("StaleConfiguration", (await fixture.Rooms.AddBot(room.Id, human.AccountId, stale)).Error);
        var database = fixture.Database;
        var access = fixture.Host.Services.GetRequiredService<BotAccessOptions>();
        var options = fixture.Host.Services.GetRequiredService<BotRoomOptions>();
        var all = new RoomStore(database, new SeededRandom(1), ReleaseScope.Full, bots: access, botRooms: options);
        configured = await all.Configure(room.Id, human.AccountId,
            new() { ExpectedVersion = configured.Room!.Version, Configuration = new() { Fate = true } });
        Assert.Equal("ExpansionsUnavailable", (await fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(configured.Room!))).Error);
        Assert.Equal("BotConfigurationUnsupported", (await all.AddBot(room.Id, human.AccountId, PveFixture.Request(configured.Room!))).Error);
        var disabled = new RoomStore(database, new SeededRandom(1), ReleaseScope.Full, bots: access, botRooms: new(false, ""));
        Assert.Equal("BotUnavailable", (await disabled.AddBot(room.Id, human.AccountId, PveFixture.Request(configured.Room!))).Error);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE room_id=$1", room.Id));
    }

    [PostgresFact]
    public async Task ReadyAcceptsOnlyCurrentConfigurationAndAuthorizedGeneration()
    {
        var human = await fixture.Human();
        var reserved = await fixture.Reserve(human, await fixture.Room(human));
        Assert.Equal("ConfirmationRequired", (await fixture.Rooms.Start(reserved.Id, human.AccountId, reserved.Version)).Error);
        var ready = await fixture.ReadyCpu(reserved);
        Assert.Equal(BotSeatStatus.Ready, ready.Room.Bot!.Status);
        var changed = await fixture.Rooms.Configure(reserved.Id, human.AccountId,
            new() { ExpectedVersion = reserved.Version, Configuration = new() });
        Assert.Equal(BotSeatStatus.Joining, changed.Room!.Bot!.Status);
        using var bot = fixture.BotClient(ready.Session.Token);
        using var stale = await bot.PutAsJsonAsync($"/api/rooms/{reserved.Id}/ready",
            new RoomReadyRequest { ExpectedVersion = reserved.Version, Ready = true }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var accepted = await bot.PutAsJsonAsync($"/api/rooms/{reserved.Id}/ready",
            new RoomReadyRequest { ExpectedVersion = changed.Room.Version, Ready = true }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        await fixture.Credentials.Reassign(ready.Grant, 1, DateTimeOffset.UtcNow.AddMinutes(10));
        await fixture.Rooms.SetReady(reserved.Id, human.AccountId, new() { ExpectedVersion = changed.Room.Version, Ready = true });
        Assert.Equal("BotNotReady", (await fixture.Rooms.Start(reserved.Id, human.AccountId, changed.Room.Version)).Error);
    }

    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovalOrClosureCancelsAndRevokesWithoutResurrection(bool close)
    {
        var human = await fixture.Human();
        var room = await fixture.Room(human);
        var request = PveFixture.Request(room);
        var reserved = (await fixture.Rooms.AddBot(room.Id, human.AccountId, request)).Room!;
        var ready = await fixture.ReadyCpu(reserved);
        var result = close ? await fixture.Rooms.Leave(room.Id, human.AccountId)
            : await fixture.Rooms.Remove(room.Id, human.AccountId);
        Assert.Equal("", result.Error);
        Assert.Null(result.Room!.Bot);
        Assert.Equal("cancelled", await fixture.Scalar("SELECT status FROM bot_assignments WHERE id=$1", ready.Grant));
        Assert.Null(await fixture.Credentials.ResolveSeat(ready.Session.Token, default));
        Assert.Equal(close ? "RoomNotFound" : "BotReservationCancelled",
            (await fixture.Rooms.AddBot(room.Id, human.AccountId, request)).Error);
    }

    [PostgresFact]
    public async Task BotPollingAndReadinessDoNotKeepAnAbandonedRoomAlive()
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        var ready = await fixture.ReadyCpu(room);
        await fixture.Scalar("UPDATE rooms SET seen_at=clock_timestamp()-interval '2 hours' WHERE id=$1", room.Id);
        using var bot = fixture.BotClient(ready.Session.Token);
        using var view = await bot.GetAsync($"/api/rooms/{room.Id}");
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);
        using var repeat = await bot.PutAsJsonAsync($"/api/rooms/{room.Id}/ready",
            new RoomReadyRequest { ExpectedVersion = room.Version, Ready = true }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(true, await fixture.Scalar("SELECT seen_at<clock_timestamp()-interval '1 hour' FROM rooms WHERE id=$1", room.Id));
        var sweeper = fixture.Host.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<EmptyRoomSweeper>().Single();
        await sweeper.Sweep();
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE room_id=$1", room.Id));
        Assert.Null(await fixture.Credentials.ResolveSeat(ready.Session.Token, default));
    }
}
