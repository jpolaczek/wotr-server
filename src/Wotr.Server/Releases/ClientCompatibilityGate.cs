using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authentication;
using Wotr.Contracts;

namespace Wotr.Server;

// DS11: an incompatible human client is stopped before authentication, rate limits, WebSocket upgrade or any
// command. Health, the release catalog, the installer list and logout stay open, so a client can still
// learn what to install; so do the website's account endpoints (AUTH-02).
// BOT-16: a bot credential must authenticate before its independent compatibility policy can be selected.
public static class ClientCompatibilityGate
{
    public static IApplicationBuilder UseClientCompatibility(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.Ordinal) && BotTokens.IsBot(authorization[7..]))
        {
            var authenticated = await context.AuthenticateAsync("Bearer");
            if (!authenticated.Succeeded || BotIdentity.Of(context) is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            context.User = authenticated.Principal!;
            if (!BotCompatibility.Compatible(context, context.RequestServices.GetRequiredService<BotAccessOptions>()))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.WriteAsJsonAsync(new { error = "BotUpdateRequired" }, context.RequestAborted);
                return;
            }
            await next();
            return;
        }
        if (!Applies(context.Request.Path))
        {
            await next();
            return;
        }
        var policy = context.RequestServices.GetRequiredService<ClientCompatibilityPolicy>();
        var catalog = context.RequestServices.GetRequiredService<ReleaseCatalogStore>().Current();
        var headers = context.Request.Headers;
        var verdict = policy.CheckRequest(Single(headers[ClientCompatibility.ClientVersionHeader]),
            Single(headers[ClientCompatibility.ProtocolVersionHeader]), catalog);
        if (verdict == CompatibilityVerdict.Compatible)
        {
            await next();
            return;
        }
        context.Response.StatusCode = verdict == CompatibilityVerdict.Invalid ? StatusCodes.Status400BadRequest : StatusCodes.Status409Conflict;
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new { error = ClientCompatibilityPolicy.ErrorCode(verdict) }, context.RequestAborted);
    });

    // AUTH-02: the website's account pages (registration, email confirmation, password reset) are not a game client;
    // neither is its admin panel (ADMIN-01).
    private static bool Applies(PathString path) =>
        path.StartsWithSegments("/api") && !path.StartsWithSegments(ClientReleaseEndpoints.Path) &&
        !path.StartsWithSegments(ClientReleaseEndpoints.DownloadsPath) && !path.StartsWithSegments(ClientReleaseEndpoints.NotesPath) &&
        !path.StartsWithSegments("/api/auth/logout") &&
        !path.StartsWithSegments("/api/auth/register") && !path.StartsWithSegments("/api/account") &&
        !path.StartsWithSegments(AdminEndpoints.Path);

    // A repeated header is not a version; it fails as invalid instead of picking one of the values.
    private static string? Single(StringValues values) => values.Count switch
    {
        0 => null,
        1 => values[0],
        _ => "\u0000"
    };
}
