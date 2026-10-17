using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Wotr.Server;

// ASP.NET Identity's PBKDF2 with 600 000 iterations (T08). A missing account is checked against a throwaway hash,
// so a wrong name costs as much time as a wrong password.
internal static class Passwords
{
    private static readonly PasswordHasher<string> Hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 600_000 }));
    private static readonly Lazy<string> Missing = new(() => Hasher.HashPassword("", SessionStore.NewToken()));

    public static string Hash(string account, string password) => Hasher.HashPassword(account, password);

    public static PasswordVerificationResult Verify(string? account, string? hash, string password) =>
        Hasher.VerifyHashedPassword(account ?? "", hash ?? Missing.Value, password);
}
