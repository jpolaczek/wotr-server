using Wotr.Contracts;

namespace Wotr.Server;

// AVATAR-02: the routes of the website's account page (infra/site/account/index.html). Only the page's own session reaches
// them; a game session gets a 401, and cannot change the avatar. Nothing is cached.
public static class AccountPageEndpoints
{
    public sealed record AccountSessionView(string Token, string Name, string Avatar, DateTimeOffset ExpiresAt);
    public sealed record AvatarRequest(string? Avatar);

    public static void Map(WebApplication app)
    {
        var page = app.MapGroup("/api/account").AddEndpointFilter(NoStore);
        page.MapPost("/session", SignIn).RequireRateLimiting(AuthenticationRateLimit.Login);
        page.MapDelete("/session", SignOut).RequireRateLimiting(GameRateLimits.Reads);
        page.MapGet("", Read).RequireRateLimiting(GameRateLimits.Reads);
        page.MapPut("/avatar", ChooseAvatar).RequireRateLimiting(GameRateLimits.Reads);
    }

    private static async Task<IResult> SignIn(LoginRequest request, AccountPage page, CancellationToken cancellation)
    {
        var result = await page.SignIn(request, cancellation);
        if (result.LockedFor is { } locked)
            return AuthenticationRateLimit.Locked(locked);
        return result.Session is { } session
            ? Results.Ok(new AccountSessionView(session.Token, session.Username, session.Avatar, session.ExpiresAt))
            : Results.Json(new ApiError { Error = "InvalidCredentials" }, statusCode: 401);
    }

    private static async Task<IResult> SignOut(HttpContext context, AccountPage page)
    {
        if (await page.Owner(context.Request, context.RequestAborted) is null)
            return Results.Unauthorized();
        await page.SignOut(context.Request, context.RequestAborted);
        return Results.NoContent();
    }

    private static async Task<IResult> Read(HttpContext context, AccountPage page) =>
        await page.Owner(context.Request, context.RequestAborted) is { } account
            ? Results.Ok(await page.View(account, context.RequestAborted))
            : Results.Unauthorized();

    private static async Task<IResult> ChooseAvatar(AvatarRequest request, HttpContext context, AccountPage page)
    {
        if (await page.Owner(context.Request, context.RequestAborted) is not { } account)
            return Results.Unauthorized();
        if (request.Avatar is not { } avatar || !AvatarIds.IsKnown(avatar))
            return Results.BadRequest(new ApiError { Error = "UnknownAvatar" });
        return Results.Ok(await page.ChooseAvatar(account, avatar, context.RequestAborted));
    }

    private static async ValueTask<object?> NoStore(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        invocation.HttpContext.Response.Headers.CacheControl = "no-store";
        return await next(invocation);
    }
}
