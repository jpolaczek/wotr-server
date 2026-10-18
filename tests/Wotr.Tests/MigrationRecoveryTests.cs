using Npgsql;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class MigrationRecoveryTests
{
    [PostgresFact]
    public async Task CancellationAndSqlFailureLeaveNoPartialMigrationAndCanBeRetried()
    {
        var schema = "migration_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(PostgresTests.Connection);
        await control.OpenAsync();
        await Execute(control, $"CREATE SCHEMA {schema}");
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(PostgresTests.Connection) { SearchPath = schema };
            await using var source = NpgsqlDataSource.Create(settings.ConnectionString);
            var database = new Database(source);
            await using (var blocker = await control.BeginTransactionAsync())
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(779201)", control, blocker);
                await command.ExecuteNonQueryAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => database.Migrate(timeout.Token));
                await blocker.RollbackAsync();
            }
            await using var connection = await source.OpenConnectionAsync();
            await using var empty = new NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_schema = @schema", connection);
            empty.Parameters.AddWithValue("schema", schema);
            Assert.Equal(0L, await empty.ExecuteScalarAsync());

            // Fail migration 002 after 001 ran inside the same transaction.
            await Execute(connection, "CREATE TABLE accounts (incompatible integer)");
            await Assert.ThrowsAsync<PostgresException>(() => database.Migrate());
            Assert.Equal(1L, await empty.ExecuteScalarAsync());
            await Execute(connection, "DROP TABLE accounts");
            await database.Migrate();
            await database.Migrate();
            await using var versions = new NpgsqlCommand("SELECT string_agg(version::text, ',' ORDER BY version) FROM schema_migrations", connection);
            Assert.Equal("1,2,3,4,5,6,7,8,9,10,11,12,13,14", await versions.ExecuteScalarAsync());
        }
        finally { await Execute(control, $"DROP SCHEMA {schema} CASCADE"); }
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
