using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotHostRecoveryTests
{
    [PostgresTheory]
    [InlineData("before-journal", false)]
    [InlineData("after-journal", false)]
    [InlineData("before-commit", false)]
    [InlineData("after-commit", false)]
    [InlineData("before-journal", true)]
    [InlineData("after-journal", true)]
    [InlineData("before-commit", true)]
    [InlineData("after-commit", true)]
    public async Task KilledKestrelRecoversOnceWithoutPlayerReadsIncludingFinalMove(string boundary, bool final)
    {
        await using var fixture = await BotHostFixture.Create();
        MatchState? seed = null;
        if (final)
        {
            seed = MordorTests.OnTrack(4, 10);
            HuntTests.Pool(seed, "base.hunt.standard.1.0.1");
            seed = HuntTests.Move(seed, new SequenceRandom(0));
        }
        var (_, _, work) = await fixture.Game(seed: seed);
        var before = (await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State;
        var pause = Path.Combine(Path.GetTempPath(), "wotr-runtime-pause-" + Guid.NewGuid().ToString("N"));
        var settings = new Dictionary<string, string?>(fixture.Settings);
        await using var held = await fixture.Database.Source.OpenConnectionAsync();
        NpgsqlTransaction? transaction = null;
        if (boundary == "before-journal")
        {
            await File.WriteAllTextAsync(pause, "pause");
            settings["WOTR_FIXTURE_PAUSE_FILE"] = pause;
        }
        else if (boundary == "after-journal")
        {
            transaction = await held.BeginTransactionAsync();
            await using var locked = Database.Command(held, transaction, "SELECT id FROM matches WHERE id=$1 FOR UPDATE", work.MatchId);
            await locked.ExecuteScalarAsync();
        }
        else if (boundary == "before-commit")
        {
            await fixture.Execute("""
                CREATE FUNCTION fixture_pause_commit() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW.status='accepted' THEN PERFORM pg_advisory_xact_lock(682018); END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER fixture_pause BEFORE UPDATE ON bot_runtime_proposals
                    FOR EACH ROW EXECUTE FUNCTION fixture_pause_commit();
                """);
            await using var locked = Database.Command(held, null, "SELECT pg_advisory_lock(682018)");
            await locked.ExecuteScalarAsync();
        }
        await fixture.Expire(work);
        string? commandId = null;
        try
        {
            await using (var first = await KestrelProcess.Start(fixture.Connection, settings))
            {
                if (boundary == "before-journal")
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    while (!File.Exists(pause + ".entered"))
                        await Task.Delay(20, timeout.Token);
                    Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_proposals"));
                }
                else if (boundary == "after-journal")
                    await fixture.Until("SELECT count(*) FROM bot_runtime_proposals WHERE status='pending'", 1L);
                else if (boundary == "before-commit")
                    await fixture.Until("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event='advisory' AND query LIKE '%UPDATE bot_runtime_proposals%')", true);
                else
                    await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId);
                commandId = await fixture.Scalar("SELECT invocation_id FROM bot_runtime_proposals WHERE assignment_id=$1", work.AssignmentId) as string;
                if (boundary != "after-commit")
                {
                    Assert.Equal(before.Revision, await fixture.Matches.ReadRevision(work.MatchId, work.ParticipantId, default));
                    Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
                    Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE account_id=$1", work.ParticipantId));
                }
            } // Actual process-tree kill, not a replacement service object.
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
            await using var unlock = Database.Command(held, null, "SELECT pg_advisory_unlock_all()");
            await unlock.ExecuteNonQueryAsync();
            File.Delete(pause);
            File.Delete(pause + ".entered");
        }
        await using var restarted = await KestrelProcess.Start(fixture.Connection, fixture.Settings);
        await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId);
        await Task.Delay(250);
        var after = (await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State;
        Assert.Equal(before.Revision + 1, after.Revision);
        Assert.Equal(final ? Side.FreePeoples : null, after.Winner);
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE account_id=$1 AND accepted", work.ParticipantId));
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal("accepted", await fixture.Scalar("SELECT status FROM bot_runtime_proposals WHERE assignment_id=$1", work.AssignmentId));
        if (commandId is not null)
            Assert.True((await fixture.Matches.Receipt(work.MatchId, commandId, work.ParticipantId))!.Accepted);
        Assert.DoesNotContain("fixture-private-memory", StateCodec.Write(after));
    }

    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrThrowingRuntimePreservesPendingDecisionUntilHealthyRestart(bool throws)
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game();
        var before = StateCodec.Write((await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State);
        var settings = new Dictionary<string, string?>(fixture.Settings);
        if (throws)
            await fixture.Execute("UPDATE bot_runtime_jobs SET memory='throw' WHERE assignment_id=$1", work.AssignmentId);
        else
            settings["Wotr:Bots:RuntimeAssembly"] = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".dll");
        await fixture.Expire(work);
        await using (var failed = await KestrelProcess.Start(fixture.Connection, settings))
        {
            if (throws)
                await fixture.Until("SELECT last_error FROM bot_runtime_jobs WHERE assignment_id=$1", "RuntimeRefused", work.AssignmentId);
            else
                await Task.Delay(300);
            Assert.Equal(before, StateCodec.Write((await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State));
            Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
            Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE account_id=$1", work.ParticipantId));
        }
        if (throws)
            await fixture.Execute("UPDATE bot_runtime_jobs SET memory=NULL WHERE assignment_id=$1", work.AssignmentId);
        await using var healthy = await KestrelProcess.Start(fixture.Connection, fixture.Settings);
        await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId);
    }
}
