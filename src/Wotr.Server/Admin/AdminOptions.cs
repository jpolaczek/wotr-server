using Wotr.Contracts;

namespace Wotr.Server;

// ADMIN-01: the administrators are named by the host's configuration, Wotr__Admin__Accounts (WOTR_ADMIN_ACCOUNTS in
// infra/.env): account names separated by commas. Without names the panel answers nobody. A malformed name stops the
// server at start, like the other options.
public sealed class AdminOptions
{
    public const string ConfigurationKey = "Wotr:Admin:Accounts";
    private static readonly char[] Separators = [',', ';', ' '];
    private readonly HashSet<string> names;

    public AdminOptions(IEnumerable<string> accounts) =>
        names = accounts.Select(Normalize).ToHashSet(StringComparer.Ordinal);

    public bool Enabled => names.Count > 0;

    // AUTH-05: an account name in any case.
    public bool Allows(string username) => names.Contains(Normalize(username));

    public static AdminOptions Read(IConfiguration configuration)
    {
        var accounts = (configuration[ConfigurationKey] ?? "")
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (accounts.Any(account => !AccountInput.ValidUsername(account)))
            throw new InvalidOperationException(ConfigurationKey + " must list account names separated by commas.");
        return new AdminOptions(accounts);
    }

    private static string Normalize(string username) => username.Trim().ToUpperInvariant();
}
