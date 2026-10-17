using System.Security.Claims;

namespace Wotr.Server;

public static class BattleReportEndpoints
{
    public static void Map(WebApplication app) =>
        app.MapGet("/api/matches/{id}/battles/{battleId}", async (string id, string battleId,
            ClaimsPrincipal user, BattleReportStore store, CancellationToken cancellation) =>
        {
            var report = await store.Read(id, battleId, user.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellation);
            return report is null ? Results.NotFound() : Results.Ok(report);
        }).RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads);
}
