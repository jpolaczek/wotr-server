namespace Wotr.Server;

// RANK-05/RANK-06/RANK-07 (docs/RANKED_PLAY.md D5, D9, D11): how often the queue pairs, how long a search stays eligible
// after its client's last poll, when a forgotten search is removed, how often the move clocks are checked, and how long
// a pause may last. An interval of zero turns its service off, so tests pair and check clocks by hand. ClockScale
// shortens every limit, and PresenceScale the socket's silence and presence's grace, for tests and playtests; they are
// apart because a real client heartbeats every 2 s whatever the clocks run at. A Production server refuses to start
// with either.
public sealed record RankedOptions(TimeSpan PairingInterval, TimeSpan Liveness, TimeSpan Forget, TimeSpan ClockInterval,
    TimeSpan PauseLimit, double ClockScale = 1, double PresenceScale = 1)
{
    public const string PairingIntervalKey = "Wotr:Ranked:PairingIntervalMilliseconds";
    public const string ClockIntervalKey = "Wotr:Ranked:ClockIntervalMilliseconds";
    public const string ClockScaleKey = "Wotr:Ranked:ClockScalePercent";
    public const string PresenceScaleKey = "Wotr:Ranked:PresenceScalePercent";
    // RANK-10: a seated player's socket may stay silent this long; a client heartbeats every 2 s. The grace covers clients
    // returning after a server start, and clients opening the board of a new match.
    public static readonly TimeSpan SocketSilence = TimeSpan.FromSeconds(8);
    public static readonly TimeSpan PresenceGrace = TimeSpan.FromSeconds(15);
    public static readonly RankedOptions Default = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6), TimeSpan.FromMinutes(10),
        TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10));

    public static RankedOptions Read(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsProduction())
            foreach (var key in new[] { ClockScaleKey, PresenceScaleKey })
                if (!string.IsNullOrEmpty(configuration[key]))
                    throw new InvalidOperationException(key + " changes the time limits of real games; Production refuses it.");
        return Default with
        {
            PairingInterval = Interval(configuration[PairingIntervalKey], Default.PairingInterval),
            ClockInterval = Interval(configuration[ClockIntervalKey], Default.ClockInterval),
            ClockScale = Scale(configuration[ClockScaleKey]),
            PresenceScale = Scale(configuration[PresenceScaleKey])
        };
    }

    public TimeSpan Scaled(TimeSpan span) => span * ClockScale;

    public TimeSpan PresenceScaled(TimeSpan span) => span * PresenceScale;

    private static double Scale(string? value) => int.TryParse(value, out var percent) && percent > 0 ? percent / 100.0 : 1;

    private static TimeSpan Interval(string? value, TimeSpan fallback) =>
        int.TryParse(value, out var milliseconds) && milliseconds >= 0 ? TimeSpan.FromMilliseconds(milliseconds) : fallback;
}
