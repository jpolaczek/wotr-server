using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

public class ActionLogPerformanceTests(ITestOutputHelper output)
{
    [PostgresFact]
    public async Task LongJournalReturnsBoundedPagesWithoutProjectingOrSendingTheSnapshot()
    {
        await using var host = new TestHost();
        var state = ActionLogPagingTests.LongHistory(5000);
        await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        var journal = host.Services.GetRequiredService<ActionLogStore>();
        await journal.Page(state.Id, "alice", ActionLogFilter.All, 50);
        var samples = new List<double>();
        var bytes = 0;
        for (var i = 0; i < 10; i++)
        {
            var clock = Stopwatch.StartNew();
            var page = (await journal.Page(state.Id, "alice", ActionLogFilter.All, 50))!;
            samples.Add(clock.Elapsed.TotalMilliseconds);
            Assert.Equal(50, page.Steps.Count);
            Assert.Equal(50, page.Actions.Count);
            bytes = JsonSerializer.SerializeToUtf8Bytes(page, BoardCatalog.Json).Length;
            Assert.True(bytes < 100_000);
        }
        samples.Sort();
        output.WriteLine($"5000 public + 1667 private + movement/legacy; 10 reads: median={samples[5]:F2} ms; max={samples[^1]:F2} ms; response={bytes} bytes.");
    }
}
