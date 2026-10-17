namespace Wotr.Tests;

// QUAL-02: throughput and concurrency tests (93 lobby configurations, parallel "seen" marks) measure other
// behaviour than the account budgets, so they run with raised per-host limits; GameRateLimitTests keep the budgets.
internal static class TestBudgets
{
    public static readonly IReadOnlyDictionary<string, string?> Stress = new Dictionary<string, string?>
    {
        ["WOTR_LIMIT_COMMANDS"] = "10000",
        ["WOTR_LIMIT_PREVIEWS"] = "10000",
        ["WOTR_LIMIT_READS"] = "10000",
        ["WOTR_LIMIT_ROOMS"] = "10000",
        ["WOTR_LIMIT_CONCURRENT"] = "256"
    };
}
