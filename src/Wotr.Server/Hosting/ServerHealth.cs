using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public static class ServerHealth
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", protocolVersion = Protocol.Version }));
        app.MapGet("/health/ready", Ready);
    }

    private static async Task<IResult> Ready(Database database, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await using var connection = await database.Source.OpenConnectionAsync(timeout.Token);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(timeout.Token);
            return Results.Ok(new { status = "ready", protocolVersion = Protocol.Version });
        }
        catch (Exception error) when (error is NpgsqlException or OperationCanceledException or TimeoutException)
        {
            return Results.Json(new { status = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    public static async Task<int> CheckLocal()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            using var response = await client.GetAsync("http://127.0.0.1:8080/health/ready");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
        {
            return 1;
        }
    }
}
