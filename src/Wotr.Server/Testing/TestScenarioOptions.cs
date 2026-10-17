namespace Wotr.Server;

// TEST-01: the test mode exists only where it is switched on. Development and Testing enable it by default;
// Production needs Wotr__Testing__Scenarios=true (WOTR_TEST_SCENARIOS in infra/.env), which it has since D19 (TEST-08).
public sealed record TestScenarioOptions(bool Enabled)
{
    public const string ConfigurationKey = "Wotr:Testing:Scenarios";

    public static TestScenarioOptions Read(IConfiguration configuration, IHostEnvironment environment) =>
        new(configuration.GetValue<bool?>(ConfigurationKey) ?? (environment.IsDevelopment() || environment.IsEnvironment("Testing")));
}
