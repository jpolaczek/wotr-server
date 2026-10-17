// Committed regression saves travel with the seeder build, not with the repository checkout.
internal static class RegressionFixtures
{
    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "base-regression", name + ".json"));
}
