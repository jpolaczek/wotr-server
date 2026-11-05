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
        await Recover(stopping);
        using var timer = new PeriodicTimer(options.ClockInterval, clock);
        while (await timer.WaitForNextTickAsync(stopping))
        {
            try
            {
                await Tick(stopping);
            }
            catch (Exception error) when (error is NpgsqlException or InvalidOperationException)
            {
                logger.LogWarning(error, "Ranked clocks were not checked this time.");
            }
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
                "UPDATE match_clocks SET deadline = deadline + $1", downtime);
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
            due = await MatchClocks.Due(connection, now, cancellation);
        }
        var ended = 0;
        foreach (var clockRun in due)
            if (await TimeOut(clockRun, cancellation))
                ended++;
        return ended;
    }

    private async Task<bool> TimeOut(DueClock due, CancellationToken cancellation)
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

    private static async Task Beat(NpgsqlConnection connection, NpgsqlTransaction? transaction, DateTimeOffset now,
        CancellationToken cancellation)
    {
        await using var beat = Database.Command(connection, transaction,
            "INSERT INTO server_heartbeat(id, beat_at) VALUES (1, $1) ON CONFLICT (id) DO UPDATE SET beat_at = excluded.beat_at", now);
        await beat.ExecuteNonQueryAsync(cancellation);
    }
}
