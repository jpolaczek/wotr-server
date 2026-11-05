using System.Security.Claims;

namespace Wotr.Server;

// RANK-10 (R35, docs/RANKED_PLAY.md, Reconnecting): a required update cannot take an active ranked match away from its
// players. A client below the catalog's minimum, with a supported protocol, may use that match's routes, find it through
// the queue's poll, and sign in, while the account holds its seat, and may read the result for a while after it ended.
// ClientCompatibilityGate, which runs before authentication, only lets such a request reach this check; every other
// request from that client is still refused, and so is the search.
public static class RankedSeatExemption
{
    private const string Waiting = "RankedSeatExemption.Waiting";
    private static readonly PathString Matches = "/api/matches";
    private static readonly PathString Queue = "/api/ranked/queue";
    private static readonly PathString Login = "/api/auth/login";

    // Whether this route could be exempt, for the gate. Nothing has been read yet.
    public static bool Covers(HttpRequest request) =>
        MatchId(request.Path) is not null ||
        HttpMethods.IsGet(request.Method) && request.Path == Queue ||
        HttpMethods.IsPost(request.Method) && request.Path == Login;

    public static void Await(HttpContext context) => context.Items[Waiting] = true;

    // True for a sign-in that passed the gate only because its account might hold a seat.
    public static bool IsWaiting(HttpContext context) => context.Items.ContainsKey(Waiting);

    // After authentication, which names the account. Signing in names it only in its body, so the endpoint checks that.
    public static IApplicationBuilder UseRankedSeatExemption(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (!IsWaiting(context) || context.Request.Path == Login || await Holds(context))
            await next();
        else
            await ClientCompatibilityGate.Refuse(context, CompatibilityVerdict.ClientUpdateRequired);
    });

    private static async Task<bool> Holds(HttpContext context)
    {
        var account = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (account is null)
            return false;
        var ranked = context.RequestServices.GetRequiredService<RankedMatches>();
        return MatchId(context.Request.Path) is { } match
            ? await ranked.MayUseBelowMinimum(match, account, context.RequestAborted)
            : await ranked.HoldsSeat(account, context.RequestAborted);
    }

    // The id in /api/matches/{id} and everything under it; the list of matches itself has none.
    private static string? MatchId(PathString path)
    {
        if (!path.StartsWithSegments(Matches, out var rest) || !rest.HasValue)
            return null;
        var id = rest.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrEmpty(id) ? null : id;
    }
}
