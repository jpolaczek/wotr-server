using System.Security.Claims;
using Wotr.Contracts;

namespace Wotr.Server;

// MENU-24: the waiting room's chat, under the rooms' authorization and request budget.
public static class RoomChatEndpoints
{
    public static void Map(RouteGroupBuilder rooms)
    {
        rooms.MapGet("/{id}/chat", async (string id, HttpContext context, RoomChat chat, long after = 0) =>
            Result(await chat.Read(id, Account(context), after, context.RequestAborted)));
        rooms.MapPost("/{id}/chat", async (string id, RoomChatRequest request, HttpContext context, RoomChat chat) =>
            Result(await chat.Post(id, Account(context), request?.Text, context.RequestAborted)));
    }

    private static string Account(HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static IResult Result(RoomChatResult result) => result.View is not null ? Results.Ok(result.View)
        : result.Error == "RoomNotFound" ? Results.NotFound()
        : Results.BadRequest(new ApiError { Error = result.Error });
}
