using System.Text.RegularExpressions;
using Npgsql;

namespace Wotr.Server;

public static class DatabaseConfiguration
{
    public static string Read(IConfiguration configuration)
    {
        var file = configuration["WOTR_DB_FILE"];
        var inline = configuration["WOTR_DB"];
        if (!string.IsNullOrWhiteSpace(file) && !string.IsNullOrWhiteSpace(inline))
            throw new InvalidOperationException("Configure only one of WOTR_DB_FILE and WOTR_DB.");
        var value = string.IsNullOrWhiteSpace(file) ? inline : File.ReadAllText(file).Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Set WOTR_DB_FILE or WOTR_DB for PostgreSQL 17.");
        var database = configuration["WOTR_DATABASE"];
        return WithPool(database == null ? value : SelectDeploymentDatabase(value, database), configuration);
    }

    // QUAL-05: an explicit pool budget per server process. The sum of all instances' pools, migrations, health
    // checks, backups, other applications and the administrator's reserve must fit max_connections (T38).
    // Waiting for a free connection is bounded; a full pool then answers 503 instead of queueing without end.
    public const int DefaultPoolSize = 40;
    public const int DefaultPoolWaitSeconds = 5;
    public const int DefaultCommandTimeoutSeconds = 30;

    private static string WithPool(string connectionString, IConfiguration configuration)
    {
        var connection = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = Setting(configuration, "WOTR_DB_POOL_MAX", DefaultPoolSize, 1, 1000),
            Timeout = Setting(configuration, "WOTR_DB_POOL_WAIT_SECONDS", DefaultPoolWaitSeconds, 1, 60),
            CommandTimeout = Setting(configuration, "WOTR_DB_COMMAND_TIMEOUT_SECONDS", DefaultCommandTimeoutSeconds, 1, 600)
        };
        if (connection.MinPoolSize > connection.MaxPoolSize)
            throw new InvalidOperationException("The connection string's Minimum Pool Size exceeds WOTR_DB_POOL_MAX.");
        return connection.ConnectionString;
    }

    private static int Setting(IConfiguration configuration, string key, int fallback, int minimum, int maximum)
    {
        var text = configuration[key];
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) ||
            value < minimum || value > maximum)
            throw new InvalidOperationException($"{key} must be a whole number from {minimum} to {maximum}.");
        return value;
    }

    private static string SelectDeploymentDatabase(string connectionString, string database)
    {
        if (!Regex.IsMatch(database, @"\A(?:wotr|wotr_restore_[a-z0-9_]{1,40})\z"))
            throw new InvalidOperationException("WOTR_DATABASE must name a WoTR deployment database.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        if (connection.Host != "database" || connection.Port != 5432 || connection.Username != "wotr")
            throw new InvalidOperationException("WOTR_DATABASE requires the Compose database service and application role.");
        connection.Database = database;
        return connection.ConnectionString;
    }
}
