using Microsoft.Extensions.Primitives;
using Wotr.Contracts;

namespace Wotr.Server;

// DS11: an incompatible client is stopped before authentication, rate limits, WebSocket upgrade or any
// command. Health, the release catalog, the installer list and logout stay open, so a client can still
// learn what to install; so do the website's account endpoints (AUTH-02).
public static class ClientCompatibilityGate
{
    public static IApplicationBuilder UseClientCompatibility(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
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

    // AUTH-02: the website's account pages (registration, email confirmation, password reset) are not a game client.
    private static bool Applies(PathString path) =>
        path.StartsWithSegments("/api") && !path.StartsWithSegments(ClientReleaseEndpoints.Path) &&
        !path.StartsWithSegments(ClientReleaseEndpoints.DownloadsPath) && !path.StartsWithSegments("/api/auth/logout") &&
        !path.StartsWithSegments("/api/auth/register") && !path.StartsWithSegments("/api/account");

    // A repeated header is not a version; it fails as invalid instead of picking one of the values.
    private static string? Single(StringValues values) => values.Count switch
    {
        0 => null,
        1 => values[0],
        _ => "\u0000"
    };
}
