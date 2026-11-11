using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(BotApiCollection.Name)]
public class BotCredentialRevocationTests(BotApiFixture fixture)
{
    [PostgresTheory]
    [InlineData("revoke")]
    [InlineData("generation")]
    [InlineData("credential")]
    [InlineData("lease")]
    [InlineData("reissue")]
    public async Task QueuedCommandRechecksAuthorityAfterTheMatchLock(string loss)
    {
        var seat = await fixture.MovingSeat();
        var command = BotApiFixture.Move(seat);
        var initial = StateCodec.Write((await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State);
        using var bot = fixture.Client(seat.Session.Token);
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var held = await connection.BeginTransactionAsync();
        await using var locked = Database.Command(connection, held, "SELECT id FROM matches WHERE id=$1 FOR UPDATE", seat.State.Id);
        await locked.ExecuteScalarAsync();
        var queued = bot.PostAsJsonAsync($"/api/matches/{seat.State.Id}/commands", command, BoardCatalog.Json);
        try
        {
            await fixture.WaitForLock("SELECT m.state::text,m.fp_account,m.events_indexed,m.recorded");
            if (loss is "credential" or "lease")
                await ExpireAfterTransactionStarted(seat, loss);
            else
                await LoseAuthority(seat, loss);
        }
        finally
        {
            await held.RollbackAsync();
        }
        using var refused = await queued;
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Contains("BotAuthorityExpired", await refused.Content.ReadAsStringAsync());
        Assert.Equal(initial, StateCodec.Write((await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State));
        Assert.Null(await fixture.Matches.Receipt(seat.State.Id, command.CommandId, seat.Participant));
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.GetAsync($"/api/matches/{seat.State.Id}")).StatusCode);
        await Assert.ThrowsAsync<WebSocketException>(() => fixture.Socket(seat.Session.Token, seat.State.Id));
    }

    [PostgresTheory]
    [InlineData("revoke")]
    [InlineData("generation")]
    [InlineData("credential")]
    [InlineData("lease")]
    public async Task OpenSocketStopsAtNextHeartbeatAndAnExpiredTokenCannotRenew(string loss)
    {
        var seat = await fixture.Seat();
        using var stream = await fixture.Socket(seat.Session.Token, seat.State.Id);
        Assert.Equal(WebSocketMessageType.Text, (await BotApiFixture.Receive(stream)).Type);
        await LoseAuthority(seat, loss);
        Assert.Equal(WebSocketMessageType.Close, (await BotApiFixture.Receive(stream)).Type);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, stream.CloseStatus);
        using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await stream.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", closeTimeout.Token);
        using var bot = fixture.Client(seat.Session.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.PostAsync("/api/bot/seat/renew", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.PostAsJsonAsync($"/api/matches/{seat.State.Id}/commands",
            BattleFixture.Command(seat.State, CommandKind.DiscardDie), BoardCatalog.Json)).StatusCode);
    }

    [PostgresFact]
    public async Task InFlightAuthorizedCommitCompletesBeforeRevocationAndCannotBeReplayedByOldToken()
    {
        var seat = await fixture.MovingSeat();
        var command = BotApiFixture.Move(seat);
        var authority = (await fixture.Credentials.Resolve(seat.Session.Token, default))!.Seat!;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executing = fixture.Matches.Execute(command, seat.Participant, beforeCommit: async () =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }, botAuthority: authority);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var revoking = fixture.Credentials.Revoke(seat.Grant);
        try
        {
            await fixture.WaitForLock("UPDATE bot_seat_grants SET revoked=true");
            Assert.False(revoking.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }
        Assert.True((await executing)!.Accepted);
        await revoking;
        Assert.Equal(seat.State.Revision + 1, (await fixture.Matches.Read(seat.State.Id, seat.Participant))!.State.Revision);
        using var bot = fixture.Client(seat.Session.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.PostAsJsonAsync($"/api/matches/{seat.State.Id}/commands", command,
            BoardCatalog.Json)).StatusCode);
        using var human = fixture.Client(seat.Human.Token, bot: false);
        Assert.Equal(seat.State.Revision + 1, (await BotApiFixture.Send(human, command)).Revision);
    }

    [PostgresFact]
    public async Task QueuedReadinessCannotSurviveReassignment()
    {
        var seat = await fixture.Room();
        using var bot = fixture.Client(seat.Session.Token);
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var held = await connection.BeginTransactionAsync();
        await using var locked = Database.Command(connection, held, "SELECT id FROM rooms WHERE id=$1 FOR UPDATE", seat.Room.Id);
        await locked.ExecuteScalarAsync();
        var queued = bot.PutAsJsonAsync($"/api/rooms/{seat.Room.Id}/ready", new RoomReadyRequest
        {
            ExpectedVersion = seat.Room.Version, Ready = true
        }, BoardCatalog.Json);
        try
        {
            await fixture.WaitForLock("SELECT state::text FROM rooms WHERE id=$1 FOR UPDATE");
            Assert.Equal(2, await fixture.Credentials.Reassign(seat.Grant, 1, DateTimeOffset.UtcNow.AddMinutes(10)));
        }
        finally
        {
            await held.RollbackAsync();
        }
        using var refused = await queued;
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        var current = await fixture.Rooms.Read(seat.Room.Id, seat.Participant);
        Assert.Equal(JsonSerializer.Serialize(seat.Room, BoardCatalog.Json), JsonSerializer.Serialize(current, BoardCatalog.Json));
    }

    [PostgresFact]
    public async Task ReadinessRechecksSupportedConfigurationUnderTheRoomLock()
    {
        var seat = await fixture.Room();
        using var bot = fixture.Client(seat.Session.Token);
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var held = await connection.BeginTransactionAsync();
        await using var locked = Database.Command(connection, held, "SELECT id FROM rooms WHERE id=$1 FOR UPDATE", seat.Room.Id);
        await locked.ExecuteScalarAsync();
        var nextVersion = seat.Room.Version + 1;
        var queued = bot.PutAsJsonAsync($"/api/rooms/{seat.Room.Id}/ready", new RoomReadyRequest
        {
            ExpectedVersion = nextVersion, Ready = true
        }, BoardCatalog.Json);
        try
        {
            await fixture.WaitForLock("SELECT state::text FROM rooms WHERE id=$1 FOR UPDATE");
            await using var configure = Database.Command(connection, held, """
                UPDATE rooms SET state=jsonb_set(jsonb_set(state,'{configuration}',$2::jsonb),'{version}',to_jsonb($3::int)) WHERE id=$1
                """, seat.Room.Id, JsonSerializer.Serialize(new MatchConfiguration { Fate = true }, BoardCatalog.Json), nextVersion);
            await configure.ExecuteNonQueryAsync();
            await held.CommitAsync();
        }
        catch
        {
            await held.RollbackAsync();
            throw;
        }
        using var refused = await queued;
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("BotConfigurationUnsupported", await refused.Content.ReadAsStringAsync());
        Assert.False((await fixture.Rooms.Read(seat.Room.Id, seat.Participant))!.ShadowAccepted);
    }

    [PostgresFact]
    public async Task RenewalPreservesScopeAndNeverExceedsLeaseOrRevivesOldGeneration()
    {
        var seat = await fixture.Seat();
        var lease = DateTimeOffset.UtcNow.AddSeconds(45);
        await fixture.Sql("UPDATE bot_seat_grants SET lease_until=$2 WHERE id=$1", seat.Grant, lease);
        await fixture.Sql("UPDATE bot_seat_credentials SET expires_at=clock_timestamp()+interval '10 seconds' WHERE grant_id=$1", seat.Grant);
        using var bot = fixture.Client(seat.Session.Token);
        using var renewed = await bot.PostAsync("/api/bot/seat/renew", null);
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.Equal("no-store", renewed.Headers.CacheControl!.ToString());
        using var json = JsonDocument.Parse(await renewed.Content.ReadAsStringAsync());
        var expires = json.RootElement.GetProperty("expiresAt").GetDateTimeOffset();
        Assert.InRange(expires, lease.AddMilliseconds(-1), lease);
        Assert.Single(json.RootElement.EnumerateObject());
        var authority = (await fixture.Credentials.Resolve(seat.Session.Token, default))!.Seat!;
        Assert.Equal(seat.State.Id, authority.MatchId);
        Assert.Equal(seat.Side, authority.Side);
        Assert.Equal(1, authority.Generation);
        Assert.Equal(2, await fixture.Credentials.Reassign(seat.Grant, 1, DateTimeOffset.UtcNow.AddMinutes(10)));
        Assert.Null(await fixture.Credentials.Reassign(seat.Grant, 1, DateTimeOffset.UtcNow.AddMinutes(10)));
        await Assert.ThrowsAsync<BotAuthorityExpiredException>(() => fixture.Credentials.Renew(authority, default));
        await Assert.ThrowsAsync<BotAuthorityExpiredException>(() => fixture.Credentials.Issue(seat.Grant, 1));
        var next = await fixture.Credentials.Issue(seat.Grant, 2);
        Assert.Null(await fixture.Credentials.Resolve(seat.Session.Token, default));
        using var replacement = fixture.Client(next.Token);
        Assert.Equal(HttpStatusCode.OK, (await replacement.GetAsync($"/api/matches/{seat.State.Id}")).StatusCode);
        await fixture.Credentials.Revoke(seat.Grant);
        await Assert.ThrowsAsync<BotAuthorityExpiredException>(() => fixture.Credentials.Issue(seat.Grant, 2));
    }

    private async Task LoseAuthority(BotTestSeat seat, string loss)
    {
        switch (loss)
        {
            case "revoke":
                await fixture.Credentials.Revoke(seat.Grant);
                break;
            case "generation":
                Assert.Equal(2, await fixture.Credentials.Reassign(seat.Grant, 1, DateTimeOffset.UtcNow.AddMinutes(10)));
                break;
            case "credential":
                await fixture.Sql("UPDATE bot_seat_credentials SET expires_at=clock_timestamp()-interval '1 second' WHERE grant_id=$1", seat.Grant);
                break;
            case "lease":
                await fixture.Sql("UPDATE bot_seat_grants SET lease_until=clock_timestamp()-interval '1 second' WHERE id=$1", seat.Grant);
                break;
            case "reissue":
                await fixture.Credentials.Issue(seat.Grant, 1);
                break;
            default:
                throw new ArgumentException(loss);
        }
    }

    private async Task ExpireAfterTransactionStarted(BotTestSeat seat, string loss)
    {
        // The deadline must be AFTER the queued request's transaction start and BEFORE its lock is released.
        // Replacing clock_timestamp() with transaction-start now() would incorrectly accept this command.
        var table = loss == "credential" ? "bot_seat_credentials" : "bot_seat_grants";
        var column = loss == "credential" ? "expires_at" : "lease_until";
        var key = loss == "credential" ? "grant_id" : "id";
        await fixture.Sql($"UPDATE {table} SET {column}=clock_timestamp()+interval '200 milliseconds' WHERE {key}=$1", seat.Grant);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var connection = await fixture.Database.Source.OpenConnectionAsync(timeout.Token);
        while (true)
        {
            await using var query = Database.Command(connection, null,
                $"SELECT bool_and({column}<=clock_timestamp()) FROM {table} WHERE {key}=$1", seat.Grant);
            if (await query.ExecuteScalarAsync(timeout.Token) is true)
                return;
            await Task.Delay(20, timeout.Token);
        }
    }
}
