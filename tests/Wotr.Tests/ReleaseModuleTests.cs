using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// EXP-01 (owner decision 2026-10-19): the expansions are released one module at a time, Fate, then Lords, then Kings.
// Any room, a named one or a challenge's table too, may use the released modules; an unreleased one is refused.
public class ReleaseModuleTests
{
    private const string Production = "Production";
    private const string Development = "Development";

    [Fact]
    public void TheModuleListReleasesTheNamedModulesInAnyCaseInReleaseOrder()
    {
        var scope = Read(Production, (ReleaseScope.ModulesKey, " Kings,fate "));
        Assert.Equal([ExpansionModules.Fate, ExpansionModules.Kings], scope.Modules);
        Assert.True(scope.OffersExpansions);
        Assert.Equal([ExpansionModules.Fate, ExpansionModules.Lords],
            Read(Production, (ReleaseScope.ModulesKey, "LORDS fate; fate")).Modules);
    }

    [Fact]
    public void TheExpansionsSwitchStaysAnAliasForEveryModuleOrNone()
    {
        Assert.Equal(ExpansionModules.All, Read(Production, (ReleaseScope.ExpansionsKey, "true")).Modules);
        var none = Read(Development, (ReleaseScope.ExpansionsKey, "false"));
        Assert.Empty(none.Modules);
        Assert.False(none.OffersExpansions);
    }

    [Fact]
    public void TheModuleListWinsOverTheExpansionsSwitchAndAnEmptyListCountsAsUnset()
    {
        Assert.Equal([ExpansionModules.Fate],
            Read(Production, (ReleaseScope.ModulesKey, "fate"), (ReleaseScope.ExpansionsKey, "true")).Modules);
        Assert.Equal([ExpansionModules.Lords],
            Read(Development, (ReleaseScope.ModulesKey, "lords"), (ReleaseScope.ExpansionsKey, "false")).Modules);
        // compose passes an empty WOTR_RELEASE_MODULES by default: the switch, or the environment, decides.
        Assert.Equal(ExpansionModules.All,
            Read(Production, (ReleaseScope.ModulesKey, ""), (ReleaseScope.ExpansionsKey, "true")).Modules);
        Assert.Empty(Read(Development, (ReleaseScope.ModulesKey, " "), (ReleaseScope.ExpansionsKey, "false")).Modules);
        Assert.Empty(Read(Production, (ReleaseScope.ModulesKey, "")).Modules);
    }

    [Theory]
    [InlineData("mordor")]
    [InlineData("fate, warriors")]
    [InlineData("fate;base")]
    public void AnUnknownModuleStopsTheServer(string modules)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Read(Production, (ReleaseScope.ModulesKey, modules)));
        Assert.Contains(ReleaseScope.ModulesKey, error.Message);
    }

    // Program resolves the scope before it serves anything, as it does the other options.
    [PostgresFact]
    public void AnUnknownModuleStopsTheServerAtStart()
    {
        using var host = new AccountHost(extra: new Dictionary<string, string?> { [ReleaseScope.ModulesKey] = "fate, mordor" });
        var error = Assert.ThrowsAny<Exception>(() => host.CreateClient());
        Assert.Contains("mordor", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutAReleaseSettingProductionReleasesNoModuleAndOtherEnvironmentsAll()
    {
        Assert.Empty(Read(Production).Modules);
        Assert.Equal(ExpansionModules.All, Read(Development).Modules);
        Assert.Equal(ExpansionModules.All, Read("Testing").Modules);
    }

    [Fact]
    public void AConfigurationIsOfferedWhenEveryModuleItUsesIsReleased()
    {
        var fate = new ReleaseScope([ExpansionModules.Fate]);
        Assert.True(fate.Offers(Normalized(new())));
        Assert.True(fate.Offers(Normalized(new() { Fate = true, NewCities = true })));
        Assert.False(fate.Offers(Normalized(new() { Fate = true, Kings = true })));
        Assert.False(fate.Offers(Normalized(new() { Lords = true, Council = true })));
        var lordsAndKings = new ReleaseScope([ExpansionModules.Lords, ExpansionModules.Kings]);
        Assert.True(lordsAndKings.Offers(Normalized(new() { Lords = true, Kings = true, RemovingRulerTokens = true })));
        Assert.False(lordsAndKings.Offers(Normalized(new() { Lords = true, Fate = true })));
        // A variant without its module is refused by ConfigurationRules, so no release offers it.
        Assert.False(ReleaseScope.Full.Offers(Normalized(new() { Council = true })));
    }

    // EXP-10: each module is released on its own. For every set of released modules and every one of the 512 flag
    // combinations, a configuration is offered exactly when it is legal and each expansion it turns on is released.
    [Fact]
    public void EveryReleaseOffersExactlyTheLegalConfigurationsOfItsOwnModules()
    {
        for (var release = 0; release < 8; release++)
        {
            var modules = ExpansionModules.All.Where((_, index) => (release & (1 << index)) != 0).ToList();
            var scope = new ReleaseScope(modules);
            for (var flags = 0; flags < 512; flags++)
            {
                var configuration = ExpansionConfigurations.FromFlags(flags);
                var used = new[]
                {
                    (configuration.Fate, ExpansionModules.Fate), (configuration.Lords, ExpansionModules.Lords),
                    (configuration.Kings, ExpansionModules.Kings)
                }.Where(m => m.Item1).Select(m => m.Item2);
                var expected = ExpansionConfigurations.Allowed(flags) && used.All(modules.Contains);
                Assert.True(expected == scope.Offers(Normalized(configuration)),
                    $"release [{string.Join(",", modules)}], flags {flags}: expected offered {expected}.");
            }
        }
    }

    [Fact]
    public void ANamedRoomTakesAReleasedModuleAndRefusesAnUnreleasedOne()
    {
        var fate = new ReleaseScope([ExpansionModules.Fate]);
        var room = NamedRoom();
        Assert.Equal("ExpansionsUnavailable", RoomRules.Configure(room, Request(1, new() { Kings = true }), fate));
        Assert.Equal(1, room.Version);
        Assert.Null(RoomRules.Configure(room, Request(1, new() { Fate = true, NewCities = true }), fate));
        Assert.Equal((2, true, true), (room.Version, room.Configuration.Fate, room.Configuration.NewCities));
        var view = room.View(room.HostAccount, fate, [], 16);
        Assert.Equal([ExpansionModules.Fate], view.OfferedModules);
        Assert.True(view.ExpansionsAvailable);
        Assert.Empty(view.StartUnavailableReason);
    }

    [Fact]
    public void ARoomConfiguredBeforeTheReleaseNarrowedCannotStart()
    {
        var room = NamedRoom();
        Assert.Null(RoomRules.Configure(room, Request(1, new() { Fate = true, Lords = true }), ReleaseScope.Full));
        Assert.Null(RoomRules.Confirm(room, room.FreePeoples, 2));
        Assert.Null(RoomRules.Confirm(room, room.Shadow, 2));
        Assert.Null(RoomRules.CanStart(room, 2, ReleaseScope.Full));
        var fate = new ReleaseScope([ExpansionModules.Fate]);
        Assert.Equal("ExpansionsUnavailable", RoomRules.CanStart(room, 2, fate));
        Assert.Equal("ExpansionsUnavailable", room.View(room.HostAccount, fate, [], 16).StartUnavailableReason);
        Assert.True(room.Configuration.Lords);
    }

    // Wotr__Release__Modules=fate over Wotr__Release__Expansions=false: a named public table takes Fate, not Kings, and
    // the Open games list shows its configuration to another player.
    [PostgresFact]
    public async Task ANamedTableOverHttpTakesTheReleasedModuleAndTheListShowsIt()
    {
        await using var host = new AccountHost(expansions: false,
            extra: new Dictionary<string, string?> { [ReleaseScope.ModulesKey] = "fate" });
        var alice = await MenuAccounts.Register(host);
        var bob = await MenuAccounts.Register(host);
        using var owner = MenuAccounts.Client(host, alice);
        using var viewer = MenuAccounts.Client(host, bob);
        var created = await owner.PostAsJsonAsync("/api/rooms", new CreateRoomRequest
        {
            Name = "EXP-01 table", Visibility = RoomVisibility.Public, Side = Side.FreePeoples
        }, BoardCatalog.Json);
        var json = await created.Content.ReadAsStringAsync();
        Assert.Contains("\"offeredModules\":[\"fate\"]", json, StringComparison.Ordinal);
        var room = System.Text.Json.JsonSerializer.Deserialize<RoomView>(json, BoardCatalog.Json)!;
        Assert.True(room.Managed);
        Assert.True(room.ExpansionsAvailable);

        using var kings = await Configure(owner, room, new() { Kings = true });
        Assert.Equal(HttpStatusCode.Conflict, kings.StatusCode);
        Assert.Equal("ExpansionsUnavailable", (await kings.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        using var fate = await Configure(owner, room, new() { Fate = true, NewCities = true });
        Assert.Equal(HttpStatusCode.OK, fate.StatusCode);
        Assert.True((await fate.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!.Configuration.Fate);

        var summary = await Listed(viewer, room.Id);
        Assert.Equal((true, true, false), (summary.Configuration.Fate, summary.Configuration.NewCities, summary.Configuration.Kings));
    }

    private static Task<HttpResponseMessage> Configure(HttpClient client, RoomView room, MatchConfiguration configuration) =>
        client.PutAsJsonAsync($"/api/rooms/{room.Id}/configuration",
            new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = configuration }, BoardCatalog.Json);

    // Other test classes share the database, so the room is looked for page by page.
    private static async Task<PublicRoomSummary> Listed(HttpClient client, string roomId)
    {
        var cursor = "";
        do
        {
            var page = (await client.GetFromJsonAsync<PublicRoomListView>(
                "/api/rooms/public?limit=50&cursor=" + Uri.EscapeDataString(cursor), BoardCatalog.Json))!;
            var room = page.Rooms.Find(summary => summary.Id == roomId);
            if (room is not null)
                return room;
            cursor = page.NextCursor;
        } while (cursor.Length > 0);
        throw new InvalidOperationException("The public room list does not show the room.");
    }

    private static ReleaseScope Read(string environment, params (string Key, string Value)[] settings)
    {
        var values = settings.ToDictionary(setting => setting.Key, setting => (string?)setting.Value);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return ReleaseScope.Read(configuration, new TestEnvironment(environment));
    }

    private static MatchConfiguration Normalized(MatchConfiguration configuration) =>
        ConfigurationRules.Normalize(configuration);

    private static ConfigureRoomRequest Request(int version, MatchConfiguration configuration) =>
        new() { ExpectedVersion = version, Configuration = configuration };

    // A named table with both seats taken, as a host and a guest leave it in the waiting room.
    private static RoomState NamedRoom() => new()
    {
        Id = "room",
        Name = "Named table",
        HostAccount = "host",
        HostName = "Host",
        Managed = true,
        FreePeoples = "host",
        FreePeoplesName = "Host",
        Shadow = "guest",
        ShadowName = "Guest"
    };

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Wotr.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
