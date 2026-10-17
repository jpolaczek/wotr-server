using Microsoft.Extensions.Configuration;
using Npgsql;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class DeploymentDatabaseTests
{
    [Theory]
    [InlineData("wotr")]
    [InlineData("wotr_restore_after_update")]
    public void DeploymentSelectsTheRestoredDatabaseWithoutChangingCredentials(string database)
    {
        var original = new NpgsqlConnectionStringBuilder
        {
            Host = "database", Database = "wotr", Username = "wotr", Password = "test;\"only"
        };
        var settings = Settings(original.ConnectionString, database);
        var selected = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.Read(settings));
        Assert.Equal(database, selected.Database);
        Assert.Equal(original.Host, selected.Host);
        Assert.Equal(original.Username, selected.Username);
        Assert.Equal(original.Password, selected.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("postgres")]
    [InlineData("wotr_restore_")]
    [InlineData("wotr_restore_test;Password=changed")]
    [InlineData("wotr\n")]
    public void InvalidDatabaseSelectionIsRejectedWithoutExposingTheSecret(string database)
    {
        var settings = Settings("Host=database;Username=wotr;Password=private-test-secret", database);
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.Read(settings));
        Assert.DoesNotContain("private-test-secret", error.Message);
    }

    [Theory]
    [InlineData("other-host", "wotr", 5432)]
    [InlineData("database", "postgres", 5432)]
    [InlineData("database", "wotr", 55432)]
    public void DeploymentCannotSilentlyUseADifferentDatabaseService(string host, string user, int port)
    {
        var settings = Settings($"Host={host};Username={user};Port={port};Password=private-test-secret", "wotr");
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.Read(settings));
        Assert.DoesNotContain("private-test-secret", error.Message);
    }

    private static IConfiguration Settings(string connection, string database) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WOTR_DB"] = connection,
            ["WOTR_DATABASE"] = database
        }).Build();
}
