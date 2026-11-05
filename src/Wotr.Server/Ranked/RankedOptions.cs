namespace Wotr.Server;

// RANK-05/RANK-06 (docs/RANKED_PLAY.md D5, D9): how often the queue pairs, how long a search stays eligible after its
// client's last poll, when a forgotten search is removed, and how often the move clocks are checked. An interval of zero
// turns its service off, so tests pair and check clocks by hand.
public sealed record RankedOptions(TimeSpan PairingInterval, TimeSpan Liveness, TimeSpan Forget, TimeSpan ClockInterval)
{
    public const string PairingIntervalKey = "Wotr:Ranked:PairingIntervalMilliseconds";
    public const string ClockIntervalKey = "Wotr:Ranked:ClockIntervalMilliseconds";
    public static readonly RankedOptions Default = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6), TimeSpan.FromMinutes(10),
        TimeSpan.FromSeconds(1));

    public static RankedOptions Read(IConfiguration configuration) => Default with
    {
        PairingInterval = Interval(configuration[PairingIntervalKey], Default.PairingInterval),
        ClockInterval = Interval(configuration[ClockIntervalKey], Default.ClockInterval)
    };

    private static TimeSpan Interval(string? value, TimeSpan fallback) =>
        int.TryParse(value, out var milliseconds) && milliseconds >= 0 ? TimeSpan.FromMilliseconds(milliseconds) : fallback;
}
