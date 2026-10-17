using Npgsql;

namespace Wotr.Server;

public enum AccountTokenPurpose
{
    VerifyEmail,
    ResetPassword
}

// AUTH-02: one-time links. The database keeps only the SHA-256 of a token, its purpose, the address it was sent to
// and its expiry. Consuming is one conditional update, so a link works once even when it is opened twice at once.
public static class AccountTokens
{
    public static readonly TimeSpan VerifyLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromMinutes(60);

    public static bool WellFormed(string? token) =>
        token is { Length: 43 } && token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    public static async Task<string> Create(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        AccountTokenPurpose purpose, string email, DateTimeOffset now, CancellationToken cancellation)
    {
        var token = SessionStore.NewToken();
        var lifetime = purpose == AccountTokenPurpose.VerifyEmail ? VerifyLifetime : ResetLifetime;
        await using var command = Database.Command(connection, transaction,
            "INSERT INTO account_tokens(token_hash,account_id,purpose,email,created_at,expires_at) VALUES ($1,$2,$3,$4,$5,$6)",
            SessionStore.Hash(token), account, Name(purpose), email, now, now + lifetime);
        await command.ExecuteNonQueryAsync(cancellation);
        return token;
    }

    // Closes every open link of this purpose: a new reset link replaces the older ones.
    public static async Task Close(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        AccountTokenPurpose purpose, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "UPDATE account_tokens SET consumed_at=$1 WHERE account_id=$2 AND purpose=$3 AND consumed_at IS NULL",
            now, account, Name(purpose));
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<(string Account, string Email)?> Consume(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string token, AccountTokenPurpose purpose, DateTimeOffset now, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "UPDATE account_tokens SET consumed_at=$1 WHERE token_hash=$2 AND purpose=$3 AND consumed_at IS NULL AND expires_at>$1 " +
            "RETURNING account_id,email", now, SessionStore.Hash(token), Name(purpose));
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? (reader.GetString(0), reader.GetString(1)) : null;
    }

    // Links of this purpose in the last cooldown and links of any purpose in the last day, for one account and for
    // all; every link is one email. The cooldown is per purpose: a confirmation link just sent does not hold back a
    // password reset requested right after confirming.
    public static async Task<(long Recent, long Today, long Everyone)> Sent(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string? account, AccountTokenPurpose purpose, DateTimeOffset now, TimeSpan cooldown, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT count(*) FILTER (WHERE account_id=$1 AND purpose=$5 AND created_at>$2), count(*) FILTER (WHERE account_id=$1), " +
            "count(*) FROM account_tokens WHERE created_at>$3 AND created_at<=$4",
            account ?? "", now - cooldown, now.AddDays(-1), now, Name(purpose));
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        await reader.ReadAsync(cancellation);
        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
    }

    private static string Name(AccountTokenPurpose purpose) =>
        purpose == AccountTokenPurpose.VerifyEmail ? "verify-email" : "reset-password";
}
