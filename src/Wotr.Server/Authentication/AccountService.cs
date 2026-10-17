using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public enum RegistrationError
{
    None,
    InvalidRegistration,
    UsernameTaken,
    EmailTaken
}

public sealed record RegistrationResult(SessionView? Session, RegistrationError Error);

public enum SetEmailResult
{
    Done,
    NoAccount,
    InvalidEmail,
    EmailTaken
}

// T08 accounts with AUTH-02 (D16) addresses: registration needs an email address, which gets a confirmation link;
// login takes the name or the address. Account invitations are gone (the invitations table stays unused).
public sealed class AccountService(Database database, AccountRecovery recovery, EmailOutbox outbox)
{
    private const string EmailIndex = "accounts_normalized_email";

    // For tools and fixtures that only create accounts (the client test seeder): no email ever leaves.
    public static AccountService WithoutEmail(Database database)
    {
        var options = new EmailOptions(EmailProvider.None, "", "", new Uri("https://localhost/"));
        var outbox = new EmailOutbox(new DisabledEmailSender(NullLogger<DisabledEmailSender>.Instance), NullLogger<EmailOutbox>.Instance);
        var recovery = new AccountRecovery(database, options, outbox, TimeProvider.System, NullLogger<AccountRecovery>.Instance);
        return new AccountService(database, recovery, outbox);
    }

    public async Task<bool> IsAdmin(string account, CancellationToken cancellation)
    {
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var command = Database.Command(connection, null, "SELECT is_admin FROM accounts WHERE id=$1", account);
        return await command.ExecuteScalarAsync(cancellation) is true;
    }

    public async Task<RegistrationResult> Register(RegisterRequest request, CancellationToken cancellation = default)
    {
        if (!AccountInput.ValidUsername(request.Username) || !AccountInput.ValidEmail(request.Email) ||
            !AccountInput.ValidPassword(request.Password))
            return new(null, RegistrationError.InvalidRegistration);
        var address = new AccountRecovery.AccountAddress(Guid.NewGuid().ToString("N"), request.Username, request.Email.Trim(),
            AccountInput.NormalizeEmail(request.Email), false);
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        try
        {
            await InsertAccount(connection, transaction, address, Passwords.Hash(address.Id, request.Password), cancellation);
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return new(null, error.ConstraintName == EmailIndex ? RegistrationError.EmailTaken : RegistrationError.UsernameTaken);
        }
        var message = await recovery.PrepareConfirmation(connection, transaction, address, cancellation);
        var session = await SessionStore.Issue(connection, transaction, address.Id, address.Username, cancellation);
        await transaction.CommitAsync(cancellation);
        if (message is not null)
            outbox.Enqueue(message);
        return new(session, RegistrationError.None);
    }

    public async Task<SessionView?> Login(LoginRequest request, CancellationToken cancellation = default)
    {
        var login = request.Username?.Trim() ?? "";
        if (!(AccountInput.ValidUsername(login) || AccountInput.ValidEmail(login)) || !AccountInput.ValidPassword(request.Password))
            return null;
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        var account = await FindAccount(connection, login, cancellation);
        var result = Passwords.Verify(account?.Id, account?.Hash, request.Password);
        if (account is null || result == PasswordVerificationResult.Failed)
            return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await using var update = Database.Command(connection, null, "UPDATE accounts SET password_hash=$1 WHERE id=$2",
                Passwords.Hash(account.Id, request.Password), account.Id);
            await update.ExecuteNonQueryAsync(cancellation);
        }
        return await SessionStore.Issue(connection, null, account.Id, account.Username, cancellation);
    }

    // Operator command (--set-email) for accounts from before AUTH-02: a new, unconfirmed address and its link,
    // which the caller sends. Open links to the previous address stop working.
    public async Task<(SetEmailResult Result, EmailMessage? Message)> SetEmail(string username, string email,
        CancellationToken cancellation = default)
    {
        if (!AccountInput.ValidEmail(email))
            return (SetEmailResult.InvalidEmail, null);
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using var find = Database.Command(connection, transaction,
            "SELECT id,username FROM accounts WHERE normalized_name=$1 AND NOT technical FOR UPDATE", username.ToUpperInvariant());
        AccountRecovery.AccountAddress? address = null;
        await using (var reader = await find.ExecuteReaderAsync(cancellation))
        {
            if (await reader.ReadAsync(cancellation))
                address = new(reader.GetString(0), reader.GetString(1), email.Trim(), AccountInput.NormalizeEmail(email), false);
        }
        if (address is null)
            return (SetEmailResult.NoAccount, null);
        try
        {
            await using var update = Database.Command(connection, transaction,
                "UPDATE accounts SET email=$1, normalized_email=$2, email_verified_at=NULL WHERE id=$3",
                address.Email, address.NormalizedEmail, address.Id);
            await update.ExecuteNonQueryAsync(cancellation);
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return (SetEmailResult.EmailTaken, null);
        }
        await using var close = Database.Command(connection, transaction,
            "UPDATE account_tokens SET consumed_at=now() WHERE account_id=$1 AND consumed_at IS NULL", address.Id);
        await close.ExecuteNonQueryAsync(cancellation);
        var message = await recovery.PrepareConfirmation(connection, transaction, address, cancellation);
        await transaction.CommitAsync(cancellation);
        return (SetEmailResult.Done, message);
    }

    private static async Task InsertAccount(NpgsqlConnection connection, NpgsqlTransaction transaction,
        AccountRecovery.AccountAddress account, string hash, CancellationToken cancellation)
    {
        await using var command = Database.Command(connection, transaction,
            "INSERT INTO accounts(id,username,normalized_name,password_hash,is_admin,email,normalized_email) " +
            "VALUES ($1,$2,$3,$4,false,$5,$6)",
            account.Id, account.Username, account.Username.ToUpperInvariant(), hash, account.Email, account.NormalizedEmail);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    private sealed record Account(string Id, string Username, string Hash);

    // A login with @ is an email address; names cannot contain it (AccountInput.ValidUsername).
    private static async Task<Account?> FindAccount(NpgsqlConnection connection, string login, CancellationToken cancellation)
    {
        var byEmail = login.Contains('@');
        await using var command = Database.Command(connection, null,
            $"SELECT id,username,password_hash FROM accounts WHERE {(byEmail ? "normalized_email" : "normalized_name")}=$1 AND NOT technical",
            byEmail ? AccountInput.NormalizeEmail(login) : login.ToUpperInvariant());
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? new(reader.GetString(0), reader.GetString(1), reader.GetString(2)) : null;
    }
}
