namespace Wotr.Server;

// RANK-05 (docs/RANKED_PLAY.md D5): pairs the ranked queue once a second. Each pass takes a PostgreSQL advisory lock,
// so several server processes never pair the same searches twice.
public sealed class RankedPairingService(RankedQueue queue, RankedOptions options, TimeProvider clock,
    ILogger<RankedPairingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        if (options.PairingInterval <= TimeSpan.Zero)
            return;
        using var timer = new PeriodicTimer(options.PairingInterval, clock);
        while (await timer.WaitForNextTickAsync(stopping))
        {
            try
            {
                await queue.PairOnce(stopping);
            }
            // An exception leaving a background service stops the whole server: a failure costs this pass and no more.
            catch (Exception error) when (!stopping.IsCancellationRequested)
            {
                logger.LogWarning(error, "The ranked queue was not paired this time.");
            }
        }
    }
}
