using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Domain;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

public sealed class BotRuntimeResourceTests(ITestOutputHelper output)
{
    [PostgresTheory]
    [InlineData(2)]
    [InlineData(8)]
    public async Task EightConcurrentMatchesObeyWorkerAndWorkBounds(int workers)
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(package.Settings());
        for (var i = 0; i < 8; i++)
        {
            var seed = MatchFactory.Create("resource-fixture", new SeededRandom(1900 + i));
            TurnFlow.Start(seed);
            var (_, _, work) = await fixture.Game(seed: seed);
            await fixture.Execute("UPDATE bot_runtime_jobs SET lease_until='infinity',memory='delay' WHERE assignment_id=$1", work.AssignmentId);
        }
        var options = package.Options(fixture.Settings) with { Workers = workers, MaximumWork = 900, MemoryPressureBytes = 1024L * 1024 * 1024 };
        using var metrics = new BotHostMetrics(options);
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics);
        await fixture.Execute("UPDATE bot_runtime_jobs SET lease_until='-infinity',available_at=clock_timestamp()");
        await scheduler.StartAsync(default);
        try { await fixture.Until("SELECT count(*) FROM bot_runtime_jobs WHERE memory_version=1", 8L); }
        finally { await scheduler.StopAsync(default); }
        var snapshot = metrics.Snapshot();
        output.WriteLine(JsonSerializer.Serialize(new { matches = 8, seeds = "1900-1907", workers, work = options.MaximumWork, snapshot }));
        Assert.Equal(8, snapshot.Evaluations);
        Assert.InRange(snapshot.PeakActive, 2, workers);
        Assert.Equal(0, snapshot.Active);
        Assert.Equal(8, snapshot.Outcomes["Accepted"]);
        Assert.True(snapshot.QueueP95Seconds > 0);
        Assert.True(snapshot.ComputeP95Seconds >= 0.25);
        Assert.InRange(snapshot.PeakManagedBytes, 1, options.MemoryPressureBytes);
        Assert.True(snapshot.PeakWorkingSetBytes > 0);
        Assert.Equal(8L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_jobs WHERE (memory::jsonb->>'workLimit')::integer=300"));
        Assert.Equal(8L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE accepted"));
    }

    [PostgresFact]
    public async Task RepeatedPreviewRequestsStopAfterThreeEvaluationsWithoutChangingMemoryOrTheDecision()
    {
        using var package = new BotPackageFixture();
        var settings = package.Settings();
        settings["Wotr:Bots:RetrySeconds"] = "60";
        await using var fixture = await BotHostFixture.Create(settings);
        var (_, _, work) = await fixture.Game();
        var before = await fixture.Scalar("SELECT state::text FROM matches");
        await fixture.Execute("UPDATE bot_runtime_jobs SET memory='preview-loop'");
        await fixture.Expire(work);
        var options = package.Options(fixture.Settings) with { MaximumWork = 900 };
        using var metrics = new BotHostMetrics(options);
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics);
        await scheduler.StartAsync(default);
        try { await fixture.Until("SELECT last_error FROM bot_runtime_jobs", "PreviewLimit"); }
        finally { await scheduler.StopAsync(default); }
        Assert.Equal(3, metrics.Snapshot().Evaluations);
        Assert.Equal(1, metrics.Snapshot().Outcomes["PreviewLimit"]);
        Assert.Equal(before, await fixture.Scalar("SELECT state::text FROM matches"));
        Assert.Equal("preview-loop", await fixture.Scalar("SELECT memory FROM bot_runtime_jobs"));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_proposals"));
    }

    // BOT-20: uncollected garbage above the limit is not memory pressure. The gate measures the live
    // heap before refusing, so an idle scheduler is not stalled by garbage no GC will ever collect.
    [Fact]
    public void GarbageAboveTheLimitIsNotMemoryPressure()
    {
        var live = GC.GetTotalMemory(forceFullCollection: true);
        var options = new BotHostOptions(true, null, "0.2.0", 1, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)) { MemoryPressureBytes = live + 64L * 1024 * 1024 };
        using var metrics = new BotHostMetrics(options);
        for (var chunk = 0; chunk < 256; chunk++)
            GC.KeepAlive(new byte[1024 * 1024]);
        Assert.True(metrics.CanEvaluate());
        Assert.False(new BotHostMetrics(options with { MemoryPressureBytes = 1 }).CanEvaluate());
    }

    [PostgresFact]
    public async Task MemoryPressureStartsNoEvaluationAndLeavesPendingDecisionUntouched()
    {
        using var package = new BotPackageFixture();
        await using var fixture = await BotHostFixture.Create(package.Settings());
        var (_, _, work) = await fixture.Game();
        await fixture.Expire(work);
        var options = package.Options(fixture.Settings) with { MemoryPressureBytes = 1, Retry = TimeSpan.FromMilliseconds(20) };
        using var metrics = new BotHostMetrics(options);
        using var scheduler = ActivatorUtilities.CreateInstance<BotScheduler>(fixture.Host.Services, options, metrics);
        await scheduler.StartAsync(default);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!metrics.Snapshot().Outcomes.ContainsKey("MemoryPressure"))
                await Task.Delay(10, timeout.Token);
        }
        finally { await scheduler.StopAsync(default); }
        Assert.Equal(0, metrics.Snapshot().Evaluations);
        Assert.Equal(work.Generation, await fixture.Scalar("SELECT generation FROM bot_runtime_jobs"));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM command_receipts"));
    }
}
