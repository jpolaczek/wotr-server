using Npgsql;

namespace Wotr.Server;

// MENU-27 (user decision): a game that has not started is not kept. A waiting room nobody has used for a while —
// everyone left it, or its host closed it — is deleted with its chat and audience; a room whose match started is never touched.
// A room's last use (rooms.seen_at) is stored with it, so every server process sees the same rooms; right after
// a start the sweeper waits as long as a room may stay unused, so the players have time to come back.
public sealed class EmptyRoomSweeper(Database database, RoomChat chat, EmptyRoomOptions options, TimeProvider clock,
    ILogger<EmptyRoomSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await Task.Delay(options.EmptyAfter, clock, stopping);
        using var timer = new PeriodicTimer(options.Interval, clock);
        do
        {
            try
            {
                await Sweep(stopping);
            }
            catch (Exception error) when (error is NpgsqlException or InvalidOperationException)
            {
                logger.LogWarning(error, "Empty waiting rooms were not swept this time.");
            }
        }
        while (await timer.WaitForNextTickAsync(stopping));
    }

    public async Task<int> Sweep(CancellationToken cancellation = default)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var delete = Database.Command(connection, null,
            // OBS-01: observers still watching keep an open waiting room, as reading players do.
            """
            DELETE FROM rooms r WHERE match_id IS NULL AND seen_at < now() - make_interval(secs => $1)
                AND (closed OR NOT EXISTS (SELECT 1 FROM room_observers o WHERE o.room_id=r.id AND o.lease_until > now()))
            RETURNING id
            """, options.EmptyAfter.TotalSeconds);
        await using var reader = await delete.ExecuteReaderAsync(cancellation);
        var deleted = 0;
        while (await reader.ReadAsync(cancellation))
        {
            chat.Forget(reader.GetString(0));
            deleted++;
        }
        return deleted;
    }
}

// How long a waiting room may stay unused and how often the sweeper looks (Wotr__Rooms__EmptySeconds/SweepSeconds).
public sealed record EmptyRoomOptions(TimeSpan EmptyAfter, TimeSpan Interval)
{
    public const string EmptyKey = "Wotr:Rooms:EmptySeconds";
    public const string IntervalKey = "Wotr:Rooms:SweepSeconds";

    public static EmptyRoomOptions Read(IConfiguration configuration) => new(
        TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue(EmptyKey, 30.0))),
        TimeSpan.FromSeconds(Math.Max(0.1, configuration.GetValue(IntervalKey, 10.0))));
}
