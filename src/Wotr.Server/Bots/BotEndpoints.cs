using Wotr.Contracts;

namespace Wotr.Server;

public static class BotEndpoints
{
    public static void Map(WebApplication app)
    {
        // Shared nonsecret runtime metadata, identical for human and bot principals.
        app.MapGet("/api/runtime", () => Results.Ok(new
        {
            protocolVersion = Protocol.Version, rulesVersion = Protocol.RulesVersion, contentVersion = Protocol.ContentVersion,
            botProtocolVersion = BotCompatibility.Version, implementedFeatures = BotAccessOptions.KnownFeatures
        })).RequireAuthorization().RequireRateLimiting(GameRateLimits.Reads).AllowBot(BotOperation.Runtime);

        app.MapPost("/api/bot/seat/renew", async (HttpContext context, BotCredentialStore credentials) =>
        {
            if (BotIdentity.Of(context)?.Seat is not { } authority)
                return Results.Forbid();
            var expires = await credentials.Renew(authority, context.RequestAborted);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { expiresAt = expires });
        }).RequireAuthorization().RequireRateLimiting(GameRateLimits.Commands).AllowBot(BotOperation.RenewSeat);
    }
}
