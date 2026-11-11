using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

[CollectionDefinition(Name)]
public sealed class BotApiCollection : ICollectionFixture<BotApiFixture>
{
    public const string Name = "Bot API isolated PostgreSQL";
}

// Real TCP HTTP/WebSocket against Kestrel, with a private temporary PostgreSQL cluster.
// Test seeding alone can mint a normal session for the same non-login participant.
public sealed class BotApiFixture : IAsyncLifetime
{
    private readonly IsolatedPostgres postgres = new();
    private KestrelProcess server = null!;
    private AccountHost seed = null!;
    public string ControlToken { get; } = BotTokens.ControlPrefix + SessionStore.NewToken();
    public string Connection => postgres.Connection;
    public Database Database => seed.Services.GetRequiredService<Database>();
    public BotCredentialStore Credentials => seed.Services.GetRequiredService<BotCredentialStore>();
    public MatchStore Matches => seed.Services.GetRequiredService<MatchStore>();
    public RoomStore Rooms => seed.Services.GetRequiredService<RoomStore>();

    public Dictionary<string, string?> Settings() => new()
    {
        ["Wotr:Bots:Enabled"] = "true",
        ["Wotr:Bots:ServiceId"] = "test-bot-service",
        ["Wotr:Bots:ControlTokenHash"] = SessionStore.Hash(ControlToken),
        ["Wotr:Bots:MinimumVersion"] = "0.1.0",
        ["Wotr:Bots:SeatLifetimeSeconds"] = "300",
        ["Wotr:Bots:Features"] = "base"
    };

    public async Task InitializeAsync()
    {
        await postgres.Start();
        seed = new AccountHost(postgres.Connection, extra: Settings());
        _ = Database; // Migrate with the ordinary application startup before seeding.
        server = await KestrelProcess.Start(postgres.Connection, Settings());
    }

    internal async Task<BotTestSeat> Seat(MatchState? state = null, Side side = Side.Shadow)
    {
        var grant = Guid.NewGuid().ToString("N");
        var participant = BotCredentialStore.Participant(grant);
        state ??= BattleFixture.State(side);
        state.Id = Guid.NewGuid().ToString("N");
        await Matches.Create(state, side == Side.FreePeoples ? participant : "fixture-opponent",
            side == Side.Shadow ? participant : "fixture-opponent");
        await Credentials.Register(grant, null, state.Id, side, DateTimeOffset.UtcNow.AddMinutes(20));
        return new(grant, participant, state, side, await Credentials.Issue(grant, 1), await HumanSession(participant));
    }

    internal async Task<BotTestRoom> Room(Side side = Side.Shadow)
    {
        var accounts = seed.Services.GetRequiredService<AccountService>();
        var host = await TestAccounts.Register(accounts, "host" + Guid.NewGuid().ToString("N")[..12], "Bot fixture password!");
        var room = await Rooms.Create(host.AccountId, new CreateRoomRequest
        {
            Side = side == Side.Shadow ? Side.FreePeoples : Side.Shadow,
            Name = "Bot authorization fixture", Visibility = RoomVisibility.Public
        });
        var grant = Guid.NewGuid().ToString("N");
        await Credentials.Register(grant, room.Id, null, side, DateTimeOffset.UtcNow.AddMinutes(20));
        var participant = BotCredentialStore.Participant(grant);
        var joined = await Rooms.JoinPublic(room.Id, participant);
        Assert.Equal("", joined.Error);
        return new(grant, participant, joined.Room!, await Credentials.Issue(grant, 1), await HumanSession(participant), host);
    }

    private async Task<SessionView> HumanSession(string participant)
    {
        await using var connection = await Database.Source.OpenConnectionAsync();
        return await SessionStore.Issue(connection, null, participant, "CPU", CancellationToken.None);
    }

    public HttpClient Client(string token, bool bot = true)
    {
        var client = server.Client();
        Authorize(client, token, bot);
        return client;
    }

    internal static void Authorize(HttpClient client, string token, bool bot = true)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        foreach (var header in Headers(bot))
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
    }

    internal static Dictionary<string, string> Headers(bool bot = true)
    {
        var headers = new Dictionary<string, string> { [ClientCompatibility.ProtocolVersionHeader] = Protocol.Version.ToString() };
        if (bot)
        {
            headers[BotCompatibility.ApplicationHeader] = "0.1.0";
            headers[BotCompatibility.ProtocolHeader] = BotCompatibility.Version.ToString();
            headers[BotCompatibility.RulesHeader] = Protocol.RulesVersion;
            headers[BotCompatibility.ContentHeader] = Protocol.ContentVersion;
        }
        else
            headers[ClientCompatibility.ClientVersionHeader] = "0.1.0";
        return headers;
    }

    public async Task<ClientWebSocket> Socket(string token, string match, bool bot = true, string? path = null,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        using var client = server.Client();
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", "Bearer " + token);
        foreach (var header in Headers(bot))
            socket.Options.SetRequestHeader(header.Key, header.Value);
        foreach (var header in overrides ?? new Dictionary<string, string>())
            socket.Options.SetRequestHeader(header.Key, header.Value);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var uri = new UriBuilder(new Uri(client.BaseAddress!, path ?? $"/api/matches/{match}/updates")) { Scheme = "ws" }.Uri;
            await socket.ConnectAsync(uri, timeout.Token);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static async Task<(WebSocketMessageType Type, string Text)> Receive(WebSocket socket)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[8192];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
        Assert.True(result.EndOfMessage);
        return (result.MessageType, Encoding.UTF8.GetString(buffer, 0, result.Count));
    }

    public async Task Sql(string sql, params object[] parameters)
    {
        await using var connection = await Database.Source.OpenConnectionAsync();
        await using var command = Wotr.Server.Database.Command(connection, null, sql, parameters);
        await command.ExecuteNonQueryAsync();
    }

    public async Task WaitForLock(string queryFragment)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var connection = await Database.Source.OpenConnectionAsync(timeout.Token);
        while (true)
        {
            await using var query = Wotr.Server.Database.Command(connection, null,
                "SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid<>pg_backend_pid() AND wait_event_type='Lock' AND query LIKE $1)",
                "%" + queryFragment + "%");
            if (await query.ExecuteScalarAsync(timeout.Token) is true)
                return;
            await Task.Delay(20, timeout.Token);
        }
    }

    public static GameCommand Move(BotTestSeat seat)
    {
        var origin = seat.Side == Side.Shadow ? "morannon" : "minas-tirith";
        var target = seat.Side == Side.Shadow ? "dagorlad" : "lossarnach";
        var unit = seat.State.Units.First(u => u.Location == origin && u.Kind == UnitKind.Regular);
        return BattleFixture.Command(seat.State, CommandKind.MoveArmies, new()
        {
            DieId = seat.State.Dice.First(d => d.Owner == seat.Side && d.Location == DieLocation.Available).Id,
            Moves = [new() { From = origin, To = target, UnitIds = [unit.Id] }]
        });
    }

    internal async Task<BotTestSeat> MovingSeat(Side side = Side.Shadow)
    {
        var state = BattleFixture.State(side);
        BattleFixture.Put(state, side == Side.Shadow ? "sauron" : "gondor", UnitKind.Regular,
            side == Side.Shadow ? "morannon" : "minas-tirith");
        return await Seat(state, side);
    }

    public static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }

    public async Task DisposeAsync()
    {
        if (server is not null)
            await server.DisposeAsync();
        if (seed is not null)
            await seed.DisposeAsync();
        await postgres.DisposeAsync();
    }
}

public sealed record BotTestSeat(string Grant, string Participant, MatchState State, Side Side, BotSeatSession Session, SessionView Human);
internal sealed record BotTestRoom(string Grant, string Participant, RoomView Room, BotSeatSession Session, SessionView Human, SessionView Host);
