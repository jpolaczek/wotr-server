using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Server;

// EXP-02: a table's requests from a client that does not play one of the table's modules are refused before the
// endpoint reads or changes anything. A client that plays every module passes without a query; only an older one
// reads the table's configuration. The answer is the gate's own 409 ClientUpdateRequired (DS11).
public static class ModuleCompatibility
{
    private const string RoomById = "SELECT (state->'configuration')::text FROM rooms WHERE id=$1";
    private const string RoomByCode = "SELECT (state->'configuration')::text FROM rooms WHERE join_code=$1";
    private const string MatchById = "SELECT (state->'configuration')::text FROM matches WHERE id=$1";

    // A room named by the route's id, or by the invitation code of a JoinRoomRequest.
    public static TBuilder ForRoom<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter((context, next) => Check(context, next, OfRoom));

    // A match (played, observed or replayed) named by the route's id.
    public static TBuilder ForMatch<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter((context, next) => Check(context, next, OfMatch));

    // The configuration a host asks for in a ConfigureRoomRequest.
    public static TBuilder ForRequestedConfiguration<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter((context, next) => Check(context, next, OfRequest));

    private static async ValueTask<object?> Check(EndpointFilterInvocationContext context, EndpointFilterDelegate next,
        Func<EndpointFilterInvocationContext, Task<MatchConfiguration?>> configurationOf)
    {
        var http = context.HttpContext;
        var policy = http.RequestServices.GetRequiredService<ModuleClientPolicy>();
        if (!policy.Restricts)
            return await next(context);
        var unplayable = policy.Unplayable(http.Request.Headers[ClientCompatibility.ClientVersionHeader].ToString());
        if (unplayable.Count == 0)
            return await next(context);
        var configuration = await configurationOf(context);
        return configuration is null || ModuleClientPolicy.Plays(unplayable, configuration)
            ? await next(context)
            : Refusal(http);
    }

    private static Task<MatchConfiguration?> OfRoom(EndpointFilterInvocationContext context) =>
        context.Arguments.OfType<JoinRoomRequest>().FirstOrDefault() is { } join
            ? Read(context.HttpContext, RoomByCode, join.Code)
            : Read(context.HttpContext, RoomById, RouteId(context.HttpContext));

    private static Task<MatchConfiguration?> OfMatch(EndpointFilterInvocationContext context) =>
        Read(context.HttpContext, MatchById, RouteId(context.HttpContext));

    private static Task<MatchConfiguration?> OfRequest(EndpointFilterInvocationContext context) =>
        Task.FromResult(context.Arguments.OfType<ConfigureRoomRequest>().FirstOrDefault()?.Configuration);

    private static string RouteId(HttpContext http) => http.Request.RouteValues["id"] as string ?? "";

    // A table that does not exist passes: the endpoint answers for it as it always does.
    private static async Task<MatchConfiguration?> Read(HttpContext http, string sql, string? key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        var database = http.RequestServices.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync(http.RequestAborted);
        await using var command = Database.Command(connection, null, sql, key);
        return await command.ExecuteScalarAsync(http.RequestAborted) is string json
            ? JsonSerializer.Deserialize<MatchConfiguration>(json, BoardCatalog.Json)
            : null;
    }

    private static IResult Refusal(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        return Results.Json(new ApiError { Error = ClientCompatibility.ClientUpdateRequired },
            statusCode: StatusCodes.Status409Conflict);
    }
}
