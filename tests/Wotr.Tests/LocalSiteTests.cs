using System.Net;
using Microsoft.AspNetCore.Hosting;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// WEB-02: a local server serves the website only in Development, and only when told where it is.
public sealed class LocalSiteTests
{
    private static readonly string Site = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../infra/site"));

    private static IReadOnlyDictionary<string, string?> Settings(string? root) =>
        new Dictionary<string, string?> { [LocalSite.RootKey] = root };

    private static HttpClient Client(AccountHost host, string environment) =>
        host.WithWebHostBuilder(builder => builder.UseEnvironment(environment))
            .CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    [PostgresFact]
    public async Task DevelopmentServesThePagesAsNginxDoesWithItsHeaders()
    {
        using var host = new AccountHost(extra: Settings(Site));
        using var client = Client(host, "Development");
        using var index = await client.GetAsync("/account/");
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        Assert.Contains("default-src 'self'", index.Headers.GetValues("Content-Security-Policy").Single());
        using var bare = await client.GetAsync("/account");
        Assert.Equal(HttpStatusCode.OK, bare.StatusCode);
        using var reset = await client.GetAsync("/account/reset");
        Assert.Contains("Choose a new password", await reset.Content.ReadAsStringAsync());
        using var picture = await client.GetAsync("/assets/avatars/eye.png");
        Assert.Equal("image/png", picture.Content.Headers.ContentType?.MediaType);
        using var api = await client.GetAsync("/api/account");
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
    }

    [PostgresFact]
    public async Task NoOtherEnvironmentServesIt()
    {
        using var host = new AccountHost(extra: Settings(Site));
        using var client = Client(host, "Production");
        using var page = await client.GetAsync("/account/reset");
        Assert.Equal(HttpStatusCode.NotFound, page.StatusCode);
    }

    [PostgresFact]
    public async Task WithoutARootDevelopmentServesNothing()
    {
        using var host = new AccountHost(extra: Settings(null));
        using var client = Client(host, "Development");
        using var page = await client.GetAsync("/account/reset");
        Assert.Equal(HttpStatusCode.NotFound, page.StatusCode);
    }
}
