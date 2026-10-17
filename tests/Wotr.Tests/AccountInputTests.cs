using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class AccountInputTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("ab", false)]
    [InlineData("Ab9", true)]
    [InlineData("Name_1-2.3", true)]
    [InlineData("name surname", false)]
    [InlineData("Żaba", false)]
    [InlineData("ｋｕｂａ", false)]
    public void UsernameUsesTheDocumentedAsciiAlphabet(string? value, bool valid)
        => Assert.Equal(valid, AccountInput.ValidUsername(value));

    [Theory]
    [InlineData(3, true)]
    [InlineData(32, true)]
    [InlineData(33, false)]
    public void UsernameLengthBoundary(int length, bool valid)
        => Assert.Equal(valid, AccountInput.ValidUsername(new string('a', length)));

    [Theory]
    [InlineData(0, false)]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void PasswordLengthBoundary(int length, bool valid)
        => Assert.Equal(valid, AccountInput.ValidPassword(new string('p', length)));

    [Fact]
    public void PasswordIsNotTrimmedOrRestrictedToAscii()
    {
        Assert.False(AccountInput.ValidPassword(null));
        Assert.True(AccountInput.ValidPassword("  Hasło gracza  "));
    }

    [Theory]
    [InlineData("player@example.org", true)]
    [InlineData("  Player.Name+wotr@Sub.Example.org  ", true)]
    [InlineData("player@localhost", false)]
    [InlineData("player@@example.org", false)]
    [InlineData("pla yer@example.org", false)]
    [InlineData("@example.org", false)]
    [InlineData("player@.example.org", false)]
    [InlineData("player@example..org", false)]
    [InlineData("player@example.org.", false)]
    [InlineData("Player <player@example.org>", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EmailNeedsOneAtADottedDomainAndNoSpaces(string? email, bool valid) =>
        Assert.Equal(valid, AccountInput.ValidEmail(email));

    [Fact]
    public void EmailIsComparedTrimmedAndInLowerCaseAndHasLengthLimits()
    {
        Assert.Equal("player.name@example.org", AccountInput.NormalizeEmail("  Player.Name@Example.ORG "));
        Assert.True(AccountInput.ValidEmail(new string('a', 64) + "@example.org"));
        Assert.False(AccountInput.ValidEmail(new string('a', 65) + "@example.org"));
        Assert.False(AccountInput.ValidEmail("a@" + new string('b', 250) + ".org"));
    }

    [PostgresFact]
    public async Task InvalidRegistrationIsRefusedAndValidAccountCanSignInByNameOrEmail()
    {
        using var host = new AccountHost();
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var name = "auth_" + Guid.NewGuid().ToString("N")[..20];
        var email = TestAccounts.Email(name);
        const string password = "  Registration regression  ";
        var invalid = new[]
        {
            new RegisterRequest { Username = name, Email = email, Password = "elevenchars" },
            new RegisterRequest { Username = "bad name", Email = email, Password = password },
            new RegisterRequest { Username = name, Email = "not an address", Password = password },
            new RegisterRequest { Username = name, Password = password }
        };
        foreach (var request in invalid)
        {
            using var response = await client.PostAsJsonAsync("/api/auth/register", request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("InvalidRegistration", (await response.Content.ReadFromJsonAsync<ApiError>())!.Error);
        }

        using var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = name,
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var registered = await registration.Content.ReadFromJsonAsync<SessionView>();
        using var byEmail = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = " " + email.ToUpperInvariant() + " ",
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, byEmail.StatusCode);
        Assert.Equal(registered!.AccountId, (await byEmail.Content.ReadFromJsonAsync<SessionView>())!.AccountId);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = name.ToUpperInvariant(),
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(registered.AccountId, (await login.Content.ReadFromJsonAsync<SessionView>())!.AccountId);
        using var alteredPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = name,
            Password = password.Trim()
        });
        Assert.Equal(HttpStatusCode.Unauthorized, alteredPassword.StatusCode);
    }
}
