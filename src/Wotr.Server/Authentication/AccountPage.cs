using Wotr.Contracts;

namespace Wotr.Server;

public sealed record AccountView(string Name, string Avatar);

// AVATAR-02 (AVATAR-01 A4-A6): the browser's account page. It signs in with name and password for its own short session, so a
// game session is never handed to a web page, and that session reaches nothing else (SessionStore.Resolve).
public sealed class AccountPage(Database database, AccountService accounts, SessionStore sessions)
{
    private const int TokenLength = 43;

    public Task<SignInResult> SignIn(LoginRequest request, CancellationToken cancellation) =>
        accounts.SignIn(request, _ => true, SessionPurpose.Account, cancellation);

    public Task SignOut(HttpRequest request, CancellationToken cancellation) =>
        BearerToken(request) is { } token ? sessions.Revoke(token, cancellation) : Task.CompletedTask;

    // The account of the page's session, or null for any other token.
    public async Task<string?> Owner(HttpRequest request, CancellationToken cancellation)
    {
        if (BearerToken(request) is not { } token)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "SELECT account_id FROM sessions WHERE token_hash=$1 AND purpose='account' AND NOT revoked AND expires_at>now()",
            SessionStore.Hash(token));
        return await command.ExecuteScalarAsync(cancellation) as string;
    }

    public async Task<AccountView?> View(string account, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT username, avatar FROM accounts WHERE id=$1", account);
        return await ReadView(command, cancellation);
    }

    // One row is changed and nothing is copied, so a change racing a rename cannot mix them, and an open room's version,
    // which its players' readiness is measured against, stays as it was.
    public async Task<AccountView?> ChooseAvatar(string account, string avatar, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "UPDATE accounts SET avatar=$2 WHERE id=$1 RETURNING username, avatar", account, avatar);
        return await ReadView(command, cancellation);
    }

    private static async Task<AccountView?> ReadView(Npgsql.NpgsqlCommand command, CancellationToken cancellation)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? new AccountView(reader.GetString(0), reader.GetString(1)) : null;
    }

    private static string? BearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.Ordinal) && header.Length == 7 + TokenLength ? header[7..] : null;
    }
}

// The people a view names, whose avatars it needs, in one query. Avatars are read when a view is built, never copied.
internal static class AccountAvatars
{
    public static async Task<Dictionary<string, string>> Read(Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction? transaction, string[] accounts, CancellationToken cancellation)
    {
        var avatars = new Dictionary<string, string>();
        if (accounts.Length == 0)
            return avatars;
        await using var command = Database.Command(connection, transaction, "SELECT id, avatar FROM accounts WHERE id=ANY($1)",
            // One array parameter: a bare string[] would spread into the params list.
            (object)accounts);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
            avatars[reader.GetString(0)] = reader.GetString(1);
        return avatars;
    }
}
