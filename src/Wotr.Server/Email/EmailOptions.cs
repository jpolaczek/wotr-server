namespace Wotr.Server;

public enum EmailProvider
{
    // Nothing is sent; the log notes the skipped message without its address or link.
    None,
    // Development only: the message, link included, goes to the log for local trials.
    Log,
    // Resend's REST API with the key from a secret file.
    Resend
}

// AUTH-02 (D16): who sends account messages and where their links point. Production sets Wotr:PublicUrl to the
// game's https address and, once the sending domain is verified in Resend, the provider, sender and key file.
public sealed record EmailOptions(EmailProvider Provider, string From, string ApiKey, Uri PublicUrl)
{
    public const string ProviderKey = "Wotr:Email:Provider";
    public const string FromKey = "Wotr:Email:From";
    public const string KeyFileKey = "Wotr:Email:ResendApiKeyFile";
    public const string PublicUrlKey = "Wotr:PublicUrl";
    // Requests that send an email from one address in AuthenticationRateLimit.EmailWindow (default 5).
    public const string PermitsPerAddressKey = "Wotr:Email:PermitsPerAddress";

    public static EmailOptions Read(IConfiguration configuration, IHostEnvironment environment)
    {
        var name = configuration[ProviderKey];
        var provider = string.IsNullOrWhiteSpace(name) ? EmailProvider.None
            : Enum.TryParse<EmailProvider>(name, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed
            : throw new InvalidOperationException($"{ProviderKey} must be none, log or resend.");
        if (provider == EmailProvider.Log && !(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
            throw new InvalidOperationException("The log email provider writes links to the log and runs only in Development.");
        var publicUrl = ParsePublicUrl(configuration[PublicUrlKey], provider);
        var from = configuration[FromKey]?.Trim() ?? "";
        var key = provider == EmailProvider.Resend ? ReadKey(configuration[KeyFileKey]) : "";
        if (provider == EmailProvider.Resend && (from.Length == 0 || !from.Contains('@')))
            throw new InvalidOperationException($"{FromKey} names the sender, e.g. War of the Ring Online <no-reply@example.org>.");
        return new EmailOptions(provider, from, key, publicUrl);
    }

    public Uri Link(string page, string token) => new(PublicUrl, $"account/{page}#token={token}");

    private static Uri ParsePublicUrl(string? value, EmailProvider provider)
    {
        if (string.IsNullOrWhiteSpace(value))
            return provider == EmailProvider.None ? new Uri("https://localhost/")
                : throw new InvalidOperationException($"{PublicUrlKey} is the game's address for links in account emails.");
        if (!Uri.TryCreate(value.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) ||
            uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new InvalidOperationException($"{PublicUrlKey} must be an https address without query or fragment.");
        return uri;
    }

    private static string ReadKey(string? file)
    {
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            throw new InvalidOperationException($"{KeyFileKey} must name the file with the Resend API key.");
        var key = File.ReadAllText(file).Trim();
        if (key.Length == 0 || key.Any(char.IsWhiteSpace))
            throw new InvalidOperationException("The Resend API key file is empty or malformed.");
        return key;
    }
}
