using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed record ActivityResult(MatchActivityView? View, string? Error = null);
public sealed record SeenResult(SeenRevisionView? View, string? Error = null);

public sealed class CorrespondenceStore(Database database)
{
    public async Task<ActivityResult> Activity(string id, string account, long? afterRevision,
        CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, """
            SELECT m.state::text, m.fp_account, COALESCE(r.revision,-1)
            FROM matches m LEFT JOIN match_reads r ON r.match_id=m.id AND r.account_id=$2
            WHERE m.id=$1 AND (m.fp_account=$2 OR m.sh_account=$2)
            """, id, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return new(null);
        var state = StateCodec.Read(reader.GetString(0));
        var side = reader.GetString(1) == account ? Side.FreePeoples : Side.Shadow;
        var seen = reader.GetInt64(2);
        var after = afterRevision ?? seen;
        if (after < -1 || after > state.Revision)
            return new(null, "InvalidRevision");
        var entries = HistoryProjection.For(state, side, after, 51);
        var truncated = entries.Count > 50;
        if (truncated)
            entries.RemoveAt(0);
        return new(new MatchActivityView
        {
            MatchId = id,
            Revision = state.Revision,
            LastSeenRevision = seen,
            AfterRevision = after,
            HasEarlierEntries = truncated,
            Entries = entries
        });
    }

    public async Task<SeenResult> MarkSeen(string id, string account, long revision,
        CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using var read = Database.Command(connection, transaction,
            "SELECT revision FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2) FOR SHARE", id, account);
        if (await read.ExecuteScalarAsync(cancellation) is not long current)
            return new(null);
        if (revision < 0 || revision > current)
            return new(null, "InvalidRevision");
        await using var update = Database.Command(connection, transaction, """
            INSERT INTO match_reads(match_id, account_id, revision) VALUES ($1,$2,$3)
            ON CONFLICT (match_id, account_id) DO UPDATE
            SET revision=GREATEST(match_reads.revision, EXCLUDED.revision)
            RETURNING revision
            """, id, account, revision);
        var seen = (long)(await update.ExecuteScalarAsync(cancellation))!;
        await transaction.CommitAsync(cancellation);
        return new(new SeenRevisionView { LastSeenRevision = seen });
    }
}
