using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Server;

public enum BotOperation { Match, Room, RenewSeat, Runtime, Control }
public sealed record BotRoute(BotOperation Operation);

public static class BotAccessGate
{
    public static TBuilder AllowBot<TBuilder>(this TBuilder builder, BotOperation operation) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpoint => endpoint.Metadata.Add(new BotRoute(operation)));
        return builder;
    }

    public static IApplicationBuilder UseBotAccess(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var identity = BotIdentity.Of(context);
        if (identity is null)
        {
            await next();
            return;
        }
        var operation = context.GetEndpoint()?.Metadata.GetMetadata<BotRoute>()?.Operation;
        var id = context.Request.RouteValues["id"] as string;
        if (!Allows(identity, operation, id) || context.Request.Query.ContainsKey("perspective") || context.Request.Query.ContainsKey("side"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        if (operation is BotOperation.Match or BotOperation.Room && !await Supported(context, operation.Value, id!))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(new { error = "BotConfigurationUnsupported" }, context.RequestAborted);
            return;
        }
        try
        {
            await next();
        }
        catch (BotAuthorityExpiredException) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(new { error = "BotAuthorityExpired" }, context.RequestAborted);
        }
    });

    private static bool Allows(BotIdentity identity, BotOperation? operation, string? id) => operation switch
    {
        BotOperation.Runtime => true,
        BotOperation.Control => identity.IsControl,
        BotOperation.RenewSeat => identity.Seat is not null,
        BotOperation.Match => id is not null && identity.Seat?.MatchId == id,
        BotOperation.Room => id is not null && identity.Seat?.RoomId == id,
        _ => false
    };

    private static async Task<bool> Supported(HttpContext context, BotOperation operation, string id)
    {
        var database = context.RequestServices.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync(context.RequestAborted);
        var sql = operation == BotOperation.Match
            ? "SELECT (state->'configuration')::text FROM matches WHERE id=$1"
            : "SELECT (state->'configuration')::text FROM rooms WHERE id=$1";
        await using var query = Database.Command(connection, null, sql, id);
        var configuration = await query.ExecuteScalarAsync(context.RequestAborted) as string;
        return configuration is not null && context.RequestServices.GetRequiredService<BotAccessOptions>()
            .Supports(JsonSerializer.Deserialize<MatchConfiguration>(configuration, BoardCatalog.Json)!);
    }
}
