using Wotr.Contracts;

namespace Wotr.Server;

// AUTH-02: the account pages of the game's website (infra/site/account) call these without a session or client
// version headers. Only the reset request sends an email, so it has the email budget; the others use the login one.
public static class AccountEndpoints
{
    public sealed record TokenRequest(string? Token);
    public sealed record ResetRequest(string? Login);
    public sealed record NewPasswordRequest(string? Token, string? Password);

    public static void Map(WebApplication app)
    {
        var account = app.MapGroup("/api/account");
        account.MapPost("/verify-email", async (TokenRequest request, AccountRecovery recovery, CancellationToken cancellation) =>
            await recovery.ConfirmEmail(request.Token, cancellation) ? Results.NoContent() : Invalid("InvalidToken"))
            .RequireRateLimiting(AuthenticationRateLimit.Login);
        // Always 202: the answer does not tell whether the account exists or has a confirmed address.
        account.MapPost("/password-reset", async (ResetRequest request, AccountRecovery recovery, CancellationToken cancellation) =>
        {
            await recovery.RequestReset(request.Login?.Trim(), cancellation);
            return Results.Accepted();
        }).RequireRateLimiting(AuthenticationRateLimit.Email);
        account.MapPost("/password-reset/confirm", async (NewPasswordRequest request, AccountRecovery recovery, CancellationToken cancellation) =>
            await recovery.ResetPassword(request.Token, request.Password, cancellation) switch
            {
                PasswordResetResult.Done => Results.NoContent(),
                PasswordResetResult.InvalidPassword => Invalid("InvalidPassword"),
                _ => Invalid("InvalidToken")
            }).RequireRateLimiting(AuthenticationRateLimit.Login);
        // AUTH-06: a signed-in player changes their account name: { username: the new name, password: the current one }.
        account.MapPost("/name", async (LoginRequest request, HttpContext context, AccountRename rename, CancellationToken cancellation) =>
            RenameResult(await rename.RenameSelf(MatchEndpoints.Account(context), request.Username, request.Password, cancellation),
                MatchEndpoints.Account(context)))
            .RequireAuthorization().RequireRateLimiting(AuthenticationRateLimit.Login);
    }

    // A wrong password is a 400, never a 401: the session itself is fine and the client must not sign out.
    private static IResult RenameResult(RenameResult result, string account) => result.Error switch
    {
        RenameError.None => Results.Ok(new SessionView { AccountId = account, Username = result.Username }),
        RenameError.NameTaken => Results.Conflict(new ApiError { Error = "NameTaken" }),
        RenameError.WrongPassword => Invalid("WrongPassword"),
        RenameError.Locked => AuthenticationRateLimit.Locked(result.LockedFor ?? TimeSpan.Zero),
        _ => Invalid("InvalidName")
    };

    private static IResult Invalid(string code) => Results.BadRequest(new ApiError { Error = code });
}
