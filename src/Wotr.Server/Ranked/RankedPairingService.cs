using Npgsql;

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
            catch (Exception error) when (error is NpgsqlException or InvalidOperationException)
            {
                logger.LogWarning(error, "The ranked queue was not paired this time.");
            }
        }
    }
}
