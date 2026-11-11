using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotHostSchedulingTests
{
    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task HostReadiesReservedSeatAndStartsWithoutExternalCredentials(Side side)
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, room, work) = await fixture.Reserve(side);
        var seen = await fixture.Scalar("SELECT seen_at FROM rooms WHERE id=$1", room.Id);
        await fixture.Expire(work);
        await using var host = await KestrelProcess.Start(fixture.Connection, fixture.Settings);
        await fixture.Until("SELECT ready_version FROM bot_runtime_jobs WHERE assignment_id=$1", room.Version, work.AssignmentId);
        Assert.Equal(seen, await fixture.Scalar("SELECT seen_at FROM rooms WHERE id=$1", room.Id));
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        var started = await fixture.Rooms.Start(room.Id, human.AccountId, room.Version);
        Assert.Equal("", started.Error);
        if (side == Side.Shadow)
        {
            var state = (await fixture.Matches.Read(started.Room!.MatchId, human.AccountId))!.State;
            Assert.True((await fixture.Matches.Execute(KernelTests.Command(state, CommandKind.ContinueFellowship), human.AccountId))!.Accepted);
        }
        await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId);
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_seat_grants"));
    }

    [PostgresFact]
    public async Task GracefulHostShutdownDoesNotWaitForCancellationIgnoringRuntime()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game();
        var pause = Path.Combine(Path.GetTempPath(), "wotr-shutdown-" + Guid.NewGuid().ToString("N"));
        var settings = new Dictionary<string, string?>(fixture.Settings)
        {
            ["WOTR_FIXTURE_PAUSE_FILE"] = pause, ["Wotr:Bots:DecisionTimeoutMilliseconds"] = "100"
        };
        await File.WriteAllTextAsync(pause, "pause");
        await fixture.Expire(work);
        try
        {
            await using var host = await KestrelProcess.Start(fixture.Connection, settings);
            await fixture.Until("SELECT last_error FROM bot_runtime_jobs WHERE assignment_id=$1", "RuntimeTimeout", work.AssignmentId);
            Assert.True(File.Exists(pause));
            await host.StopGracefully();
            Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        }
        finally
        {
            File.Delete(pause);
            File.Delete(pause + ".entered");
        }
    }

    [PostgresFact]
    public async Task HumanDecisionAndAnotherSaveAreUntouchedWhileCpuWaits()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, _, work) = await fixture.Game(Side.Shadow, advanceHuman: false);
        var before = StateCodec.Write((await fixture.Matches.Read(work.MatchId, human.AccountId))!.State);
        var other = await MenuAccounts.Register(fixture.Host);
        var saved = MatchFactory.Create(Guid.NewGuid().ToString("N"), new SeededRandom(18));
        TurnFlow.Start(saved);
        await fixture.Matches.Create(saved, human.AccountId, other.AccountId);
        var savedBefore = StateCodec.Write((await fixture.Matches.Read(saved.Id, human.AccountId))!.State);
        await fixture.Expire(work);
        await using var host = await KestrelProcess.Start(fixture.Connection, fixture.Settings);
        await fixture.Until("SELECT generation>3 FROM bot_runtime_jobs WHERE assignment_id=$1", true, work.AssignmentId);
        Assert.Equal(before, StateCodec.Write((await fixture.Matches.Read(work.MatchId, human.AccountId))!.State));
        Assert.Equal(savedBefore, StateCodec.Write((await fixture.Matches.Read(saved.Id, human.AccountId))!.State));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_proposals"));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal(2L, await fixture.Scalar("SELECT count(*) FROM matches"));
    }

    [PostgresFact]
    public async Task CancellationIgnoringLibraryRetainsItsSlotAndLateAnswerCannotCommit()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game();
        await fixture.Execute("UPDATE bot_runtime_jobs SET lease_until='infinity' WHERE assignment_id=$1", work.AssignmentId);
        var (_, _, other) = await fixture.Game();
        var pause = Path.Combine(Path.GetTempPath(), "wotr-slot-" + Guid.NewGuid().ToString("N"));
        var settings = new Dictionary<string, string?>(fixture.Settings)
        {
            ["WOTR_FIXTURE_PAUSE_FILE"] = pause, ["Wotr:Bots:DecisionTimeoutMilliseconds"] = "100"
        };
        await File.WriteAllTextAsync(pause, "pause");
        await fixture.Expire(work);
        await fixture.Expire(other);
        try
        {
            await using var host = await KestrelProcess.Start(fixture.Connection, settings);
            await fixture.Until("SELECT count(*) FROM bot_runtime_jobs WHERE last_error='RuntimeTimeout'", 1L);
            await Task.Delay(250);
            var invocations = await File.ReadAllLinesAsync(pause + ".entered");
            Assert.Single(invocations);
            Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_proposals"));
            Assert.Equal(0L, await fixture.Scalar("SELECT sum(memory_version)::bigint FROM bot_runtime_jobs"));
            File.Delete(pause);
            await fixture.Until("SELECT sum(memory_version)::bigint FROM bot_runtime_jobs", 2L);
            Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE command_id=$1", invocations[0]));
            Assert.Equal(2L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE accepted"));
        }
        finally
        {
            File.Delete(pause);
            File.Delete(pause + ".entered");
        }
    }

    [PostgresFact]
    public async Task CommitRechecksLeaseAfterMatchLockAndDeletionRemovesPrivateState()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, _, work) = await fixture.Game();
        var (_, proposal) = await fixture.Stage(work);
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var held = Database.Command(connection, transaction, "SELECT id FROM matches WHERE id=$1 FOR UPDATE", work.MatchId))
            await held.ExecuteScalarAsync();
        var submit = fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work);
        await fixture.Until("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event_type='Lock' AND query LIKE '%SELECT m.state::text,m.fp_account%')", true);
        await fixture.Expire(work);
        await transaction.CommitAsync();
        await Assert.ThrowsAsync<BotWorkExpiredException>(() => submit);
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.True(await fixture.Host.Services.GetRequiredService<MatchRemoval>().Delete(work.MatchId, human.AccountId, default));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_jobs"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_proposals"));
        Assert.Null(await fixture.Jobs.Claim("deleted"));
    }
}
