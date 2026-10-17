using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// TEST-01: the catalog and the owner's test session. Without the option the routes do not exist (404),
// which is how a client knows to hide the test mode. TEST-08: every signed-in account may use it, so a start
// (a new match and its seat) counts against the account's room budget like a demo start.
public static class TestScenarioEndpoints
{
    public static void Map(WebApplication app)
    {
        if (!app.Services.GetRequiredService<TestScenarioOptions>().Enabled)
            return;
        var api = app.MapGroup("/api/test-scenarios").RequireAuthorization();
        api.MapGet("", () => Results.Ok(new TestScenarioCatalogView
        {
            Scenarios = TestScenarioCatalog.All.Select(scenario => scenario.View()).ToList()
        }));
        api.MapPost("/{id}/start", async (string id, HttpContext context, TestSessionStore store) =>
        {
            var scenario = TestScenarioCatalog.Find(id);
            if (scenario is null)
                return Results.NotFound(new ApiError { Error = "ScenarioNotFound" });
            return Results.Ok(await store.Start(MatchEndpoints.Account(context), scenario, context.RequestAborted));
        }).RequireRateLimiting(GameRateLimits.Rooms);
        api.MapDelete("/session", async (HttpContext context, TestSessionStore store) =>
        {
            await store.End(MatchEndpoints.Account(context), context.RequestAborted);
            return Results.NoContent();
        });
    }
}
