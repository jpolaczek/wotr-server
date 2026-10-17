using System.Net;
using System.Net.WebSockets;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// QUAL-05 (review R12): an explicit, validated pool budget; a full pool after its bounded wait answers 503
// ServerBusy (never 500), stores no receipt, changes nothing, closes update sockets with 1013, and the server
// recovers once connections free up, running a retried command exactly once.
public sealed class DatabaseBudgetTests
{
    private static Dictionary<string, string?> SmallPool() => new(TestBudgets.Stress)
    {
        ["WOTR_DB_POOL_MAX"] = "2",
        ["WOTR_DB_POOL_WAIT_SECONDS"] = "1"
    };

    [PostgresFact]
    public async Task AFullPoolAnswersServerBusyAndTheServerRecoversWithoutDoubleCost()
    {
        await using var host = new TestHost(settings: SmallPool());
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        var store = await UI10ProtocolFixture.Save(host, state);
        using var alice = host.Client("alice");
        var command = Continue(state);
        var held = await Hold(host, 2);
        try
        {
            using var read = await alice.GetAsync($"/api/matches/{state.Id}");
            await AssertBusy(read);
            using var sent = await UI10ProtocolFixture.Post(alice, state.Id, "commands", command);
            await AssertBusy(sent);
            await Assert.ThrowsAnyAsync<Exception>(() => Socket(host, state.Id));
        }
        finally
        {
            foreach (var connection in held)
                await connection.DisposeAsync();
        }
        Assert.Null(await store.Receipt(state.Id, command["commandId"]!.GetValue<string>(), "alice"));
        Assert.Equal(state.Revision, (await store.Read(state.Id, "alice"))!.State.Revision);
        var first = await UI10ProtocolFixture.Response<CommandReceipt>(await UI10ProtocolFixture.Post(alice, state.Id, "commands", command));
        Assert.True(first.Accepted, first.Error);
        var again = await UI10ProtocolFixture.Response<CommandReceipt>(await UI10ProtocolFixture.Post(alice, state.Id, "commands", command));
        Assert.Equal(first.Revision, again.Revision);
        Assert.Equal(state.Revision + 1, (await store.Read(state.Id, "alice"))!.State.Revision);
    }

    [PostgresFact]
    public async Task AnOpenUpdateSocketClosesWithTryAgainLaterWhenTheDatabaseIsBusy()
    {
        await using var host = new TestHost(settings: SmallPool());
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        await UI10ProtocolFixture.Save(host, state);
        using var socket = await Socket(host, state.Id);
        var buffer = new byte[1024];
        using (var first = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            await socket.ReceiveAsync(buffer, first.Token);
        var held = await Hold(host, 2);
        try
        {
            host.Services.GetRequiredService<RevisionHub>().Publish(state.Id, state.Revision);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var message = await socket.ReceiveAsync(buffer, timeout.Token);
            Assert.Equal(WebSocketMessageType.Close, message.MessageType);
            Assert.Equal(MatchSocket.TryAgainLater, (int)socket.CloseStatus!.Value);
            Assert.Equal(DatabaseOverloadHandling.ServerBusy, socket.CloseStatusDescription);
        }
        finally
        {
            foreach (var connection in held)
                await connection.DisposeAsync();
        }
        await WaitFor(() => host.Services.GetRequiredService<UpdateSocketBudget>().Accounts == 0);
    }

    [Theory]
    [InlineData("WOTR_DB_POOL_MAX", "0")]
    [InlineData("WOTR_DB_POOL_MAX", "5000")]
    [InlineData("WOTR_DB_POOL_WAIT_SECONDS", "0")]
    [InlineData("WOTR_DB_COMMAND_TIMEOUT_SECONDS", "never")]
    public void PoolSettingsAreValidated(string key, string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.Read(Configuration((key, value))));
        Assert.Contains(key, error.Message);
    }

    [Fact]
    public void ThePoolBudgetIsExplicitInTheConnectionString()
    {
        var defaults = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.Read(Configuration()));
        Assert.Equal(DatabaseConfiguration.DefaultPoolSize, defaults.MaxPoolSize);
        Assert.Equal(DatabaseConfiguration.DefaultPoolWaitSeconds, defaults.Timeout);
        Assert.Equal(DatabaseConfiguration.DefaultCommandTimeoutSeconds, defaults.CommandTimeout);
        var chosen = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.Read(Configuration(("WOTR_DB_POOL_MAX", "12"),
            ("WOTR_DB_POOL_WAIT_SECONDS", "3"), ("WOTR_DB_COMMAND_TIMEOUT_SECONDS", "45"))));
        Assert.Equal(12, chosen.MaxPoolSize);
        Assert.Equal(3, chosen.Timeout);
        Assert.Equal(45, chosen.CommandTimeout);
        var minimumAboveMaximum = Configuration(("WOTR_DB_POOL_MAX", "2"));
        minimumAboveMaximum["WOTR_DB"] = "Host=localhost;Database=wotr;Username=wotr;Minimum Pool Size=5";
        Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.Read(minimumAboveMaximum));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
    {
        var settings = values.ToDictionary(v => v.Key, v => (string?)v.Value);
        settings["WOTR_DB"] = "Host=localhost;Database=wotr;Username=wotr";
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static JsonObject Continue(MatchState state)
    {
        var command = KernelTests.Command(state, CommandKind.ContinueFellowship);
        command.Payload = new CommandPayload();
        return UI10ProtocolFixture.Json(command);
    }

    // Takes connections from the server's own pool, as a burst of slow work would.
    private static async Task<List<NpgsqlConnection>> Hold(TestHost host, int count)
    {
        var source = host.Services.GetRequiredService<NpgsqlDataSource>();
        var held = new List<NpgsqlConnection>();
        for (var i = 0; i < count; i++)
            held.Add(await source.OpenConnectionAsync());
        return held;
    }

    private static async Task AssertBusy(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(DatabaseOverloadHandling.ServerBusy, body);
        Assert.DoesNotContain("Host=", body);
        Assert.DoesNotContain("Exception", body);
    }

    private static async Task<WebSocket> Socket(TestHost host, string match)
    {
        var client = host.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = "Bearer alice";
        return await client.ConnectAsync(new Uri($"wss://localhost/api/matches/{match}/updates"), CancellationToken.None);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(50);
        Assert.True(condition());
    }
}
