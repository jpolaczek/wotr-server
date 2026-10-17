using Npgsql;

namespace Wotr.Server;

// MENU-21: either player removes a match for both, with everything stored for it. Removal locks the match row
// first, so a command waiting for that lock finds no match afterwards and stores nothing.
public sealed class MatchRemoval(Database database)
{
    public async Task<bool> Delete(string id, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        if (!await LockSeat(connection, transaction, id, account, cancellation))
            return false;
        await Remove(connection, transaction, [id], cancellation);
        await transaction.CommitAsync(cancellation);
        return true;
    }

    // Rows without a cascade go first, including the room the match started from. Read positions and the
    // test_matches row cascade from matches.
    public static async Task Remove(NpgsqlConnection connection, NpgsqlTransaction transaction, string[] matches,
        CancellationToken cancellation)
    {
        if (matches.Length == 0)
            return;
        foreach (var sql in new[]
                 {
                     "DELETE FROM command_receipts WHERE match_id = ANY($1)",
                     "DELETE FROM match_events WHERE match_id = ANY($1)",
                     "DELETE FROM rooms WHERE match_id = ANY($1)",
                     "DELETE FROM matches WHERE id = ANY($1)"
                 })
        {
            // One text[] parameter: a bare string[] would expand into the params array.
            await using var delete = Database.Command(connection, transaction, sql, (object)matches);
            await delete.ExecuteNonQueryAsync(cancellation);
        }
    }

    private static async Task<bool> LockSeat(NpgsqlConnection connection, NpgsqlTransaction transaction, string id,
        string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT 1 FROM matches WHERE id=$1 AND (fp_account=$2 OR sh_account=$2) FOR UPDATE", id, account);
        return await command.ExecuteScalarAsync(cancellation) is not null;
    }
}
