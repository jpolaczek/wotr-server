using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Wotr.Server;

public interface ISessionResolver
{
    Task<string?> Resolve(string token, CancellationToken cancellation);
}

public sealed class BearerAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    ISessionResolver sessions, BotCredentialStore bots)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var auth = Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ", StringComparison.Ordinal) || auth.Length > 512)
            return AuthenticateResult.NoResult();
        if (BotTokens.IsBot(auth[7..]))
        {
            var identity = await bots.Resolve(auth[7..], Context.RequestAborted);
            if (identity is null)
                return AuthenticateResult.Fail("Invalid bot credential");
            Context.Items[typeof(BotIdentity)] = identity;
            return AuthenticateResult.Success(new(identity.Principal(Scheme.Name), Scheme.Name));
        }
        var account = await sessions.Resolve(auth[7..], Context.RequestAborted);
        return account is null ? AuthenticateResult.Fail("Invalid session") : AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account)], Scheme.Name)), Scheme.Name));
    }
}
