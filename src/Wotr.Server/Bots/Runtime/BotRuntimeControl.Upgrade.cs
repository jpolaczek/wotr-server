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
        await using (var query = Database.Command(connection, transaction, """
            SELECT a.runtime_version,a.profile_id,a.policy_version,j.compatibility::text,
                r.state::text FROM bot_assignments a JOIN bot_runtime_jobs j ON j.assignment_id=a.id
                JOIN rooms r ON r.id=a.room_id WHERE a.status<>'cancelled' AND a.runtime_version IS NOT NULL
            ORDER BY a.id FOR UPDATE OF a,j
            """))
        await using (var row = await query.ExecuteReaderAsync(cancellation))
        {
            while (await row.ReadAsync(cancellation))
            {
                if (row.GetString(0) != sourceVersion)
                    return new(false, 0, "MixedRuntimeVersions");
                if (row.IsDBNull(3) || JsonSerializer.Deserialize<BotRuntimeIdentity>(row.GetString(3), BotRuntimeProtocol.Json) != compatibility)
                    return new(false, 0, "MemoryOrRulesIncompatible");
                var policy = target.Policies.SingleOrDefault(p => p.ProfileId == row.GetString(1) && p.PolicyVersion == row.GetString(2));
                var state = JsonSerializer.Deserialize<RoomState>(row.GetString(4), BoardCatalog.Json)!;
                if (!target.GameplayReady || policy is null ||
                    !BotAccessOptions.Supports(state.Configuration, policy.Features.ToHashSet(StringComparer.Ordinal)))
                    return new(false, 0, "PolicyOrModulesIncompatible");
                count++;
            }
        }
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
