using System.Security.Claims;
using Wotr.Contracts;

namespace Wotr.Server;

public static class LobbyEndpoints
{
    public static void Map(WebApplication app)
    {
        // AUTH-02: the game's web page registers (the client only opens it); registration sends an email, so it
        // has the stricter email budget instead of the login one.
        app.MapPost("/api/auth/register", async (RegisterRequest request, AccountService accounts, CancellationToken cancellation) =>
            RegistrationResult(await accounts.Register(request, cancellation))).RequireRateLimiting(AuthenticationRateLimit.Email);
        app.MapPost("/api/auth/login", async (LoginRequest request, AccountService accounts, CancellationToken cancellation) =>
            SessionResult(await accounts.Login(request, cancellation))).RequireRateLimiting(AuthenticationRateLimit.Login);
        app.MapPost("/api/auth/logout", async (HttpContext context, SessionStore sessions) =>
        {
            await sessions.Revoke(context.Request.Headers.Authorization.ToString()[7..], context.RequestAborted);
            return Results.NoContent();
        }).RequireAuthorization();
        MapRooms(app.MapGroup("/api/rooms").RequireAuthorization().RequireRateLimiting(GameRateLimits.Rooms));
    }

    private static void MapRooms(RouteGroupBuilder rooms)
    {
        rooms.MapGet("/", async (HttpContext context, RoomStore store) =>
            Results.Ok(await store.List(Account(context), context.RequestAborted)));
        rooms.MapGet("/public", async (HttpContext context, RoomStore store, string cursor = "", int limit = 20) =>
            limit is < 1 or > 50 || cursor.Length > 32 || cursor.Any(char.IsControl)
                ? Results.BadRequest(new ApiError { Error = "InvalidPage" })
                : Results.Ok(await store.ListPublic(Account(context), cursor, limit, context.RequestAborted)));
        // OBS-01: ?role=Observer joins the audience of a public room, also after the start.
        rooms.MapPost("/{id}/join", async (string id, HttpContext context, RoomStore store, RoomRole role = RoomRole.Player) =>
            !Enum.IsDefined(role) ? Results.BadRequest(new ApiError { Error = "InvalidRole" })
                : RoomResult(await store.JoinPublic(id, Account(context), role, context.RequestAborted)));
        rooms.MapGet("/{id}", async (string id, HttpContext context, RoomStore store) =>
            await store.Read(id, Account(context), context.RequestAborted) is { } room ? Results.Ok(room)
                : await store.WasRemoved(id, Account(context), context.RequestAborted)
                    ? Results.Json(new ApiError { Error = "RemovedFromRoom" }, statusCode: StatusCodes.Status410Gone)
                    : Results.NotFound());
        rooms.MapPost("/", async (CreateRoomRequest request, HttpContext context, RoomStore store) =>
            Enum.IsDefined(request.Side) && Enum.IsDefined(request.Visibility) && request.Name is not null &&
                (request.Name.Length == 0 || RoomInput.ValidName(request.Name))
                ? Results.Ok(await store.Create(Account(context), request, context.RequestAborted))
                : Results.BadRequest(new ApiError { Error = "InvalidRoomSettings" }));
        rooms.MapPost("/join", async (JoinRoomRequest request, HttpContext context, RoomStore store) =>
            request.Code is not { Length: 43 } ? Results.NotFound()
                : !Enum.IsDefined(request.Role) ? Results.BadRequest(new ApiError { Error = "InvalidRole" })
                : RoomResult(await store.Join(request.Code, Account(context), request.Role, context.RequestAborted)));
        rooms.MapPut("/{id}/configuration", async (string id, ConfigureRoomRequest request, HttpContext context, RoomStore store) =>
            request.Configuration is null ? Results.BadRequest(new ApiError { Error = "InvalidConfiguration" }) :
                RoomResult(await store.Configure(id, Account(context), request, context.RequestAborted)));
        rooms.MapPost("/{id}/confirm", async (string id, RoomRevisionRequest request, HttpContext context, RoomStore store) =>
            RoomResult(await store.Confirm(id, Account(context), request.ExpectedVersion, context.RequestAborted)));
        rooms.MapPut("/{id}/ready", async (string id, RoomReadyRequest request, HttpContext context, RoomStore store) =>
            RoomResult(await store.SetReady(id, Account(context), request, context.RequestAborted)));
        rooms.MapPost("/{id}/invitation", async (string id, HttpContext context, RoomStore store) =>
            RoomResult(await store.Invitation(id, Account(context), context.RequestAborted)));
        rooms.MapPost("/{id}/leave", async (string id, HttpContext context, RoomStore store, RoomChat chat) =>
        {
            var result = await store.Leave(id, Account(context), context.RequestAborted);
            // A room the host closed or deleted keeps no chat.
            if (result.Room?.Status == RoomStatus.Closed)
                chat.Forget(id);
            return result.Error is "" or "RoomNotFound" ? Results.NoContent() : RoomResult(result);
        });
        rooms.MapPost("/{id}/start", async (string id, RoomRevisionRequest request, HttpContext context, RoomStore store) =>
            RoomResult(await store.Start(id, Account(context), request.ExpectedVersion, context.RequestAborted)));
        // OBS-01: an optional body names the participant (a player or an observer) the host removes.
        rooms.MapPost("/{id}/remove", async (string id, HttpContext context, RoomStore store, RemoveParticipantRequest? request) =>
            RoomResult(await store.Remove(id, Account(context), request ?? new(), context.RequestAborted)));
        rooms.MapPost("/{id}/role", async (string id, RoomRoleRequest request, HttpContext context, RoomStore store) =>
            !Enum.IsDefined(request.Role) || !Enum.IsDefined(request.Side)
                ? Results.BadRequest(new ApiError { Error = "InvalidRole" })
                : RoomResult(await store.ChangeRole(id, Account(context), request, context.RequestAborted)));
        // OBS-01: the observer's heartbeat (every 15 s); a lapsed place or a closed room answers 404.
        rooms.MapPost("/{id}/presence", async (string id, HttpContext context, RoomStore store) =>
            await store.Presence(id, Account(context), context.RequestAborted) ? Results.NoContent() : Results.NotFound());
        RoomChatEndpoints.Map(rooms);
    }

    private static string Account(HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static IResult SessionResult(SessionView? session) => session is null
        ? Results.Json(new ApiError { Error = "InvalidCredentialsOrInvitation" }, statusCode: 401) : Results.Ok(session);
    private static IResult RegistrationResult(RegistrationResult result) => result.Error switch
    {
        RegistrationError.None => Results.Ok(result.Session),
        RegistrationError.InvalidRegistration => Results.BadRequest(new ApiError { Error = "InvalidRegistration" }),
        _ => Results.Conflict(new ApiError { Error = result.Error.ToString() })
    };
    private static IResult RoomResult(RoomResult result) => result.Room is not null ? Results.Ok(result.Room)
        : result.Error == "RoomNotFound" ? Results.NotFound() : Results.Conflict(new ApiError { Error = result.Error });
}
