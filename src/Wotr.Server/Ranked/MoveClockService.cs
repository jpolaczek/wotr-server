using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// RANK-06 (R35, docs/RANKED_PLAY.md D9): ends ranked matches whose decision ran out of time. Each second it beats a
// heartbeat and issues TimeOut for every passed deadline; the timeout names the timed decision and the revision read
// with it, so a move that went through first makes it stale and ends nothing. At start it gives every running clock
// the time the server was down, measured from the last heartbeat, so a deployment loses nobody a game.
public sealed class MoveClockService(Database database, MatchStore store, RevisionHub hub, RankedOptions options,
    TimeProvider clock, ILogger<MoveClockService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        if (options.ClockInterval <= TimeSpan.Zero)
            return;
        using var timer = new PeriodicTimer(options.ClockInterval, clock);
        // No deadline is judged before the running clocks have their downtime back.
        while (!await Attempt(() => Recover(stopping), "Ranked clocks did not get their downtime back.", stopping))
            await timer.WaitForNextTickAsync(stopping);
        while (await timer.WaitForNextTickAsync(stopping))
            await Attempt(() => Tick(stopping), "Ranked clocks were not checked this time.", stopping);
    }

    // An exception leaving a background service stops the whole server by default, and every game on it with the server.
    // A failure costs this attempt and nothing else.
    private async Task<bool> Attempt(Func<Task> work, string failure, CancellationToken stopping)
    {
        try
        {
            await work();
            return true;
        }
        catch (Exception error) when (!stopping.IsCancellationRequested)
        {
            logger.LogWarning(error, "{Failure}", failure);
            return false;
        }
    }

    // Moves every running deadline by the downtime since the last heartbeat (longer than two intervals), then beats.
    public async Task<TimeSpan> Recover(CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var now = clock.GetUtcNow();
        await using var read = Database.Command(connection, transaction, "SELECT beat_at FROM server_heartbeat WHERE id = 1 FOR UPDATE");
        var last = await read.ExecuteScalarAsync(cancellation) is DateTime beat ? new DateTimeOffset(beat, TimeSpan.Zero) : (DateTimeOffset?)null;
        var downtime = last is { } at && now - at > options.ClockInterval * 2 ? now - at : TimeSpan.Zero;
        if (downtime > TimeSpan.Zero)
        {
            await using var shift = Database.Command(connection, transaction,
                "UPDATE match_clocks SET deadline = deadline + $1, paused_at = paused_at + $1", downtime);
            await shift.ExecuteNonQueryAsync(cancellation);
            logger.LogInformation("Ranked clocks moved by {Downtime} of downtime.", downtime);
        }
        await Beat(connection, transaction, now, cancellation);
        await transaction.CommitAsync(cancellation);
        return downtime;
    }

    // One check: the heartbeat, then a timeout for every passed deadline. Returns how many matches ended.
    public async Task<int> Tick(CancellationToken cancellation = default)
    {
        var now = clock.GetUtcNow();
        List<DueClock> due;
        await using (var connection = await database.Source.OpenConnectionAsync(cancellation))
        {
            await Beat(connection, null, now, cancellation);
            foreach (var (match, revision) in await MatchClocks.ResumeLapsed(connection, now, options.PauseLimit, cancellation))
                hub.Publish(match, revision);
            due = await MatchClocks.Due(connection, now, cancellation);
        }
        var ended = 0;
        foreach (var clockRun in due)
            if (await TimeOut(clockRun, cancellation))
                ended++;
        return ended;
    }

    // One match that cannot be ended (its finishing transaction rolled back) waits for the next check and holds back
    // no other match.
    private async Task<bool> TimeOut(DueClock due, CancellationToken cancellation)
    {
        try
        {
            var receipt = await store.Execute(new GameCommand
            {
                MatchId = due.Match, CommandId = "timeout-" + due.DecisionId, Kind = CommandKind.TimeOut,
                DecisionId = due.DecisionId, ExpectedRevision = due.Revision, Payload = new()
            }, due.OwnerAccount, cancellation);
            if (receipt is not { Accepted: true })
                return false;
            hub.Publish(due.Match, receipt.Revision);
            return true;
        }
        catch (Exception error) when (!cancellation.IsCancellationRequested)
        {
            logger.LogError(error, "Ranked match {Match} could not be timed out.", due.Match);
            return false;
        }
    }

    private static async Task Beat(NpgsqlConnection connection, NpgsqlTransaction? transaction, DateTimeOffset now,
        CancellationToken cancellation)
    {
        await using var beat = Database.Command(connection, transaction,
            "INSERT INTO server_heartbeat(id, beat_at) VALUES (1, $1) ON CONFLICT (id) DO UPDATE SET beat_at = excluded.beat_at", now);
        await beat.ExecuteNonQueryAsync(cancellation);
    }
}
