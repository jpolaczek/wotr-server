using System.Security.Claims;
using Wotr.Domain;

namespace Wotr.Server;

public static class MatchEndpoints
{
    public static string Account(HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api/matches").RequireAuthorization();
        api.MapGet("/{id}", async (string id, HttpContext context, MatchStore store, Projection projection, DemoBotRunner demos, MatchPresence presence) =>
        {
            var match = await store.Read(id, Account(context), context.RequestAborted);
            // DEMO-03: opening a demo resumes its CPU, also after a server restart; other matches are not affected.
            if (match is null)
                return Results.NotFound();
            demos.Wake(id);
            var view = projection.For(match.State, match.Side);
            // RANK-06: a ranked match's running clock rides on the view, counted at this moment; never in a frame.
            view.Clock = store.ClockView(match);
            if (view.Clock is not null)
                view.Clock.OpponentOnline = presence.Online(id, TurnFlow.Other(match.Side));
            return Results.Ok(view);
        }).RequireRateLimiting(GameRateLimits.Reads).ForMatch();
        api.MapPost("/{id}/commands", async (string id, Wotr.Contracts.GameCommand request, HttpContext context, MatchStore store, RevisionHub hub,
            DemoBotRunner demos) =>
        {
            // A command without a usable id cannot be answered with a receipt (QUAL-01).
            if (string.IsNullOrWhiteSpace(request.CommandId) || request.CommandId.Length > CommandShape.MaxCommandIdLength)
                return Results.BadRequest(new
                {
                    error = CommandShape.MalformedCommand
                });
            if (id != request.MatchId)
                return Results.BadRequest(new
                {
                    error = "IllegalChoice"
                });
            // RANK-03: a concession and a timeout are the server's to issue, through their own paths (RANK-04, RANK-06).
            if (request.Kind is Wotr.Contracts.CommandKind.Concede or Wotr.Contracts.CommandKind.TimeOut)
                return Results.BadRequest(new
                {
                    error = "IllegalChoice"
                });
            var receipt = await store.Execute(request, Account(context), context.RequestAborted);
            if (receipt is null)
                return Results.NotFound();
            if (receipt.Accepted && receipt.PublicStateChanged)
            {
                hub.Publish(id, receipt.Revision);
                demos.Wake(id);
            }
            else if (receipt.Accepted && receipt.AutoDecision is not null)
                hub.Publish(id, receipt.Revision, Account(context));
            return Results.Ok(receipt);
        }).RequireRateLimiting(GameRateLimits.Commands).ForMatch();
        api.MapGet("/{id}/receipts/{commandId}", async (string id, string commandId, HttpContext context, MatchStore store) =>
        {
            var receipt = await store.Receipt(id, commandId, Account(context), context.RequestAborted);
            return receipt is null ? Results.NotFound() : Results.Ok(receipt);
        }).RequireRateLimiting(GameRateLimits.Reads);
        api.MapPost("/{id}/preview", async (string id, Wotr.Contracts.GameCommand request, HttpContext context, MatchStore store, PreviewService preview) =>
        {
            var match = await store.Read(id, Account(context), context.RequestAborted);
            return match is null ? Results.NotFound() : Results.Ok(preview.Preview(match.State, match.Side, request));
        }).RequireRateLimiting(GameRateLimits.Previews).ForMatch();
        api.MapDelete("/{id}", async (string id, HttpContext context, MatchRemoval removal, RankedMatches ranked, RevisionHub hub) =>
        {
            // RANK-04: removing a ranked match would escape its loss; whether a match is ranked never changes.
            if (await ranked.RankedFor(id, Account(context), context.RequestAborted) == true)
                return Results.Conflict(new Wotr.Contracts.ApiError { Error = RoomStore.RankedRefusal });
            if (!await removal.Delete(id, Account(context), context.RequestAborted))
                return Results.NotFound();
            // MENU-21: open update sockets find no match and close; their clients then read 404.
            hub.Publish(id, 0);
            return Results.NoContent();
        }).RequireRateLimiting(GameRateLimits.Commands);
        // EXP-02: a client older than one of the match's modules gets ClientUpdateRequired instead of the game.
        api.MapGet("/{id}/updates", (string id, HttpContext context, MatchSocket socket) => socket.Run(id, context)).ForMatch();
        RankedMatchEndpoints.Map(api);
    }
}
