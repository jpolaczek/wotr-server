using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class HostingTests
{
    [Theory]
    [InlineData(null, "192.0.2.9", "https", "/api/probe", 400)]
    [InlineData("192.0.2.1", "192.0.2.9", "https", "/api/probe", 400)]
    [InlineData("192.0.2.1", "192.0.2.1", "https", "/api/probe", 200)]
    [InlineData("192.0.2.1", "192.0.2.1", "http", "/api/probe", 400)]
    [InlineData(null, "127.0.0.1", "https", "/api/probe", 400)]
    [InlineData(null, "127.0.0.1", null, "/health/ready", 200)]
    [InlineData(null, "192.0.2.9", null, "/health/ready", 400)]
    public async Task OnlyTheExplicitProxyCanForwardHttpsAndClientAddress(string? trusted, string peer, string? scheme, string path, int expected)
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WOTR_TRUSTED_PROXY"] = trusted
        }).Build();
        using var host = await new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(services => services.AddServerTransport(settings))
            .Configure(app =>
            {
                app.Use((context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
                    return next();
                });
                app.UseServerTransport(false);
                app.Run(context => context.Response.WriteAsync(context.Connection.RemoteIpAddress!.ToString()));
            })).StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (scheme != null)
        {
            request.Headers.Add("X-Forwarded-Proto", scheme);
            request.Headers.Add("X-Forwarded-For", "198.51.100.2");
        }
        using var response = await host.GetTestClient().SendAsync(request);
        Assert.Equal(expected, (int)response.StatusCode);
        if (expected == 200)
            Assert.Equal(scheme == "https" ? "198.51.100.2" : peer, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void SecretFileIsReadWithoutCompetingInlineConfiguration()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "Host=database;Username=wotr;Password=test-only\n");
            var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WOTR_DB_FILE"] = file
            }).Build();
            // QUAL-05 adds the pool budget after the secret, so read the password itself.
            Assert.Equal("test-only", new Npgsql.NpgsqlConnectionStringBuilder(DatabaseConfiguration.Read(settings)).Password);
            settings["WOTR_DB"] = "competing-secret";
            var error = Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.Read(settings));
            Assert.DoesNotContain("competing-secret", error.Message);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void InvalidProxyConfigurationFailsAtStartup()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WOTR_TRUSTED_PROXY"] = "0.0.0.0/0"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddServerTransport(settings));
    }
}
