using Wotr.Contracts;

namespace Wotr.Server;

// LOBBY-01: one poll of the Open games screen: it marks the player online and returns the new chat messages (relayed in
// memory, LOBBY-03), the players online and the player's challenges, read on one connection.
public sealed class Lobby(Database database, LobbyChat chat, LobbyPresence presence, LobbyChallenges challenges)
{
    public async Task<LobbyView> Poll(string account, long after, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await LobbyPresence.Mark(connection, account, cancellation);
        var (messages, latest) = chat.Read(after);
        return new LobbyView
        {
            Messages = messages,
            Latest = latest,
            Online = await presence.Online(connection, account, cancellation),
            Incoming = await challenges.Incoming(connection, account, cancellation),
            Outgoing = await challenges.Outgoing(connection, account, cancellation)
        };
    }
}
