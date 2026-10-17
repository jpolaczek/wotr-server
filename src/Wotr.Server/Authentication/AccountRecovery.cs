using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public enum PasswordResetResult
{
    Done,
    InvalidToken,
    InvalidPassword
}

// AUTH-02 (D16): email confirmation and password reset. A reset link goes only to a confirmed address; an account
// whose address is not confirmed gets the confirmation link again instead. The reply to a reset request never says
// whether the account exists. Limits per account and per day keep the endpoints from becoming a mail cannon.
public sealed class AccountRecovery(Database database, EmailOptions options, EmailOutbox outbox, TimeProvider clock,
    ILogger<AccountRecovery> logger)
{
    public static readonly TimeSpan AccountCooldown = TimeSpan.FromMinutes(2);
    public const int AccountDailyLimit = 10;
    public const int DailyLimit = 90;

    internal sealed record AccountAddress(string Id, string Username, string Email, string NormalizedEmail, bool Verified);

    // Inside the caller's transaction: the confirmation link for this address, or null when a limit stops it.
    internal async Task<EmailMessage?> PrepareConfirmation(NpgsqlConnection connection, NpgsqlTransaction transaction,
        AccountAddress account, CancellationToken cancellation)
    {
        var now = clock.GetUtcNow();
        if (!await WithinLimits(connection, transaction, account.Id, AccountTokenPurpose.VerifyEmail, now, cancellation))
            return null;
        var token = await AccountTokens.Create(connection, transaction, account.Id, AccountTokenPurpose.VerifyEmail,
            account.NormalizedEmail, now, cancellation);
        return AccountMail.Verification(account.Username, account.Email, options.Link("verify", token));
    }

    public async Task<bool> ConfirmEmail(string? token, CancellationToken cancellation)
    {
        if (!AccountTokens.WellFormed(token))
            return false;
        var now = clock.GetUtcNow();
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var link = await AccountTokens.Consume(connection, transaction, token!, AccountTokenPurpose.VerifyEmail, now, cancellation);
        if (link is null)
            return false;
        // The address may have changed since the link was sent; then the link confirms nothing.
        await using var confirm = Database.Command(connection, transaction,
            "UPDATE accounts SET email_verified_at=COALESCE(email_verified_at,$1) WHERE id=$2 AND normalized_email=$3",
            now, link.Value.Account, link.Value.Email);
        if (await confirm.ExecuteNonQueryAsync(cancellation) != 1)
            return false;
        await transaction.CommitAsync(cancellation);
        return true;
    }

    public async Task RequestReset(string? login, CancellationToken cancellation)
    {
        if (login is null || !(AccountInput.ValidUsername(login) || AccountInput.ValidEmail(login)))
            return;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var account = await LockAccount(connection, transaction, login, cancellation);
        if (account is null)
            return;
        var message = account.Verified
            ? await PrepareReset(connection, transaction, account, cancellation)
            : await PrepareConfirmation(connection, transaction, account, cancellation);
        await transaction.CommitAsync(cancellation);
        if (message is not null)
            outbox.Enqueue(message);
    }

    public async Task<PasswordResetResult> ResetPassword(string? token, string? password, CancellationToken cancellation)
    {
        if (!AccountInput.ValidPassword(password))
            return PasswordResetResult.InvalidPassword;
        if (!AccountTokens.WellFormed(token))
            return PasswordResetResult.InvalidToken;
        var now = clock.GetUtcNow();
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var link = await AccountTokens.Consume(connection, transaction, token!, AccountTokenPurpose.ResetPassword, now, cancellation);
        if (link is null)
            return PasswordResetResult.InvalidToken;
        var account = link.Value.Account;
        await using var update = Database.Command(connection, transaction,
            "UPDATE accounts SET password_hash=$1, email_verified_at=COALESCE(email_verified_at,$2) WHERE id=$3 AND normalized_email=$4",
            Passwords.Hash(account, password!), now, account, link.Value.Email);
        if (await update.ExecuteNonQueryAsync(cancellation) != 1)
            return PasswordResetResult.InvalidToken;
        await AccountTokens.Close(connection, transaction, account, AccountTokenPurpose.ResetPassword, now, cancellation);
        // Whoever knew the old password is signed out everywhere.
        await using var revoke = Database.Command(connection, transaction,
            "UPDATE sessions SET revoked=true WHERE account_id=$1 AND NOT revoked", account);
        await revoke.ExecuteNonQueryAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return PasswordResetResult.Done;
    }

    private async Task<EmailMessage?> PrepareReset(NpgsqlConnection connection, NpgsqlTransaction transaction,
        AccountAddress account, CancellationToken cancellation)
    {
        var now = clock.GetUtcNow();
        if (!await WithinLimits(connection, transaction, account.Id, AccountTokenPurpose.ResetPassword, now, cancellation))
            return null;
        await AccountTokens.Close(connection, transaction, account.Id, AccountTokenPurpose.ResetPassword, now, cancellation);
        var token = await AccountTokens.Create(connection, transaction, account.Id, AccountTokenPurpose.ResetPassword,
            account.NormalizedEmail, now, cancellation);
        return AccountMail.PasswordReset(account.Username, account.Email, options.Link("reset", token));
    }

    private async Task<bool> WithinLimits(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        AccountTokenPurpose kind, DateTimeOffset now, CancellationToken cancellation)
    {
        var sent = await AccountTokens.Sent(connection, transaction, account, kind, now, AccountCooldown, cancellation);
        if (sent.Everyone >= DailyLimit)
        {
            logger.LogWarning("The daily limit of account emails is reached; a {Kind} email was not sent.", kind);
            return false;
        }
        if (sent.Recent > 0 || sent.Today >= AccountDailyLimit)
        {
            logger.LogWarning("An account asked for emails too often; a {Kind} email was not sent.", kind);
            return false;
        }
        return true;
    }

    // Serialises requests for one account, so two at once cannot both pass the cooldown.
    private static async Task<AccountAddress?> LockAccount(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string login, CancellationToken cancellation)
    {
        var byEmail = login.Contains('@');
        await using var command = Database.Command(connection, transaction,
            "SELECT id,username,email,normalized_email,email_verified_at IS NOT NULL FROM accounts " +
            $"WHERE {(byEmail ? "normalized_email" : "normalized_name")}=$1 AND email IS NOT NULL AND NOT technical FOR UPDATE",
            byEmail ? AccountInput.NormalizeEmail(login) : login.ToUpperInvariant());
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation)
            ? new AccountAddress(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4))
            : null;
    }
}
