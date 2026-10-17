using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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

// services: test replacements registered after the server's own (AUTH-02: a recording email sender, a clock).
public sealed class AccountHost(string? connectionString = null, bool? expansions = null, IReadOnlyDictionary<string, string?>? extra = null,
    Action<IServiceCollection>? services = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Tests register many accounts from one address; AUTH-02's email budget per address is tested on its own.
        var settings = new Dictionary<string, string?> { [EmailOptions.PermitsPerAddressKey] = "1000" };
        foreach (var setting in extra ?? new Dictionary<string, string?>())
            settings[setting.Key] = setting.Value;
        settings["WOTR_DB"] = connectionString ?? PostgresTests.Connection;
        if (expansions.HasValue)
            settings[ReleaseScope.ConfigurationKey] = expansions.Value ? "true" : "false";
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        if (services is not null)
            builder.ConfigureTestServices(services);
    }
}

public class LobbyTests
{
    private const string Password = "A long test password!";

    [Fact]
    public void EveryExplicitConfigurationCombinationHasExpectedLegality()
    {
        var valid = 0;
        for (var bits = 0; bits < 512; bits++)
        {
            bool Flag(int bit) => (bits & (1 << bit)) != 0;
            var configuration = new MatchConfiguration
            {
                Lords = Flag(0),
                Kings = Flag(1),
                Fate = Flag(2),
                Council = Flag(3),
                Dismay = Flag(4),
                VisibleCorruption = Flag(5),
                SequentialCorruption = Flag(6),
                RemovingRulerTokens = Flag(7),
                NewCities = Flag(8)
            };
            var expected = !(Flag(3) && !Flag(0)) && !(Flag(4) && (!Flag(0) || !Flag(1))) &&
                !((Flag(5) || Flag(6)) && !Flag(1)) && !(Flag(7) && (!Flag(0) || !Flag(1) || !Flag(4))) &&
                !(Flag(8) && !Flag(2));
            Assert.Equal(expected, ConfigurationRules.IsValid(ConfigurationRules.Normalize(configuration)));
            if (expected)
                valid++;
        }
        // 31 legal Lords/Kings/variant combinations × 3 Fate/New Cities combinations.
        Assert.Equal(93, valid);
        Assert.True(ConfigurationRules.Normalize(new()
        {
            Lords = true,
            Kings = true
        }).Dismay);
        Assert.False(ConfigurationRules.Normalize(new()
        {
            Lords = true,
            Kings = true,
            Dismay = false
        }).Dismay);
        Assert.False(ConfigurationRules.Normalize(new()).Dismay);
    }

    [PostgresFact]
    public async Task OneNameAndOneAddressMakeOneAccountAndSessionsExpireAndCanBeRevoked()
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var database = new Database(data);
        await database.Migrate();
        var accounts = AccountService.WithoutEmail(database);
        var sessions = new SessionStore(database);
        var name = Name();
        var registrations = await Task.WhenAll(
            accounts.Register(new() { Username = name, Email = TestAccounts.Email(name), Password = Password }),
            accounts.Register(new() { Username = name.ToUpperInvariant(), Email = TestAccounts.Email(Name()), Password = Password }));
        var registered = Assert.Single(registrations, result => result.Session is not null).Session!;
        Assert.Equal(RegistrationError.UsernameTaken, Assert.Single(registrations, result => result.Session is null).Error);
        var sameAddress = await accounts.Register(new() { Username = Name(), Email = TestAccounts.Email(name).ToUpperInvariant(), Password = Password });
        Assert.Equal(RegistrationError.EmailTaken, sameAddress.Error);
        Assert.Equal(registered.AccountId, await sessions.Resolve(registered.Token));
        Assert.Null(await accounts.Login(new()
        {
            Username = registered.Username,
            Password = "wrong password"
        }));
        var login = await accounts.Login(new()
        {
            Username = registered.Username.ToUpperInvariant(),
            Password = Password
        });
        Assert.NotNull(login);
        await sessions.Revoke(registered.Token);
        Assert.Null(await sessions.Resolve(registered.Token));
        Assert.Equal(registered.AccountId, await sessions.Resolve(login.Token));
        await using var expire = data.CreateCommand("UPDATE sessions SET expires_at=now()-interval '1 second' WHERE token_hash=$1");
        expire.Parameters.AddWithValue(SessionStore.Hash(login.Token));
        await expire.ExecuteNonQueryAsync();
        Assert.Null(await sessions.Resolve(login.Token));
    }

    [PostgresFact]
    public async Task SeatsAndStartAreAtomicAndConfigurationInvalidatesBothConfirmations()
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var database = new Database(data);
        await database.Migrate();
        var accounts = AccountService.WithoutEmail(database);
        var alice = await Register(accounts);
        var bob = await Register(accounts);
        var charlie = await Register(accounts);
        var rooms = new RoomStore(database, new ServerRandom());
        var room = await rooms.Create(alice.AccountId, Side.FreePeoples);
        var joined = await Task.WhenAll(rooms.Join(room.JoinCode, bob.AccountId), rooms.Join(room.JoinCode, charlie.AccountId));
        Assert.Single(joined, result => result.Room is not null);
        Assert.Single(joined, result => result.Error == "RoomFull");
        var opponent = joined[0].Room is not null ? bob : charlie;
        var outsider = joined[0].Room is not null ? charlie : bob;
        Assert.Null(await rooms.Read(room.Id, outsider.AccountId));
        Assert.Empty(await rooms.List(outsider.AccountId));
        Assert.Equal("RoomNotFound", (await rooms.Confirm(room.Id, outsider.AccountId, 1)).Error);
        await rooms.Confirm(room.Id, alice.AccountId, 1);
        var changed = await rooms.Configure(room.Id, opponent.AccountId, new()
        {
            ExpectedVersion = 1,
            Configuration = new()
        });
        Assert.Equal(2, changed.Room!.Version);
        Assert.False(changed.Room.FreePeoplesAccepted);
        Assert.Equal("StaleConfiguration", (await rooms.Confirm(room.Id, alice.AccountId, 1)).Error);
        Assert.Equal("ConfirmationRequired", (await rooms.Start(room.Id, alice.AccountId, 2)).Error);
        await rooms.Confirm(room.Id, alice.AccountId, 2);
        await rooms.Confirm(room.Id, opponent.AccountId, 2);
        var starts = await Task.WhenAll(rooms.Start(room.Id, alice.AccountId, 2), rooms.Start(room.Id, opponent.AccountId, 2));
        var match = starts[0].Room!.MatchId;
        Assert.NotEmpty(match);
        Assert.Equal(match, starts[1].Room!.MatchId);
        var store = new MatchStore(database, new GameEngine(), new ServerRandom());
        Assert.NotNull(await store.Read(match, alice.AccountId));
        Assert.Null(await store.Read(match, outsider.AccountId));
        Assert.Equal("MatchAlreadyStarted", (await rooms.Configure(room.Id, alice.AccountId, new()
        {
            ExpectedVersion = 2
        })).Error);
    }

    [PostgresFact]
    public async Task InvalidConfigurationIsRejectedAndAcceptedExpansionsStartWithTheirSetup()
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var database = new Database(data);
        await database.Migrate();
        var accounts = AccountService.WithoutEmail(database);
        var alice = await Register(accounts);
        var bob = await Register(accounts);
        var rooms = new RoomStore(database, new ServerRandom());
        var room = await rooms.Create(alice.AccountId, Side.Shadow);
        await rooms.Join(room.JoinCode, bob.AccountId);
        Assert.Equal("InvalidConfiguration", (await rooms.Configure(room.Id, alice.AccountId,
            new()
            {
                ExpectedVersion = 1,
                Configuration = new()
                {
                    Council = true
                }
            })).Error);
        var changed = await rooms.Configure(room.Id, alice.AccountId,
            new()
            {
                ExpectedVersion = 1,
                Configuration = new()
                {
                    Lords = true,
                    Kings = true
                }
            });
        Assert.True(changed.Room!.Configuration.Dismay);
        Assert.Empty(changed.Room.StartUnavailableReason);
        await rooms.Confirm(room.Id, alice.AccountId, 2);
        await rooms.Confirm(room.Id, bob.AccountId, 2);
        var started = await rooms.Start(room.Id, alice.AccountId, 2);
        Assert.Empty(started.Error);
        var match = await new MatchStore(database, new GameEngine(), new ServerRandom()).Read(started.Room!.MatchId, bob.AccountId);
        Assert.True(match!.State.Configuration.Lords);
        Assert.True(match.State.Configuration.Kings);
        Assert.Equal(DecisionKind.CouncilVersions, match.State.Pending!.Kind);
    }

    [PostgresFact]
    public async Task HttpLoginLogoutEnforcesRealSessionAndAccountInvitationsAreGone()
    {
        await using var host = new AccountHost();
        using var client = host.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
        var name = Name();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = name,
            Email = TestAccounts.Email(name),
            Password = Password
        });
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<SessionView>(BoardCatalog.Json))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        // AUTH-02: account invitations are gone, also for administrators.
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/invitations", null)).StatusCode);
        var created = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Side = Side.Shadow }, BoardCatalog.Json);
        created.EnsureSuccessStatusCode();
        var room = (await created.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.Equal(Side.Shadow, room.You);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/rooms")).StatusCode);
    }

    [Fact]
    public void ProductionOffersOnlyTheBaseGameUnlessExpansionsAreEnabled()
    {
        var empty = new ConfigurationBuilder().Build();
        Assert.False(ReleaseScope.Read(empty, new TestEnvironment("Production")).Expansions);
        Assert.True(ReleaseScope.Read(empty, new TestEnvironment("Development")).Expansions);
        Assert.True(ReleaseScope.Read(empty, new TestEnvironment("Testing")).Expansions);
        var enabled = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [ReleaseScope.ConfigurationKey] = "true" }).Build();
        Assert.True(ReleaseScope.Read(enabled, new TestEnvironment("Production")).Expansions);
        Assert.True(ReleaseScope.BaseOnly.Offers(ConfigurationRules.Normalize(new())));
        foreach (var expansion in new[]
        {
            new MatchConfiguration { Lords = true }, new MatchConfiguration { Kings = true }, new MatchConfiguration { Fate = true },
            new MatchConfiguration { Lords = true, Council = true }, new MatchConfiguration { Fate = true, NewCities = true }
        })
        {
            Assert.False(ReleaseScope.BaseOnly.Offers(ConfigurationRules.Normalize(expansion)));
            Assert.True(ReleaseScope.Full.Offers(ConfigurationRules.Normalize(expansion)));
        }
    }

    [PostgresFact]
    public async Task BaseOnlyReleaseRejectsExpansionTablesAndStartsOnlyTheBaseGame()
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var database = new Database(data);
        await database.Migrate();
        var accounts = AccountService.WithoutEmail(database);
        var alice = await Register(accounts);
        var bob = await Register(accounts);
        var full = new RoomStore(database, new ServerRandom());
        var baseOnly = new RoomStore(database, new ServerRandom(), ReleaseScope.BaseOnly);
        // A table set up with Lords before the release narrowed keeps its settings but cannot start.
        var early = await full.Create(alice.AccountId, Side.FreePeoples);
        await full.Join(early.JoinCode, bob.AccountId);
        await full.Configure(early.Id, alice.AccountId, new() { ExpectedVersion = 1, Configuration = new() { Lords = true } });
        await full.Confirm(early.Id, alice.AccountId, 2);
        await full.Confirm(early.Id, bob.AccountId, 2);
        Assert.True((await full.Read(early.Id, alice.AccountId))!.ExpansionsAvailable);
        var seen = (await baseOnly.Read(early.Id, alice.AccountId))!;
        Assert.False(seen.ExpansionsAvailable);
        Assert.Equal("ExpansionsUnavailable", seen.StartUnavailableReason);
        Assert.Equal("ExpansionsUnavailable", (await baseOnly.Start(early.Id, alice.AccountId, 2)).Error);
        Assert.Equal("ExpansionsUnavailable", (await baseOnly.Configure(early.Id, bob.AccountId,
            new() { ExpectedVersion = 2, Configuration = new() { Kings = true } })).Error);
        var reset = await baseOnly.Configure(early.Id, bob.AccountId, new() { ExpectedVersion = 2, Configuration = new() });
        Assert.Empty(reset.Room!.StartUnavailableReason);
        await baseOnly.Confirm(early.Id, alice.AccountId, 3);
        await baseOnly.Confirm(early.Id, bob.AccountId, 3);
        var started = await baseOnly.Start(early.Id, alice.AccountId, 3);
        Assert.Empty(started.Error);
        var match = await new MatchStore(database, new GameEngine(), new ServerRandom()).Read(started.Room!.MatchId, bob.AccountId);
        Assert.True(ConfigurationRules.IsBase(match!.State.Configuration));
        Assert.Empty(new Projection().For(match.State, Side.Shadow).Sovereigns);
    }

    [PostgresFact]
    public async Task HttpRoomRejectsExpansionsWhenTheReleaseOffersOnlyTheBaseGame()
    {
        await using var host = new AccountHost(expansions: false);
        using var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var accounts = host.Services.GetRequiredService<AccountService>();
        var session = await Register(accounts);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        var created = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, BoardCatalog.Json);
        var room = (await created.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
        Assert.False(room.ExpansionsAvailable);
        var rejected = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/configuration",
            new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = new() { Lords = true } }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal("ExpansionsUnavailable", (await rejected.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error);
        var accepted = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/configuration",
            new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = new() }, BoardCatalog.Json);
        accepted.EnsureSuccessStatusCode();
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Wotr.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static string Name() => "u" + Guid.NewGuid().ToString("N")[..20];
    private static Task<SessionView> Register(AccountService accounts) => TestAccounts.Register(accounts, Name(), Password);
}
