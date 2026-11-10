namespace Wotr.Contracts
{
    public enum SeatOccupantKind { Empty, Human, Cpu }
    public enum BotSeatStatus { Joining, Ready, Unavailable }

    // BOT-37: the CPU's pace, chosen in the room before the game and saved with it. Fast answers
    // in about three seconds (at most five); Thorough looks further ahead in about ten (at most
    // fifteen). Both are the same opponent; a room or save without a pace is Fast.
    public enum BotThinkingMode { Fast, Thorough }

    public sealed class AddRoomBotRequest
    {
        public string RequestId { get; set; } = "";
        public int ExpectedVersion { get; set; }
        public BotThinkingMode ThinkingMode { get; set; }
    }

    // BOT-37: the host changes the CPU's pace while the room waits; it is a change of the room
    // (its version rises and both players are asked to confirm again) and is refused once started.
    public sealed class ConfigureRoomBotRequest
    {
        public int ExpectedVersion { get; set; }
        public BotThinkingMode ThinkingMode { get; set; }
    }

    public sealed class BotSeatView
    {
        public Side Side { get; set; }
        public string ProfileId { get; set; } = "";
        public string PolicyVersion { get; set; } = "";
        public BotSeatStatus Status { get; set; }
        public BotThinkingMode ThinkingMode { get; set; }
    }
}
