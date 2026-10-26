using Wotr.Contracts;

namespace Wotr.Server;

public static class CorrespondenceEndpoints
{
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api/matches").RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads);
        api.MapGet("", List);
        api.MapGet("/{id}/activity", Activity);
        api.MapPut("/{id}/seen", Seen);
    }

    // LOBBY-05: no status is My games (the unfinished games); status=All also lists the finished ones.
    private static async Task<IResult> List(HttpContext context, MatchIndexStore store,
        MatchListFilter? status = null, string cursor = "", int limit = 20)
    {
        if (limit is < 1 or > 50 || cursor.Length > 100 || cursor.Any(char.IsControl) ||
            status is { } filter && !Enum.IsDefined(filter))
            return Results.BadRequest(new ApiError { Error = "InvalidPage" });
        return Results.Ok(await store.List(MatchEndpoints.Account(context), status, cursor, limit, context.RequestAborted));
    }

    private static async Task<IResult> Activity(string id, HttpContext context, CorrespondenceStore store, long? afterRevision = null)
    {
        var result = await store.Activity(id, MatchEndpoints.Account(context), afterRevision, context.RequestAborted);
        if (result.Error is not null)
            return Results.BadRequest(new ApiError { Error = result.Error });
        return result.View is null ? Results.NotFound() : Results.Ok(result.View);
    }

    private static async Task<IResult> Seen(string id, SeenRevisionRequest request, HttpContext context, CorrespondenceStore store)
    {
        if (request.Revision is not { } revision)
            return Results.BadRequest(new ApiError { Error = "InvalidRevision" });
        var result = await store.MarkSeen(id, MatchEndpoints.Account(context), revision, context.RequestAborted);
        if (result.Error is not null)
            return Results.BadRequest(new ApiError { Error = result.Error });
        return result.View is null ? Results.NotFound() : Results.Ok(result.View);
    }
}
