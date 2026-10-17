using System.Diagnostics.Metrics;

namespace Wotr.Server;

// QUAL-03: safe counters of receipt traffic (no payloads, ids or accounts), readable with dotnet-counters
// under the meter "Wotr.Server.Receipts".
internal static class ReceiptMetrics
{
    private static readonly Meter Meter = new("Wotr.Server.Receipts");
    private static readonly Counter<long> AcceptedStored = Meter.CreateCounter<long>("receipts.accepted.stored");
    private static readonly Counter<long> RejectedStored = Meter.CreateCounter<long>("receipts.rejected.stored");
    private static readonly Counter<long> RejectedRemoved = Meter.CreateCounter<long>("receipts.rejected.removed");
    private static readonly Counter<long> MalformedAnswered = Meter.CreateCounter<long>("commands.malformed");

    public static void Stored(bool accepted) => (accepted ? AcceptedStored : RejectedStored).Add(1);

    public static void Removed(int count)
    {
        if (count > 0)
            RejectedRemoved.Add(count);
    }

    public static void Malformed() => MalformedAnswered.Add(1);
}
