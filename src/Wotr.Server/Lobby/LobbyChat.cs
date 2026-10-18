using Wotr.Contracts;

namespace Wotr.Server;

// LOBBY-01, LOBBY-03: the Open games screen's chat is relayed, never stored. A posted message waits in this server's
// memory only as long as the menus, which poll every few seconds, need to fetch it — RelayWindow, and at most
// LobbyLimits.MessagesKept messages — and a restart forgets it; nothing reaches the database or the logs. Each client keeps
// what it saw for its own sign-in (LOBBY-02). A first read (no sequence yet) gets no history, only the sequence to go on
// from. The text follows the room chat's rule (RoomChat.Clean: 1–200 characters after trimming, no control characters);
// a message carries its author's name as it was when written.
public sealed class LobbyChat(Database database, TimeProvider clock)
{
    public static readonly TimeSpan RelayWindow = TimeSpan.FromMinutes(1);
    private readonly Lock gate = new();
    private readonly List<LobbyMessage> relay = new();
    // Sequences start from the clock, so after a restart they go on above any sequence a client still holds.
    private long sequence = clock.GetUtcNow().ToUnixTimeMilliseconds();

    public async Task<LobbyMessage?> Post(string account, string? text, CancellationToken cancellation = default)
    {
        var clean = RoomChat.Clean(text);
        if (clean is null)
            return null;
        var author = await Author(account, cancellation);
        if (author is null)
            return null;
        lock (gate)
        {
            var message = new LobbyMessage { Sequence = ++sequence, Author = author, Text = clean, At = clock.GetUtcNow() };
            relay.Add(message);
            Forget(message.At);
            return message;
        }
    }

    // Messages after `after`, oldest first, with the sequence to continue from. A first read (0) or a sequence this
    // server never gave out gets no messages.
    public (List<LobbyMessage> Messages, long Latest) Read(long after)
    {
        lock (gate)
        {
            Forget(clock.GetUtcNow());
            var messages = after <= 0 || after > sequence
                ? new List<LobbyMessage>()
                : relay.Where(message => message.Sequence > after).ToList();
            return (messages, sequence);
        }
    }

    public int Relayed
    {
        get
        {
            lock (gate)
                return relay.Count;
        }
    }

    private void Forget(DateTimeOffset now)
    {
        relay.RemoveAll(message => now - message.At >= RelayWindow);
        if (relay.Count > LobbyLimits.MessagesKept)
            relay.RemoveRange(0, relay.Count - LobbyLimits.MessagesKept);
    }

    private async Task<string?> Author(string account, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT username FROM accounts WHERE id = $1", account);
        return await command.ExecuteScalarAsync(cancellation) as string;
    }
}
