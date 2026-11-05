using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// RANK-05 (R35, docs/RANKED_PLAY.md D5): the ranked search. POST starts or replaces it, GET is the client's poll every
// 2 s, DELETE cancels it; each answers with the search's state, or Matched with the opponent once paired.
public static class RankedQueueEndpoints
{
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api/ranked/queue").RequireAuthorization();
        api.MapPost("", async (RankedSearchRequest request, HttpContext context, RankedQueue queue) =>
        {
            var version = context.Request.Headers[ClientCompatibility.ClientVersionHeader].ToString();
            var result = await queue.Search(MatchEndpoints.Account(context), request, version, context.RequestAborted);
            return result.Error switch
            {
                "" => Results.Ok(result.View),
                RankedRules.IllegalSearch => Results.BadRequest(new ApiError { Error = result.Error }),
                _ => Refused(context, result.Error)
            };
        }).RequireRateLimiting(GameRateLimits.Commands);
        api.MapGet("", async (HttpContext context, RankedQueue queue) =>
            Results.Ok(await queue.Poll(MatchEndpoints.Account(context), context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Reads);
        api.MapDelete("", async (HttpContext context, RankedQueue queue) =>
            Results.Ok(await queue.Cancel(MatchEndpoints.Account(context), context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Commands);
        // RANK-08 (D13): the top 25 and the player's own place.
        app.MapGet("/api/leaderboard", async (HttpContext context, Leaderboard leaderboard) =>
            Results.Ok(await leaderboard.Read(MatchEndpoints.Account(context), context.RequestAborted)))
            .RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads);
    }

    // ClientUpdateRequired is the module gate's own answer, which the client's updater already handles.
    private static IResult Refused(HttpContext context, string error)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(new ApiError { Error = error }, statusCode: StatusCodes.Status409Conflict);
    }
}
