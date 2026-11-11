using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotHostRoomTests
{
    [PostgresFact]
    public async Task ReadinessDoesNotKeepAbandonedRoomsAliveAndConfigurationFencesCapturedWork()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, room, old) = await fixture.Reserve();
        var seen = await fixture.Scalar("SELECT seen_at FROM rooms WHERE id=$1", room.Id);
        Assert.True(await fixture.Rooms.ReadyRuntime(old));
        Assert.Equal(seen, await fixture.Scalar("SELECT seen_at FROM rooms WHERE id=$1", room.Id));
        Assert.Equal(BotSeatStatus.Ready, (await fixture.Rooms.Read(room.Id, human.AccountId, refreshPresence: false))!.Bot!.Status);
        var configured = await fixture.Rooms.Configure(room.Id, human.AccountId,
            new() { ExpectedVersion = room.Version, Configuration = new() });
        Assert.Equal("", configured.Error);
        Assert.False(await fixture.Rooms.ReadyRuntime(old));
        Assert.Null(await fixture.Scalar("SELECT ready_version FROM bot_runtime_jobs WHERE assignment_id=$1", old.AssignmentId) as int?);
        var fresh = (await fixture.Jobs.Claim("new-configuration"))!;
        Assert.True(await fixture.Rooms.ReadyRuntime(fresh));
        await fixture.Rooms.Remove(room.Id, human.AccountId);
        Assert.False(await fixture.Rooms.ReadyRuntime(fresh));
        Assert.Null((await fixture.Rooms.Read(room.Id, human.AccountId))!.Bot);
        Assert.Equal("cancelled", await fixture.Scalar("SELECT status FROM bot_assignments WHERE id=$1", fresh.AssignmentId));
        Assert.Null(await fixture.Jobs.Claim("removed"));
    }

    [PostgresFact]
    public async Task StartRechecksFreshDatabaseTimeAfterWaitingForJobLock()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, room, work) = await fixture.Reserve();
        Assert.True(await fixture.Rooms.ReadyRuntime(work));
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var held = Database.Command(connection, transaction,
            "SELECT assignment_id FROM bot_runtime_jobs WHERE assignment_id=$1 FOR UPDATE", work.AssignmentId))
            await held.ExecuteScalarAsync();
        var start = fixture.Rooms.Start(room.Id, human.AccountId, room.Version);
        await fixture.Until("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event_type='Lock' AND query LIKE '%SELECT health_until,ready_version%')", true);
        await using (var expire = Database.Command(connection, transaction,
            "UPDATE bot_runtime_jobs SET health_until=clock_timestamp()-interval '1 second' WHERE assignment_id=$1", work.AssignmentId))
            await expire.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        Assert.Equal("BotNotReady", (await start).Error);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM matches"));
        Assert.Equal(BotSeatStatus.Unavailable, (await fixture.Rooms.Read(room.Id, human.AccountId))!.Bot!.Status);
    }

    [PostgresFact]
    public async Task AcceptedPolicyVersionModulesAndRuntimePinAreIndependentAdmissionGates()
    {
        await using var fixture = await BotHostFixture.Create();
        var services = fixture.Host.Services;
        var options = services.GetRequiredService<BotHostOptions>();
        var rooms = services.GetRequiredService<BotRoomOptions>();
        var access = services.GetRequiredService<BotAccessOptions>();
        var accepted = services.GetRequiredService<BotRuntimeCapabilities>();
        Assert.Null(accepted.Unavailable(new()));
        Assert.Equal("BotVersionUnsupported", accepted.Unavailable(new(), "different"));
        Assert.Equal("BotVersionUnsupported", accepted.Unavailable(new(), "fixture-1", "0.1.0"));
        var enabledModules = access with { Features = BotAccessOptions.KnownFeatures.ToHashSet() };
        Assert.Equal("BotConfigurationUnsupported", new BotRuntimeCapabilities(options, rooms, enabledModules, fixture.Runtime)
            .Unavailable(new() { Lords = true }));
        Assert.Equal("BotUnavailable", new BotRuntimeCapabilities(options with { Enabled = false }, rooms, access, fixture.Runtime).Unavailable(new()));
        using var absent = EmbeddedBotRuntime.Load(null);
        Assert.Equal("BotUnavailable", new BotRuntimeCapabilities(options, rooms, access, absent).Unavailable(new()));
        // Embedded mode neither creates nor needs any remote control or seat credential.
        await fixture.Reserve();
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_seat_grants"));
        Assert.False(access.Enabled);
    }

    [PostgresFact]
    public async Task ReadinessCannotReviveASeatAfterWaitingPastItsEvaluationLease()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, room, work) = await fixture.Reserve();
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var held = Database.Command(connection, transaction, "SELECT id FROM rooms WHERE id=$1 FOR UPDATE", room.Id))
            await held.ExecuteScalarAsync();
        var ready = fixture.Rooms.ReadyRuntime(work);
        await fixture.Until("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event_type='Lock' AND query LIKE '%SELECT state::text FROM rooms%')", true);
        await fixture.Expire(work);
        await transaction.CommitAsync();
        await Assert.ThrowsAsync<BotWorkExpiredException>(() => ready);
        Assert.Equal(BotSeatStatus.Joining, (await fixture.Rooms.Read(room.Id, human.AccountId))!.Bot!.Status);
    }
}
