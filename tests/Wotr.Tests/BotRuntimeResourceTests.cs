using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
            var (_, _, work) = await fixture.Game();
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
        output.WriteLine(JsonSerializer.Serialize(new { matches = 8, workers, work = options.MaximumWork, snapshot }));
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
