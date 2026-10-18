using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Server;

// ADMIN-01: the admin panel's API, called by the website's /admin page without client version headers. To anyone but
// an administrator every answer is an empty 404, as for a path that does not exist; nothing is cached.
public static class AdminEndpoints
{
    public const string Path = "/api/admin";
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
    public const int MaxPage = 100_000;

    public static void Map(WebApplication app)
    {
        var admin = app.MapGroup(Path).AddEndpointFilter(NoStore);
        admin.MapPost("/session", SignIn).RequireRateLimiting(AuthenticationRateLimit.Login);
        var data = admin.MapGroup("").AddEndpointFilter(AdministratorsOnly).RequireRateLimiting(GameRateLimits.Reads);
        data.MapGet("/summary", async (AdminStatisticsStore statistics, CancellationToken cancellation) =>
            Results.Ok(await statistics.Summary(cancellation)));
        data.MapGet("/users", async (AdminStatisticsStore statistics, CancellationToken cancellation,
                int page = 0, int size = DefaultPageSize) =>
            page is < 0 or > MaxPage || size is < 1 or > MaxPageSize
                ? Results.BadRequest(new ApiError { Error = "InvalidPage" })
                : Results.Ok(await statistics.Users(page, size, cancellation)));
    }

    private static async Task<IResult> SignIn(HttpContext context, AdminAccess access)
    {
        var request = await ReadSignIn(context);
        if (request is null)
            return Results.NotFound();
        var login = new LoginRequest { Username = request.Username ?? "", Password = request.Password ?? "" };
        var result = await access.SignIn(login, context.RequestAborted);
        if (result.LockedFor is { } locked)
            return AuthenticationRateLimit.Locked(locked);
        return result.Session is not { } session ? Results.NotFound()
            : Results.Ok(new AdminSessionView(session.Token, session.Username, session.ExpiresAt));
    }

    // A body that is not the sign-in request is answered like a refused sign-in.
    private static async Task<AdminSignInRequest?> ReadSignIn(HttpContext context)
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<AdminSignInRequest>(context.RequestAborted);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or BadHttpRequestException)
        {
            return null;
        }
    }

    private static async ValueTask<object?> AdministratorsOnly(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var context = invocation.HttpContext;
        var access = context.RequestServices.GetRequiredService<AdminAccess>();
        var admin = await access.Administrator(context.Request, context.RequestAborted);
        if (admin is null)
            return Results.NotFound();
        access.Viewed(admin, context.Request.Path);
        return await next(invocation);
    }

    private static async ValueTask<object?> NoStore(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        invocation.HttpContext.Response.Headers.CacheControl = "no-store";
        return await next(invocation);
    }
}
