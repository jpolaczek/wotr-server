using Wotr.Contracts;

namespace Wotr.Server;

// REPLAY-02: the .wotrreplay download of a finished match — for its players and for an observer still watching it.
// 404 outside the match, 409 MatchNotFinished during the game and 409 ReplayUnavailable for a match without recording.
public static class ReplayEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/matches/{id}/replay", async (string id, HttpContext context, ReplayExporter exporter) =>
                Result(await exporter.ForPlayer(id, MatchEndpoints.Account(context), context.RequestAborted)))
            .RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads);
        app.MapGet("/api/observe/{id}/replay", async (string id, HttpContext context, ReplayExporter exporter) =>
                Result(await exporter.ForObserver(id, MatchEndpoints.Account(context), context.RequestAborted)))
            .RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads);
    }

    private static IResult Result(ReplayExport export) => export.Error switch
    {
        "" => Results.File(export.Archive!, "application/zip", export.FileName),
        "MatchNotFound" => Results.NotFound(),
        _ => Results.Conflict(new ApiError { Error = export.Error })
    };
}
