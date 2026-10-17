using System.Collections.Generic;

namespace Wotr.Contracts
{
    // MENU-24: the players of a waiting room talk; the server keeps only recent messages in memory.
    public sealed class RoomChatMessage
    {
        public long Sequence { get; set; }
        public string Author { get; set; } = "";
        // OBS-01: Side names a player's seat; an observer's message has Role Observer and no meaningful side.
        public Side Side { get; set; }
        public RoomRole Role { get; set; }
        public string Text { get; set; } = "";
    }

    public sealed class RoomChatView
    {
        public List<RoomChatMessage> Messages { get; set; } = new List<RoomChatMessage>();
        public long Latest { get; set; }
    }

    public sealed class RoomChatRequest
    {
        public string Text { get; set; } = "";
    }
}
