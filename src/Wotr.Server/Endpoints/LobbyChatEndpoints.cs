using Wotr.Contracts;

namespace Wotr.Server;

// LOBBY-01: the Open games screen's chat, players online and challenges, for signed-in players. The poll uses the
// reads budget (the menu polls every LobbyLimits.PollSeconds); posts and challenge answers use the rooms budget.
public static class LobbyChatEndpoints
{
    public static void Map(WebApplication app)
    {
        var lobby = app.MapGroup("/api/lobby").RequireAuthorization();
        lobby.MapGet("", async (HttpContext context, Lobby poll, long after = 0) =>
            Results.Ok(await poll.Poll(Account(context), after, context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Reads);
        lobby.MapPost("/messages", async (LobbyMessageRequest request, HttpContext context, LobbyChat chat) =>
            await chat.Post(Account(context), request?.Text, context.RequestAborted) is { } message
                ? Results.Ok(message)
                : Results.BadRequest(new ApiError { Error = "InvalidMessage" }))
            .RequireRateLimiting(GameRateLimits.Rooms);
        lobby.MapPost("/challenges", async (ChallengeRequest request, HttpContext context, LobbyChallenges challenges) =>
            Result(await challenges.Send(Account(context), request?.Name, context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Rooms);
        lobby.MapPost("/challenges/{id}/accept", async (string id, HttpContext context, ChallengeAcceptance acceptance) =>
            Result(await acceptance.Accept(Account(context), id, context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Rooms);
        lobby.MapPost("/challenges/{id}/decline", async (string id, HttpContext context, LobbyChallenges challenges) =>
            Answer(await challenges.Decline(Account(context), id, context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Rooms);
        lobby.MapPost("/challenges/{id}/cancel", async (string id, HttpContext context, LobbyChallenges challenges) =>
            Answer(await challenges.Cancel(Account(context), id, context.RequestAborted)))
            .RequireRateLimiting(GameRateLimits.Rooms);
    }

    private static string Account(HttpContext context) => MatchEndpoints.Account(context);

    private static IResult Result(ChallengeResult result) =>
        result.Challenge is not null ? Results.Ok(result.Challenge) : Refusal(result.Error);

    private static IResult Result(AcceptResult result) =>
        result.Room is not null ? Results.Ok(result.Room) : Refusal(result.Error);

    private static IResult Answer(ChallengeError error) => error == ChallengeError.None ? Results.NoContent() : Refusal(error);

    // An unknown player or challenge is 404; every other refusal is a 409 naming its reason.
    private static IResult Refusal(ChallengeError error)
    {
        var body = new ApiError { Error = error.ToString() };
        return error is ChallengeError.PlayerNotFound or ChallengeError.ChallengeNotFound
            ? Results.NotFound(body)
            : Results.Conflict(body);
    }
}
