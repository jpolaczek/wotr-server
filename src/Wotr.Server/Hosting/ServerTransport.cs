using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Wotr.Server;

public static class ServerTransport
{
    public static IServiceCollection AddServerTransport(this IServiceCollection services, IConfiguration configuration)
    {
        var proxy = configuration["WOTR_TRUSTED_PROXY"];
        IPAddress? address = null;
        if (!string.IsNullOrWhiteSpace(proxy) && !IPAddress.TryParse(proxy, out address))
            throw new InvalidOperationException("WOTR_TRUSTED_PROXY must be one explicit IP address.");
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
            if (address == null)
                return;
            options.KnownProxies.Add(address);
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        return services;
    }

    public static IApplicationBuilder UseServerTransport(this IApplicationBuilder app, bool development)
    {
        app.UseForwardedHeaders();
        return app.Use(async (context, next) =>
        {
            if (!development && !context.Request.IsHttps && !IsLocalProbe(context))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "HttpsRequired" });
                return;
            }
            await next();
        });
    }

    private static bool IsLocalProbe(HttpContext context) =>
        HttpMethods.IsGet(context.Request.Method)
        && context.Request.Path is { Value: "/health" or "/health/ready" }
        && context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
}
