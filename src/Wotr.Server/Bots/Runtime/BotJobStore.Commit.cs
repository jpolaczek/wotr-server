using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public sealed partial class BotJobStore
{
    // Called only inside ordinary MatchStore.Execute, after its match lock.
    internal static async Task Verify(NpgsqlConnection connection, NpgsqlTransaction transaction,
        BotWork work, GameCommand request, string account, Side side, CancellationToken cancellation)
    {
        if (work.MatchId != request.MatchId || work.ParticipantId != account || work.Side != side)
            throw new BotWorkExpiredException();
        await Lock(connection, transaction, work, cancellation);
        await using var query = Database.Command(connection, transaction, """
            SELECT EXISTS(SELECT 1 FROM bot_runtime_jobs j JOIN bot_assignments a ON a.id=j.assignment_id
                JOIN bot_runtime_proposals p ON p.assignment_id=j.assignment_id
                WHERE a.id=$1 AND a.status='started' AND a.match_id=$2 AND a.participant_id=$3 AND a.side=$4
                    AND a.runtime_version=$5 AND a.policy_version=$6 AND p.status='pending'
                    AND p.invocation_id=$7 AND p.fingerprint=$8 AND p.memory_version=j.memory_version)
            """, work.AssignmentId, work.MatchId, account, side.ToString(), work.RuntimeVersion, work.PolicyVersion,
            request.CommandId, CommandFingerprint.Create(request));
        if (await query.ExecuteScalarAsync(cancellation) is not true)
            throw new BotWorkExpiredException();
    }

    internal static async Task Commit(NpgsqlConnection connection, NpgsqlTransaction transaction,
        BotWork work, CommandReceipt receipt, CancellationToken cancellation)
    {
        if (receipt.Accepted)
        {
            await using var memory = Database.Command(connection, transaction, """
                UPDATE bot_runtime_jobs j SET memory=p.proposed_memory,memory_version=j.memory_version+1,
                    history_position=p.history_position,accepted_revision=$2
                FROM bot_runtime_proposals p WHERE j.assignment_id=$1 AND p.assignment_id=j.assignment_id
                    AND p.status='pending' AND p.memory_version=j.memory_version
                """, work.AssignmentId, receipt.Revision);
            if (await memory.ExecuteNonQueryAsync(cancellation) != 1)
                throw new BotWorkExpiredException();
        }
        await using var finish = Database.Command(connection, transaction, """
            UPDATE bot_runtime_proposals SET status=$2,result_revision=$3,error=$4,proposed_memory=NULL
            WHERE assignment_id=$1 AND status='pending'
            """, work.AssignmentId, receipt.Accepted ? "accepted" : "rejected", receipt.Revision, receipt.Error);
        if (await finish.ExecuteNonQueryAsync(cancellation) != 1)
            throw new BotWorkExpiredException();
    }
}
