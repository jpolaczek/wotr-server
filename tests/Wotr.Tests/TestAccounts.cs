using Wotr.Contracts;
using Wotr.Server;

namespace Wotr.Tests;

// AUTH-02: test accounts register with an address on the reserved .test domain, derived from the unique name.
internal static class TestAccounts
{
    public static string Email(string username) => username.ToLowerInvariant() + "@example.test";

    public static async Task<SessionView> Register(AccountService accounts, string username, string password)
    {
        var result = await accounts.Register(new RegisterRequest { Username = username, Email = Email(username), Password = password });
        return result.Session ?? throw new InvalidOperationException($"Test account registration failed: {result.Error}.");
    }
}
