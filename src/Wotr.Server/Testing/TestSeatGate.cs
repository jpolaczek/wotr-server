using System.Security.Claims;

namespace Wotr.Server;

// TEST-01: a technical seat session reaches only the match routes; MatchStore limits it to its own test match.
public static class TestSeatGate
{
    public static IApplicationBuilder UseTestSeatGate(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var account = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (account is not null && TestSessionStore.IsSeat(account) && !context.Request.Path.StartsWithSegments("/api/matches"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        await next();
    });
}
