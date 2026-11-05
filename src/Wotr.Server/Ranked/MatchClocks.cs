using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record MoveClockRow(string DecisionId, Side Owner, MoveClockKind Kind, int LimitMilliseconds, DateTimeOffset Deadline,
    DateTimeOffset? PausedAt, Side? PausedBy, int FreePeoplesPauses, int ShadowPauses, RankedPreset Preset);

public sealed record DueClock(string Match, string DecisionId, string OwnerAccount, long Revision);

// RANK-06 (R35, docs/RANKED_PLAY.md D9): the clock of a ranked match. Every new decision (a change of decision id or
// owner) starts its owner's clock with no credit carried over; a command that leaves the decision unchanged leaves the
// clock alone. The deadline is the shown limit plus MoveClock.Grace (plus the first decision's bonus).
public static class MatchClocks
{
    public const string ExpiredRefusal = "TimeExpired";
    public const string PausedRefusal = "MatchPaused";

    // The columns Row reads, in its order, for a query that joins match_clocks as c and ranked_matches as k.
    public const string Columns = "c.decision_id, c.owner, c.kind, c.limit_ms, c.deadline, c.paused_at, c.paused_by, k.fp_pauses, k.sh_pauses, k.preset";

    public static Task Start(NpgsqlConnection connection, NpgsqlTransaction transaction, MatchState state,
        RankedPreset preset, DateTimeOffset now, RankedOptions options, CancellationToken cancellation) =>
        Set(connection, transaction, state, preset, now + options.Scaled(MoveClock.FirstDecisionBonus), options, cancellation);

    // After an accepted command: a finished match has no clock; a new decision starts its owner's.
    public static async Task Follow(NpgsqlConnection connection, NpgsqlTransaction transaction, MatchState before,
        MatchState after, RankedPreset preset, DateTimeOffset now, RankedOptions options, CancellationToken cancellation)
    {
        if (after.Winner is not null || after.Pending is null)
        {
            await using var stop = Database.Command(connection, transaction, "DELETE FROM match_clocks WHERE match_id = $1", after.Id);
            await stop.ExecuteNonQueryAsync(cancellation);
            return;
        }
        if (before.Pending?.Id != after.Pending.Id || before.Pending?.Owner != after.Pending.Owner)
            await Set(connection, transaction, after, preset, now, options, cancellation);
    }

    // Why a ranked match refuses this command now, if it does. During a pause only a concession goes through; past the
    // deadline only the server's own timeout.
    public static async Task<string?> Refusal(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        CommandKind kind, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT paused_at IS NOT NULL, deadline < $2 FROM match_clocks WHERE match_id = $1", match, now);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return null;
        if (reader.GetBoolean(0))
            return kind == CommandKind.Concede ? null : PausedRefusal;
        return reader.GetBoolean(1) && kind != CommandKind.TimeOut ? ExpiredRefusal : null;
    }

    // The running clocks whose deadline has passed, with what the server needs to time them out.
    public static async Task<List<DueClock>> Due(NpgsqlConnection connection, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            SELECT c.match_id, c.decision_id, CASE WHEN c.owner = 'FreePeoples' THEN m.fp_account ELSE m.sh_account END, m.revision
            FROM match_clocks c JOIN matches m ON m.id = c.match_id
            WHERE c.deadline < $1 AND c.paused_at IS NULL ORDER BY c.deadline
            """, now);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var due = new List<DueClock>();
        while (await reader.ReadAsync(cancellation))
            due.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3)));
        return due;
    }

    // RANK-07 (D11): a pause ends by itself after its limit, so a match whose players are both gone cannot stay frozen.
    public static async Task<List<(string Match, long Revision)>> ResumeLapsed(NpgsqlConnection connection, DateTimeOffset now,
        TimeSpan limit, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            UPDATE match_clocks c SET deadline = c.deadline + ($1 - c.paused_at), paused_at = NULL, paused_by = NULL,
                version = c.version + 1
            FROM matches m
            WHERE m.id = c.match_id AND c.paused_at <= $2
            RETURNING c.match_id, m.revision
            """, now, now - limit);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var resumed = new List<(string, long)>();
        while (await reader.ReadAsync(cancellation))
            resumed.Add((reader.GetString(0), reader.GetInt64(1)));
        return resumed;
    }

    // As the client shows it, for one player: a paused clock stands at the moment it was paused.
    public static MoveClockView View(MoveClockRow row, Side viewer, DateTimeOffset now, RankedOptions options)
    {
        var (mine, theirs) = viewer == Side.FreePeoples
            ? (row.FreePeoplesPauses, row.ShadowPauses)
            : (row.ShadowPauses, row.FreePeoplesPauses);
        var from = row.PausedAt ?? now;
        return new()
        {
            Owner = row.Owner, Kind = row.Kind, DecisionId = row.DecisionId, LimitMilliseconds = row.LimitMilliseconds,
            RemainingMilliseconds = Math.Max(0, (long)(row.Deadline - options.Scaled(MoveClock.Grace) - from).TotalMilliseconds),
            WarningMilliseconds = (long)options.Scaled(MoveClock.Warning).TotalMilliseconds,
            Paused = row.PausedAt is not null, PausedBy = row.PausedBy,
            PausesLeft = MatchPauses.PerPlayer - mine, OpponentPausesLeft = MatchPauses.PerPlayer - theirs
        };
    }

    public static MoveClockRow? Row(NpgsqlDataReader reader, int first) => reader.IsDBNull(first)
        ? null
        : new(reader.GetString(first), Enum.Parse<Side>(reader.GetString(first + 1)), Enum.Parse<MoveClockKind>(reader.GetString(first + 2)),
            reader.GetInt32(first + 3), reader.GetFieldValue<DateTimeOffset>(first + 4),
            reader.IsDBNull(first + 5) ? null : reader.GetFieldValue<DateTimeOffset>(first + 5),
            reader.IsDBNull(first + 6) ? null : Enum.Parse<Side>(reader.GetString(first + 6)),
            reader.GetInt32(first + 7), reader.GetInt32(first + 8), Enum.Parse<RankedPreset>(reader.GetString(first + 9)));

    private static async Task Set(NpgsqlConnection connection, NpgsqlTransaction transaction, MatchState state,
        RankedPreset preset, DateTimeOffset from, RankedOptions options, CancellationToken cancellation)
    {
        var pending = state.Pending!;
        var kind = MoveClock.Kind(pending.Kind);
        var limit = options.Scaled(MoveClock.Limit(preset, kind));
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO match_clocks(match_id, decision_id, owner, kind, limit_ms, deadline) VALUES ($1, $2, $3, $4, $5, $6)
            ON CONFLICT (match_id) DO UPDATE SET decision_id = excluded.decision_id, owner = excluded.owner,
                kind = excluded.kind, limit_ms = excluded.limit_ms, deadline = excluded.deadline
            """, state.Id, pending.Id, pending.Owner.ToString(), kind.ToString(), (int)limit.TotalMilliseconds,
            from + limit + options.Scaled(MoveClock.Grace));
        await command.ExecuteNonQueryAsync(cancellation);
    }
}
