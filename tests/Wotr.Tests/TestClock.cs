namespace Wotr.Tests;

// Wall-clock time a test sets and advances; timestamps stay real so refresh intervals still work.
public sealed class TestClock(DateTimeOffset? start = null) : TimeProvider
{
    private DateTimeOffset now = start ?? new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan by) => now += by;
}
