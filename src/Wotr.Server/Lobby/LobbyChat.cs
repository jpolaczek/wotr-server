using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// LOBBY-01: the Open games screen's chat, kept in the database and nowhere else. A message keeps its author's name as
// it was when written; only the newest LobbyLimits.MessagesKept stay. The text follows the room chat's rule
// (RoomChat.Clean: 1–200 characters after trimming, no control characters). Posts take turns, so messages commit in
// sequence order: a poll after a sequence never misses an earlier message committed later, and the cut stays exact.
public sealed class LobbyChat(Database database)
{
    // A first read (no sequence yet) gets this many of the latest messages.
    public const int FirstRead = 50;

    public async Task<LobbyMessage?> Post(string account, string? text, CancellationToken cancellation = default)
    {
        var clean = RoomChat.Clean(text);
        if (clean is null)
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await TakeTurn(connection, transaction, cancellation);
        var message = await Insert(connection, transaction, account, clean, cancellation);
        await Prune(connection, transaction, cancellation);
        await transaction.CommitAsync(cancellation);
        return message;
    }

    // Messages after `after`, oldest first; a first read or a sequence from before a reset gets the latest ones.
    public async Task<(List<LobbyMessage> Messages, long Latest)> Read(NpgsqlConnection connection, long after,
        CancellationToken cancellation)
    {
        var latest = await Latest(connection, cancellation);
        var messages = after <= 0 || after > latest
            ? await Newest(connection, cancellation)
            : await After(connection, after, cancellation);
        return (messages, latest);
    }

    private static async Task TakeTurn(NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "SELECT pg_advisory_xact_lock(hashtext('lobby-chat'))");
        await command.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task<LobbyMessage> Insert(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        string text, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO lobby_messages(account_id, username, text)
            SELECT id, username, $2 FROM accounts WHERE id = $1
            RETURNING sequence, username, text, created_at
            """, account, text);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        await reader.ReadAsync(cancellation);
        return Message(reader);
    }

    private static async Task Prune(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            """
            DELETE FROM lobby_messages WHERE sequence < (
                SELECT min(sequence) FROM (SELECT sequence FROM lobby_messages ORDER BY sequence DESC LIMIT $1) kept)
            """, LobbyLimits.MessagesKept);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task<long> Latest(NpgsqlConnection connection, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null, "SELECT COALESCE(max(sequence), 0) FROM lobby_messages");
        return (long)(await command.ExecuteScalarAsync(cancellation))!;
    }

    private static async Task<List<LobbyMessage>> Newest(NpgsqlConnection connection, CancellationToken cancellation)
    {
        var messages = await Query(connection,
            "SELECT sequence, username, text, created_at FROM lobby_messages ORDER BY sequence DESC LIMIT $1",
            FirstRead, cancellation);
        messages.Reverse();
        return messages;
    }

    private static Task<List<LobbyMessage>> After(NpgsqlConnection connection, long after, CancellationToken cancellation) =>
        Query(connection,
            "SELECT sequence, username, text, created_at FROM lobby_messages WHERE sequence > $1 ORDER BY sequence LIMIT " +
            LobbyLimits.MessagesKept, after, cancellation);

    private static async Task<List<LobbyMessage>> Query(NpgsqlConnection connection, string sql, object value,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null, sql, value);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var messages = new List<LobbyMessage>();
        while (await reader.ReadAsync(cancellation))
            messages.Add(Message(reader));
        return messages;
    }

    private static LobbyMessage Message(NpgsqlDataReader reader) => new()
    {
        Sequence = reader.GetInt64(0),
        Author = reader.GetString(1),
        Text = reader.GetString(2),
        At = reader.GetFieldValue<DateTimeOffset>(3)
    };
}
