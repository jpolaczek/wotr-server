using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// OBS-01: up to sixteen observers per room. A client renews its lease every 15 s; a lease not renewed for 60 s (the
// database clock) lapses and frees the place. Several connections of one account share one row.
public sealed record ObserverOptions(int Limit, TimeSpan Lease)
{
    public const string LeaseKey = "Wotr:Rooms:ObserverLeaseSeconds";
    public static readonly ObserverOptions Default = new(16, TimeSpan.FromSeconds(60));

    public static ObserverOptions Read(IConfiguration configuration) => Default with
    {
        Lease = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue(LeaseKey, Default.Lease.TotalSeconds)))
    };
}

public sealed record RoomObserver(string Account, string Name);

// Observer rows. Every change runs inside the transaction that holds the room's row lock (LockedRoom); reads only
// filter by the lease, so they never need that lock.
internal static class RoomObservers
{
    private const string Columns = "room_id,account_id,username";
    private const string Live = "lease_until > now()";

    public static async Task<List<RoomObserver>> Active(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string room, CancellationToken cancellation)
    {
        var observers = await ActiveIn(connection, transaction, [room], cancellation);
        return observers.TryGetValue(room, out var list) ? list : [];
    }

    // The audience of several rooms in one query (the room list of an account).
    public static async Task<Dictionary<string, List<RoomObserver>>> ActiveIn(NpgsqlConnection connection,
        NpgsqlTransaction? transaction, string[] rooms, CancellationToken cancellation)
    {
        await using var read = Database.Command(connection, transaction,
            $"SELECT {Columns} FROM room_observers WHERE room_id=ANY($1) AND {Live} ORDER BY joined_at,account_id",
            // One array parameter: a bare string[] would spread into the params list.
            (object)rooms);
        await using var reader = await read.ExecuteReaderAsync(cancellation);
        var observers = new Dictionary<string, List<RoomObserver>>();
        while (await reader.ReadAsync(cancellation))
        {
            var room = reader.GetString(0);
            if (!observers.TryGetValue(room, out var list))
                observers[room] = list = [];
            list.Add(new RoomObserver(reader.GetString(1), reader.GetString(2)));
        }
        return observers;
    }

    // Lapsed leases are deleted under the room lock, so the table holds only the audience that may still come back.
    public static async Task Prune(NpgsqlConnection connection, NpgsqlTransaction transaction, string room,
        CancellationToken cancellation)
    {
        await using var prune = Database.Command(connection, transaction,
            $"DELETE FROM room_observers WHERE room_id=$1 AND NOT ({Live})", room);
        await prune.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task Add(NpgsqlConnection connection, NpgsqlTransaction transaction, string room,
        string account, string name, TimeSpan lease, CancellationToken cancellation)
    {
        await using var insert = Database.Command(connection, transaction,
            """
            INSERT INTO room_observers(room_id,account_id,username,lease_until)
            VALUES ($1,$2,$3,now() + make_interval(secs => $4))
            ON CONFLICT (room_id,account_id) DO UPDATE SET lease_until=EXCLUDED.lease_until
            """, room, account, name, lease.TotalSeconds);
        await insert.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task Remove(NpgsqlConnection connection, NpgsqlTransaction transaction, string room,
        string account, CancellationToken cancellation)
    {
        await using var delete = Database.Command(connection, transaction,
            "DELETE FROM room_observers WHERE room_id=$1 AND account_id=$2", room, account);
        await delete.ExecuteNonQueryAsync(cancellation);
    }

    // A heartbeat renews a lease that has not lapsed; a lapsed one needs a new join (and may find the room full).
    public static async Task<bool> Renew(NpgsqlConnection connection, string room, string account, TimeSpan lease,
        CancellationToken cancellation)
    {
        await using var renew = Database.Command(connection, null,
            $"""
            UPDATE room_observers SET lease_until=now() + make_interval(secs => $3)
            WHERE room_id=$1 AND account_id=$2 AND {Live}
            """, room, account, lease.TotalSeconds);
        return await renew.ExecuteNonQueryAsync(cancellation) > 0;
    }

    public static List<RoomObserverView> Views(RoomState room, IEnumerable<RoomObserver> observers) =>
        observers.Select(o => new RoomObserverView { Name = o.Name, IsHost = o.Account == room.HostAccount }).ToList();
}
