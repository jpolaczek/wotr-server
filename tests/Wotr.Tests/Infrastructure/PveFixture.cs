using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[CollectionDefinition(Name)]
public sealed class PveCollection : ICollectionFixture<PveFixture>
{
    public const string Name = "Saved PvE isolated PostgreSQL";
}

public sealed class PveFixture : IAsyncLifetime
{
    private readonly IsolatedPostgres postgres = new();
    private readonly string control = BotTokens.ControlPrefix + SessionStore.NewToken();
    public AccountHost Host { get; private set; } = null!;
    public Database Database => Host.Services.GetRequiredService<Database>();
    public RoomStore Rooms => Host.Services.GetRequiredService<RoomStore>();
    public BotCredentialStore Credentials => Host.Services.GetRequiredService<BotCredentialStore>();

    private Dictionary<string, string?> Settings() => new(TestBudgets.Stress)
    {
        ["Wotr:Bots:Enabled"] = "true", ["Wotr:Bots:ServiceId"] = "pve-fixture",
        ["Wotr:Bots:ControlTokenHash"] = SessionStore.Hash(control),
        ["Wotr:Bots:RoomsEnabled"] = "true", ["Wotr:Bots:AcceptedPolicyVersion"] = "fixture-1",
        ["Wotr:Bots:Features"] = "base", ["Wotr:Rooms:EmptySeconds"] = "3600"
    };

    public async Task InitializeAsync()
    {
        await postgres.Start();
        Host = new(postgres.Connection, expansions: false, extra: Settings());
        _ = Database;
    }

    public void RestartHost()
    {
        Host.Dispose();
        Host = new(postgres.Connection, expansions: false, extra: Settings());
        _ = Database;
    }

    public Task<SessionView> Human() => MenuAccounts.Register(Host);
    public HttpClient HumanClient(SessionView session) => MenuAccounts.Client(Host, session);
    public HttpClient BotClient(string token)
    {
        var client = Host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        BotApiFixture.Authorize(client, token);
        return client;
    }

    public async Task<RoomView> Room(SessionView human, Side side = Side.FreePeoples) =>
        await Rooms.Create(human.AccountId, new CreateRoomRequest { Name = "Saved PvE fixture", Side = side, Visibility = RoomVisibility.Public });

    public static AddRoomBotRequest Request(RoomView room) => new()
    {
        RequestId = Guid.NewGuid().ToString("N"), ExpectedVersion = room.Version
    };

    public async Task<RoomView> Reserve(SessionView human, RoomView room)
    {
        using var client = HumanClient(human);
        using var response = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/bot", Request(room), BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!;
    }

    // Trusted test-only stand-in for BOT-17 claim. No production claim route is introduced here.
    public async Task<(RoomView Room, string Grant, BotSeatSession Session)> ReadyCpu(RoomView room)
    {
        var grant = (string)(await Scalar("SELECT id FROM bot_assignments WHERE room_id=$1 AND status='pending'", room.Id))!;
        await Credentials.Register(grant, room.Id, null, room.Bot!.Side, DateTimeOffset.UtcNow.AddMinutes(20));
        var session = await Credentials.Issue(grant, 1);
        using var client = BotClient(session.Token);
        using var response = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/ready",
            new RoomReadyRequest { ExpectedVersion = room.Version, Ready = true }, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ((await response.Content.ReadFromJsonAsync<RoomView>(BoardCatalog.Json))!, grant, session);
    }

    public async Task<RoomView> Start(SessionView human, RoomView room)
    {
        var ready = await Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        Assert.Equal("", ready.Error);
        var started = await Rooms.Start(room.Id, human.AccountId, room.Version);
        Assert.Equal("", started.Error);
        return started.Room!;
    }

    public async Task<object?> Scalar(string sql, params object[] arguments)
    {
        await using var connection = await Database.Source.OpenConnectionAsync();
        await using var command = Wotr.Server.Database.Command(connection, null, sql, arguments);
        return await command.ExecuteScalarAsync();
    }

    public async Task WaitForLock(string fragment)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (await Scalar("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE wait_event_type='Lock' AND query LIKE $1)",
            "%" + fragment + "%") is not true)
            await Task.Delay(20, timeout.Token);
    }

    public async Task DisposeAsync()
    {
        Host.Dispose();
        await postgres.DisposeAsync();
    }
}
