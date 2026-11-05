namespace Wotr.Server;

// RANK-05 (docs/RANKED_PLAY.md D5): how often the queue pairs, how long a search stays eligible after its client's
// last poll, and when a forgotten search is removed. A pairing interval of zero turns the pairing service off, so tests
// pair by hand.
public sealed record RankedOptions(TimeSpan PairingInterval, TimeSpan Liveness, TimeSpan Forget)
{
    public const string PairingIntervalKey = "Wotr:Ranked:PairingIntervalMilliseconds";
    public static readonly RankedOptions Default = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6), TimeSpan.FromMinutes(10));

    public static RankedOptions Read(IConfiguration configuration) =>
        int.TryParse(configuration[PairingIntervalKey], out var milliseconds) && milliseconds >= 0
            ? Default with { PairingInterval = TimeSpan.FromMilliseconds(milliseconds) }
            : Default;
}
