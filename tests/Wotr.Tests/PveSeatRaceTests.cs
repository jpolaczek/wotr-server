using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[Collection(PveCollection.Name)]
public sealed class PveSeatRaceTests(PveFixture fixture)
{
    [PostgresFact]
    public async Task DuplicateConcurrentRequestsCreateOneReservation()
    {
        var human = await fixture.Human();
        var room = await fixture.Room(human);
        var request = PveFixture.Request(room);
        var results = await Task.WhenAll(fixture.Rooms.AddBot(room.Id, human.AccountId, request),
            fixture.Rooms.AddBot(room.Id, human.AccountId, request));
        Assert.All(results, result => Assert.Equal("", result.Error));
        Assert.Equal(results[0].Room!.Version, results[1].Room!.Version);
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE room_id=$1", room.Id));
    }

    [PostgresFact]
    public async Task HumanAdmissionAndCpuReservationNeverReplaceEachOther()
    {
        var human = await fixture.Human();
        var guest = await fixture.Human();
        var room = await fixture.Room(human);
        var results = await Task.WhenAll(fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(room)),
            fixture.Rooms.JoinPublic(room.Id, guest.AccountId));
        Assert.Single(results, r => r.Error == "");
        var actual = (await fixture.Rooms.Read(room.Id, human.AccountId))!;
        Assert.Equal(actual.Bot is null ? 0L : 1L,
            await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE room_id=$1", room.Id));
        Assert.Equal(actual.Bot is null ? guest.Username : "CPU", actual.Shadow);
    }

    [PostgresFact]
    public async Task ConfigurationAndReservationUseOneVersionBoundary()
    {
        var human = await fixture.Human();
        var room = await fixture.Room(human);
        var results = await Task.WhenAll(fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(room)),
            fixture.Rooms.Configure(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Configuration = new() }));
        Assert.Single(results, r => r.Error == "");
        Assert.Single(results, r => r.Error == "StaleConfiguration");
    }

    [PostgresFact]
    public async Task StartAndRemovalLeaveEitherOneSavedGameOrCancelledReservation()
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        var ready = await fixture.ReadyCpu(room);
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        var results = await Task.WhenAll(fixture.Rooms.Start(room.Id, human.AccountId, room.Version),
            fixture.Rooms.Remove(room.Id, human.AccountId));
        Assert.Single(results, r => r.Error == "");
        var actual = (await fixture.Rooms.Read(room.Id, human.AccountId))!;
        var status = await fixture.Scalar("SELECT status FROM bot_assignments WHERE id=$1", ready.Grant);
        Assert.Equal(actual.MatchId.Length > 0 ? "started" : "cancelled", status);
        Assert.Equal(actual.MatchId.Length > 0 ? 1L : 0L,
            await fixture.Scalar("SELECT count(*) FROM matches WHERE fp_account=$1", human.AccountId));
    }

    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartRechecksRevocationOrLeaseAfterWaitingForGrantLock(bool expiry)
    {
        var human = await fixture.Human();
        var room = await fixture.Reserve(human, await fixture.Room(human));
        var ready = await fixture.ReadyCpu(room);
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        if (expiry)
            await fixture.Scalar("UPDATE bot_seat_grants SET lease_until=clock_timestamp()+interval '2 seconds' WHERE id=$1", ready.Grant);
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var hold = Database.Command(connection, transaction,
            "SELECT id FROM bot_seat_grants WHERE id=$1 FOR UPDATE", ready.Grant);
        await hold.ExecuteScalarAsync();
        var start = fixture.Rooms.Start(room.Id, human.AccountId, room.Version);
        await fixture.WaitForLock("SELECT lease_until FROM bot_seat_grants");
        if (expiry)
            await Task.Delay(2200);
        else
        {
            await using var revoke = Database.Command(connection, transaction,
                "UPDATE bot_seat_grants SET revoked=true WHERE id=$1", ready.Grant);
            await revoke.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        Assert.Equal("BotNotReady", (await start).Error);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM matches WHERE fp_account=$1", human.AccountId));
    }
}
