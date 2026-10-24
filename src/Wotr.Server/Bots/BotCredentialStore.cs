using System.Security.Cryptography;
using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public sealed record BotSeatSession(string Token, DateTimeOffset ExpiresAt)
{
    public override string ToString() => "BotSeatSession [redacted]";
}

public sealed class BotCredentialStore(Database database, BotAccessOptions options)
{
    public static string Participant(string grantId) => "botseat-" + grantId;

    public async Task<BotIdentity?> Resolve(string token, CancellationToken cancellation)
    {
        if (!options.Enabled)
            return null;
        if (token.StartsWith(BotTokens.ControlPrefix, StringComparison.Ordinal))
            return token.Length == BotTokens.ControlPrefix.Length + 43 && CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(SessionStore.Hash(token)), Convert.FromHexString(options.ControlTokenHash))
                ? new(options.ServiceId, null) : null;
        var seat = await ResolveSeat(token, cancellation);
        return seat is null ? null : new(options.ServiceId, seat);
    }

    public async Task<BotSeatAuthority?> ResolveSeat(string token, CancellationToken cancellation)
    {
        if (!options.Enabled || !token.StartsWith(BotTokens.SeatPrefix, StringComparison.Ordinal) || token.Length != BotTokens.SeatPrefix.Length + 43)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var query = Database.Command(connection, null, """
            SELECT g.id,g.service_id,g.generation,g.participant_id,g.room_id,g.match_id,g.side
            FROM bot_seat_credentials c JOIN bot_seat_grants g ON g.id=c.grant_id
            WHERE c.token_hash=$1 AND NOT c.revoked AND NOT g.revoked AND c.generation=g.generation
                AND c.expires_at>clock_timestamp() AND g.lease_until>clock_timestamp() AND g.service_id=$2
                AND (g.match_id IS NULL OR EXISTS (SELECT 1 FROM matches m WHERE m.id=g.match_id AND
                    CASE WHEN g.side='FreePeoples' THEN m.fp_account ELSE m.sh_account END=g.participant_id))
                AND (g.room_id IS NULL OR EXISTS (SELECT 1 FROM rooms r WHERE r.id=g.room_id AND NOT r.closed AND
                    CASE WHEN g.side='FreePeoples' THEN r.fp_account ELSE r.sh_account END=g.participant_id))
            """, SessionStore.Hash(token), options.ServiceId);
        await using var reader = await query.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? new(SessionStore.Hash(token), reader.GetString(0), reader.GetString(1), reader.GetInt64(2),
            reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5),
            Enum.Parse<Side>(reader.GetString(6))) : null;
    }

    // Trusted server lifecycle seam for BOT-12/BOT-17. No public route accepts arbitrary grants or participants.
    public async Task Register(string id, string? roomId, string? matchId, Side side, DateTimeOffset leaseUntil, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await Register(connection, transaction, id, roomId, matchId, side, leaseUntil, cancellation);
        await transaction.CommitAsync(cancellation);
    }

    public async Task Register(NpgsqlConnection connection, NpgsqlTransaction transaction, string id, string? roomId,
        string? matchId, Side side, DateTimeOffset leaseUntil, CancellationToken cancellation)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(id) || id.Length > 100 || !Enum.IsDefined(side) ||
            roomId is null && matchId is null || leaseUntil <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Invalid bot seat grant.");
        var participant = Participant(id);
        await using var account = Database.Command(connection, transaction, """
            INSERT INTO accounts(id,username,normalized_name,password_hash,is_admin,technical)
            VALUES ($1,'CPU',$2,$3,false,true) ON CONFLICT(id) DO NOTHING
            """, participant, "#" + participant, "!" + SessionStore.NewToken());
        await account.ExecuteNonQueryAsync(cancellation);
        await using var technical = Database.Command(connection, transaction,
            "SELECT technical AND NOT is_admin FROM accounts WHERE id=$1", participant);
        if (await technical.ExecuteScalarAsync(cancellation) is not true)
            throw new InvalidOperationException("Bot authority requires a non-login technical participant.");
        await using var grant = Database.Command(connection, transaction, """
            INSERT INTO bot_seat_grants(id,service_id,participant_id,room_id,match_id,side,generation,lease_until)
            VALUES ($1,$2,$3,$4,$5,$6,1,$7)
            """, id, options.ServiceId, participant, (object?)roomId ?? DBNull.Value, (object?)matchId ?? DBNull.Value, side.ToString(), leaseUntil);
        await grant.ExecuteNonQueryAsync(cancellation);
    }

    public async Task<BotSeatSession> Issue(string grantId, long generation, CancellationToken cancellation = default)
    {
        if (!options.Enabled)
            throw new BotAuthorityExpiredException();
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using var grant = Database.Command(connection, transaction, """
            SELECT lease_until FROM bot_seat_grants
            WHERE id=$1 AND generation=$2 AND service_id=$3 AND NOT revoked FOR UPDATE
            """, grantId, generation, options.ServiceId);
        if (await grant.ExecuteScalarAsync(cancellation) is not DateTime until || until <= await Now(connection, transaction, cancellation))
            throw new BotAuthorityExpiredException();
        var expires = new DateTimeOffset(Min(until, (await Now(connection, transaction, cancellation)).Add(options.SeatLifetime)));
        var token = BotTokens.SeatPrefix + SessionStore.NewToken();
        await using var revoke = Database.Command(connection, transaction,
            "UPDATE bot_seat_credentials SET revoked=true WHERE grant_id=$1 AND NOT revoked", grantId);
        await revoke.ExecuteNonQueryAsync(cancellation);
        await using var issue = Database.Command(connection, transaction,
            "INSERT INTO bot_seat_credentials(token_hash,grant_id,generation,expires_at) VALUES ($1,$2,$3,$4)",
            SessionStore.Hash(token), grantId, generation, expires);
        await issue.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return new(token, expires);
    }

    public async Task<DateTimeOffset> Renew(BotSeatAuthority authority, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        // Renewal uses UPDATE locks in the same grant -> credential order as issuance and mutation validation.
        var until = await VerifyLocked(connection, transaction, authority, cancellation, forRenewal: true);
        var expires = new DateTimeOffset(Min(until, (await Now(connection, transaction, cancellation)).Add(options.SeatLifetime)));
        await using var update = Database.Command(connection, transaction,
            "UPDATE bot_seat_credentials SET expires_at=$2 WHERE token_hash=$1", authority.TokenHash, expires);
        await update.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return expires;
    }

    public async Task Revoke(string grantId, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "UPDATE bot_seat_grants SET revoked=true WHERE id=$1 AND service_id=$2", grantId, options.ServiceId);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public async Task<long?> Reassign(string grantId, long expectedGeneration, DateTimeOffset leaseUntil, CancellationToken cancellation = default)
    {
        if (leaseUntil <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Invalid bot lease.");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, """
            UPDATE bot_seat_grants SET generation=generation+1,lease_until=$3
            WHERE id=$1 AND generation=$2 AND NOT revoked AND service_id=$4 RETURNING generation
            """, grantId, expectedGeneration, leaseUntil, options.ServiceId);
        return await command.ExecuteScalarAsync(cancellation) as long?;
    }

    public static async Task<DateTime> VerifyLocked(NpgsqlConnection connection, NpgsqlTransaction transaction,
        BotSeatAuthority authority, CancellationToken cancellation, bool forRenewal = false)
    {
        var lockMode = forRenewal ? " FOR UPDATE" : " FOR SHARE";
        await using var grant = Database.Command(connection, transaction, """
            SELECT service_id,generation,participant_id,room_id,match_id,side,lease_until,revoked
            FROM bot_seat_grants WHERE id=$1
            """ + lockMode, authority.GrantId);
        DateTime lease;
        await using (var row = await grant.ExecuteReaderAsync(cancellation))
        {
            if (!await row.ReadAsync(cancellation) || row.GetBoolean(7) || row.GetString(0) != authority.ServiceId ||
                row.GetInt64(1) != authority.Generation || row.GetString(2) != authority.ParticipantId ||
                (row.IsDBNull(3) ? null : row.GetString(3)) != authority.RoomId ||
                (row.IsDBNull(4) ? null : row.GetString(4)) != authority.MatchId || row.GetString(5) != authority.Side.ToString())
                throw new BotAuthorityExpiredException();
            lease = row.GetDateTime(6);
        }
        await using var credential = Database.Command(connection, transaction, """
            SELECT expires_at FROM bot_seat_credentials
            WHERE token_hash=$1 AND grant_id=$2 AND generation=$3 AND NOT revoked
            """ + lockMode, authority.TokenHash, authority.GrantId, authority.Generation);
        if (await credential.ExecuteScalarAsync(cancellation) is not DateTime expires)
            throw new BotAuthorityExpiredException();
        // PostgreSQL now() is the transaction START time: use a fresh clock after both locks instead.
        var now = await Now(connection, transaction, cancellation);
        if (lease <= now || expires <= now)
            throw new BotAuthorityExpiredException();
        return lease;
    }

    private static async Task<DateTime> Now(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellation)
    {
        await using var query = Database.Command(connection, transaction, "SELECT clock_timestamp()");
        return (DateTime)(await query.ExecuteScalarAsync(cancellation))!;
    }

    private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
}
