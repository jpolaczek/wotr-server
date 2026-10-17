using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed record AuthorizedMatch(MatchState State, Side Side, bool EventsIndexed = false, bool Recorded = false);
public sealed record MatchVersion(long Revision, long PrivateVersion);

public sealed class MatchStore(Database database, GameEngine engine, IRandomSource entropy)
{
    public async Task Create(MatchState state, string fp, string sh, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await Insert(connection, transaction, state, fp, sh, cancellation);
        await transaction.CommitAsync(cancellation);
    }
    public static Task Insert(NpgsqlConnection connection, NpgsqlTransaction? transaction, MatchState state,
        string fp, string sh, CancellationToken cancellation) =>
        Insert(connection, transaction, state, fp, sh, MatchKind.Game, cancellation);

    // ADMIN-01: the match ledger counts the match by its kind from its start.
    public static async Task Insert(NpgsqlConnection connection, NpgsqlTransaction? transaction, MatchState state,
        string fp, string sh, MatchKind kind, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "INSERT INTO matches(id,fp_account,sh_account,revision,state,events_indexed,recorded) VALUES ($1,$2,$3,$4,$5::jsonb,true,true)",
            state.Id, fp, sh, state.Revision, StateCodec.Write(state));
        await command.ExecuteNonQueryAsync(cancellation);
        await MatchEventStore.Append(connection, transaction, state, 0, cancellation);
        // REPLAY-01: the first frame is the state before the first command of a player.
        await MatchFrames.Record(connection, transaction, state, cancellation);
        await MatchLedger.Started(connection, transaction, state.Id, kind, fp, sh, cancellation);
    }
    public async Task<AuthorizedMatch?> Read(string id, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT state::text,fp_account FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2)", id, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? new(StateCodec.Read(reader.GetString(0)), reader.GetString(1) == account ? Side.FreePeoples : Side.Shadow) : null;
    }
    // OBS-02: the state an observer may read: no side is attached, the reader picks the perspective.
    public async Task<MatchState?> ReadObserved(string id, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT m.state::text FROM matches m WHERE m.id=$1 AND " + MatchObservers.Watching, id, account);
        return await command.ExecuteScalarAsync(cancellation) is string json ? StateCodec.Read(json) : null;
    }

    public async Task<long?> ReadObservedRevision(string id, string account, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT m.revision FROM matches m WHERE m.id=$1 AND " + MatchObservers.Watching, id, account);
        return await command.ExecuteScalarAsync(cancellation) as long?;
    }

    public async Task<CommandReceipt?> Receipt(string matchId, string commandId, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT r.receipt::text FROM command_receipts r JOIN matches m ON m.id=r.match_id WHERE r.match_id=$1 AND r.command_id=$2 AND r.account_id=$3 AND (m.fp_account=$3 OR m.sh_account=$3)", matchId, commandId, account);
        var json = await command.ExecuteScalarAsync(cancellation) as string;
        return json is null ? null : JsonSerializer.Deserialize<CommandReceipt>(json, BoardCatalog.Json);
    }
    public async Task<long?> ReadRevision(string id, string account, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT revision FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2)", id, account);
        return await command.ExecuteScalarAsync(cancellation) as long?;
    }

    public async Task<MatchVersion?> ReadVersion(string id, string account, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, """
            SELECT revision, COALESCE((SELECT (player->'autoDecision'->>'version')::bigint
                FROM jsonb_array_elements(state->'players') AS player
                WHERE player->>'side'=CASE WHEN fp_account=$2 THEN 'FreePeoples' ELSE 'Shadow' END), 0)
            FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2)
            """, id, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? new(reader.GetInt64(0), reader.GetInt64(1)) : null;
    }

    // QUAL-03: rejections of one account in one match kept at the latest revision; older ones go when it moves on.
    public const int RejectionsKept = 32;

    public async Task<CommandReceipt?> Execute(GameCommand request, string account, CancellationToken cancellation = default, Func<Task>? beforeCommit = null)
    {
        if (!CommandShape.IsValid(request))
            return await Malformed(request, account, cancellation);
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var match = await LockMatch(connection, transaction, request.MatchId, account, cancellation);
        if (match is null)
            return null;
        var fingerprint = CommandFingerprint.Create(request);
        var existing = await ExistingReceipt(connection, transaction, request, account, fingerprint, match.State.Revision, cancellation);
        if (existing is not null)
            return existing;
        var result = engine.Execute(match.State, match.Side, request, entropy);
        if (result.Receipt.Accepted)
            await SaveState(connection, transaction, result.State, match.State.Events.Count, match.EventsIndexed, cancellation);
        // REPLAY-01: a private preference changes no public state and adds no frame.
        if (result.Receipt.Accepted && result.Receipt.PublicStateChanged && match.Recorded)
            await MatchFrames.Record(connection, transaction, result.State, cancellation);
        await SaveReceipt(connection, transaction, request, account, fingerprint, result.Receipt, cancellation);
        await TrimRejections(connection, transaction, request.MatchId, account, result.Receipt, cancellation);
        if (beforeCommit is not null)
            await beforeCommit(); // Fault-injection boundary, never exposed over HTTP.
        await transaction.CommitAsync(cancellation);
        return result.Receipt;
    }

    private static async Task<AuthorizedMatch?> LockMatch(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string id, string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT state::text,fp_account,events_indexed,recorded FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2) FOR UPDATE",
            id, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation)
            ? new(StateCodec.Read(reader.GetString(0)), reader.GetString(1) == account ? Side.FreePeoples : Side.Shadow,
                reader.GetBoolean(2), reader.GetBoolean(3)) : null;
    }

    private static async Task<CommandReceipt?> ExistingReceipt(NpgsqlConnection connection, NpgsqlTransaction transaction,
        GameCommand request, string account, string fingerprint, long revision, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT fingerprint,receipt::text FROM command_receipts WHERE match_id=$1 AND account_id=$2 AND command_id=$3",
            request.MatchId, account, request.CommandId);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation))
            return null;
        return CommandFingerprint.Matches(reader.GetString(0), request, fingerprint)
            ? JsonSerializer.Deserialize<CommandReceipt>(reader.GetString(1), BoardCatalog.Json)
            : new()
            {
                CommandId = request.CommandId,
                Revision = revision,
                Error = "CommandIdConflict"
            };
    }

    private static async Task SaveState(NpgsqlConnection connection, NpgsqlTransaction transaction,
        MatchState state, int previousEventCount, bool eventsIndexed, CancellationToken cancellation)
    {
        await using var update = Database.Command(connection, transaction,
            "UPDATE matches SET state=$1::jsonb,revision=$2,updated_at=now(),events_indexed=true WHERE id=$3", StateCodec.Write(state), state.Revision, state.Id);
        await update.ExecuteNonQueryAsync(cancellation);
        if (!eventsIndexed)
        {
            await using var clearIndex = Database.Command(connection, transaction, "DELETE FROM match_events WHERE match_id=$1", state.Id);
            await clearIndex.ExecuteNonQueryAsync(cancellation);
        }
        await MatchEventStore.Append(connection, transaction, state, eventsIndexed ? previousEventCount : 0, cancellation);
        if (state.Winner is { } winner)
            await MatchLedger.Finished(connection, transaction, state.Id, winner, cancellation);
    }

    private static async Task SaveReceipt(NpgsqlConnection connection, NpgsqlTransaction transaction, GameCommand request,
        string account, string fingerprint, CommandReceipt receipt, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "INSERT INTO command_receipts(match_id,account_id,command_id,fingerprint,receipt,accepted,revision) VALUES ($1,$2,$3,$4,$5::jsonb,$6,$7)",
            request.MatchId, account, request.CommandId, fingerprint, JsonSerializer.Serialize(receipt, BoardCatalog.Json),
            receipt.Accepted, receipt.Revision);
        await command.ExecuteNonQueryAsync(cancellation);
        ReceiptMetrics.Stored(receipt.Accepted);
    }

    // An accepted command moves the match on: every rejection answered at an older revision would now be
    // validated afresh (StaleRevision), so it no longer needs its receipt. A rejection keeps at most
    // RejectionsKept of the account's rejections in the match. Both deletes use the partial index.
    private static async Task TrimRejections(NpgsqlConnection connection, NpgsqlTransaction transaction, string match,
        string account, CommandReceipt receipt, CancellationToken cancellation)
    {
        await using var command = receipt.Accepted
            ? Database.Command(connection, transaction,
                "DELETE FROM command_receipts WHERE match_id=$1 AND NOT accepted AND revision < $2", match, receipt.Revision)
            : Database.Command(connection, transaction,
                "DELETE FROM command_receipts WHERE match_id=$1 AND account_id=$2 AND NOT accepted AND command_id IN " +
                "(SELECT command_id FROM command_receipts WHERE match_id=$1 AND account_id=$2 AND NOT accepted " +
                "ORDER BY stored_at DESC, command_id OFFSET $3)", match, account, RejectionsKept);
        ReceiptMetrics.Removed(await command.ExecuteNonQueryAsync(cancellation));
    }

    // QUAL-01/03: malformed input gets a controlled answer without a lock, a transaction or a stored receipt.
    private async Task<CommandReceipt?> Malformed(GameCommand request, string account, CancellationToken cancellation)
    {
        var revision = await ReadRevision(request.MatchId, account, cancellation);
        if (revision is null)
            return null;
        ReceiptMetrics.Malformed();
        return new()
        {
            CommandId = request.CommandId,
            Revision = revision.Value,
            Error = CommandShape.MalformedCommand
        };
    }
}
