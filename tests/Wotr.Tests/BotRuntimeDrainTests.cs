using Microsoft.Extensions.DependencyInjection;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotRuntimeDrainTests
{
    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DrainFencesCapturedEvaluationOrJournalAndResumeCannotReviveIt(bool staged)
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(package.Settings());
        var (_, _, work) = await fixture.Game();
        var input = (await fixture.Inputs.Read(work))!;
        var proposal = fixture.Runtime.Decide(input.Request);
        if (staged)
            await fixture.Jobs.Stage(work, input.Request, proposal, input.HistoryPosition);
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var epoch = await control.Drain();
        Assert.Null(await fixture.Jobs.Claim("drained"));
        await Assert.ThrowsAsync<BotRuntimeDrainingException>(() => fixture.Jobs.Stage(work, input.Request, proposal, input.HistoryPosition));
        if (staged)
            await Assert.ThrowsAsync<BotRuntimeDrainingException>(() => fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs"));
        Assert.True(await control.Resume(epoch, "0.2.0", package.Hash));
        await Assert.ThrowsAsync<BotWorkExpiredException>(() => fixture.Jobs.Stage(work, input.Request, proposal, input.HistoryPosition));
        var fresh = (await fixture.Jobs.Claim("after-drain"))!;
        if (!staged)
            await fixture.Jobs.Stage(fresh, input.Request, proposal, input.HistoryPosition);
        var pending = (await fixture.Jobs.Pending(fresh))!;
        Assert.Equal(proposal.Command!.CommandId, pending.CommandId);
        Assert.True((await fixture.Matches.Execute(pending, fresh.ParticipantId, runtimeWork: fresh))!.Accepted);
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs"));
    }

    [PostgresFact]
    public async Task DrainAcknowledgesBeforeAQueuedMoveCanAcquireTheMatchLockAndThatMoveIsRefused()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(package.Settings());
        var (_, _, work) = await fixture.Game();
        var (_, proposal) = await fixture.Stage(work);
        await using var connection = await fixture.Database.Source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var held = Database.Command(connection, transaction, "SELECT id FROM matches WHERE id=$1 FOR UPDATE", work.MatchId))
            await held.ExecuteScalarAsync();
        var submission = fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work);
        await fixture.Until("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event_type='Lock' AND query LIKE '%SELECT state::text,fp_account%')", true);
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        await control.Drain();
        Assert.False(submission.IsCompleted);
        await transaction.CommitAsync();
        await Assert.ThrowsAsync<BotRuntimeDrainingException>(() => submission);
        Assert.Equal("pending", await fixture.Scalar("SELECT status FROM bot_runtime_proposals"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM command_receipts"));
    }

    [PostgresFact]
    public async Task DrainWaitsForAnAlreadyCommittingMoveAndNeverAcknowledgesTooEarly()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game();
        var (_, proposal) = await fixture.Stage(work);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var submission = fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work,
            beforeCommit: async () => { entered.SetResult(); await release.Task; });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var drain = control.Drain();
        try
        {
            await fixture.Until("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event_type='Lock' AND query LIKE '%UPDATE bot_runtime_control SET draining=true%')", true);
            Assert.False(drain.IsCompleted);
        }
        finally { release.SetResult(); }
        Assert.True((await submission)!.Accepted);
        await drain;
        Assert.True((await control.Status()).Draining);
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs"));
    }

    [PostgresFact]
    public async Task DrainDisablesReadinessAndNewSeatsAndSurvivesActualServerRestart()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(package.Settings());
        var (human, room, work) = await fixture.Reserve();
        Assert.True(await fixture.Rooms.ReadyRuntime(work));
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var epoch = await control.Drain();
        Assert.False(await fixture.Rooms.ReadyRuntime(work));
        Assert.Equal("BotUnavailable", (await fixture.Rooms.Start(room.Id, human.AccountId, room.Version)).Error);
        var other = await MenuAccounts.Register(fixture.Host);
        var empty = await fixture.Rooms.Create(other.AccountId, new Wotr.Contracts.CreateRoomRequest { Name = "Drain fixture" });
        Assert.Equal("BotUnavailable", (await fixture.Rooms.AddBot(empty.Id, other.AccountId, PveFixture.Request(empty))).Error);
        await using (var host = await KestrelProcess.Start(fixture.Connection, fixture.Settings))
        {
            var status = await package.Operator(fixture.Connection, "status");
            Assert.Equal(0, status.ExitCode);
            Assert.Contains("\"draining\":true", status.Output);
            Assert.Equal(DBNull.Value, await fixture.Scalar("SELECT ready_version FROM bot_runtime_jobs"));
            Assert.True((await control.Status()).Draining);
        }
        Assert.False(await control.Resume(epoch - 1, "0.2.0", package.Hash));
        Assert.True(await control.Resume(epoch, "0.2.0", package.Hash));
        var fresh = (await fixture.Jobs.Claim("resumed"))!;
        Assert.True(await fixture.Rooms.ReadyRuntime(fresh));
        Assert.Equal("", (await fixture.Rooms.Start(room.Id, human.AccountId, room.Version)).Error);
    }
}
