using Npgsql;

namespace Wotr.Server;

public sealed class Database(NpgsqlDataSource source)
{
    public NpgsqlDataSource Source { get; } = source;
    public async Task Migrate(CancellationToken cancellation = default)
    {
        await using var connection = await Source.OpenConnectionAsync(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(779201)", connection, transaction))
            await lockCommand.ExecuteNonQueryAsync(cancellation);
        await using (var ledger = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS schema_migrations (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())", connection, transaction))
            await ledger.ExecuteNonQueryAsync(cancellation);
        var assembly = typeof(Database).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order())
        {
            var version = int.Parse(name.Split('.')[^2].Split('_')[0], System.Globalization.CultureInfo.InvariantCulture);
            await using var exists = Command(connection, transaction, "SELECT EXISTS(SELECT 1 FROM schema_migrations WHERE version=$1)", version);
            if ((bool)(await exists.ExecuteScalarAsync(cancellation))!)
                continue;
            using var stream = assembly.GetManifestResourceStream(name)!;
            var sql = await new StreamReader(stream).ReadToEndAsync(cancellation);
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync(cancellation);
            await using var record = Command(connection, transaction, "INSERT INTO schema_migrations(version) VALUES ($1)", version);
            await record.ExecuteNonQueryAsync(cancellation);
        }
        await transaction.CommitAsync(cancellation);
    }
    public static NpgsqlCommand Command(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params object[] values)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var value in values)
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        return command;
    }
}
