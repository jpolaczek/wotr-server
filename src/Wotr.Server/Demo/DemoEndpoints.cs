using Wotr.Contracts;

namespace Wotr.Server;

// DEMO-01: a member of a waiting room starts their demo against the CPU; the room keeps waiting.
public static class DemoEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/rooms/{id}/demo", async (string id, HttpContext context, DemoSessionStore demos, DemoBotRunner cpu) =>
        {
            var started = await demos.Start(MatchEndpoints.Account(context), id, context.RequestAborted);
            // The CPU may hold the first decision (the Fellowship phase when the player is the Shadow).
            if (started.Demo is not null)
                cpu.Wake(started.Demo.MatchId);
            return started.Demo is not null ? Results.Ok(started.Demo)
                : started.Error == "RoomNotFound" ? Results.NotFound()
                : Results.Conflict(new ApiError { Error = started.Error });
        }).RequireAuthorization().RequireRateLimiting(GameRateLimits.Rooms);
    }
}
