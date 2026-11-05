using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record MoveClockRow(string DecisionId, Side Owner, MoveClockKind Kind, int LimitMilliseconds, DateTimeOffset Deadline);

public sealed record DueClock(string Match, string DecisionId, string OwnerAccount, long Revision);

// RANK-06 (R35, docs/RANKED_PLAY.md D9): the clock of a ranked match. Every new decision (a change of decision id or
// owner) starts its owner's clock with no credit carried over; a command that leaves the decision unchanged leaves the
// clock alone. The deadline is the shown limit plus MoveClock.Grace (plus the first decision's bonus).
public static class MatchClocks
{
    public const string ExpiredRefusal = "TimeExpired";

    public static Task Start(NpgsqlConnection connection, NpgsqlTransaction transaction, MatchState state,
        RankedPreset preset, DateTimeOffset now, CancellationToken cancellation) =>
        Set(connection, transaction, state, preset, now + MoveClock.FirstDecisionBonus, cancellation);

    // After an accepted command: a finished match has no clock; a new decision starts its owner's.
    public static async Task Follow(NpgsqlConnection connection, NpgsqlTransaction transaction, MatchState before,
        MatchState after, RankedPreset preset, DateTimeOffset now, CancellationToken cancellation)
    {
        if (after.Winner is not null || after.Pending is null)
        {
            await using var stop = Database.Command(connection, transaction, "DELETE FROM match_clocks WHERE match_id = $1", after.Id);
            await stop.ExecuteNonQueryAsync(cancellation);
            return;
        }
        if (before.Pending?.Id != after.Pending.Id || before.Pending?.Owner != after.Pending.Owner)
            await Set(connection, transaction, after, preset, now, cancellation);
    }

    public static async Task<bool> Expired(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT deadline < $2 FROM match_clocks WHERE match_id = $1", match, now);
        return await command.ExecuteScalarAsync(cancellation) is true;
    }

    // The clocks whose deadline has passed, with what the server needs to time them out.
    public static async Task<List<DueClock>> Due(NpgsqlConnection connection, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            SELECT c.match_id, c.decision_id, CASE WHEN c.owner = 'FreePeoples' THEN m.fp_account ELSE m.sh_account END, m.revision
            FROM match_clocks c JOIN matches m ON m.id = c.match_id
            WHERE c.deadline < $1 ORDER BY c.deadline
            """, now);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var due = new List<DueClock>();
        while (await reader.ReadAsync(cancellation))
            due.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3)));
        return due;
    }

    public static MoveClockView View(MoveClockRow row, DateTimeOffset now) => new()
    {
        Owner = row.Owner, Kind = row.Kind, DecisionId = row.DecisionId, LimitMilliseconds = row.LimitMilliseconds,
        RemainingMilliseconds = Math.Max(0, (long)(row.Deadline - MoveClock.Grace - now).TotalMilliseconds),
        WarningMilliseconds = (long)MoveClock.Warning.TotalMilliseconds
    };

    public static MoveClockRow? Row(NpgsqlDataReader reader, int first) => reader.IsDBNull(first)
        ? null
        : new(reader.GetString(first), Enum.Parse<Side>(reader.GetString(first + 1)), Enum.Parse<MoveClockKind>(reader.GetString(first + 2)),
            reader.GetInt32(first + 3), reader.GetFieldValue<DateTimeOffset>(first + 4));

    // The columns Row reads, in its order, for a query that joins match_clocks as c.
    public const string Columns = "c.decision_id, c.owner, c.kind, c.limit_ms, c.deadline";

    private static async Task Set(NpgsqlConnection connection, NpgsqlTransaction transaction, MatchState state,
        RankedPreset preset, DateTimeOffset from, CancellationToken cancellation)
    {
        var pending = state.Pending!;
        var kind = MoveClock.Kind(pending.Kind);
        var limit = MoveClock.Limit(preset, kind);
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO match_clocks(match_id, decision_id, owner, kind, limit_ms, deadline) VALUES ($1, $2, $3, $4, $5, $6)
            ON CONFLICT (match_id) DO UPDATE SET decision_id = excluded.decision_id, owner = excluded.owner,
                kind = excluded.kind, limit_ms = excluded.limit_ms, deadline = excluded.deadline
            """, state.Id, pending.Id, pending.Owner.ToString(), kind.ToString(), (int)limit.TotalMilliseconds,
            from + limit + MoveClock.Grace);
        await command.ExecuteNonQueryAsync(cancellation);
    }
}
