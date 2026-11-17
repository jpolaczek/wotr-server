using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// BOT-225 (owner, 2026-11-17): the scripted runtime never plays for the protocol seat. Without the sidecar the CPU's decision
// waits with its error recorded (the seat shows as unavailable, nothing is played); once the protocol bot answers, the same
// decision is played through it and the runtime's memory is never written. BOT-226: the engine is fixed with the seat when
// the CPU joins (AlphaWOTR while the operator runs its sidecar), a seat saved before it keeps the runtime, and a room whose
// CPU is AlphaWOTR is the base game only.
public sealed class BotProtocolSchedulerTests
{
    private static Dictionary<string, string?> Settings(BotPackageFixture package)
    {
        var settings = package.Settings();
        settings["Wotr:Bots:Protocol:Enabled"] = "true";
        return settings;
    }

    [PostgresFact]
    public async Task AnAlphaWotrRoomRecordsItsEngineAndRefusesExpansions()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(Settings(package));
        var (human, room, _) = await fixture.Reserve();
        Assert.Equal(ProtocolSeatHost.AlphaWotr, room.Bot!.Engine);
        Assert.Equal("alphawotr", await fixture.Scalar("SELECT state->'bot'->>'engine' FROM rooms WHERE id=$1", room.Id));
        var lords = await fixture.Rooms.Configure(room.Id, human.AccountId,
            new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = new MatchConfiguration { Lords = true } });
        Assert.Equal("CpuBaseGameOnly", lords.Error);
    }

    // The owner plays AlphaWOTR first: while the seat is open only to named accounts, everyone else's new CPU is the runtime.
    [PostgresFact]
    public async Task AnAccountOutsideTheOpeningGetsTheRuntime()
    {
        using var package = new BotPackageFixture();
        var settings = Settings(package);
        settings["Wotr:Bots:Protocol:Accounts"] = "someone-else";
        await using var fixture = await BotHostFixture.Create(settings);
        var (_, room, _) = await fixture.Reserve();
        Assert.Equal("", room.Bot!.Engine);
    }

    // At most MaxGames AlphaWOTR games at once: a further CPU game is refused while they play (CpuBusy).
    [PostgresFact]
    public async Task AlphaWotrBeyondItsLimitIsBusy()
    {
        using var package = new BotPackageFixture();
        var settings = Settings(package);
        settings["Wotr:Bots:Protocol:MaxGames"] = "1";
        await using var fixture = await BotHostFixture.Create(settings);
        var (_, first, _) = await fixture.Reserve();
        Assert.Equal(ProtocolSeatHost.AlphaWotr, first.Bot!.Engine);
        var human = await MenuAccounts.Register(fixture.Host);
        var room = await fixture.Rooms.Create(human.AccountId, new CreateRoomRequest { Side = Side.Shadow, Name = "Second" });
        var second = await fixture.Rooms.AddBot(room.Id, human.AccountId, PveFixture.Request(room));
        Assert.Equal("CpuBusy", second.Error);
    }

    [PostgresFact]
    public async Task ASeatSavedBeforeAlphaWotrKeepsTheRuntime()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(Settings(package));
        var (_, room, work) = await fixture.Game();
        await fixture.Execute("UPDATE rooms SET state = state #- '{bot,engine}' WHERE id=$1", room.Id);
        await fixture.Expire(work);
        var options = package.Options(fixture.Settings) with { MemoryPressureBytes = 1024L * 1024 * 1024 };
        using var metrics = new BotHostMetrics(options);
        using var host = Seat(ClosedPort());
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host);
        await scheduler.StartAsync(default);
        try { await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId); }
        finally { await scheduler.StopAsync(default); }
        Assert.NotEqual(DBNull.Value, await fixture.Scalar("SELECT memory FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
    }

    [PostgresFact]
    public async Task WithoutTheSidecarTheGameWaitsAndTheRuntimeNeverPlays()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(Settings(package));
        var (_, room, work) = await fixture.Game();
        await fixture.Expire(work);
        var revision = (long)(await fixture.Scalar("SELECT revision FROM matches WHERE id=$1", room.MatchId))!;
        var options = package.Options(fixture.Settings) with { MemoryPressureBytes = 1024L * 1024 * 1024 };

        // No sidecar listens: the decision is released with its error and the match does not move; the player's view would
        // say the CPU waits (BOT-226), from when, and the kind of failure only.
        ProtocolSeatHost? waitingHost;
        using (var metrics = new BotHostMetrics(options))
        using (var host = Seat(ClosedPort()))
        using (var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host))
        {
            await scheduler.StartAsync(default);
            try { await fixture.Until("SELECT last_error FROM bot_runtime_jobs WHERE assignment_id=$1", "ProtocolSidecarUnavailable", work.AssignmentId); }
            finally { await scheduler.StopAsync(default); }
            var waiting = host.Waiting(room.MatchId, Side.FreePeoples);
            Assert.NotNull(waiting);
            Assert.True(waiting!.Waiting);
            Assert.Equal("Unavailable", waiting.Problem);
            Assert.NotNull(waiting.Since);
        }
        Assert.Equal(revision, (long)(await fixture.Scalar("SELECT revision FROM matches WHERE id=$1", room.MatchId))!);
        Assert.Equal(0L, (long)(await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId))!);

        // The protocol bot comes up: the same decision is played through it; the runtime's memory stays unwritten, and the
        // waiting state goes once AlphaWOTR answers.
        await fixture.Expire(work);
        using var bot = new ProtocolFakeBot(7);
        using (var metrics = new BotHostMetrics(options))
        using (var host = Seat(bot.Port))
        using (var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host))
        {
            waitingHost = host;
            host.Unavailable(room.MatchId, Side.FreePeoples, "SidecarUnavailable");
            await scheduler.StartAsync(default);
            try { await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId); }
            finally { await scheduler.StopAsync(default); }
        }
        Assert.True((long)(await fixture.Scalar("SELECT revision FROM matches WHERE id=$1", room.MatchId))! > revision);
        Assert.Null(waitingHost!.Waiting(room.MatchId, Side.FreePeoples));
        Assert.Equal(DBNull.Value, await fixture.Scalar("SELECT memory FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal(1, bot.Connections);
    }

    // A game that ends on the other side's move gets no further CPU work; the sweep still ends its session (the bot hears
    // game_over) so the sidecar's slot is free for the next game.
    [PostgresFact]
    public async Task AGameThatEndsClosesItsSession()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(Settings(package));
        var (_, room, work) = await fixture.Game();
        await fixture.Expire(work);
        var options = package.Options(fixture.Settings) with { MemoryPressureBytes = 1024L * 1024 * 1024 };
        using var bot = new ProtocolFakeBot(11);
        using var metrics = new BotHostMetrics(options);
        using var host = Seat(bot.Port);
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host);
        await scheduler.StartAsync(default);
        try
        {
            await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId);
            Assert.Contains(room.MatchId, host.OpenMatches());
            await fixture.Execute("UPDATE matches SET state = jsonb_set(jsonb_set(state, '{winner}', '\"Shadow\"'), '{victory}', " +
                "'\"ShadowMilitary\"') WHERE id=$1", room.MatchId);
            for (var i = 0; i < 100 && host.OpenMatches().Contains(room.MatchId); i++)
                await Task.Delay(200);
        }
        finally { await scheduler.StopAsync(default); }
        Assert.DoesNotContain(room.MatchId, host.OpenMatches());
        JsonObject? over;
        lock (bot.Received)
            over = bot.Received.LastOrDefault(m => (string?)m["type"] == "game_over");
        Assert.NotNull(over);
        Assert.Equal("SHADOW", (string?)over!["winner"]);
        Assert.Equal("SHADOW_MILITARY", (string?)over["how"]);
    }

    // AlphaWOTR may think longer than the host's lease (a decision of several questions): the lease is renewed meanwhile,
    // so no other worker takes the same game.
    [PostgresFact]
    public async Task TheLeaseIsKeptWhileTheBotThinks()
    {
        using var package = new BotPackageFixture();
        var settings = Settings(package);
        settings["Wotr:Bots:RuntimeLeaseSeconds"] = "5";
        await using var fixture = await BotHostFixture.Create(settings);
        var (_, _, work) = await fixture.Game();
        await fixture.Expire(work);
        var options = package.Options(fixture.Settings) with { MemoryPressureBytes = 1024L * 1024 * 1024 };
        using var bot = new ProtocolFakeBot(13) { Delay = TimeSpan.FromSeconds(9) };
        using var metrics = new BotHostMetrics(options);
        using var host = Seat(bot.Port);
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host);
        await scheduler.StartAsync(default);
        try
        {
            for (var i = 0; i < 150; i++)
            {
                lock (bot.Received)
                    if (bot.Received.Any(m => (string?)m["type"] == "decide"))
                        break;
                await Task.Delay(100);
            }
            var claimed = await fixture.Scalar("SELECT generation FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId);
            await Task.Delay(TimeSpan.FromSeconds(7));
            Assert.Equal(true, await fixture.Scalar("SELECT lease_until > clock_timestamp() FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
            Assert.Equal(claimed, await fixture.Scalar("SELECT generation FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        }
        finally { await scheduler.StopAsync(default); }
    }

    private static ProtocolSeatHost Seat(int port) => new(
new ProtocolSeatOptions(true, "127.0.0.1", port, TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30), new Dictionary<string, JsonNode?>()), GameRuntime.Engine(EffectCatalog.All),
        NullLogger<ProtocolSeatHost>.Instance);

    // A port nothing listens on.
    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
