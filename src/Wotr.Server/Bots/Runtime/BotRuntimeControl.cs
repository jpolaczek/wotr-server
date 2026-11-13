using Npgsql;

namespace Wotr.Server;

public sealed record BotControlStatus(long Epoch, bool Draining, string? RuntimeVersion, long Assignments, long PendingProposals);
public sealed class BotRuntimeDrainingException : Exception;

public sealed partial class BotRuntimeControl(Database database)
{
    // Room/match -> control -> job. Drain has no room/match dependency.
    internal static async Task<bool> Running(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string runtimeVersion, CancellationToken cancellation, string? manifestHash = null)
    {
        await using var command = Database.Command(connection, transaction, """
            SELECT NOT draining AND (runtime_version IS NULL OR runtime_version=$1)
                AND (manifest_sha256 IS NULL OR manifest_sha256=$2)
            FROM bot_runtime_control WHERE id=1 FOR SHARE
            """, runtimeVersion, (manifestHash ?? "").ToUpperInvariant());
        return await command.ExecuteScalarAsync(cancellation) is true;
    }

    internal static async Task RequireRunning(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string runtimeVersion, CancellationToken cancellation, string? manifestHash = null)
    {
        if (!await Running(connection, transaction, runtimeVersion, cancellation, manifestHash))
            throw new BotRuntimeDrainingException();
    }

    public async Task<BotControlStatus> Status(CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var query = Database.Command(connection, null, """
            SELECT epoch,draining,runtime_version,
                (SELECT count(*) FROM bot_assignments WHERE runtime_version IS NOT NULL AND status<>'cancelled'),
                (SELECT count(*) FROM bot_runtime_proposals WHERE status='pending')
            FROM bot_runtime_control WHERE id=1
            """);
        await using var row = await query.ExecuteReaderAsync(cancellation);
        await row.ReadAsync(cancellation);
        return new(row.GetInt64(0), row.GetBoolean(1), row.IsDBNull(2) ? null : row.GetString(2), row.GetInt64(3), row.GetInt64(4));
    }

    public async Task<long> Drain(CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using var control = Database.Command(connection, transaction, """
            UPDATE bot_runtime_control SET draining=true,epoch=epoch+1,updated_at=clock_timestamp() WHERE id=1 RETURNING epoch
            """);
        var epoch = (long)(await control.ExecuteScalarAsync(cancellation))!;
        await using var fence = Database.Command(connection, transaction, """
            UPDATE bot_runtime_jobs SET generation=generation+1,owner_id='',lease_until='-infinity',
                health_until='-infinity',ready_version=NULL,last_error='Drained',available_at=clock_timestamp()
            """);
        await fence.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return epoch;
    }

    public async Task<bool> Resume(long epoch, string runtimeVersion, string manifestHash,
        CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, """
            UPDATE bot_runtime_control SET draining=false,updated_at=clock_timestamp(),
                runtime_version=COALESCE(runtime_version,$2),manifest_sha256=COALESCE(manifest_sha256,$3)
            WHERE id=1 AND draining AND epoch=$1 AND (runtime_version IS NULL OR runtime_version=$2)
                AND (manifest_sha256 IS NULL OR manifest_sha256=$3)
                AND NOT EXISTS(SELECT 1 FROM bot_assignments WHERE status<>'cancelled'
                    AND runtime_version IS NOT NULL AND runtime_version<>$2)
            """, epoch, runtimeVersion, manifestHash);
        return await command.ExecuteNonQueryAsync(cancellation) == 1;
    }
}
