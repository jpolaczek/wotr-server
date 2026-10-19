using System.Net;
using System.Net.WebSockets;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// QUAL-02 (review R01): budgets of a signed-in account are spent before any lock, transaction or receipt; a
// rejected request is 429 with Retry-After, leaves no receipt and changes nothing; budgets follow the account.
public sealed class GameRateLimitTests
{
    private static Dictionary<string, string?> Limits(int commands = 5, int previews = 120, int sockets = 4, int window = 60) => new()
    {
        ["WOTR_LIMIT_COMMANDS"] = commands.ToString(),
        ["WOTR_LIMIT_PREVIEWS"] = previews.ToString(),
        ["WOTR_LIMIT_SOCKETS"] = sockets.ToString(),
        ["WOTR_LIMIT_WINDOW_SECONDS"] = window.ToString()
    };

    [PostgresFact]
    public async Task AFloodOfRejectedCommandsStopsAtTheBudgetWithoutReceiptsOrMutation()
    {
        await using var host = new TestHost(settings: Limits());
        var state = await Game(host);
        var store = host.Services.GetRequiredService<MatchStore>();
        var before = StateCodec.Write((await store.Read(state.Id, "alice"))!.State);
        using var alice = host.Client("alice");
        var accepted = new List<string>();
        var limited = new List<string>();
        for (var i = 0; i < 12; i++)
        {
            var command = Stale(state);
            using var response = await UI10ProtocolFixture.Post(alice, state.Id, "commands", command);
            var id = command["commandId"]!.GetValue<string>();
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero, "A 429 tells when to retry.");
                Assert.Contains("TooManyRequests", await response.Content.ReadAsStringAsync());
                limited.Add(id);
            }
            else
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                accepted.Add(id);
            }
        }
        Assert.Equal(5, accepted.Count);
        Assert.Equal(7, limited.Count);
        foreach (var id in limited)
            Assert.Null(await store.Receipt(state.Id, id, "alice"));
        foreach (var id in accepted)
            Assert.NotNull(await store.Receipt(state.Id, id, "alice"));
        Assert.Equal(before, StateCodec.Write((await store.Read(state.Id, "alice"))!.State));
        using var bob = host.Client("bob");
        using (var opponent = await UI10ProtocolFixture.Post(bob, state.Id, "commands", Stale(state)))
            Assert.Equal(HttpStatusCode.OK, opponent.StatusCode);
    }

    [PostgresFact]
    public async Task TwoSessionsOfOneAccountShareItsBudgetAndPreviewsKeepTheirOwn()
    {
        await using var host = new TestHost(settings: Limits(commands: 4, previews: 3));
        var state = await Game(host);
        using var first = host.Client("alice");
        using var second = host.Client("alice");
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add(await Status(first, state.Id, "commands"));
            statuses.Add(await Status(second, state.Id, "commands"));
        }
        Assert.Equal(4, statuses.Count(s => s == HttpStatusCode.OK));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
        var previews = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            previews.Add(await Status(first, state.Id, "preview"));
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests }, previews);
    }

    [PostgresFact]
    public async Task ARetryWithTheSameCommandIdSucceedsOnceTheWindowHasPassed()
    {
        await using var host = new TestHost(settings: Limits(commands: 1, window: 1));
        var state = await Game(host);
        using var alice = host.Client("alice");
        Assert.Equal(HttpStatusCode.OK, await Status(alice, state.Id, "commands"));
        var retried = Stale(state);
        using (var limited = await UI10ProtocolFixture.Post(alice, state.Id, "commands", retried))
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        var receipt = await UI10ProtocolFixture.Response<CommandReceipt>(await UI10ProtocolFixture.Post(alice, state.Id, "commands", retried));
        Assert.Equal(retried["commandId"]!.GetValue<string>(), receipt.CommandId);
        Assert.Equal("DecisionExpired", receipt.Error);
    }

    [PostgresFact]
    public async Task UpdateSocketsHaveABudgetPerAccountAndGiveTheirSlotBack()
    {
        await using var host = new TestHost(settings: Limits(sockets: 2));
        var state = await Game(host);
        var budget = host.Services.GetRequiredService<UpdateSocketBudget>();
        var one = await Socket(host, state.Id, "alice");
        var two = await Socket(host, state.Id, "alice");
        await Assert.ThrowsAnyAsync<Exception>(() => Socket(host, state.Id, "alice"));
        var opponent = await Socket(host, state.Id, "bob");
        Assert.Equal(2, budget.Open("alice"));
        one.Abort();
        one.Dispose();
        await WaitFor(() => budget.Open("alice") == 1);
        var three = await Socket(host, state.Id, "alice");
        foreach (var socket in new[] { two, three, opponent })
        {
            socket.Abort();
            socket.Dispose();
        }
        await WaitFor(() => budget.Accounts == 0);
    }

    [Theory]
    [InlineData("WOTR_LIMIT_COMMANDS", "0")]
    [InlineData("WOTR_LIMIT_PREVIEWS", "-3")]
    [InlineData("WOTR_LIMIT_SOCKETS", "1000")]
    [InlineData("WOTR_LIMIT_WINDOW_SECONDS", "ten")]
    [InlineData("WOTR_LIMIT_SIGNINS", "0")]
    public void ConfiguredBudgetsAreValidated(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [key] = value }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => GameRateLimitOptions.Read(configuration));
        Assert.Contains(key, error.Message);
        Assert.Equal(new GameRateLimitOptions(), GameRateLimitOptions.Read(new ConfigurationBuilder().Build()));
    }

    // EXP-00: the per-address sign-in budget comes from configuration; 20 per minute unless raised.
    [Fact]
    public void TheSignInBudgetIsConfigurable()
    {
        Assert.Equal(20, GameRateLimitOptions.Read(new ConfigurationBuilder().Build()).SignInsPerMinute);
        var raised = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["WOTR_LIMIT_SIGNINS"] = "5000" }).Build();
        Assert.Equal(5000, GameRateLimitOptions.Read(raised).SignInsPerMinute);
    }

    private static async Task<MatchState> Game(TestHost host)
    {
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        await UI10ProtocolFixture.Save(host, state);
        return state;
    }

    // A well-formed command for a decision that has already passed: rejected with a stored receipt.
    private static JsonObject Stale(MatchState state)
    {
        var command = KernelTests.Command(state, CommandKind.ContinueFellowship);
        command.DecisionId = "expired";
        return UI10ProtocolFixture.Json(command);
    }

    private static async Task<HttpStatusCode> Status(HttpClient client, string match, string endpoint)
    {
        var command = new JsonObject
        {
            ["matchId"] = match, ["commandId"] = Guid.NewGuid().ToString("N"), ["expectedRevision"] = 0,
            ["decisionId"] = "expired", ["kind"] = "ContinueFellowship", ["payload"] = new JsonObject()
        };
        using var response = await UI10ProtocolFixture.Post(client, match, endpoint, command);
        return response.StatusCode;
    }

    private static async Task<WebSocket> Socket(TestHost host, string match, string account)
    {
        var client = host.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = "Bearer " + account;
        return await client.ConnectAsync(new Uri($"wss://localhost/api/matches/{match}/updates"), CancellationToken.None);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(50);
        Assert.True(condition());
    }
}
