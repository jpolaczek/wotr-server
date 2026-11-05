namespace Wotr.Server;

// RANK-05/RANK-06/RANK-07 (docs/RANKED_PLAY.md D5, D9, D11): how often the queue pairs, how long a search stays eligible
// after its client's last poll, when a forgotten search is removed, how often the move clocks are checked, and how long
// a pause may last. An interval of zero turns its service off, so tests pair and check clocks by hand. ClockScale
// shortens every limit for tests and playtests, and a Production server refuses to start with it.
public sealed record RankedOptions(TimeSpan PairingInterval, TimeSpan Liveness, TimeSpan Forget, TimeSpan ClockInterval,
    TimeSpan PauseLimit, double ClockScale = 1)
{
    public const string PairingIntervalKey = "Wotr:Ranked:PairingIntervalMilliseconds";
    public const string ClockIntervalKey = "Wotr:Ranked:ClockIntervalMilliseconds";
    public const string ClockScaleKey = "Wotr:Ranked:ClockScalePercent";
    // RANK-10: a seated player's socket may stay silent this long; a client heartbeats every 2 s. The grace covers clients
    // returning after a server start, and clients opening the board of a new match.
    public static readonly TimeSpan SocketSilence = TimeSpan.FromSeconds(8);
    public static readonly TimeSpan PresenceGrace = TimeSpan.FromSeconds(15);
    public static readonly RankedOptions Default = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6), TimeSpan.FromMinutes(10),
        TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10));

    public static RankedOptions Read(IConfiguration configuration, IHostEnvironment environment)
    {
        var scale = configuration[ClockScaleKey];
        if (!string.IsNullOrEmpty(scale) && environment.IsProduction())
            throw new InvalidOperationException(ClockScaleKey + " shortens the clocks of real games; Production refuses it.");
        return Default with
        {
            PairingInterval = Interval(configuration[PairingIntervalKey], Default.PairingInterval),
            ClockInterval = Interval(configuration[ClockIntervalKey], Default.ClockInterval),
            ClockScale = int.TryParse(scale, out var percent) && percent > 0 ? percent / 100.0 : Default.ClockScale
        };
    }

    public TimeSpan Scaled(TimeSpan span) => span * ClockScale;

    private static TimeSpan Interval(string? value, TimeSpan fallback) =>
        int.TryParse(value, out var milliseconds) && milliseconds >= 0 ? TimeSpan.FromMilliseconds(milliseconds) : fallback;
}
