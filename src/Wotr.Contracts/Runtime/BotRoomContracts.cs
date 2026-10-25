namespace Wotr.Contracts
{
    public enum SeatOccupantKind { Empty, Human, Cpu }
    public enum BotSeatStatus { Joining, Ready, Unavailable }

    public sealed class AddRoomBotRequest
    {
        public string RequestId { get; set; } = "";
        public int ExpectedVersion { get; set; }
    }

    public sealed class BotSeatView
    {
        public Side Side { get; set; }
        public string ProfileId { get; set; } = "";
        public string PolicyVersion { get; set; } = "";
        public BotSeatStatus Status { get; set; }
    }
}
