using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// LOBBY-01: who is on the Open games screen. Each poll of the lobby stamps its account; an account is online while its
// stamp is younger than LobbyOptions.Online. Names come from the accounts, so a renamed player shows their new name.
public sealed class LobbyPresence(LobbyOptions options)
{
    public static async Task Mark(NpgsqlConnection connection, string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            INSERT INTO lobby_presence(account_id, seen_at) VALUES ($1, now())
            ON CONFLICT (account_id) DO UPDATE SET seen_at = EXCLUDED.seen_at
            """, account);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public async Task<List<OnlinePlayer>> Online(NpgsqlConnection connection, string viewer, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            SELECT a.id, a.username FROM lobby_presence p JOIN accounts a ON a.id = p.account_id
            WHERE p.seen_at > now() - make_interval(secs => $1) AND NOT a.technical
            ORDER BY a.normalized_name
            """, options.Online.TotalSeconds);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var players = new List<OnlinePlayer>();
        while (await reader.ReadAsync(cancellation))
            players.Add(new OnlinePlayer { Name = reader.GetString(1), You = reader.GetString(0) == viewer });
        return players;
    }

    public async Task<bool> IsOnline(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT 1 FROM lobby_presence WHERE account_id = $1 AND seen_at > now() - make_interval(secs => $2)",
            account, options.Online.TotalSeconds);
        return await command.ExecuteScalarAsync(cancellation) is not null;
    }
}
