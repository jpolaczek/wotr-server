using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(BotApiCollection.Name)]
public class BotSeatAuthorizationTests(BotApiFixture fixture)
{
    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task SeatCannotEscalateScopeOrReachUnlistedOperations(Side side)
    {
        var seat = await fixture.MovingSeat(side);
        var foreign = await fixture.Seat();
        using var bot = fixture.Client(seat.Session.Token);
        var own = $"/api/matches/{seat.State.Id}";
        var other = $"/api/matches/{foreign.State.Id}";
        (string Method, string Path)[] denied =
        [
            ("GET", other), ("POST", other + "/preview"), ("POST", other + "/commands"),
            ("GET", other + "/actions"), ("GET", other + "/battles/unknown"), ("GET", other + "/receipts/unknown"),
            ("GET", own + "?perspective=Shadow"), ("GET", own + "?side=FreePeoples"),
            ("GET", own + "/replay"), ("GET", own + "/save"), ("DELETE", own), ("PUT", own),
            ("GET", "/api/observe/" + seat.State.Id), ("GET", "/api/observe/" + seat.State.Id + "/actions"),
            ("GET", "/api/observe/" + seat.State.Id + "/battles/unknown"), ("GET", "/api/matches"),
            ("GET", "/api/rooms"), ("POST", "/api/rooms"), ("GET", "/api/rooms/foreign"),
            ("POST", "/api/auth/login"), ("POST", "/api/auth/logout"), ("POST", "/api/auth/register"),
            ("GET", "/api/account"), ("GET", "/api/admin/accounts"), ("POST", "/api/test-scenarios/base/start")
        ];
        foreach (var (method, path) in denied)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };
            using var response = await bot.SendAsync(request);
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{method} {path}: {response.StatusCode}");
        }
        await Assert.ThrowsAsync<WebSocketException>(() => fixture.Socket(seat.Session.Token, foreign.State.Id));
        await Assert.ThrowsAsync<WebSocketException>(() => fixture.Socket(seat.Session.Token, seat.State.Id,
            path: $"/api/observe/{seat.State.Id}/updates"));
        Assert.Equal(seat.State.Revision, (await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State.Revision);
    }

    [PostgresFact]
    public async Task ControlIdentityHasNoSeatAndCannotActOrRenew()
    {
        var seat = await fixture.MovingSeat();
        using var control = fixture.Client(fixture.ControlToken);
        Assert.Null((await fixture.Credentials.Resolve(fixture.ControlToken, default))!.Seat);
        foreach (var path in new[] { $"/api/matches/{seat.State.Id}", "/api/rooms", "/api/admin/accounts" })
            Assert.Equal(HttpStatusCode.Forbidden, (await control.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await control.PostAsJsonAsync($"/api/matches/{seat.State.Id}/commands",
            BotApiFixture.Move(seat), BoardCatalog.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await control.PostAsync("/api/bot/seat/renew", null)).StatusCode);
        await Assert.ThrowsAsync<WebSocketException>(() => fixture.Socket(fixture.ControlToken, seat.State.Id));
        Assert.Equal(seat.State.Revision, (await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State.Revision);
    }

    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task RoomTokenOnlyReadsAndReadiesItsAssignedSeat(Side side)
    {
        var seat = await fixture.Room(side);
        using var bot = fixture.Client(seat.Session.Token);
        using var human = fixture.Client(seat.Human.Token, bot: false);
        var path = $"/api/rooms/{seat.Room.Id}";
        await BotPlayerApiParityTests.EqualResponses(bot.GetAsync(path), human.GetAsync(path));
        var request = new RoomReadyRequest { ExpectedVersion = seat.Room.Version, Ready = true };
        using var ready = await bot.PutAsJsonAsync(path + "/ready", request, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        await BotPlayerApiParityTests.EqualResponses(bot.GetAsync(path), human.GetAsync(path));
        foreach (var operation in new[] { "start", "remove", "leave", "confirm", "demo", "role", "invitation" })
        {
            using var response = await bot.PostAsJsonAsync(path + "/" + operation, new { });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await bot.PutAsJsonAsync(path + "/configuration", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await human.PostAsync("/api/bot/seat/renew", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bot.GetAsync("/api/matches/foreign")).StatusCode);
    }

    [PostgresFact]
    public async Task CredentialsAreHashedNonHumanNonAdminAndFollowActualSeatMembership()
    {
        var seat = await fixture.MovingSeat();
        Assert.Null(await new SessionStore(fixture.Database).Resolve(seat.Session.Token));
        Assert.DoesNotContain(seat.Session.Token, seat.Session.ToString());
        Assert.DoesNotContain(seat.Session.Token, (await fixture.Credentials.Resolve(seat.Session.Token, default))!.ToString());
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var query = Database.Command(connection, null, """
            SELECT a.technical AND NOT a.is_admin, c.token_hash
            FROM accounts a JOIN bot_seat_grants g ON g.participant_id=a.id
            JOIN bot_seat_credentials c ON c.grant_id=g.id WHERE g.id=$1
            """, seat.Grant);
        await using (var row = await query.ExecuteReaderAsync())
        {
            Assert.True(await row.ReadAsync());
            Assert.True(row.GetBoolean(0));
            Assert.Equal(SessionStore.Hash(seat.Session.Token), row.GetString(1));
        }
        await fixture.Sql("UPDATE matches SET sh_account='replacement' WHERE id=$1", seat.State.Id);
        using var bot = fixture.Client(seat.Session.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.GetAsync($"/api/matches/{seat.State.Id}")).StatusCode);
        var room = await fixture.Room();
        Assert.Equal("", (await fixture.Rooms.Remove(room.Room.Id, room.Host.AccountId)).Error);
        using var removed = fixture.Client(room.Session.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await removed.GetAsync($"/api/rooms/{room.Room.Id}")).StatusCode);
    }

    [PostgresFact]
    public async Task BotRefreshAndReadinessDoNotKeepAnAbandonedHumanRoomAlive()
    {
        var seat = await fixture.Room();
        await fixture.Sql("UPDATE rooms SET seen_at=clock_timestamp()-interval '1 minute' WHERE id=$1", seat.Room.Id);
        var before = await Seen();
        using var bot = fixture.Client(seat.Session.Token);
        var path = $"/api/rooms/{seat.Room.Id}";
        Assert.Equal(HttpStatusCode.OK, (await bot.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bot.PutAsJsonAsync(path + "/ready",
            new RoomReadyRequest { ExpectedVersion = seat.Room.Version, Ready = true }, BoardCatalog.Json)).StatusCode);
        Assert.Equal(before, await Seen());
        using var human = fixture.Client(seat.Host.Token, bot: false);
        Assert.Equal(HttpStatusCode.OK, (await human.GetAsync(path)).StatusCode);
        Assert.True(await Seen() > before);

        async Task<DateTime> Seen()
        {
            await using var connection = await fixture.Database.Source.OpenConnectionAsync();
            await using var query = Database.Command(connection, null, "SELECT seen_at FROM rooms WHERE id=$1", seat.Room.Id);
            return (DateTime)(await query.ExecuteScalarAsync())!;
        }
    }
}
