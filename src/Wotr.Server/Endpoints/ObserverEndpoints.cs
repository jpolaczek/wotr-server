using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// OBS-02: read-only access for an observer of a match's room. The perspective (Free Peoples by default) picks whose
// hand and knowledge the observer sees, and the board, history, battle reports and updates all use it. Players keep
// their own endpoints and get 404 here, so no query string shows them the other side; nothing here changes the match,
// wakes the CPU or marks anything as seen.
public static class ObserverEndpoints
{
    public static void Map(WebApplication app)
    {
        var observe = app.MapGroup("/api/observe/{id}").RequireAuthorization();
        observe.MapGet("", View).RequireRateLimiting(GameRateLimits.Reads);
        observe.MapGet("/actions", Actions).RequireRateLimiting(GameRateLimits.Reads);
        observe.MapGet("/actions/{actionId}", Action).RequireRateLimiting(GameRateLimits.Reads);
        observe.MapGet("/battles/{battleId}", Battle).RequireRateLimiting(GameRateLimits.Reads);
        observe.MapGet("/updates", (string id, HttpContext context, MatchSocket socket) => socket.Run(id, context, observing: true));
    }

    private static async Task<IResult> View(string id, HttpContext context, MatchStore store, Projection projection,
        Side perspective = Side.FreePeoples)
    {
        if (!Enum.IsDefined(perspective))
            return InvalidPerspective();
        var state = await store.ReadObserved(id, MatchEndpoints.Account(context), context.RequestAborted);
        return state is null ? Results.NotFound() : Results.Ok(projection.Observe(state, perspective));
    }

    private static async Task<IResult> Actions(string id, HttpContext context, ActionLogStore store,
        Side perspective = Side.FreePeoples, ActionLogFilter filter = ActionLogFilter.All, int limit = 50,
        string? before = null, string? after = null)
    {
        if (!Enum.IsDefined(perspective))
            return InvalidPerspective();
        try
        {
            var page = await store.Page(id, Reader(context, perspective), filter, limit, before, after, context.RequestAborted);
            return page is null ? Results.NotFound() : Results.Ok(page);
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new ApiError { Error = "InvalidPage" });
        }
    }

    private static async Task<IResult> Action(string id, string actionId, HttpContext context, ActionLogStore store,
        Side perspective = Side.FreePeoples, ActionLogFilter filter = ActionLogFilter.All)
    {
        if (!Enum.IsDefined(perspective))
            return InvalidPerspective();
        if (!Enum.IsDefined(filter))
            return Results.BadRequest(new ApiError { Error = "InvalidPage" });
        var action = await store.Action(id, Reader(context, perspective), actionId, filter, context.RequestAborted);
        return action is null ? Results.NotFound() : Results.Ok(action);
    }

    private static async Task<IResult> Battle(string id, string battleId, HttpContext context, BattleReportStore store,
        Side perspective = Side.FreePeoples)
    {
        if (!Enum.IsDefined(perspective))
            return InvalidPerspective();
        var report = await store.Read(id, battleId, Reader(context, perspective), context.RequestAborted);
        return report is null ? Results.NotFound() : Results.Ok(report);
    }

    private static MatchReader Reader(HttpContext context, Side perspective) =>
        MatchReader.Observer(MatchEndpoints.Account(context), perspective);

    private static IResult InvalidPerspective() => Results.BadRequest(new ApiError { Error = "InvalidPerspective" });
}
