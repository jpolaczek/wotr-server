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

        // No sidecar listens: the decision is released with its error and the match does not move.
        using (var metrics = new BotHostMetrics(options))
        using (var host = Seat(ClosedPort()))
        using (var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host))
        {
            await scheduler.StartAsync(default);
            try { await fixture.Until("SELECT last_error FROM bot_runtime_jobs WHERE assignment_id=$1", "ProtocolSidecarUnavailable", work.AssignmentId); }
            finally { await scheduler.StopAsync(default); }
        }
        Assert.Equal(revision, (long)(await fixture.Scalar("SELECT revision FROM matches WHERE id=$1", room.MatchId))!);
        Assert.Equal(0L, (long)(await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId))!);

        // The protocol bot comes up: the same decision is played through it; the runtime's memory stays unwritten.
        await fixture.Expire(work);
        using var bot = new ProtocolFakeBot(7);
        using (var metrics = new BotHostMetrics(options))
        using (var host = Seat(bot.Port))
        using (var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics, host))
        {
            await scheduler.StartAsync(default);
            try { await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId); }
            finally { await scheduler.StopAsync(default); }
        }
        Assert.True((long)(await fixture.Scalar("SELECT revision FROM matches WHERE id=$1", room.MatchId))! > revision);
        Assert.Equal(DBNull.Value, await fixture.Scalar("SELECT memory FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal(1, bot.Connections);
    }

    private static ProtocolSeatHost Seat(int port) => new(new ProtocolSeatOptions(true, "127.0.0.1", port, TimeSpan.FromSeconds(5),
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
