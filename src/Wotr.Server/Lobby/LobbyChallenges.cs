using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public enum ChallengeError
{
    None,
    PlayerNotFound,
    SelfChallenge,
    PlayerOffline,
    AlreadyChallenged,
    TooManyChallenges,
    ChallengeNotFound,
    ChallengeClosed,
    RoomUnavailable
}

public sealed record ChallengeResult(ChallengeView? Challenge, ChallengeError Error);

// The parties of a challenge the challenged player has just claimed for acceptance.
public sealed record ClaimedChallenge(string Id, string From, string FromName, string To, string ToName);

// LOBBY-01: challenges between players on the Open games screen. A player challenges another online player by name; a
// challenge waits LobbyOptions.ChallengeLifetime for the challenged player, who accepts (ChallengeAcceptance makes the
// room) or declines; the challenger may cancel it. Statuses are stored as ChallengeStatus names; a pending challenge
// past its time reads as Expired without being rewritten.
public sealed class LobbyChallenges(Database database, LobbyPresence presence, LobbyOptions options)
{
    // At most this many pending challenges from one player at a time.
    public const int MostPending = 3;
    // The challenger's own challenges stay in their poll this long, so its outcome reaches them.
    private static readonly TimeSpan Recent = TimeSpan.FromMinutes(5);
    private const string Pending = nameof(ChallengeStatus.Pending);
    private const string Live = "status = 'Pending' AND expires_at > now()";

    public async Task<ChallengeResult> Send(string from, string? name, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var target = await Account(connection, transaction, name?.Trim() ?? "", cancellation);
        var error = target is null ? ChallengeError.PlayerNotFound
            : target.Value.Id == from ? ChallengeError.SelfChallenge
            : !await presence.IsOnline(connection, transaction, target.Value.Id, cancellation) ? ChallengeError.PlayerOffline
            : await Refusal(connection, transaction, from, target.Value.Id, cancellation);
        if (error != ChallengeError.None)
            return new(null, error);
        var id = await Insert(connection, transaction, from, target!.Value.Id, cancellation);
        var view = await Read(connection, transaction, id, cancellation);
        await transaction.CommitAsync(cancellation);
        return new(view, ChallengeError.None);
    }

    public Task<ChallengeError> Decline(string account, string id, CancellationToken cancellation = default) =>
        Close(id, "to_account", account, nameof(ChallengeStatus.Declined), cancellation);

    public Task<ChallengeError> Cancel(string account, string id, CancellationToken cancellation = default) =>
        Close(id, "from_account", account, nameof(ChallengeStatus.Cancelled), cancellation);

    public Task<List<ChallengeView>> Incoming(NpgsqlConnection connection, string account, CancellationToken cancellation) =>
        List(connection, $"c.to_account = $1 AND c.{Live}", account, cancellation);

    public Task<List<ChallengeView>> Outgoing(NpgsqlConnection connection, string account, CancellationToken cancellation) =>
        List(connection, $"c.from_account = $1 AND c.created_at > now() - make_interval(secs => {Recent.TotalSeconds})",
            account, cancellation);

    // ChallengeAcceptance: the challenged player takes a live challenge; a second accept finds it taken. Until its room
    // is set the challenge still reads as Pending, so nobody is sent to a room that does not exist yet.
    public async Task<(ClaimedChallenge? Claimed, ChallengeError Error)> Claim(string account, string id,
        CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            $"""
            UPDATE lobby_challenges c SET status = 'Accepted' FROM accounts f, accounts t
            WHERE c.id = $1 AND c.to_account = $2 AND c.{Live} AND f.id = c.from_account AND t.id = c.to_account
            RETURNING c.from_account, f.username, c.to_account, t.username
            """, id, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (await reader.ReadAsync(cancellation))
            return (new(id, reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)), ChallengeError.None);
        await reader.CloseAsync();
        return (null, await Missing(connection, id, "to_account", account, cancellation));
    }

    public async Task SetRoom(string id, string roomId, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "UPDATE lobby_challenges SET room_id = $2 WHERE id = $1", id, roomId);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    // A room that could not be made gives the challenge back, so it can be accepted again while it lasts.
    public async Task Release(string id, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            "UPDATE lobby_challenges SET status = 'Pending' WHERE id = $1 AND status = 'Accepted' AND room_id IS NULL", id);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    private static async Task<(string Id, string Name)?> Account(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string name, CancellationToken cancellation)
    {
        if (!AccountInput.ValidUsername(name))
            return null;
        await using var command = Database.Command(connection, transaction,
            "SELECT id, username FROM accounts WHERE normalized_name = $1 AND NOT technical", name.ToUpperInvariant());
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? (reader.GetString(0), reader.GetString(1)) : null;
    }

    // One challenge between two players at a time, either way, and a few from one player. The pair's lock keeps two
    // simultaneous challenges between the same players from both passing this check.
    private static async Task<ChallengeError> Refusal(NpgsqlConnection connection, NpgsqlTransaction transaction, string from,
        string to, CancellationToken cancellation)
    {
        await using var pairLock = Database.Command(connection, transaction,
            "SELECT pg_advisory_xact_lock(hashtext('lobby-challenge:' || least($1, $2) || ':' || greatest($1, $2)))", from, to);
        await pairLock.ExecuteNonQueryAsync(cancellation);
        await using var check = Database.Command(connection, transaction,
            $"""
            SELECT
                EXISTS (SELECT 1 FROM lobby_challenges WHERE {Live} AND
                    ((from_account = $1 AND to_account = $2) OR (from_account = $2 AND to_account = $1))),
                (SELECT count(*) FROM lobby_challenges WHERE {Live} AND from_account = $1)
            """, from, to);
        await using var reader = await check.ExecuteReaderAsync(cancellation);
        await reader.ReadAsync(cancellation);
        return reader.GetBoolean(0) ? ChallengeError.AlreadyChallenged
            : reader.GetInt64(1) >= MostPending ? ChallengeError.TooManyChallenges
            : ChallengeError.None;
    }

    private async Task<string> Insert(NpgsqlConnection connection, NpgsqlTransaction transaction, string from, string to,
        CancellationToken cancellation)
    {
        var id = Guid.NewGuid().ToString("N");
        await using var command = Database.Command(connection, transaction,
            """
            INSERT INTO lobby_challenges(id, from_account, to_account, status, expires_at)
            VALUES ($1, $2, $3, $4, now() + make_interval(secs => $5))
            """, id, from, to, Pending, options.ChallengeLifetime.TotalSeconds);
        await command.ExecuteNonQueryAsync(cancellation);
        return id;
    }

    private async Task<ChallengeError> Close(string id, string party, string account, string status,
        CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null,
            $"UPDATE lobby_challenges SET status = $3 WHERE id = $1 AND {party} = $2 AND {Live}", id, account, status);
        return await command.ExecuteNonQueryAsync(cancellation) > 0
            ? ChallengeError.None
            : await Missing(connection, id, party, account, cancellation);
    }

    // A challenge of this party that is no longer pending is closed; any other id is not theirs to answer.
    private static async Task<ChallengeError> Missing(NpgsqlConnection connection, string id, string party, string account,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            $"SELECT 1 FROM lobby_challenges WHERE id = $1 AND {party} = $2", id, account);
        return await command.ExecuteScalarAsync(cancellation) is null ? ChallengeError.ChallengeNotFound : ChallengeError.ChallengeClosed;
    }

    private static async Task<ChallengeView> Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string id,
        CancellationToken cancellation)
    {
        var views = await List(connection, transaction, "c.id = $1", id, cancellation);
        return views[0];
    }

    private static Task<List<ChallengeView>> List(NpgsqlConnection connection, string filter, string value,
        CancellationToken cancellation) =>
        List(connection, null, filter, value, cancellation);

    private static async Task<List<ChallengeView>> List(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string filter, string value, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            $"""
            SELECT c.id, f.username, t.username, c.status, c.expires_at, COALESCE(c.room_id, ''), c.expires_at <= now()
            FROM lobby_challenges c JOIN accounts f ON f.id = c.from_account JOIN accounts t ON t.id = c.to_account
            WHERE {filter} ORDER BY c.created_at
            """, value);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var views = new List<ChallengeView>();
        while (await reader.ReadAsync(cancellation))
            views.Add(View(reader));
        return views;
    }

    private static ChallengeView View(NpgsqlDataReader reader)
    {
        var roomId = reader.GetString(5);
        return new ChallengeView
        {
            Id = reader.GetString(0),
            From = reader.GetString(1),
            To = reader.GetString(2),
            Status = Status(reader.GetString(3), roomId, reader.GetBoolean(6)),
            ExpiresAt = reader.GetFieldValue<DateTimeOffset>(4),
            RoomId = roomId
        };
    }

    // An accepted challenge without its room yet still reads as Pending; either past its time is Expired.
    private static ChallengeStatus Status(string stored, string roomId, bool pastTime)
    {
        var status = Enum.Parse<ChallengeStatus>(stored);
        if (status == ChallengeStatus.Accepted && roomId.Length == 0)
            status = ChallengeStatus.Pending;
        return status == ChallengeStatus.Pending && pastTime ? ChallengeStatus.Expired : status;
    }
}
