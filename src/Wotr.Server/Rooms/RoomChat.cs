using Wotr.Contracts;

namespace Wotr.Server;

public sealed record RoomChatResult(RoomChatView? View = null, string Error = "");

// MENU-24: a plain chat for the participants of a room (OBS-01: observers too), held only in this process's memory
// (no history is stored, a restart clears it): the latest messages of each room, numbered within the room; rooms
// silent for a while drop out.
public sealed class RoomChat(RoomStore rooms, TimeProvider clock)
{
    public const int Kept = 50;
    public const int LongestText = 200;
    private static readonly TimeSpan Idle = TimeSpan.FromHours(2);
    private readonly Dictionary<string, RoomLog> logs = [];
    private readonly object gate = new();

    public async Task<RoomChatResult> Read(string roomId, string account, long after, CancellationToken cancellation = default)
    {
        if (await Member(roomId, account, cancellation) is null)
            return new(Error: "RoomNotFound");
        return new(View(roomId, after));
    }

    public async Task<RoomChatResult> Post(string roomId, string account, string? text, CancellationToken cancellation = default)
    {
        var clean = Clean(text);
        if (clean is null)
            return new(Error: "InvalidMessage");
        if (await Member(roomId, account, cancellation) is not { } member)
            return new(Error: "RoomNotFound");
        Append(roomId, new RoomChatMessage
        {
            Author = member.Name, Role = member.Room.Role, Side = member.Room.You, Text = clean
        });
        return new(View(roomId, 0));
    }

    // MENU-27: a deleted room takes its chat with it.
    public void Forget(string roomId)
    {
        lock (gate)
            logs.Remove(roomId);
    }

    // 1–200 characters after trimming, with no control characters (a line break included).
    public static string? Clean(string? text)
    {
        var trimmed = text?.Trim() ?? "";
        return trimmed.Length is 0 or > LongestText || trimmed.Any(char.IsControl) ? null : trimmed;
    }

    private async Task<RoomParticipant?> Member(string roomId, string account, CancellationToken cancellation)
    {
        var member = await rooms.Participant(roomId, account, cancellation);
        return member is null || member.Room.Status == RoomStatus.Closed ? null : member;
    }

    private void Append(string roomId, RoomChatMessage message)
    {
        lock (gate)
        {
            DropIdle();
            if (!logs.TryGetValue(roomId, out var log))
                logs[roomId] = log = new RoomLog();
            message.Sequence = ++log.Latest;
            log.Messages.Enqueue(message);
            while (log.Messages.Count > Kept)
                log.Messages.Dequeue();
            log.Touched = clock.GetUtcNow();
        }
    }

    // A number beyond the latest belongs to messages lost in a restart: the reader gets what there is now.
    private RoomChatView View(string roomId, long after)
    {
        lock (gate)
        {
            if (!logs.TryGetValue(roomId, out var log))
                return new RoomChatView();
            var from = after > log.Latest ? 0 : after;
            return new RoomChatView
            {
                Latest = log.Latest,
                Messages = log.Messages.Where(m => m.Sequence > from).Select(Copy).ToList()
            };
        }
    }

    private void DropIdle()
    {
        var now = clock.GetUtcNow();
        foreach (var room in logs.Where(l => now - l.Value.Touched > Idle).Select(l => l.Key).ToList())
            logs.Remove(room);
    }

    private static RoomChatMessage Copy(RoomChatMessage message) => new()
    {
        Sequence = message.Sequence, Author = message.Author, Side = message.Side, Role = message.Role, Text = message.Text
    };

    private sealed class RoomLog
    {
        public long Latest { get; set; }
        public Queue<RoomChatMessage> Messages { get; } = new();
        public DateTimeOffset Touched { get; set; }
    }
}
