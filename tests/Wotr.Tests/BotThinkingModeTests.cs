using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// BOT-37 (owner audit, 2026-11-10): the player chooses the CPU's pace in the room before the game.
// It is saved with the room and its game, a change is a change of the room (its version rises and
// both confirmations are cleared), it is locked once the game starts, and an older room without a
// pace is Fast. The host gives the pace's work and deadline to the runtime through ABI 3.
public sealed class BotThinkingModeTests
{
    [PostgresFact]
    public async Task ThePaceIsChosenWithTheSeatAndChangesLikeTheRoom()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, room, work) = await fixture.Reserve(mode: BotThinkingMode.Thorough);
        Assert.Equal(BotThinkingMode.Thorough, room.Bot!.ThinkingMode);
        Assert.Equal(BotThinkingMode.Thorough, work.ThinkingMode);
        Assert.True(await fixture.Rooms.ReadyRuntime(work));
        var ready = await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        Assert.Equal("", ready.Error);
        Assert.True(ready.Room!.FreePeoplesAccepted || ready.Room.ShadowAccepted);

        Assert.Equal("StaleConfiguration", (await fixture.Rooms.SetBotThinking(room.Id, human.AccountId,
            new() { ExpectedVersion = room.Version - 1, ThinkingMode = BotThinkingMode.Fast })).Error);
        var changed = await fixture.Rooms.SetBotThinking(room.Id, human.AccountId,
            new() { ExpectedVersion = ready.Room.Version, ThinkingMode = BotThinkingMode.Fast });
        Assert.Equal("", changed.Error);
        Assert.Equal(BotThinkingMode.Fast, changed.Room!.Bot!.ThinkingMode);
        Assert.Equal(ready.Room.Version + 1, changed.Room.Version);
        Assert.False(changed.Room.FreePeoplesAccepted);
        Assert.False(changed.Room.ShadowAccepted);
        Assert.Equal("Fast", await fixture.Scalar("SELECT state->'bot'->>'thinkingMode' FROM rooms WHERE id=$1", room.Id));
        Assert.Equal("InvalidBotRequest", (await fixture.Rooms.SetBotThinking(room.Id, human.AccountId,
            new() { ExpectedVersion = changed.Room.Version, ThinkingMode = (BotThinkingMode)7 })).Error);
    }

    [PostgresFact]
    public async Task ThePaceIsLockedOnceTheGameStartsAndAnOlderRoomIsFast()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, room, work) = await fixture.Game(mode: BotThinkingMode.Thorough);
        Assert.Equal(BotThinkingMode.Thorough, work.ThinkingMode);
        Assert.Equal("MatchAlreadyStarted", (await fixture.Rooms.SetBotThinking(room.Id, human.AccountId,
            new() { ExpectedVersion = room.Version, ThinkingMode = BotThinkingMode.Fast })).Error);

        // A room saved before the pace existed has no thinkingMode: its CPU plays Fast.
        await fixture.Execute("UPDATE rooms SET state = state #- '{bot,thinkingMode}' WHERE id=$1", room.Id);
        await fixture.Expire(work);
        var legacy = await fixture.Jobs.Claim("fixture-owner-2");
        Assert.NotNull(legacy);
        Assert.Equal(BotThinkingMode.Fast, legacy!.ThinkingMode);
    }

    [PostgresTheory]
    [InlineData(BotThinkingMode.Fast)]
    [InlineData(BotThinkingMode.Thorough)]
    public async Task TheHostGivesThePacesWorkAndDeadlineToTheRuntime(BotThinkingMode mode)
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(package.Settings());
        var (_, _, work) = await fixture.Game(mode: mode);
        await fixture.Expire(work);
        // The shared test host's live heap is no CPU host's: the memory gate (BOT-20) is not what this test measures.
        var options = package.Options(fixture.Settings) with { MaximumWork = 900, MemoryPressureBytes = 1024L * 1024 * 1024 };
        using var metrics = new BotHostMetrics(options);
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics);
        await scheduler.StartAsync(default);
        try { await fixture.Until("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", 1L, work.AssignmentId); }
        finally { await scheduler.StopAsync(default); }
        Assert.Equal(mode.ToString(), await fixture.Scalar("SELECT memory::jsonb->>'thinkingMode' FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        var limit = (string)(await fixture.Scalar("SELECT memory::jsonb->>'workLimit' FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId))!;
        Assert.Equal(mode == BotThinkingMode.Thorough ? "900" : "300", limit);
        var deadline = int.Parse((string)(await fixture.Scalar("SELECT memory::jsonb->>'deadline' FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId))!);
        Assert.InRange(deadline, 50, (int)options.Timeout(mode).TotalMilliseconds);
        if (mode == BotThinkingMode.Thorough)
            Assert.True(deadline > options.DecisionTimeout.TotalMilliseconds, $"deadline {deadline}");
    }
}
