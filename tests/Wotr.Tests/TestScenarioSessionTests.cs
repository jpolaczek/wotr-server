using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// TEST-01: a test session is a real match played by the owner and a technical seat, never a saved game.
public class TestScenarioSessionTests
{
    private const string Password = "A long test password!";

    [Fact]
    public void EveryScenarioBuildsTheSameStateAndStartsWithItsSide()
    {
        foreach (var scenario in TestScenarioCatalog.All)
        {
            var first = scenario.Create("repeatable");
            Assert.Equal(StateCodec.Write(first), StateCodec.Write(scenario.Create("repeatable")));
            Assert.Equal(StateCodec.Write(first), StateCodec.Write(StateCodec.Read(StateCodec.Write(first))));
            Assert.Equal(scenario.StartingSide, first.Pending?.Owner);
        }
        Assert.Equal(TestScenarioCatalog.All.Count, TestScenarioCatalog.All.Select(s => s.Id).Distinct().Count());
    }

    [Fact]
    public void TheModeIsOnOnlyInDevelopmentAndTestingUnlessConfigured()
    {
        static bool Enabled(string environment, string? value = null) => TestScenarioOptions.Read(
            new ConfigurationBuilder().AddInMemoryCollection(value is null ? [] : new Dictionary<string, string?>
            {
                [TestScenarioOptions.ConfigurationKey] = value
            }).Build(), new TestEnvironment(environment)).Enabled;
        Assert.True(Enabled(Environments.Development));
        Assert.True(Enabled("Testing"));
        Assert.False(Enabled(Environments.Production));
        Assert.True(Enabled(Environments.Production, "true"));
        Assert.False(Enabled(Environments.Development, "false"));
    }

    [PostgresFact]
    public async Task BothSidesPlayRestartRebuildsTheSituationAndNothingStays()
    {
        await using var host = new AccountHost();
        using var owner = await Owner(host);
        var catalog = await owner.GetFromJsonAsync<TestScenarioCatalogView>("/api/test-scenarios", BoardCatalog.Json);
        Assert.Contains(catalog!.Scenarios, scenario => scenario.Id == "setup" && scenario.Title.Length > 0);

        var session = await Start(owner, "setup");
        Assert.Equal(Side.FreePeoples, session.OwnerSide);
        using var seat = Client(host, session.Seat.Token);
        var ownerView = await View(owner, session.MatchId);
        var seatView = await View(seat, session.MatchId);
        Assert.Equal(Side.FreePeoples, ownerView.You);
        Assert.Equal(Side.Shadow, seatView.You);
        Assert.Equal(Canonical(StateCodec.Write(TestScenarioCatalog.Find("setup")!.Create(session.MatchId))), await StoredState(session.MatchId));

        var acting = ownerView.DecisionOwner == Side.FreePeoples ? owner : seat;
        var played = await Play(acting, ownerView.DecisionOwner == Side.FreePeoples ? ownerView : seatView);
        Assert.True(played.Accepted, played.Error);
        Assert.Equal(ownerView.Revision + 1, (await View(owner, session.MatchId)).Revision);
        Assert.Empty((await owner.GetFromJsonAsync<MatchListView>("/api/matches", BoardCatalog.Json))!.Matches);

        var restarted = await Start(owner, "setup");
        Assert.NotEqual(session.MatchId, restarted.MatchId);
        Assert.Equal(Canonical(StateCodec.Write(TestScenarioCatalog.Find("setup")!.Create(restarted.MatchId))), await StoredState(restarted.MatchId));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/matches/{session.MatchId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await seat.GetAsync($"/api/matches/{restarted.MatchId}")).StatusCode);
        Assert.Equal(0, await Remaining(session.MatchId));
        using var newSeat = Client(host, restarted.Seat.Token);
        Assert.Equal(Side.Shadow, (await View(newSeat, restarted.MatchId)).You);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync("/api/test-scenarios/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/matches/{restarted.MatchId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await newSeat.GetAsync($"/api/matches/{restarted.MatchId}")).StatusCode);
        Assert.Equal(0, await Remaining(restarted.MatchId));
    }

    // TEST-02: every catalog scenario starts through the server and the starting side holds the decision.
    // TEST-10: with only Fate released, Fate's scenario is listed and the Lords and Kings ones are hidden and refused
    // like an unknown id; the base-game scenarios stay.
    [PostgresFact]
    public async Task AScenarioOfAnUnreleasedModuleIsHiddenAndRefused()
    {
        await using var host = new AccountHost(extra: new Dictionary<string, string?> { [ReleaseScope.ModulesKey] = "fate" });
        using var owner = await Owner(host);
        var catalog = (await owner.GetFromJsonAsync<TestScenarioCatalogView>("/api/test-scenarios", BoardCatalog.Json))!;
        Assert.Contains(catalog.Scenarios, s => s.Id == "setup" && s.Modules.Count == 0);
        Assert.Contains(catalog.Scenarios, s => s.Id == "fate-gimli-renown" && s.Modules.SequenceEqual(["fate"]));
        Assert.DoesNotContain(catalog.Scenarios, s => s.Modules.Contains("lords") || s.Modules.Contains("kings"));
        using var refused = await owner.PostAsync("/api/test-scenarios/lords-fp-vilya/start", null);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, refused.StatusCode);
        Assert.Equal("ScenarioNotFound", (await refused.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        Assert.Equal(Side.FreePeoples, (await Start(owner, "fate-gimli-renown")).OwnerSide);
    }

    [PostgresFact]
    public async Task EveryScenarioStartsOverHttpWithItsSideToAct()
    {
        // TEST-09: the catalog has over two hundred scenarios; one account starts them all in a few seconds here.
        await using var host = new AccountHost(extra: new Dictionary<string, string?>
        {
            ["WOTR_LIMIT_ROOMS"] = "1000",
            ["WOTR_LIMIT_READS"] = "1000"
        });
        using var owner = await Owner(host);
        foreach (var scenario in TestScenarioCatalog.All)
        {
            var session = await Start(owner, scenario.Id);
            using var seat = Client(host, session.Seat.Token);
            var view = await View(scenario.StartingSide == Side.FreePeoples ? owner : seat, session.MatchId);
            Assert.Equal(scenario.StartingSide, view.You);
            Assert.True(view.DecisionOwner == scenario.StartingSide, scenario.Id);
            Assert.Contains(view.Options, option => option.Enabled);
        }
        Assert.Single(await OwnedTestMatches(OwnerOf(await Start(owner, "setup"))));
    }

    [PostgresFact]
    public async Task TheSeatReachesOnlyItsTestMatchAndCannotSignIn()
    {
        await using var host = new AccountHost();
        using var owner = await Owner(host);
        var session = await Start(owner, "setup");
        using var seat = Client(host, session.Seat.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await seat.GetAsync("/api/rooms")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seat.GetAsync("/api/test-scenarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seat.PostAsync("/api/test-scenarios/setup/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await seat.GetAsync($"/api/matches/{Guid.NewGuid():N}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seat.GetAsync($"/api/matches/{session.MatchId}")).StatusCode);
        var accounts = host.Services.GetRequiredService<AccountService>();
        Assert.Null(await accounts.Login(new() { Username = TestSessionStore.SeatPrefix + session.Seat.AccountId, Password = Password }));
        Assert.True(TestSessionStore.IsSeat(session.Seat.AccountId));
        var missing = await owner.PostAsync("/api/test-scenarios/no-such-scenario/start", null);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("ScenarioNotFound", (await missing.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
    }

    [PostgresFact]
    public async Task ConcurrentStartsLeaveOneTestMatch()
    {
        await using var host = new AccountHost();
        using var owner = await Owner(host);
        var sessions = await Task.WhenAll(Start(owner, "setup"), Start(owner, "setup"), Start(owner, "setup"));
        var owned = await OwnedTestMatches(OwnerOf(sessions[0]));
        Assert.Single(owned);
        Assert.Contains(owned[0], sessions.Select(s => s.MatchId));
    }

    [PostgresFact]
    public async Task WithoutTheOptionTheRoutesDoNotExist()
    {
        await using var host = new AccountHost(extra: new Dictionary<string, string?> { [TestScenarioOptions.ConfigurationKey] = "false" });
        using var owner = await Owner(host);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync("/api/test-scenarios")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsync("/api/test-scenarios/setup/start", null)).StatusCode);
    }

    private static string OwnerOf(TestSessionView session) => session.Seat.AccountId[TestSessionStore.SeatPrefix.Length..];

    // TEST-08: on a server open to every account, starts count against the account's room budget.
    [PostgresFact]
    public async Task StartsFasterThanTheRoomBudgetAreRefusedForThatAccountOnly()
    {
        await using var host = new AccountHost(extra: new Dictionary<string, string?>
        {
            ["WOTR_LIMIT_ROOMS"] = "2",
            ["WOTR_LIMIT_WINDOW_SECONDS"] = "600"
        });
        using var owner = await Owner(host);
        using var other = await Owner(host);
        await Start(owner, "setup");
        await Start(owner, "setup");
        var refused = await owner.PostAsync("/api/test-scenarios/setup/start", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.NotNull(refused.Headers.RetryAfter);
        Assert.Equal(Side.FreePeoples, (await Start(other, "setup")).OwnerSide);
    }

    private static async Task<HttpClient> Owner(AccountHost host)
    {
        var accounts = host.Services.GetRequiredService<AccountService>();
        var session = await TestAccounts.Register(accounts, "u" + Guid.NewGuid().ToString("N")[..20], Password);
        return Client(host, session.Token);
    }

    private static HttpClient Client(AccountHost host, string token)
    {
        var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<TestSessionView> Start(HttpClient owner, string scenario)
    {
        var response = await owner.PostAsync($"/api/test-scenarios/{scenario}/start", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TestSessionView>(BoardCatalog.Json))!;
    }

    private static async Task<PlayerView> View(HttpClient client, string match) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{match}", BoardCatalog.Json))!;

    // The first enabled option without a payload; the setup scenario offers one to the deciding side.
    private static async Task<CommandReceipt> Play(HttpClient client, PlayerView view)
    {
        var option = view.Options.First(o => o.Enabled && o.Kind is CommandKind.ContinueFellowship or CommandKind.Pass);
        var response = await client.PostAsJsonAsync($"/api/matches/{view.MatchId}/commands", new GameCommand
        {
            MatchId = view.MatchId,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = view.Revision,
            DecisionId = view.Decision!.Id,
            Kind = option.Kind
        }, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }

    private static async Task<string> StoredState(string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand("SELECT state::text FROM matches WHERE id=$1");
        command.Parameters.AddWithValue(match);
        return Canonical((string)(await command.ExecuteScalarAsync())!);
    }

    // jsonb reorders object keys, so states compare with keys sorted at every level.
    private static string Canonical(string json) => Sorted(JsonNode.Parse(json))?.ToJsonString() ?? "null";

    private static JsonNode? Sorted(JsonNode? node) => node switch
    {
        JsonObject value => new JsonObject(value.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => KeyValuePair.Create(p.Key, Sorted(p.Value)))),
        JsonArray value => new JsonArray(value.Select(Sorted).ToArray()),
        _ => node?.DeepClone()
    };

    private static async Task<long> Remaining(string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand("""
            SELECT (SELECT count(*) FROM matches WHERE id=$1) + (SELECT count(*) FROM match_events WHERE match_id=$1)
                + (SELECT count(*) FROM command_receipts WHERE match_id=$1) + (SELECT count(*) FROM test_matches WHERE match_id=$1)
                + (SELECT count(*) FROM match_reads WHERE match_id=$1)
            """);
        command.Parameters.AddWithValue(match);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<List<string>> OwnedTestMatches(string owner)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand("SELECT match_id FROM test_matches WHERE owner_account=$1");
        command.Parameters.AddWithValue(owner);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<string>();
        while (await reader.ReadAsync())
            result.Add(reader.GetString(0));
        return result;
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Wotr.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
