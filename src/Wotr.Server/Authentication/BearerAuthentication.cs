using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Wotr.Server;

public interface ISessionResolver
{
    Task<string?> Resolve(string token, CancellationToken cancellation);
}

public sealed class BearerAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISessionResolver sessions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var auth = Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ", StringComparison.Ordinal) || auth.Length > 512)
            return AuthenticateResult.NoResult();
        var account = await sessions.Resolve(auth[7..], Context.RequestAborted);
        return account is null ? AuthenticateResult.Fail("Invalid session") : AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account)], Scheme.Name)), Scheme.Name));
    }
}
