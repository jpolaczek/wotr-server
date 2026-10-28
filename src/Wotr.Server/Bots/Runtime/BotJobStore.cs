using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed record BotWork(string AssignmentId, string RoomId, int RoomVersion, string MatchId,
    string ParticipantId, Side Side, string PolicyVersion, string RuntimeVersion, long Generation,
    string OwnerId, long MemoryVersion, long HistoryPosition, string? Memory, BotRuntimeIdentity? Compatibility = null,
    double QueueSeconds = 0, string? ManifestSha256 = null)
{
    public override string ToString() => "BotWork [private]";
}

public sealed class BotWorkExpiredException : Exception;

// Claims never hold a room/match lock. Mutations lock room/match before this job.
public sealed partial class BotJobStore(Database database, BotHostOptions options, EmptyRoomOptions emptyRooms)
{
    public async Task<BotWork?> Claim(string owner, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        if (!await BotRuntimeControl.Running(connection, transaction, options.RuntimeVersion!, cancellation, options.ManifestSha256))
            return null;
        await using var query = Database.Command(connection, transaction, """
            SELECT j.assignment_id,a.room_id,a.room_version,COALESCE(a.match_id,''),a.participant_id,a.side,
                a.policy_version,a.runtime_version,j.generation,j.memory_version,j.history_position,j.memory,
                j.compatibility::text,CASE WHEN j.available_at='-infinity' THEN 0 ELSE
                    GREATEST(0,extract(epoch FROM clock_timestamp()-j.available_at))::double precision END
            FROM bot_runtime_jobs j JOIN bot_assignments a ON a.id=j.assignment_id
            JOIN rooms r ON r.id=a.room_id LEFT JOIN matches m ON m.id=a.match_id
            WHERE a.status<>'cancelled' AND a.runtime_version=$1 AND NOT r.closed
                AND j.available_at<=clock_timestamp() AND j.lease_until<=clock_timestamp()
                AND (a.status='started' AND (m.state->>'winner' IS NULL OR EXISTS
                    (SELECT 1 FROM bot_runtime_proposals p WHERE p.assignment_id=a.id AND p.status='pending')) OR a.status='pending'
                    AND r.seen_at>clock_timestamp()-make_interval(secs => $2))
            ORDER BY j.available_at,j.assignment_id FOR UPDATE OF j SKIP LOCKED LIMIT 1
            """, options.RuntimeVersion!, emptyRooms.EmptyAfter.TotalSeconds);
        BotWork work;
        await using (var row = await query.ExecuteReaderAsync(cancellation))
        {
            if (!await row.ReadAsync(cancellation))
                return null;
            work = new(row.GetString(0), row.GetString(1), row.GetInt32(2), row.GetString(3), row.GetString(4),
                Enum.Parse<Side>(row.GetString(5)), row.GetString(6), row.GetString(7), row.GetInt64(8) + 1,
                owner, row.GetInt64(9), row.GetInt64(10), row.IsDBNull(11) ? null : row.GetString(11),
                row.IsDBNull(12) ? null : JsonSerializer.Deserialize<BotRuntimeIdentity>(row.GetString(12), BotRuntimeProtocol.Json),
                row.GetDouble(13), options.ManifestSha256);
        }
        await using var claim = Database.Command(connection, transaction, """
            UPDATE bot_runtime_jobs SET generation=$2,owner_id=$3,
                lease_until=clock_timestamp()+make_interval(secs => $4) WHERE assignment_id=$1
            """, work.AssignmentId, work.Generation, owner, options.Lease.TotalSeconds);
        await claim.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return work;
    }

    internal static async Task Lock(NpgsqlConnection connection, NpgsqlTransaction transaction,
        BotWork work, CancellationToken cancellation)
    {
        await using var query = Database.Command(connection, transaction,
            "SELECT generation,owner_id,lease_until FROM bot_runtime_jobs WHERE assignment_id=$1 FOR UPDATE", work.AssignmentId);
        DateTime expiry;
        await using (var row = await query.ExecuteReaderAsync(cancellation))
        {
            if (!await row.ReadAsync(cancellation) || row.GetInt64(0) != work.Generation || row.GetString(1) != work.OwnerId)
                throw new BotWorkExpiredException();
            expiry = row.GetDateTime(2);
        }
        // now() is the transaction start, which may precede a long lock wait.
        await using var clock = Database.Command(connection, transaction, "SELECT clock_timestamp()");
        if (expiry <= (DateTime)(await clock.ExecuteScalarAsync(cancellation))!)
            throw new BotWorkExpiredException();
    }

    public async Task<GameCommand?> Pending(BotWork work, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var query = Database.Command(connection, null,
            "SELECT command::text FROM bot_runtime_proposals WHERE assignment_id=$1 AND status='pending'", work.AssignmentId);
        return await query.ExecuteScalarAsync(cancellation) is string json
            ? JsonSerializer.Deserialize<GameCommand>(json, BoardCatalog.Json) : null;
    }

    public async Task Stage(BotWork work, BotRuntimeRequest input, BotRuntimeResponse proposal,
        long historyPosition, CancellationToken cancellation = default)
    {
        if (input.MatchId != work.MatchId || input.Side != work.Side || input.Memory != work.Memory ||
            proposal.Status != "Proposal" || proposal.PolicyVersion != work.PolicyVersion ||
            !BotRuntimeProtocol.Valid(proposal, input) || !CommandShape.IsValid(proposal.Command!))
            throw new ArgumentException("Invalid bot proposal.");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await BotRuntimeControl.RequireRunning(connection, transaction, work.RuntimeVersion, cancellation, work.ManifestSha256);
        await Lock(connection, transaction, work, cancellation);
        await using var stage = Database.Command(connection, transaction, """
            INSERT INTO bot_runtime_proposals(assignment_id,invocation_id,memory_version,history_position,
                command,fingerprint,proposed_memory,status)
            SELECT $1,$2,$3,$4,$5::jsonb,$6,$7,'pending' FROM bot_runtime_jobs j
            JOIN bot_assignments a ON a.id=j.assignment_id
            WHERE j.assignment_id=$1 AND j.memory_version=$3 AND a.status='started' AND a.match_id=$8
                AND a.runtime_version=$9 AND a.policy_version=$10 AND a.participant_id=$11
            ON CONFLICT (assignment_id) DO UPDATE SET invocation_id=EXCLUDED.invocation_id,
                memory_version=EXCLUDED.memory_version,history_position=EXCLUDED.history_position,
                command=EXCLUDED.command,fingerprint=EXCLUDED.fingerprint,proposed_memory=EXCLUDED.proposed_memory,
                status='pending',result_revision=NULL,error=''
            WHERE bot_runtime_proposals.status<>'pending'
            """, work.AssignmentId, input.InvocationId, work.MemoryVersion, historyPosition,
            JsonSerializer.Serialize(proposal.Command, BoardCatalog.Json), CommandFingerprint.Create(proposal.Command!),
            proposal.ProposedMemory!, work.MatchId, work.RuntimeVersion, work.PolicyVersion, work.ParticipantId);
        if (await stage.ExecuteNonQueryAsync(cancellation) != 1)
            throw new BotWorkExpiredException();
        await transaction.CommitAsync(cancellation);
    }

    public async Task Release(BotWork work, string error = "", CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var query = Database.Command(connection, null, """
            UPDATE bot_runtime_jobs SET owner_id='',lease_until='-infinity',
                available_at=clock_timestamp()+make_interval(secs => $4),last_error=$5,
                health_until=CASE WHEN $5='' THEN clock_timestamp()+make_interval(secs => $6) ELSE '-infinity' END
            WHERE assignment_id=$1 AND generation=$2 AND owner_id=$3
            """, work.AssignmentId, work.Generation, work.OwnerId,
            (error.Length == 0 ? options.Poll : options.Retry).TotalSeconds, error, options.Lease.TotalSeconds);
        await query.ExecuteNonQueryAsync(cancellation);
    }
}
