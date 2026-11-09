using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Wotr.Server;

public sealed record BotResourceSnapshot(long Evaluations, int Active, int PeakActive, long PeakManagedBytes,
    long PeakWorkingSetBytes, double QueueP95Seconds, double ComputeP95Seconds, IReadOnlyDictionary<string, long> Outcomes);

// Operator diagnostics only. Bounded samples and fixed labels; never player/command/memory identifiers.
public sealed class BotHostMetrics(BotHostOptions options) : IDisposable
{
    private readonly object gate = new();
    private readonly Meter meter = new("Wotr.Server.Bots");
    private readonly Queue<double> queue = new();
    private readonly Queue<double> compute = new();
    private readonly Dictionary<string, long> outcomes = new(StringComparer.Ordinal);
    private readonly HashSet<string> allowed = ["Accepted", "NotReady", "BotUnavailable", "BotVersionUnsupported",
        "BotConfigurationUnsupported", "MemoryIncompatible", "RuntimeTimeout", "RuntimeRefused", "PreviewLimit",
        "CommandRejected", "HostFailure", "ContextTooLarge", "StaleWork", "Drained", "MemoryPressure",
        "NotConfigured", "Unavailable", "Incompatible", "LoadFailed", "PackageRequired", "PackageInvalid", "PolicyUnavailable"];
    private Counter<long>? outcomeCounter;
    private Histogram<double>? queueHistogram;
    private Histogram<double>? computeHistogram;
    private long evaluations;
    private int active;
    private int peak;
    private long managed;
    private long workingSet;

    public bool CanEvaluate()
    {
        var current = GC.GetTotalMemory(forceFullCollection: false);
        // BOT-20: a reading over the limit may be garbage the server GC has not collected yet. An idle
        // scheduler allocates nothing, so the reading would never fall and every CPU game would stall
        // (observed in a local 42-turn game). Measure the live heap before refusing work.
        if (current >= options.MemoryPressureBytes)
            current = GC.GetTotalMemory(forceFullCollection: true);
        lock (gate)
        {
            managed = Math.Max(managed, current);
            workingSet = Math.Max(workingSet, Environment.WorkingSet);
        }
        return current < options.MemoryPressureBytes;
    }

    public void QueueWait(double seconds)
    {
        lock (gate)
        {
            Instruments();
            Add(queue, seconds);
            queueHistogram!.Record(seconds);
        }
    }

    public IDisposable Evaluate()
    {
        var started = Stopwatch.GetTimestamp();
        lock (gate)
        {
            Instruments();
            evaluations++;
            peak = Math.Max(peak, ++active);
        }
        return new Measurement(() =>
        {
            CanEvaluate();
            var seconds = Stopwatch.GetElapsedTime(started).TotalSeconds;
            lock (gate)
            {
                active--;
                Add(compute, seconds);
                computeHistogram!.Record(seconds);
            }
        });
    }

    public void Outcome(string value)
    {
        var name = allowed.Contains(value) ? value : "RuntimeRefused";
        lock (gate)
        {
            Instruments();
            outcomes[name] = outcomes.GetValueOrDefault(name) + 1;
            outcomeCounter!.Add(1, new KeyValuePair<string, object?>("outcome", name));
        }
    }

    public BotResourceSnapshot Snapshot()
    {
        CanEvaluate();
        lock (gate)
            return new(evaluations, active, peak, managed, workingSet, Percentile(queue), Percentile(compute),
                new Dictionary<string, long>(outcomes, StringComparer.Ordinal));
    }

    private void Instruments()
    {
        if (outcomeCounter is not null)
            return;
        outcomeCounter = meter.CreateCounter<long>("bot.work.outcomes");
        queueHistogram = meter.CreateHistogram<double>("bot.queue.seconds", "s");
        computeHistogram = meter.CreateHistogram<double>("bot.compute.seconds", "s");
        meter.CreateObservableGauge("bot.evaluations.active", () => Volatile.Read(ref active));
        meter.CreateObservableGauge("bot.evaluations.peak", () => Volatile.Read(ref peak));
        meter.CreateObservableGauge("bot.host.managed.peak", () => Interlocked.Read(ref managed), "By");
        meter.CreateObservableGauge("bot.host.working_set.peak", () => Interlocked.Read(ref workingSet), "By");
    }

    private static void Add(Queue<double> samples, double value)
    {
        if (samples.Count == 2048)
            samples.Dequeue();
        samples.Enqueue(value);
    }

    private static double Percentile(Queue<double> values) => values.Count == 0 ? 0 :
        values.Order().ElementAt((int)Math.Ceiling(values.Count * 0.95) - 1);

    private sealed class Measurement(Action completed) : IDisposable
    {
        public void Dispose() => completed();
    }

    public void Dispose() => meter.Dispose();
}
