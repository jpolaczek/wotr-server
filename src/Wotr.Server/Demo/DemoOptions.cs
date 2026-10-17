namespace Wotr.Server;

// DEMO-03: the CPU's pace. A short pause before each of its moves lets the player's client show them one by one;
// tests set it to 0 (Wotr__Demo__StepDelayMilliseconds).
public sealed record DemoOptions(TimeSpan StepDelay, int StepLimit)
{
    public const string DelayKey = "Wotr:Demo:StepDelayMilliseconds";
    public const int DefaultDelay = 800;
    public const int DefaultStepLimit = 500;

    public static DemoOptions Read(IConfiguration configuration) =>
        new(TimeSpan.FromMilliseconds(Math.Max(0, configuration.GetValue(DelayKey, DefaultDelay))), DefaultStepLimit);
}
