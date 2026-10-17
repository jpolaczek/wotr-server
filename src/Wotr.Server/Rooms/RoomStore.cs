using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

// The room as one of its participants sees it, with the name they appear under (the room chat, OBS-01).
public sealed record RoomParticipant(RoomView Room, string Name);

// UI-96: the release scope decides which configurations new tables may use; tests built without DI keep everything.
// OBS-01: a room has two seats and an audience of up to sixteen observers (ObserverOptions).
public sealed partial class RoomStore(Database database, IRandomSource random, ReleaseScope? release = null,
    ObserverOptions? observerOptions = null)
{
    private readonly ReleaseScope scope = release ?? ReleaseScope.Full;
    private readonly ObserverOptions audience = observerOptions ?? ObserverOptions.Default;

    // Who may change a room: anyone holding its code or public id (joining), a participant, or a seated player.
    private enum Access { Anyone, Participant, Player }

    public Task<RoomView> Create(string account, Side side, CancellationToken cancellation = default) =>
        Create(account, new CreateRoomRequest { Side = side }, cancellation);

    public async Task<RoomView> Create(string account, CreateRoomRequest request, CancellationToken cancellation = default)
    {
        if (!Enum.IsDefined(request.Side) || !Enum.IsDefined(request.Visibility) || request.Name is null ||
            (request.Name.Length > 0 && !RoomInput.ValidName(request.Name)))
            throw new ArgumentException("Invalid room settings.");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        var username = await AccountName(connection, null, account, cancellation);
        var managed = request.Name.Length > 0;
        var room = new RoomState
        {
            Id = Guid.NewGuid().ToString("N"), JoinCode = SessionStore.NewToken(),
            Name = managed ? request.Name.Trim() : username + "'s game",
            HostAccount = account, HostName = username, Managed = managed,
            Visibility = managed ? request.Visibility : RoomVisibility.Private,
            InvitationEnabled = !managed
        };
        TakeSeat(room, account, username, request.Side);
        await using var command = Database.Command(connection, null,
            """
            INSERT INTO rooms(id,join_code,fp_account,sh_account,state,room_name,host_account,visibility)
            VALUES ($1,$2,NULLIF($3,''),NULLIF($4,''),$5::jsonb,$6,$7,$8)
            """, room.Id, room.JoinCode, room.FreePeoples, room.Shadow, Serialize(room),
            room.Name, account, room.Visibility.ToString());
        await command.ExecuteNonQueryAsync(cancellation);
        return room.View(account, scope, [], audience.Limit);
    }

    // The open rooms this account plays in or watches.
    public async Task<List<RoomView>> List(string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        var rooms = await Participating(connection, account, cancellation);
        var observers = await RoomObservers.ActiveIn(connection, null, rooms.Select(r => r.Id).ToArray(), cancellation);
        return rooms.Select(room => room.View(account, scope, observers.GetValueOrDefault(room.Id) ?? [], audience.Limit))
            .ToList();
    }

    private static async Task<List<RoomState>> Participating(NpgsqlConnection connection, string account,
        CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, null,
            """
            SELECT state::text FROM rooms r
            WHERE NOT closed AND (fp_account=$1 OR sh_account=$1 OR EXISTS (SELECT 1 FROM room_observers o
                WHERE o.room_id=r.id AND o.account_id=$1 AND o.lease_until > now()))
            ORDER BY id
            """, account);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var rooms = new List<RoomState>();
        while (await reader.ReadAsync(cancellation))
            rooms.Add(Deserialize(reader.GetString(0)));
        return rooms;
    }

    public async Task<RoomView?> Read(string id, string account, CancellationToken cancellation = default) =>
        (await Participant(id, account, cancellation))?.Room;

    public async Task<RoomParticipant?> Participant(string id, string account, CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        // OBS-01: an observer reading the room renews the lease, as the heartbeat does; a lapsed one is out.
        var observing = await RoomObservers.Renew(connection, id, account, audience.Lease, cancellation);
        await using var command = Database.Command(connection, null,
            // MENU-27: a participant reading their room (the waiting room refreshes every 2 s) keeps it alive.
            "UPDATE rooms SET seen_at=now() WHERE id=$1 AND (fp_account=$2 OR sh_account=$2 OR $3) RETURNING state::text",
            id, account, observing);
        if (await command.ExecuteScalarAsync(cancellation) is not string json)
            return null;
        var room = Deserialize(json);
        var observers = await RoomObservers.Active(connection, null, id, cancellation);
        var name = room.Contains(account) ? room.PlayerName(account) : observers.Find(o => o.Account == account)?.Name;
        return new RoomParticipant(room.View(account, scope, observers, audience.Limit), name ?? "");
    }

    // OBS-01: the heartbeat of a participant who is not reading the waiting room (a live observer, every 15 s).
    public async Task<bool> Presence(string id, string account, CancellationToken cancellation = default) =>
        await Read(id, account, cancellation) is { Status: not RoomStatus.Closed };

    public Task<RoomResult> Configure(string id, string account, ConfigureRoomRequest request, CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Participant, room => Task.FromResult(
            Manages(room.State, account) ? RoomRules.Configure(room.State, request, scope) : "HostRequired"), cancellation);

    public Task<RoomResult> Confirm(string id, string account, int version, CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Player, room => Task.FromResult(RoomRules.Confirm(room.State, account, version)),
            cancellation);

    public Task<RoomResult> Start(string id, string account, int version, CancellationToken cancellation = default) =>
        Change("id", id, account, Access.Participant, async room =>
        {
            var state = room.State;
            if (!Manages(state, account))
                return "HostRequired";
            if (state.MatchId.Length > 0)
                return null;
            var error = RoomRules.CanStart(state, version, scope);
            if (error is not null)
                return error;
            var match = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, state.Configuration);
            TurnFlow.Start(match);
            await MatchStore.Insert(room.Connection, room.Transaction, match, state.FreePeoples, state.Shadow, cancellation);
            state.MatchId = match.Id;
            await using var title = Database.Command(room.Connection, room.Transaction,
                "UPDATE matches SET display_name=$1 WHERE id=$2", state.Name, match.Id);
            await title.ExecuteNonQueryAsync(cancellation);
            return null;
        }, cancellation);

    // The host runs the room even from the audience; in a quick game without a name either player does.
    private static bool Manages(RoomState room, string account) =>
        room.HostAccount == account || (!room.Managed && room.Contains(account));

    private async Task<RoomResult> Change(string lookupColumn, string key, string account, Access access,
        Func<LockedRoom, Task<string?>> change, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        // Column comes only from the private, literal call sites above. All user data is parameterized.
        var sql = lookupColumn == "join_code"
            ? "SELECT state::text FROM rooms WHERE join_code=$1 FOR UPDATE"
            : "SELECT state::text FROM rooms WHERE id=$1 FOR UPDATE";
        await using var read = Database.Command(connection, transaction, sql, key);
        if (await read.ExecuteScalarAsync(cancellation) is not string json)
            return new(Error: "RoomNotFound");
        var state = Deserialize(json);
        if (state.Closed)
            return new(Error: "RoomNotFound");
        var room = await LockedRoom.Load(state, audience, connection, transaction, cancellation);
        var error = Refusal(room, account, access) ?? await change(room);
        if (error is not null)
            return new(Error: error);
        await Save(room, cancellation);
        await transaction.CommitAsync(cancellation);
        return new(state.View(account, scope, room.Observers, audience.Limit));
    }

    private static string? Refusal(LockedRoom room, string account, Access access)
    {
        if (access == Access.Anyone)
            return null;
        if (!room.Participates(account))
            return "RoomNotFound";
        return access == Access.Player && !room.State.Contains(account) ? "PlayerRequired" : null;
    }

    private static async Task Save(LockedRoom room, CancellationToken cancellation)
    {
        var state = room.State;
        await using var save = Database.Command(room.Connection, room.Transaction,
            """
            UPDATE rooms SET fp_account=NULLIF($1,''),sh_account=NULLIF($2,''),state=$3::jsonb,
                join_code=$5,closed=$6,match_id=NULLIF($7,''),seen_at=now() WHERE id=$4
            """, state.FreePeoples, state.Shadow, Serialize(state), state.Id, state.JoinCode, state.Closed, state.MatchId);
        await save.ExecuteNonQueryAsync(cancellation);
    }

    private static void TakeSeat(RoomState room, string account, string name, Side side)
    {
        if (side == Side.FreePeoples)
        {
            room.FreePeoples = account;
            room.FreePeoplesName = name;
        }
        else
        {
            room.Shadow = account;
            room.ShadowName = name;
        }
    }

    private static async Task<string> AccountName(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string account, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction, "SELECT username FROM accounts WHERE id=$1", account);
        return (string?)await command.ExecuteScalarAsync(cancellation) ?? throw new InvalidOperationException("Account missing.");
    }

    private static string Serialize(RoomState room) => JsonSerializer.Serialize(room, BoardCatalog.Json);
    private static RoomState Deserialize(string json) => JsonSerializer.Deserialize<RoomState>(json, BoardCatalog.Json)!;
}
