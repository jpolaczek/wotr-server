using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

public sealed partial class RoomStore
{
    // OBS-01: the open public rooms of other hosts with their seats and audience. The join code never leaves through
    // this list. OBS-07: a room the viewer plays in is theirs (My games), never one to watch. LOBBY-05: only rooms
    // whose match has not started; a game in progress is watched by its invitation code. EXP-01: each room carries its
    // configuration, so the list can show its expansions.
    public async Task<PublicRoomListView> ListPublic(string account, string cursor, int limit,
        CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var query = Database.Command(connection, null, """
            SELECT r.id,r.room_name,a.username,r.fp_account IS NULL,r.sh_account IS NULL,
                (SELECT count(*)::int FROM room_observers o WHERE o.room_id=r.id AND o.lease_until > now()),
                (r.state->'configuration')::text
            FROM rooms r JOIN accounts a ON a.id=r.host_account
            WHERE r.visibility='Public' AND NOT r.closed AND r.match_id IS NULL
                AND r.host_account<>$1 AND r.fp_account IS DISTINCT FROM $1 AND r.sh_account IS DISTINCT FROM $1
                AND r.id COLLATE "C">$2
            ORDER BY r.id COLLATE "C" LIMIT $3
            """, account, cursor, limit + 1);
        await using var reader = await query.ExecuteReaderAsync(cancellation);
        var page = new PublicRoomListView();
        while (await reader.ReadAsync(cancellation))
            page.Rooms.Add(Summary(reader));
        if (page.Rooms.Count > limit)
        {
            page.Rooms.RemoveAt(limit);
            page.NextCursor = page.Rooms[^1].Id;
        }
        return page;
    }

    private PublicRoomSummary Summary(NpgsqlDataReader reader)
    {
        var freePeoplesOpen = reader.GetBoolean(3);
        var shadowOpen = reader.GetBoolean(4);
        var observers = reader.GetInt32(5);
        return new PublicRoomSummary
        {
            Id = reader.GetString(0), Name = reader.GetString(1), Host = reader.GetString(2),
            OpenSide = freePeoplesOpen ? Side.FreePeoples : Side.Shadow,
            Status = RoomStatus.Waiting,
            Players = (freePeoplesOpen ? 0 : 1) + (shadowOpen ? 0 : 1),
            Observers = observers,
            CanPlay = freePeoplesOpen || shadowOpen,
            CanObserve = observers < audience.Limit,
            Configuration = SummaryConfiguration(reader, 6)
        };
    }

    // A room saved without a configuration plays the base game, as RoomState's default says.
    private static MatchConfiguration SummaryConfiguration(NpgsqlDataReader reader, int column)
    {
        var stored = reader.IsDBNull(column)
            ? null
            : JsonSerializer.Deserialize<MatchConfiguration>(reader.GetString(column), BoardCatalog.Json);
        return ConfigurationRules.Normalize(stored ?? new MatchConfiguration());
    }
}
