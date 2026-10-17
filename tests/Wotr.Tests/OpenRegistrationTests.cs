using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class OpenRegistrationTests
{
    [PostgresFact]
    public async Task RegistrationWithEmailIssuesOrdinarySessionAndLogoutRevokesIt()
    {
        using var host = new AccountHost();
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var name = Name();
        var request = new RegisterRequest { Username = name, Email = TestAccounts.Email(name), Password = "  open registration password  " };
        using var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<SessionView>())!;
        var sessions = host.Services.GetRequiredService<SessionStore>();
        Assert.Equal(session.AccountId, await sessions.Resolve(session.Token));
        Assert.False(await host.Services.GetRequiredService<AccountService>().IsAdmin(session.AccountId, default));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rooms")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/invitations", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Null(await sessions.Resolve(session.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/rooms")).StatusCode);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = request.Username.ToUpperInvariant(), Password = request.Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(session.AccountId, (await login.Content.ReadFromJsonAsync<SessionView>())!.AccountId);
    }

    [PostgresFact]
    public async Task DuplicateNormalizedNamesAreAtomicAndRegistrationNeedsAnAddress()
    {
        using var host = new AccountHost();
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var name = Name();
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Username = name, Email = TestAccounts.Email(name), Password = "a sufficiently long password" }),
            client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Username = name.ToUpperInvariant(), Email = TestAccounts.Email(Name()), Password = "a sufficiently long password" }));
        Assert.Single(attempts, r => r.StatusCode == HttpStatusCode.OK);
        var refused = Assert.Single(attempts, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("UsernameTaken", (await refused.Content.ReadFromJsonAsync<ApiError>())!.Error);
        foreach (var response in attempts)
            response.Dispose();
        var accounts = host.Services.GetRequiredService<AccountService>();
        var withoutAddress = Name();
        Assert.Equal(RegistrationError.InvalidRegistration,
            (await accounts.Register(new() { Username = withoutAddress, Password = "a sufficiently long password" })).Error);
        Assert.Null(await accounts.Login(new() { Username = withoutAddress, Password = "a sufficiently long password" }));
    }

    private static string Name() => "menu_" + Guid.NewGuid().ToString("N")[..20];
}
