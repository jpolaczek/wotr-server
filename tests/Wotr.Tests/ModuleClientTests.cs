using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// EXP-02: a client older than the first version that plays a module is refused at every table using that module with
// ClientUpdateRequired, which its updater turns into a required update; base tables stay open to it.
public class ModuleClientTests
{
    private const string Current = "0.10.0";
    private const string Older = "0.9.0";

    [Fact]
    public void TheSettingPairsModulesWithTheirFirstClientVersionsInAnyCase()
    {
        var versions = ModuleClientPolicy.Parse(" FATE=0.10.0, lords=0.11.0-rc.2 ");
        Assert.Equal(["fate", "lords"], versions.Keys.Order());
        Assert.Equal("0.11.0-rc.2", versions["lords"].Text);
        Assert.Empty(ModuleClientPolicy.Parse(""));
        Assert.False(ModuleClientPolicy.Open.Restricts);
    }

    [Theory]
    [InlineData("mordor=0.10.0")]
    [InlineData("fate")]
    [InlineData("fate=ten")]
    [InlineData("fate=0.10")]
    [InlineData("fate=0.10.0,fate=0.11.0")]
    public void ABrokenSettingStopsTheServer(string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => ModuleClientPolicy.Parse(value));
        Assert.Contains(ModuleClientPolicy.Key, error.Message);
    }

    [Fact]
    public void AClientPlaysTheModulesWhoseFirstVersionItHasReached()
    {
        var policy = new ModuleClientPolicy(ModuleClientPolicy.Parse("fate=0.10.0, lords=0.11.0"));
        Assert.Equal(["fate", "lords"], policy.Unplayable(Older).Order());
        Assert.Equal(["fate", "lords"], policy.Unplayable("0.10.0-rc.3").Order());
        Assert.Equal(["lords"], policy.Unplayable(Current));
        Assert.Empty(policy.Unplayable("0.11.0"));
        // A client without a version (accepted only outside Production) plays only the unrestricted modules.
        Assert.Equal(["fate", "lords"], policy.Unplayable("").Order());
        var lords = policy.Unplayable(Current);
        Assert.True(ModuleClientPolicy.Plays(lords, new()));
        Assert.True(ModuleClientPolicy.Plays(lords, new() { Fate = true, Kings = true }));
        Assert.False(ModuleClientPolicy.Plays(lords, new() { Fate = true, Lords = true, Council = true }));
    }

    [Fact]
    public void ProductionNeedsTheFirstClientVersionOfEveryReleasedModule()
    {
        var fate = new ReleaseScope([ExpansionModules.Fate]);
        var error = Assert.Throws<InvalidOperationException>(() => Read("Production", fate, "lords=0.11.0"));
        Assert.Contains("'fate'", error.Message);
        Assert.True(Read("Production", fate, "fate=0.10.0").Restricts);
        Assert.False(Read("Production", ReleaseScope.BaseOnly, "").Restricts);
        Assert.False(Read("Development", ReleaseScope.Full, "").Restricts);
    }

    // Wotr__Release__Modules=fate with Wotr__Release__ModuleClients=fate=0.10.0: Bob's 0.9.0 client is refused at a
    // Fate table (joining, reading, confirming, the match, its updates and its replay) and plays base tables; his
    // 0.10.0 client plays the Fate table. Alice's 0.9.0 client cannot configure Fate.
    [PostgresFact]
    public async Task AnOlderClientIsSentToTheUpdateAtEveryRequestOfAFateTable()
    {
        await using var host = new AccountHost(expansions: false, extra: new Dictionary<string, string?>
        {
            [ReleaseScope.ModulesKey] = "fate", [ModuleClientPolicy.Key] = "fate=" + Current
        });
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var aliceCurrent = Client(host, alice, Current);
        using var aliceOlder = Client(host, alice, Older);
        using var bobCurrent = Client(host, bob, Current);
        using var bobOlder = Client(host, bob, Older);
        using var bobUnversioned = MenuAccounts.Client(host, bob);

        var fate = await Room(aliceCurrent);
        fate = await Configured(aliceCurrent, fate, new() { Fate = true });
        var code = new JoinRoomRequest { Code = fate.JoinCode };
        await UpdateRequired(await bobOlder.PostAsJsonAsync("/api/rooms/join", code, BoardCatalog.Json));
        await UpdateRequired(await bobUnversioned.PostAsJsonAsync("/api/rooms/join", code, BoardCatalog.Json));
        await Succeeds(await bobCurrent.PostAsJsonAsync("/api/rooms/join", code, BoardCatalog.Json));
        await UpdateRequired(await bobOlder.GetAsync($"/api/rooms/{fate.Id}"));
        var revision = new RoomRevisionRequest { ExpectedVersion = fate.Version };
        await UpdateRequired(await bobOlder.PostAsJsonAsync($"/api/rooms/{fate.Id}/confirm", revision, BoardCatalog.Json));
        await Succeeds(await aliceCurrent.PostAsJsonAsync($"/api/rooms/{fate.Id}/confirm", revision, BoardCatalog.Json));
        await Succeeds(await bobCurrent.PostAsJsonAsync($"/api/rooms/{fate.Id}/confirm", revision, BoardCatalog.Json));
        await UpdateRequired(await aliceOlder.PostAsJsonAsync($"/api/rooms/{fate.Id}/start", revision, BoardCatalog.Json));
        var started = await Read<RoomView>(await aliceCurrent.PostAsJsonAsync($"/api/rooms/{fate.Id}/start", revision,
            BoardCatalog.Json));

        await UpdateRequired(await bobOlder.GetAsync($"/api/matches/{started.MatchId}"));
        await UpdateRequired(await bobOlder.GetAsync($"/api/matches/{started.MatchId}/updates"));
        await UpdateRequired(await bobOlder.GetAsync($"/api/matches/{started.MatchId}/replay"));
        await UpdateRequired(await bobOlder.GetAsync($"/api/observe/{started.MatchId}"));
        Assert.True((await Read<PlayerView>(await bobCurrent.GetAsync($"/api/matches/{started.MatchId}"))).Configuration.Fate);

        var basic = await Room(aliceOlder);
        await UpdateRequired(await Configure(aliceOlder, basic, new() { Fate = true }));
        await Succeeds(await Configure(aliceOlder, basic, new() { NewCities = false }));
        await Succeeds(await bobOlder.PostAsJsonAsync("/api/rooms/join", new JoinRoomRequest { Code = basic.JoinCode },
            BoardCatalog.Json));
        await Succeeds(await bobOlder.GetAsync($"/api/rooms/{basic.Id}"));
    }

    // Without the setting no module is restricted and no table is read for it.
    [PostgresFact]
    public async Task WithoutTheSettingEveryClientPlaysEveryReleasedModule()
    {
        await using var host = new AccountHost(extra: new Dictionary<string, string?> { [ReleaseScope.ModulesKey] = "fate" });
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var aliceOlder = Client(host, alice, Older);
        using var bobOlder = Client(host, bob, Older);
        var room = await Configured(aliceOlder, await Room(aliceOlder), new() { Fate = true });
        await Succeeds(await bobOlder.PostAsJsonAsync("/api/rooms/join", new JoinRoomRequest { Code = room.JoinCode },
            BoardCatalog.Json));
    }

    private static HttpClient Client(AccountHost host, SessionView session, string version)
    {
        var client = MenuAccounts.Client(host, session);
        client.DefaultRequestHeaders.Add(ClientCompatibility.ClientVersionHeader, version);
        client.DefaultRequestHeaders.Add(ClientCompatibility.ProtocolVersionHeader, Protocol.Version.ToString());
        return client;
    }

    private static async Task<RoomView> Room(HttpClient host) =>
        await Read<RoomView>(await host.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Side = Side.FreePeoples },
            BoardCatalog.Json));

    private static async Task<RoomView> Configured(HttpClient host, RoomView room, MatchConfiguration configuration) =>
        await Read<RoomView>(await Configure(host, room, configuration));

    private static Task<HttpResponseMessage> Configure(HttpClient host, RoomView room, MatchConfiguration configuration) =>
        host.PutAsJsonAsync($"/api/rooms/{room.Id}/configuration",
            new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = configuration }, BoardCatalog.Json);

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        using (response)
        {
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>(BoardCatalog.Json))!;
        }
    }

    private static async Task Succeeds(HttpResponseMessage response)
    {
        using (response)
            Assert.True(response.IsSuccessStatusCode, response.StatusCode + " " + await response.Content.ReadAsStringAsync());
    }

    private static async Task UpdateRequired(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(ClientCompatibility.ClientUpdateRequired,
                (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        }
    }

    private static ModuleClientPolicy Read(string environment, ReleaseScope release, string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ModuleClientPolicy.Key] = value })
            .Build();
        return ModuleClientPolicy.Read(configuration, new TestEnvironment(environment), release);
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Wotr.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
