using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// AVATAR-02 (AVATAR-01 A5): the browser's account page has its own session (8 hours), which reaches only that page's routes.
// ADMIN-01: a session is for the game (30 days) or for the admin panel (8 hours); only an admin session reaches the
// panel's data, so a game session left on a player's computer never does.
public enum SessionPurpose
{
    Game,
    Admin,
    Account
}

public sealed class SessionStore(Database database) : ISessionResolver
{
    public static readonly TimeSpan GameLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan AdminLifetime = TimeSpan.FromHours(8);
    public static readonly TimeSpan AccountLifetime = TimeSpan.FromHours(8);

    // RANK-10: a session must outlast the ranked match it is playing, since a ranked seat can't be left and come back to.
    public static async Task ExtendForRankedMatch(NpgsqlConnection connection, NpgsqlTransaction transaction, string[] accounts,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            UPDATE sessions SET expires_at = now() + interval '7 days'
            WHERE account_id = ANY($1) AND purpose = 'game' AND NOT revoked
                AND expires_at > now() AND expires_at < now() + interval '7 days'
            """, (object)accounts);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<string?> Resolve(string token, CancellationToken cancellation = default)
    {
        if (token.Length != 43)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT account_id FROM sessions WHERE token_hash=$1 AND purpose<>'account' AND NOT revoked AND expires_at>now()",
            Hash(token));
        return await command.ExecuteScalarAsync(cancellation) as string;
    }

    public async Task Revoke(string token, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "UPDATE sessions SET revoked=true WHERE token_hash=$1", Hash(token));
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static Task<SessionView> Issue(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string account, string username, CancellationToken cancellation) =>
        Issue(connection, transaction, account, username, SessionPurpose.Game, cancellation);

    public static async Task<SessionView> Issue(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string account, string username, SessionPurpose purpose, CancellationToken cancellation)
    {
        var session = new SessionView
        {
            Token = NewToken(),
            AccountId = account,
            Username = username,
            ExpiresAt = DateTimeOffset.UtcNow.Add(Lifetime(purpose))
        };
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO sessions(token_hash,account_id,expires_at,purpose) VALUES ($1,$2,$3,$4)
            RETURNING (SELECT avatar FROM accounts WHERE id=$2)
            """,
            Hash(session.Token), account, session.ExpiresAt, Purpose(purpose));
        session.Avatar = await command.ExecuteScalarAsync(cancellation) as string ?? "";
        return session;
    }

    private static TimeSpan Lifetime(SessionPurpose purpose) => purpose switch
    {
        SessionPurpose.Admin => AdminLifetime,
        SessionPurpose.Account => AccountLifetime,
        _ => GameLifetime
    };

    public static string Purpose(SessionPurpose purpose) => purpose switch
    {
        SessionPurpose.Admin => "admin",
        SessionPurpose.Account => "account",
        _ => "game"
    };
}
