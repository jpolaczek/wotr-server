using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// RANK-04 (R35, docs/RANKED_PLAY.md): a ranked match's concession and the player's own rating change.
public static class RankedMatchEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/{id}/concede", async (string id, ConcedeRequest request, HttpContext context, MatchStore store,
            RankedMatches ranked, RevisionHub hub) =>
        {
            if (string.IsNullOrWhiteSpace(request.CommandId) || request.CommandId.Length > CommandShape.MaxCommandIdLength)
                return Results.BadRequest(new ApiError { Error = CommandShape.MalformedCommand });
            var account = MatchEndpoints.Account(context);
            var isRanked = await ranked.RankedFor(id, account, context.RequestAborted);
            if (isRanked is null)
                return Results.NotFound();
            if (isRanked == false)
                return Results.Conflict(new ApiError { Error = RankedMatches.NotRankedRefusal });
            var receipt = await store.Execute(RankedMatches.Concession(id, request.CommandId), account, context.RequestAborted);
            if (receipt is null)
                return Results.NotFound();
            if (receipt.Accepted)
                hub.Publish(id, receipt.Revision);
            return Results.Ok(receipt);
        }).RequireRateLimiting(GameRateLimits.Commands).ForMatch();
        api.MapGet("/{id}/rating", async (string id, HttpContext context, Database database) =>
        {
            await using var connection = await database.Source.OpenConnectionAsync(context.RequestAborted);
            var result = await RatingLedger.Result(connection, id, MatchEndpoints.Account(context), context.RequestAborted);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireRateLimiting(GameRateLimits.Reads);
    }
}
