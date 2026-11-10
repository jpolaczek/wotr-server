using System.Text.Json;
using Npgsql;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed record BotUpgradeResult(bool Applied, int Assignments, string Error = "");

public sealed partial class BotRuntimeControl
{
    public async Task<BotUpgradeResult> Upgrade(long epoch, string sourceVersion, BotRuntimeDescription target,
        string manifestHash, CancellationToken cancellation = default)
    {
        var compatibility = BotRuntimeIdentity.Of(target);
        if (!BotRuntimeProtocol.Compatible(target) || compatibility is null ||
            !System.Version.TryParse(sourceVersion, out _) || sourceVersion == target.RuntimeVersion ||
            !System.Text.RegularExpressions.Regex.IsMatch(manifestHash, "^[a-fA-F0-9]{64}$"))
            return new(false, 0, "InvalidTarget");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using var control = Database.Command(connection, transaction,
            "SELECT draining AND epoch=$1 AND (runtime_version IS NULL OR runtime_version=$2) FROM bot_runtime_control WHERE id=1 FOR UPDATE", epoch, sourceVersion);
        if (await control.ExecuteScalarAsync(cancellation) is not true)
            return new(false, 0, "DrainRequired");
        var count = 0;
        var migrations = new List<(string Assignment, string Room, string Policy)>();
        await using (var query = Database.Command(connection, transaction, """
            SELECT a.runtime_version,a.profile_id,a.policy_version,j.compatibility::text,
                r.state::text,a.id,a.room_id FROM bot_assignments a JOIN bot_runtime_jobs j ON j.assignment_id=a.id
                JOIN rooms r ON r.id=a.room_id WHERE a.status<>'cancelled' AND a.runtime_version IS NOT NULL
            ORDER BY a.id FOR UPDATE OF a,j
            """))
        await using (var row = await query.ExecuteReaderAsync(cancellation))
        {
            while (await row.ReadAsync(cancellation))
            {
                if (row.GetString(0) != sourceVersion)
                    return new(false, 0, "MixedRuntimeVersions");
                // BOT-37: the ABI is how the host calls the runtime, not what its memory means: a target
                // that speaks the host's newer ABI keeps the seat when protocol, rules, content and memory
                // format are the same, and the seat's identity is updated with the upgrade.
                if (row.IsDBNull(3) || JsonSerializer.Deserialize<BotRuntimeIdentity>(row.GetString(3), BotRuntimeProtocol.Json) is not { } saved ||
                    saved with { AbiVersion = compatibility.AbiVersion } != compatibility)
                    return new(false, 0, "MemoryOrRulesIncompatible");
                // BOT-37: a seat on an earlier policy the target continues is re-pinned to the target's version.
                var policy = target.Policies.SingleOrDefault(p => p.ProfileId == row.GetString(1) &&
                    (p.PolicyVersion == row.GetString(2) || p.MigratesFrom?.Contains(row.GetString(2)) == true));
                var state = JsonSerializer.Deserialize<RoomState>(row.GetString(4), BoardCatalog.Json)!;
                if (!target.GameplayReady || policy is null ||
                    !BotAccessOptions.Supports(state.Configuration, policy.Features.ToHashSet(StringComparer.Ordinal)))
                    return new(false, 0, "PolicyOrModulesIncompatible");
                if (policy.PolicyVersion != row.GetString(2))
                    migrations.Add((row.GetString(5), row.GetString(6), policy.PolicyVersion));
                count++;
            }
        }
        foreach (var (assignment, room, policyVersion) in migrations)
        {
            await using var seat = Database.Command(connection, transaction,
                "UPDATE bot_assignments SET policy_version=$2 WHERE id=$1", assignment, policyVersion);
            await seat.ExecuteNonQueryAsync(cancellation);
            await using var shown = Database.Command(connection, transaction,
                "UPDATE rooms SET state=jsonb_set(state,'{bot,policyVersion}',to_jsonb($2::text)) WHERE id=$1 AND state->'bot'->>'assignmentId'=$3",
                room, policyVersion, assignment);
            await shown.ExecuteNonQueryAsync(cancellation);
        }
        await using (var identity = Database.Command(connection, transaction, """
            UPDATE bot_runtime_jobs SET compatibility=$2::jsonb WHERE assignment_id IN
                (SELECT id FROM bot_assignments WHERE runtime_version=$1 AND status<>'cancelled')
            """, sourceVersion, JsonSerializer.Serialize(compatibility, BotRuntimeProtocol.Json)))
            await identity.ExecuteNonQueryAsync(cancellation);
        await using var change = Database.Command(connection, transaction,
            "UPDATE bot_assignments SET runtime_version=$2 WHERE runtime_version=$1 AND status<>'cancelled'", sourceVersion, target.RuntimeVersion);
        await change.ExecuteNonQueryAsync(cancellation);
        await using var pin = Database.Command(connection, transaction,
            "UPDATE bot_runtime_control SET runtime_version=$1,manifest_sha256=$2,updated_at=clock_timestamp() WHERE id=1",
            target.RuntimeVersion, manifestHash.ToUpperInvariant());
        await pin.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return new(true, count);
    }
}
