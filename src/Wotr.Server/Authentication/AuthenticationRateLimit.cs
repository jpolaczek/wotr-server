using System.Globalization;
using System.Threading.RateLimiting;

namespace Wotr.Server;

internal static class AuthenticationRateLimit
{
    public const string Login = "auth";
    // AUTH-02: requests that send an email (registration, password reset) — 5 per address in 10 minutes.
    public const string Email = "email";
    public const int EmailPermits = 5;
    public static readonly TimeSpan EmailWindow = TimeSpan.FromMinutes(10);

    public static void AddAuthenticationRateLimit(this IServiceCollection services) => services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellation) =>
        {
            // A window gives its own retry time; a full concurrency budget frees within a moment (QUAL-02).
            var delay = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)) : 1;
            context.HttpContext.Response.Headers.RetryAfter = delay.ToString(CultureInfo.InvariantCulture);
            await context.HttpContext.Response.WriteAsJsonAsync(new { error = "TooManyRequests" }, cancellation);
        };
        options.AddPolicy(Login, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        options.AddPolicy(Email, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue(EmailOptions.PermitsPerAddressKey, EmailPermits),
                Window = EmailWindow,
                QueueLimit = 0
            }));
    });
}
