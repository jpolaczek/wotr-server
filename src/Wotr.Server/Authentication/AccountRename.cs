using Microsoft.AspNetCore.Identity;
using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

public enum RenameError
{
    None,
    InvalidName,
    NameTaken,
    WrongPassword,
    NoAccount,
    // SEC-02: the account's name is locked after failed sign-ins; LockedFor is the time left.
    Locked
}

public sealed record RenameResult(RenameError Error, string Username, TimeSpan? LockedFor = null);

// AUTH-06: a player changes their account name; the operator renames any account (--rename). The new name follows the
// registration rules (AccountInput: 3–32 letters a–z, digits or . _ -, never an email address), is unique in any case
// (AUTH-05), and a player must give their current password. A name on the administrators' list (ADMIN-01) is never
// handed to another account, and the answer does not tell it apart from a taken one. The rename runs in one
// transaction with the copies of the name in open rooms (host, seats, audience); game lists, replays and the admin
// panel read the name from the account, and chat lines keep the name they were written under. Sessions stay valid.
// SEC-02: the password check shares the sign-in lock of the account's current name (SignInGuard).
public sealed class AccountRename(Database database, AdminOptions admins, SignInGuard guard, ILogger<AccountRename> log)
{
    // A player's own rename, confirmed with the current password.
    public Task<RenameResult> RenameSelf(string account, string? newName, string? password,
        CancellationToken cancellation = default) =>
        Rename(new AccountKey(account, null), newName, password ?? "", true, cancellation);

    // The operator's rename by the current name, without a password.
    public Task<RenameResult> RenameByOperator(string currentName, string? newName, CancellationToken cancellation = default) =>
        Rename(new AccountKey(null, currentName), newName, "", false, cancellation);

    private async Task<RenameResult> Rename(AccountKey key, string? requested, string password, bool checkPassword,
        CancellationToken cancellation)
    {
        var name = requested?.Trim() ?? "";
        if (!AccountInput.ValidUsername(name) || AccountInput.ValidEmail(name))
            return new(RenameError.InvalidName, "");
        await using var connection = await database.Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        var current = await Lock(connection, transaction, key, cancellation);
        if (checkPassword && CheckPassword(current, password) is { } refused)
            return refused;
        if (current is null)
            return new(RenameError.NoAccount, "");
        var sameName = Normalize(name) == Normalize(current.Username);
        if (!sameName && admins.Allows(name))
            return new(RenameError.NameTaken, "");
        if (!await UpdateAccount(connection, transaction, current.Id, name, cancellation))
            return new(RenameError.NameTaken, "");
        await UpdateRooms(connection, transaction, current.Id, name, cancellation);
        await transaction.CommitAsync(cancellation);
        log.LogInformation("Account renamed from {Previous} to {Name}.", current.Username, name);
        return new(RenameError.None, name);
    }

    private sealed record AccountKey(string? Id, string? Name);

    private sealed record LockedAccount(string Id, string Username, string Hash);

    private static async Task<LockedAccount?> Lock(NpgsqlConnection connection, NpgsqlTransaction transaction, AccountKey key,
        CancellationToken cancellation)
    {
        await using var command = key.Id is not null
            ? Database.Command(connection, transaction,
                "SELECT id,username,password_hash FROM accounts WHERE id=$1 AND NOT technical FOR UPDATE", key.Id)
            : Database.Command(connection, transaction,
                "SELECT id,username,password_hash FROM accounts WHERE normalized_name=$1 AND NOT technical FOR UPDATE",
                Normalize(key.Name ?? ""));
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? new(reader.GetString(0), reader.GetString(1), reader.GetString(2)) : null;
    }

    // A locked name is refused before its password is checked; a wrong password counts towards the lock.
    private RenameResult? CheckPassword(LockedAccount? account, string password)
    {
        if (account is not null && guard.LockedFor(account.Username) is { } locked)
            return new(RenameError.Locked, "", locked);
        if (PasswordMatches(account, password))
        {
            guard.Succeeded(account!.Username);
            return null;
        }
        if (account is not null)
            guard.Failed(account.Username);
        return new(RenameError.WrongPassword, "");
    }

    // A missing account is checked against a throwaway hash, so its answer costs the same time.
    private static bool PasswordMatches(LockedAccount? account, string password) =>
        AccountInput.ValidPassword(password)
        && Passwords.Verify(account?.Id, account?.Hash, password) != PasswordVerificationResult.Failed
        && account is not null;

    private static async Task<bool> UpdateAccount(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        string name, CancellationToken cancellation)
    {
        await using var savepoint = Database.Command(connection, transaction, "SAVEPOINT account_rename");
        await savepoint.ExecuteNonQueryAsync(cancellation);
        try
        {
            await using var update = Database.Command(connection, transaction,
                "UPDATE accounts SET username=$2, normalized_name=$3 WHERE id=$1", account, name, Normalize(name));
            await update.ExecuteNonQueryAsync(cancellation);
            return true;
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await using var rollback = Database.Command(connection, transaction, "ROLLBACK TO SAVEPOINT account_rename");
            await rollback.ExecuteNonQueryAsync(cancellation);
            return false;
        }
    }

    // The room's copies of the name (RoomState: hostName, freePeoplesName, shadowName) and the audience rows; each
    // changed room's version moves on, so its members read it again.
    private static async Task UpdateRooms(NpgsqlConnection connection, NpgsqlTransaction transaction, string account,
        string name, CancellationToken cancellation)
    {
        // The column comes from this fixed list, never from input; the JSON path is a parameter.
        foreach (var (column, field) in new[] { ("host_account", "hostName"), ("fp_account", "freePeoplesName"), ("sh_account", "shadowName") })
        {
            await using var room = Database.Command(connection, transaction,
                "UPDATE rooms SET state = jsonb_set(jsonb_set(state, $3::text[], to_jsonb($2::text)), '{version}', " +
                "to_jsonb(COALESCE((state->>'version')::int, 0) + 1)) WHERE " + column + "=$1",
                account, name, new[] { field });
            await room.ExecuteNonQueryAsync(cancellation);
        }
        await using var audience = Database.Command(connection, transaction,
            "UPDATE room_observers SET username=$2 WHERE account_id=$1", account, name);
        await audience.ExecuteNonQueryAsync(cancellation);
    }

    private static string Normalize(string name) => name.Trim().ToUpperInvariant();
}
