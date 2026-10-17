using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// QUAL-03 (review R01): accepted receipts stay for retries; domain rejections are bounded per account and match
// and go once the match moves on; a retry of a removed rejection is validated afresh and cannot run its old
// intent; malformed input stores nothing.
public sealed class ReceiptRetentionTests
{
    [PostgresFact]
    public async Task AFloodOfRejectionsKeepsABoundedNumberOfReceipts()
    {
        await using var host = new TestHost(settings: TestBudgets.Stress);
        var state = await Game(host);
        using var alice = host.Client("alice");
        for (var i = 0; i < 100; i++)
            await Send(alice, Stale(state));
        Assert.Equal(MatchStore.RejectionsKept, await Rejections(state.Id, "alice"));
        Assert.Equal(0, await Accepted(state.Id));
    }

    [PostgresFact]
    public async Task AnAcceptedCommandRemovesOutdatedRejectionsAndKeepsAcceptedReceipts()
    {
        await using var host = new TestHost(settings: TestBudgets.Stress);
        var state = await Game(host);
        using var alice = host.Client("alice");
        using var bob = host.Client("bob");
        var rejected = Stale(state);
        await Send(alice, rejected);
        await Send(bob, Stale(state));
        Assert.Equal(1, await Rejections(state.Id, "alice"));
        var continued = Valid(state);
        var first = await Send(alice, continued);
        Assert.True(first.Accepted, first.Error);
        Assert.Equal(0, await Rejections(state.Id, "alice"));
        Assert.Equal(0, await Rejections(state.Id, "bob"));
        // A lost response after the commit: the same command returns the same accepted receipt.
        var again = await Send(alice, continued);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(again));
        // A retry of the removed rejection is validated at the new revision and runs nothing.
        var store = host.Services.GetRequiredService<MatchStore>();
        var before = StateCodec.Write((await store.Read(state.Id, "alice"))!.State);
        var retried = await Send(alice, rejected);
        Assert.False(retried.Accepted);
        Assert.Contains(retried.Error, new[] { "StaleRevision", "DecisionExpired", "NotAuthorized" });
        Assert.Equal(before, StateCodec.Write((await store.Read(state.Id, "alice"))!.State));
    }

    [PostgresFact]
    public async Task MalformedInputStoresNoReceipt()
    {
        await using var host = new TestHost();
        var state = await Game(host);
        using var alice = host.Client("alice");
        var command = Valid(state);
        command["payload"]!["regionId"] = null;
        var receipt = await Send(alice, command);
        Assert.Equal(CommandShape.MalformedCommand, receipt.Error);
        Assert.Equal(0, await Rejections(state.Id, "alice"));
        using var lookup = await alice.GetAsync($"/api/matches/{state.Id}/receipts/{command["commandId"]!.GetValue<string>()}");
        Assert.Equal(HttpStatusCode.NotFound, lookup.StatusCode);
    }

    [PostgresFact]
    public async Task MigrationKeepsAcceptedReceiptsAndRemovesOutdatedRejections()
    {
        await using var admin = NpgsqlDataSource.Create(PostgresTests.Connection);
        var schema = "receipts_" + Guid.NewGuid().ToString("N");
        await Execute(admin, "CREATE SCHEMA " + schema);
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(PostgresTests.Connection) { SearchPath = schema };
            await using var data = NpgsqlDataSource.Create(settings.ConnectionString);
            await ApplyUpTo(data, 7);
            await Execute(data, "INSERT INTO matches(id,fp_account,sh_account,state,revision) VALUES ('m','a','b','{}'::jsonb,5)");
            await Receipt(data, "accepted", true, 3);
            await Receipt(data, "old-rejection", false, 4);
            await Receipt(data, "current-rejection", false, 5);
            var database = new Database(data);
            await database.Migrate();
            await database.Migrate();
            await using var query = data.CreateCommand("SELECT command_id, accepted, revision FROM command_receipts ORDER BY command_id");
            await using var reader = await query.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
                rows.Add($"{reader.GetString(0)}:{reader.GetBoolean(1)}:{reader.GetInt64(2)}");
            Assert.Equal(new[] { "accepted:True:3", "current-rejection:False:5" }, rows);
        }
        finally
        {
            await Execute(admin, "DROP SCHEMA " + schema + " CASCADE");
        }
    }

    private static async Task<MatchState> Game(TestHost host)
    {
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        await UI10ProtocolFixture.Save(host, state);
        return state;
    }

    private static JsonObject Valid(MatchState state)
    {
        var command = KernelTests.Command(state, CommandKind.ContinueFellowship);
        command.Payload = new CommandPayload();
        return UI10ProtocolFixture.Json(command);
    }

    private static JsonObject Stale(MatchState state)
    {
        var command = Valid(state);
        command["decisionId"] = "expired";
        return command;
    }

    private static async Task<CommandReceipt> Send(HttpClient client, JsonObject command) =>
        await UI10ProtocolFixture.Response<CommandReceipt>(
            await UI10ProtocolFixture.Post(client, command["matchId"]!.GetValue<string>(), "commands", command));

    private static async Task<long> Rejections(string match, string account) =>
        await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1 AND account_id=$2 AND NOT accepted", match, account);

    private static async Task<long> Accepted(string match) =>
        await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1 AND accepted", match);

    private static async Task<long> Count(string sql, params object[] values)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var connection = await data.OpenConnectionAsync();
        await using var command = Database.Command(connection, null, sql, values);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ApplyUpTo(NpgsqlDataSource data, int last)
    {
        await using var connection = await data.OpenConnectionAsync();
        await using (var ledger = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS schema_migrations (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())", connection))
            await ledger.ExecuteNonQueryAsync();
        var assembly = typeof(Database).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order())
        {
            var version = int.Parse(name.Split('.')[^2].Split('_')[0], System.Globalization.CultureInfo.InvariantCulture);
            if (version > last)
                continue;
            using var stream = assembly.GetManifestResourceStream(name)!;
            await using var ddl = new NpgsqlCommand(await new StreamReader(stream).ReadToEndAsync(), connection);
            await ddl.ExecuteNonQueryAsync();
            await using var record = Database.Command(connection, null, "INSERT INTO schema_migrations(version) VALUES ($1)", version);
            await record.ExecuteNonQueryAsync();
        }
    }

    private static async Task Receipt(NpgsqlDataSource data, string id, bool accepted, long revision)
    {
        var receipt = JsonSerializer.Serialize(new CommandReceipt { CommandId = id, Accepted = accepted, Revision = revision }, BoardCatalog.Json);
        await using var connection = await data.OpenConnectionAsync();
        await using var insert = Database.Command(connection, null,
            "INSERT INTO command_receipts(match_id,account_id,command_id,fingerprint,receipt) VALUES ('m','a',$1,'f',$2::jsonb)", id, receipt);
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task Execute(NpgsqlDataSource data, string sql)
    {
        await using var command = data.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }
}
