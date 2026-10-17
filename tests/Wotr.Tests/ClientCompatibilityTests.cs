using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

// DS11: compatible, too old, too new, missing and malformed client identities over HTTP and WSS.
public class ClientCompatibilityTests
{
    public static TheoryData<string?, string?, HttpStatusCode, string?> Matrix => new()
    {
        { "0.3.0", "2", HttpStatusCode.OK, null },
        { "0.5.0-rc.1", "2", HttpStatusCode.OK, null },
        { "0.2.9", "2", HttpStatusCode.Conflict, ClientCompatibility.ClientUpdateRequired },
        { "0.3.0-rc.1", "2", HttpStatusCode.Conflict, ClientCompatibility.ClientUpdateRequired },
        { "0.3.0", "1", HttpStatusCode.Conflict, ClientCompatibility.ClientUpdateRequired },
        { "0.3.0", "3", HttpStatusCode.Conflict, ClientCompatibility.ServerUpdateRequired },
        { null, null, HttpStatusCode.Conflict, ClientCompatibility.ClientUpdateRequired },
        { "0.3.0", null, HttpStatusCode.BadRequest, ClientCompatibility.InvalidClientVersion },
        { "three", "2", HttpStatusCode.BadRequest, ClientCompatibility.InvalidClientVersion },
        { "0.3.0", "+2", HttpStatusCode.BadRequest, ClientCompatibility.InvalidClientVersion }
    };

    [PostgresTheory]
    [MemberData(nameof(Matrix))]
    public async Task GameRequestsAndSocketsFollowTheCompatibilityMatrix(string? version, string? protocol, HttpStatusCode expected, string? error)
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        await using var host = new TestHost(settings: releases.ServerSettings());
        var state = KernelTests.NewGame();
        state.Id = Guid.NewGuid().ToString("N");
        await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        using var alice = ReleaseFixture.Versioned(host.Client("alice"), version, protocol);

        using var view = await alice.GetAsync($"/api/matches/{state.Id}");
        Assert.Equal(expected, view.StatusCode);
        if (error is not null)
        {
            Assert.Equal(error, (await view.Content.ReadFromJsonAsync<ApiError>())!.Error);
            Assert.Equal("no-store", view.Headers.CacheControl!.ToString());
        }
        using var login = await alice.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = "nobody", Password = "not a real password" });
        Assert.Equal(error is null ? HttpStatusCode.Unauthorized : expected, login.StatusCode);

        var socket = host.Server.CreateWebSocketClient();
        socket.ConfigureRequest = request =>
        {
            request.Headers.Authorization = "Bearer alice";
            if (version is not null)
                request.Headers[ClientCompatibility.ClientVersionHeader] = version;
            if (protocol is not null)
                request.Headers[ClientCompatibility.ProtocolVersionHeader] = protocol;
        };
        var uri = new Uri($"wss://localhost/api/matches/{state.Id}/updates");
        if (error is null)
            (await socket.ConnectAsync(uri, CancellationToken.None)).Abort();
        else
            Assert.Contains(((int)expected).ToString(), (await Assert.ThrowsAsync<InvalidOperationException>(() => socket.ConnectAsync(uri, CancellationToken.None))).Message);

        // Health, the catalog and logout never require a compatible client.
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync("/api/client/releases?platform=windows-x64")).StatusCode);
        Assert.NotEqual(HttpStatusCode.Conflict, (await alice.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [PostgresFact]
    public async Task RepeatedHeadersAreInvalidAndTheTransitionAcceptsOnlyUnversionedOldClients()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        await using (var strict = new TestHost(settings: releases.ServerSettings()))
        {
            using var repeated = strict.Client("alice");
            repeated.DefaultRequestHeaders.Add(ClientCompatibility.ClientVersionHeader, ["0.3.0", "0.4.0"]);
            repeated.DefaultRequestHeaders.Add(ClientCompatibility.ProtocolVersionHeader, "2");
            Assert.Equal(HttpStatusCode.BadRequest, (await repeated.GetAsync("/api/rooms")).StatusCode);
        }
        await using var transition = new TestHost(settings: releases.ServerSettings(acceptUnversioned: true));
        using var zip = transition.Client("alice");
        Assert.Equal(HttpStatusCode.OK, (await zip.GetAsync("/api/rooms")).StatusCode);
        using var old = ReleaseFixture.Versioned(transition.Client("alice"), "0.2.0");
        Assert.Equal(HttpStatusCode.Conflict, (await old.GetAsync("/api/rooms")).StatusCode);
    }

    [PostgresFact]
    public async Task RejectedCommandChangesNoStateOrRandomnessAndAMinimumChangeKeepsMatchesAndReceipts()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        var state = State();
        state.Id = Guid.NewGuid().ToString("N");
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = Execute(Begin(state, [attacker.Id]), CommandKind.CommitCombatCard);
        var owner = state.Pending!.Owner == Side.FreePeoples ? "alice" : "bob";
        var rng = new SequenceRandom(0, 0, 0, 0, 0, 0);
        await using var host = new TestHost(rng, settings: releases.ServerSettings());
        await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        using var current = ReleaseFixture.Versioned(host.Client(owner), "0.3.0");
        using var outdated = ReleaseFixture.Versioned(host.Client(owner), "0.2.0");
        using var future = ReleaseFixture.Versioned(host.Client(owner), "0.3.0", "3");
        var before = await View(current, state.Id);
        var roll = Choice(before, CommandKind.CommitCombatCard);

        foreach (var client in new[] { outdated, future })
        {
            using var response = await client.PostAsJsonAsync($"/api/matches/{state.Id}/commands", roll, BoardCatalog.Json);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        Assert.Equal(0, rng.Calls);
        Assert.Equal(before.Revision, (await View(current, state.Id)).Revision);
        Assert.Equal(HttpStatusCode.NotFound, (await current.GetAsync($"/api/matches/{state.Id}/receipts/{roll.CommandId}")).StatusCode);

        var receipt = await Send(current, roll);
        Assert.True(receipt.Accepted, receipt.Error);
        var draws = rng.Calls;
        Assert.True(draws > 0);

        // Raising the minimum later leaves the match, its receipt and its random samples intact for the new client.
        releases.Publisher.SetMinimum("0.4.0");
        Assert.Equal(HttpStatusCode.Conflict, (await current.GetAsync($"/api/matches/{state.Id}/receipts/{roll.CommandId}")).StatusCode);
        using var updated = ReleaseFixture.Versioned(host.Client(owner), "0.4.0");
        var recovered = await updated.GetFromJsonAsync<CommandReceipt>($"/api/matches/{state.Id}/receipts/{roll.CommandId}", BoardCatalog.Json);
        Assert.Equal(receipt.Revision, recovered!.Revision);
        Assert.Equal(receipt.Revision, (await View(updated, state.Id)).Revision);
        Assert.Equal(receipt.Revision, (await Send(updated, roll)).Revision);
        Assert.Equal(draws, rng.Calls);
    }

    [PostgresFact]
    public async Task AccountsAndSessionsSurviveAClientMinimumChange()
    {
        using var releases = new ReleaseFixture();
        releases.Publish("0.3.0", minimum: true);
        releases.Publish("0.4.0");
        using var host = new AccountHost(extra: releases.ServerSettings());
        using var current = ReleaseFixture.Versioned(host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }), "0.3.0");
        var name = "dist05_" + Guid.NewGuid().ToString("N")[..20];
        var request = new RegisterRequest { Username = name, Email = TestAccounts.Email(name), Password = "a sufficiently long password" };
        using var registered = await current.PostAsJsonAsync("/api/auth/register", request);
        var session = (await registered.Content.ReadFromJsonAsync<SessionView>())!;
        ReleaseFixture.Authorize(current, session.Token);
        using var created = await current.PostAsJsonAsync("/api/rooms", new CreateRoomRequest { Name = "Release table", Side = Side.FreePeoples });
        var room = (await created.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;

        releases.Publisher.SetMinimum("0.4.0");
        Assert.Equal(HttpStatusCode.Conflict, (await current.GetAsync("/api/rooms")).StatusCode);
        using var refused = await current.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = request.Username, Password = request.Password });
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        using var updated = ReleaseFixture.Versioned(host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }), "0.4.0");
        ReleaseFixture.Authorize(updated, session.Token);
        Assert.Equal(room.Id, (await updated.GetFromJsonAsync<RoomView>($"/api/rooms/{room.Id}", BoardCatalog.Json))!.Id);
        updated.DefaultRequestHeaders.Authorization = null;
        using var login = await updated.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = request.Username, Password = request.Password });
        Assert.Equal(session.AccountId, (await login.Content.ReadFromJsonAsync<SessionView>())!.AccountId);
    }

    private static async Task<PlayerView> View(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;

    private static GameCommand Choice(PlayerView view, CommandKind kind) => new()
    {
        MatchId = view.MatchId,
        DecisionId = view.Decision!.Id,
        ExpectedRevision = view.Revision,
        CommandId = Guid.NewGuid().ToString("N"),
        Kind = kind
    };

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }
}
