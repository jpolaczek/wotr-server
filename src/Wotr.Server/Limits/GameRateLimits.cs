using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Wotr.Server;

// QUAL-02: rate and concurrency budgets of signed-in accounts, applied after authentication and before any
// match lock, transaction or receipt. A rejected request is 429 with Retry-After and changes nothing.
internal static class GameRateLimits
{
    public const string Commands = "game-commands";
    public const string Previews = "game-previews";
    public const string Reads = "game-reads";
    public const string Rooms = "rooms";

    // Values are read from the final configuration when first needed (and checked at start by Program).
    public static void AddGameRateLimits(this IServiceCollection services)
    {
        services.AddSingleton(provider => GameRateLimitOptions.Read(provider.GetRequiredService<IConfiguration>()));
        services.AddSingleton<UpdateSocketBudget>();
        services.AddOptions<RateLimiterOptions>().Configure<GameRateLimitOptions>((options, limits) =>
        {
            options.AddPolicy(Commands, context => Window(MatchKey(context), limits.CommandsPerWindow, limits));
            options.AddPolicy(Previews, context => Window(MatchKey(context), limits.PreviewsPerWindow, limits));
            options.AddPolicy(Reads, context => Window(AccountKey(context), limits.ReadsPerWindow, limits));
            options.AddPolicy(Rooms, context => Window(AccountKey(context), limits.RoomRequestsPerWindow, limits));
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => Concurrency(context, limits));
        });
    }

    // Update sockets live for the whole game and have their own budget (UpdateSocketBudget).
    private static RateLimitPartition<string> Concurrency(HttpContext context, GameRateLimitOptions limits) =>
        Account(context) is { } account && !context.WebSockets.IsWebSocketRequest
            ? RateLimitPartition.GetConcurrencyLimiter(account, _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = limits.ConcurrentRequests,
                QueueLimit = 0
            })
            : RateLimitPartition.GetNoLimiter("");

    private static RateLimitPartition<string> Window(string key, int permits, GameRateLimitOptions limits) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = limits.Window,
            QueueLimit = 0
        });

    // Signed-in budgets follow the account, never only the address, so a shared NAT does not block a group and
    // one account cannot spend its opponent's budget.
    private static string AccountKey(HttpContext context) =>
        Account(context) ?? "address:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    private static string MatchKey(HttpContext context) =>
        AccountKey(context) + "|" + (context.Request.RouteValues["id"] as string ?? "");

    private static string? Account(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
}
