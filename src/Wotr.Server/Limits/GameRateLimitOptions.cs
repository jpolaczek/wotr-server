using System.Globalization;

namespace Wotr.Server;

// QUAL-02: per-instance budgets of a signed-in account. Commands and previews are counted per account and match,
// reads and rooms per account, concurrent work and update sockets per account. Values come from configuration
// (environment variables) and are validated at start; a limiter is local to one server process.
public sealed record GameRateLimitOptions
{
    public int WindowSeconds { get; init; } = 10;
    public int CommandsPerWindow { get; init; } = 60;
    public int PreviewsPerWindow { get; init; } = 120;
    public int ReadsPerWindow { get; init; } = 240;
    public int RoomRequestsPerWindow { get; init; } = 60;
    public int ConcurrentRequests { get; init; } = 8;
    public int UpdateSockets { get; init; } = 4;

    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);

    public static GameRateLimitOptions Read(IConfiguration configuration)
    {
        var defaults = new GameRateLimitOptions();
        return new GameRateLimitOptions
        {
            WindowSeconds = Value(configuration, "WOTR_LIMIT_WINDOW_SECONDS", defaults.WindowSeconds, 1, 3600),
            CommandsPerWindow = Value(configuration, "WOTR_LIMIT_COMMANDS", defaults.CommandsPerWindow, 1, 10000),
            PreviewsPerWindow = Value(configuration, "WOTR_LIMIT_PREVIEWS", defaults.PreviewsPerWindow, 1, 10000),
            ReadsPerWindow = Value(configuration, "WOTR_LIMIT_READS", defaults.ReadsPerWindow, 1, 10000),
            RoomRequestsPerWindow = Value(configuration, "WOTR_LIMIT_ROOMS", defaults.RoomRequestsPerWindow, 1, 10000),
            ConcurrentRequests = Value(configuration, "WOTR_LIMIT_CONCURRENT", defaults.ConcurrentRequests, 1, 256),
            UpdateSockets = Value(configuration, "WOTR_LIMIT_SOCKETS", defaults.UpdateSockets, 1, 64)
        };
    }

    private static int Value(IConfiguration configuration, string key, int fallback, int minimum, int maximum)
    {
        var text = configuration[key];
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"{key} must be a whole number from {minimum} to {maximum}.");
        return value;
    }
}
