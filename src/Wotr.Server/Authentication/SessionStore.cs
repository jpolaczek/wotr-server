using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public sealed class SessionStore(Database database) : ISessionResolver
{
    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<string?> Resolve(string token, CancellationToken cancellation = default)
    {
        if (token.Length != 43)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT account_id FROM sessions WHERE token_hash=$1 AND NOT revoked AND expires_at>now()", Hash(token));
        return await command.ExecuteScalarAsync(cancellation) as string;
    }

    public async Task Revoke(string token, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "UPDATE sessions SET revoked=true WHERE token_hash=$1", Hash(token));
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<SessionView> Issue(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string account, string username, CancellationToken cancellation)
    {
        var session = new SessionView
        {
            Token = NewToken(),
            AccountId = account,
            Username = username,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        await using var command = Database.Command(connection, transaction,
            "INSERT INTO sessions(token_hash,account_id,expires_at) VALUES ($1,$2,$3)",
            Hash(session.Token), account, session.ExpiresAt);
        await command.ExecuteNonQueryAsync(cancellation);
        return session;
    }
}
