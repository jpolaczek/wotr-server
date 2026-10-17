using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class LegacyContractFixture
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Legacy", name));
    public static MatchState State(string name) => StateCodec.Read(Read(name));
    public static GameCommand Command() => JsonSerializer.Deserialize<GameCommand>(Read("command.json"), BoardCatalog.Json)!;
}
