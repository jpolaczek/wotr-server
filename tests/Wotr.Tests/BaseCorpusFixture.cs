using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class BaseCorpusFixture
{
    private static readonly string DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Base");
    public static readonly Projection Views = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands());
    public static readonly IReadOnlyList<CorpusCase> Cases = JsonSerializer.Deserialize<CorpusManifest>(
        File.ReadAllText(Path.Combine(DirectoryPath, "manifest.json")), BoardCatalog.Json)!.Cases;

    public static CorpusCase Case(string name) => Cases.Single(c => c.Name == name);
    public static string Raw(CorpusCase entry) => File.ReadAllText(Path.Combine(DirectoryPath, entry.File));
    public static MatchState Load(CorpusCase entry) => StateCodec.Read(Raw(entry));
    public static string Json(object value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal sealed class ZeroRandom : IRandomSource
    {
        public int Calls { get; private set; }
        public int Next(int upperBound)
        {
            Calls++;
            return 0;
        }
    }

    private sealed class CorpusManifest
    {
        public List<CorpusCase> Cases { get; set; } = [];
    }

    internal sealed class CorpusCase
    {
        public string Name { get; set; } = "";
        public string File { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public int SchemaVersion { get; set; }
        public DecisionKind? Decision { get; set; }
        public Side? Owner { get; set; }
        public VictoryCondition? Victory { get; set; }
        public GameCommand? Command { get; set; }
        public string? ExpectedStateSha256 { get; set; }
        public DecisionKind? ExpectedDecision { get; set; }
        public Side? ExpectedOwner { get; set; }
        public VictoryCondition? ExpectedVictory { get; set; }
        public int? ExpectedRandomSamples { get; set; }
    }
}
