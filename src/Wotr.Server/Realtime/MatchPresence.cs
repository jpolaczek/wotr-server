using Wotr.Contracts;

namespace Wotr.Server;

// RANK-10 (R35, docs/RANKED_PLAY.md, Reconnecting): which seats of ranked matches are connected, held in memory and built
// from live sockets, so it changes no clock, revision or replay. A seat is online while it has a socket, and until its
// silence limit after the last frame it sent, so a dropped socket that returns at once shows nothing. A server start
// counts every seat as online for a short grace while the clients return, and a new match its seats while they open
// the board.
public sealed class MatchPresence(RevisionHub hub, TimeProvider clock, RankedOptions options)
{
    private sealed class Seat
    {
        public int Sockets;
        public DateTimeOffset OnlineUntil;
    }

    private readonly Dictionary<(string Match, Side Side), Seat> seats = [];
    private readonly DateTimeOffset startGraceEnds = clock.GetUtcNow() + options.PresenceScaled(RankedOptions.PresenceGrace);

    // How long a seated player's socket may stay silent before the server closes it.
    public TimeSpan Silence { get; } = options.PresenceScaled(RankedOptions.SocketSilence);

    public bool Online(string match, Side side)
    {
        var now = clock.GetUtcNow();
        if (now < startGraceEnds)
            return true;
        lock (seats)
            return IsOnline(seats.GetValueOrDefault((match, side)), now);
    }

    // A new match: its players are told at their next poll and then open the board.
    public void Expect(string match)
    {
        var until = clock.GetUtcNow() + options.PresenceScaled(RankedOptions.PresenceGrace);
        lock (seats)
        {
            seats.TryAdd((match, Side.FreePeoples), new() { OnlineUntil = until });
            seats.TryAdd((match, Side.Shadow), new() { OnlineUntil = until });
        }
        _ = ExpireLater(match, Side.FreePeoples, until);
        _ = ExpireLater(match, Side.Shadow, until);
    }

    public SeatSocket Join(string match, Side side)
    {
        var now = clock.GetUtcNow();
        bool returned;
        lock (seats)
        {
            var key = (match, side);
            var seat = seats.GetValueOrDefault(key);
            returned = !IsOnline(seat, now);
            seat ??= seats[key] = new();
            seat.Sockets++;
            seat.OnlineUntil = now + Silence;
        }
        if (returned)
            hub.Publish(match, 0);
        return new(this, match, side);
    }

    private static bool IsOnline(Seat? seat, DateTimeOffset now) =>
        seat is not null && (seat.Sockets > 0 || now < seat.OnlineUntil);

    private void Heard(string match, Side side)
    {
        lock (seats)
            if (seats.TryGetValue((match, side), out var seat))
                seat.OnlineUntil = clock.GetUtcNow() + Silence;
    }

    private void Leave(string match, Side side)
    {
        DateTimeOffset? until = null;
        lock (seats)
            if (seats.TryGetValue((match, side), out var seat) && --seat.Sockets == 0)
                until = seat.OnlineUntil;
        if (until is { } moment)
            _ = ExpireLater(match, side, moment);
    }

    // When a seat's time runs out, it goes offline, the opponent's socket hears at once and the entry is removed. A
    // pending wait costs nothing if the player returned: the seat then has a socket or a later moment.
    private async Task ExpireLater(string match, Side side, DateTimeOffset until)
    {
        var wait = until - clock.GetUtcNow();
        await Task.Delay((wait > TimeSpan.Zero ? wait : TimeSpan.Zero) + ExpiryMargin);
        var now = clock.GetUtcNow();
        lock (seats)
        {
            if (!seats.TryGetValue((match, side), out var seat) || IsOnline(seat, now))
                return;
            seats.Remove((match, side));
        }
        hub.Publish(match, 0);
    }

    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMilliseconds(50);

    // One live socket of a seat. Every frame it receives counts as heard; disposing it leaves.
    public sealed class SeatSocket(MatchPresence presence, string match, Side side) : IDisposable
    {
        public TimeSpan Silence => presence.Silence;

        public void Heard() => presence.Heard(match, side);

        public void Dispose() => presence.Leave(match, side);
    }
}
