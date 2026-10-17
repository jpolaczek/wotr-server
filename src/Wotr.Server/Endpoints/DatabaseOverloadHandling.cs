using System.Diagnostics.Metrics;
using Npgsql;

namespace Wotr.Server;

// QUAL-05: a transient database failure (a full pool after its wait, too many connections, a restart or a lost
// connection) answers 503 ServerBusy with Retry-After. For a command the result is then unknown, never a sure
// rejection: the client keeps it pending and recovers its receipt with the same commandId. Logs carry only the
// SQL state, never the connection string, session or payload.
public static class DatabaseOverloadHandling
{
    public const string ServerBusy = "ServerBusy";
    private static readonly Meter Meter = new("Wotr.Server.Database");
    private static readonly Counter<long> Rejected = Meter.CreateCounter<long>("database.overload.rejected");

    public static bool IsTransient(Exception error) => error is NpgsqlException { IsTransient: true } ||
        error is NpgsqlException { InnerException: TimeoutException };

    public static void UseDatabaseOverloadHandling(this WebApplication app) => app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception error) when (IsTransient(error) && !context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            Rejected.Add(1);
            app.Logger.LogWarning("Database busy, request answered 503 (SQL state {State})",
                (error as PostgresException)?.SqlState ?? "none");
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "2";
            await context.Response.WriteAsJsonAsync(new { error = ServerBusy }, context.RequestAborted);
        }
    });

    public static void Count() => Rejected.Add(1);
}
