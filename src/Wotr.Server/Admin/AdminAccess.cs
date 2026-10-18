using Wotr.Contracts;

namespace Wotr.Server;

// ADMIN-01: who may read the admin panel, checked afresh on every request: an open admin session (SessionPurpose.Admin)
// of a non-technical account that the configuration still names. A game session never qualifies, so a token left on a
// player's computer cannot read the panel. Sign-ins and views go to the log as the panel's audit trail.
public sealed class AdminAccess(Database database, AccountService accounts, AdminOptions options, ILogger<AdminAccess> log)
{
    private const int TokenLength = 43;

    // SEC-02: the panel shares the game's sign-in lock, so a locked name is refused here too.
    public async Task<SignInResult> SignIn(LoginRequest request, CancellationToken cancellation)
    {
        var result = await accounts.SignIn(request, options.Allows, SessionPurpose.Admin, cancellation);
        if (result.LockedFor is not null)
            log.LogWarning("Admin panel sign-in refused: the name is locked after failed sign-ins.");
        else if (result.Session is null)
            log.LogWarning("Admin panel sign-in refused.");
        else
            log.LogInformation("Admin panel sign-in by {Admin}.", result.Session.Username);
        return result;
    }

    // The administrator's account name, or null for anyone else.
    public async Task<string?> Administrator(HttpRequest request, CancellationToken cancellation)
    {
        var token = BearerToken(request);
        if (!options.Enabled || token is null)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            """
            SELECT a.username FROM sessions s JOIN accounts a ON a.id = s.account_id
            WHERE s.token_hash = $1 AND s.purpose = 'admin' AND NOT s.revoked AND s.expires_at > now() AND NOT a.technical
            """, SessionStore.Hash(token));
        return await command.ExecuteScalarAsync(cancellation) is string username && options.Allows(username) ? username : null;
    }

    public void Viewed(string admin, string view) => log.LogInformation("Admin panel {View} read by {Admin}.", view, admin);

    private static string? BearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.Ordinal) && header.Length == 7 + TokenLength ? header[7..] : null;
    }
}
