using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public sealed record PauseResult(string Error, long Revision = 0);

// RANK-07 (R35, docs/RANKED_PLAY.md D11): each player may pause a ranked match three times, spent when the pause starts,
// and either player may unpause. Unpausing moves the deadline by the pause's length, so the same time is left. A pause
// takes the match's lock as commands do, and changes no game state, so the replay never sees it.
public sealed class MatchPauses(Database database, TimeProvider clock)
{
    public const int PerPlayer = 3;
    public const string AlreadyPaused = "AlreadyPaused";
    public const string NotPaused = "NotPaused";
    public const string NoPausesLeft = "NoPausesLeft";
    public const string GameFinished = "GameFinished";

    private sealed record Locked(Side Side, long Revision, DateTimeOffset Deadline, DateTimeOffset? PausedAt, int Spent);

    public async Task<PauseResult> Pause(string match, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var now = clock.GetUtcNow();
        var locked = await Lock(connection, transaction, match, account, cancellation);
        if (locked is null)
            return new(GameFinished);
        if (locked.PausedAt is not null)
            return new(AlreadyPaused);
        if (locked.Deadline < now)
            return new(MatchClocks.ExpiredRefusal);
        if (locked.Spent >= PerPlayer)
            return new(NoPausesLeft);
        await using var pause = Database.Command(connection, transaction,
            "UPDATE match_clocks SET paused_at = $2, paused_by = $3, version = version + 1 WHERE match_id = $1",
            match, now, locked.Side.ToString());
        await pause.ExecuteNonQueryAsync(cancellation);
        var column = locked.Side == Side.FreePeoples ? "fp_pauses" : "sh_pauses";
        await using var spend = Database.Command(connection, transaction,
            $"UPDATE ranked_matches SET {column} = {column} + 1 WHERE match_id = $1", match);
        await spend.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return new("", locked.Revision);
    }

    public async Task<PauseResult> Unpause(string match, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var locked = await Lock(connection, transaction, match, account, cancellation);
        if (locked is null)
            return new(GameFinished);
        if (locked.PausedAt is null)
            return new(NotPaused);
        await using var resume = Database.Command(connection, transaction,
            """
            UPDATE match_clocks SET deadline = deadline + ($2 - paused_at), paused_at = NULL, paused_by = NULL,
                version = version + 1
            WHERE match_id = $1
            """, match, clock.GetUtcNow());
        await resume.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return new("", locked.Revision);
    }

    // The match row first, as a command locks it; no row means the match ended and its clock went with it.
    private static async Task<Locked?> Lock(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            SELECT CASE WHEN m.fp_account = $2 THEN 'FreePeoples' ELSE 'Shadow' END, m.revision, c.deadline, c.paused_at,
                CASE WHEN m.fp_account = $2 THEN k.fp_pauses ELSE k.sh_pauses END
            FROM matches m
            JOIN match_clocks c ON c.match_id = m.id
            JOIN ranked_matches k ON k.match_id = m.id
            WHERE m.id = $1 AND (m.fp_account = $2 OR m.sh_account = $2)
            FOR UPDATE OF m
            """, match, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return null;
        return new(Enum.Parse<Side>(reader.GetString(0)), reader.GetInt64(1), reader.GetFieldValue<DateTimeOffset>(2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3), reader.GetInt32(4));
    }
}
