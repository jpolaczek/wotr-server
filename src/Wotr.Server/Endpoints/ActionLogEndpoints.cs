using System.Security.Claims;
using Wotr.Contracts;

namespace Wotr.Server;

public static class ActionLogEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/matches").RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads);
        group.MapGet("/{id}/actions", async (string id, ClaimsPrincipal user, ActionLogStore store, CancellationToken ct,
            ActionLogFilter filter = ActionLogFilter.All, int limit = 50, string? before = null, string? after = null) =>
        {
            try
            {
                var page = await store.Page(id, user.FindFirstValue(ClaimTypes.NameIdentifier)!, filter, limit, before, after, ct);
                return page is null ? Results.NotFound() : Results.Ok(page);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(new { error = "InvalidPage" });
            }
        });
        group.MapGet("/{id}/actions/{actionId}", async (string id, string actionId, ClaimsPrincipal user,
            ActionLogStore store, CancellationToken ct, ActionLogFilter filter = ActionLogFilter.All) =>
        {
            if (!Enum.IsDefined(filter))
                return Results.BadRequest(new { error = "InvalidPage" });
            var action = await store.Action(id, user.FindFirstValue(ClaimTypes.NameIdentifier)!, actionId, filter, ct);
            return action is null ? Results.NotFound() : Results.Ok(action);
        });
    }
}
