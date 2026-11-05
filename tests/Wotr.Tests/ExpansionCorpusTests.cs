using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// EXP-03: the frozen expansion saves (tests/fixtures/expansion-regression) read back to themselves, the demo CPU's
// recorded answer to each is accepted and leads to the recorded decision, and neither side's projection reveals the
// other side's hidden information.
public sealed class ExpansionCorpusTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Expansions");
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly IReadOnlyList<CorpusCase> Cases = JsonSerializer.Deserialize<CorpusManifest>(
        File.ReadAllText(Path.Combine(Folder, "manifest.json")), BoardCatalog.Json)!.Cases;

    public static IEnumerable<object[]> AllCases() => Cases.Select(c => new object[] { c.Name });

    // Every expansion decision the CPU meets is frozen; Dismay's saves are KingsIntegrationPersistenceTests'.
    [Fact]
    public void TheCorpusHoldsEveryExpansionDecisionTheCpuMeets()
    {
        var decisions = Cases.Select(c => c.Decision).ToHashSet();
        foreach (var kind in new[]
        {
            DecisionKind.CouncilVersions, DecisionKind.SpecialDieChoice, DecisionKind.KeepActionDie,
            DecisionKind.CouncilHomelands, DecisionKind.CouncilToken, DecisionKind.RulerDiscard, DecisionKind.CorruptionTile
        })
            Assert.Contains(kind, decisions);
        Assert.Contains(Cases, c => c.Configuration.VisibleCorruption && c.Decision == DecisionKind.CorruptionTile);
        Assert.Contains(Cases, c => c.Configuration.SequentialCorruption && c.Decision == DecisionKind.CorruptionTile);
        Assert.Contains(Cases, c => c.Configuration.Fate && c.Configuration.NewCities);
        Assert.Equal(Cases.Count, Cases.Select(c => c.File).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void AFrozenSaveReadsBackWithItsConfigurationAndDecision(string name)
    {
        var entry = Case(name);
        var raw = File.ReadAllText(Path.Combine(Folder, entry.File));
        Assert.Equal(entry.Sha256, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw))));
        var state = StateCodec.Read(raw);
        Assert.Equal(entry.SchemaVersion, JsonSerializer.Deserialize<MatchState>(raw, BoardCatalog.Json)!.SchemaVersion);
        Assert.Equal(Json(entry.Configuration), Json(state.Configuration));
        Assert.Equal(entry.Decision, state.Pending!.Kind);
        Assert.Equal(entry.Owner, state.Pending.Owner);
        var saved = StateCodec.Write(state);
        var restored = StateCodec.Read(saved);
        Assert.Equal(saved, StateCodec.Write(restored));
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(Json(Views.For(state, side)), Json(Views.For(restored, side)));
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void TheCpusRecordedAnswerResumesTheSave(string name)
    {
        var entry = Case(name);
        var state = StateCodec.Read(File.ReadAllText(Path.Combine(Folder, entry.File)));
        var before = StateCodec.Write(state);
        var result = Engine.Execute(state, state.Pending!.Owner, entry.Command, new BaseCorpusFixture.ZeroRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal(entry.ExpectedDecision, result.State.Pending?.Kind);
        Assert.Equal(entry.ExpectedOwner, result.State.Pending?.Owner);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void NeitherSideSeesTheOthersHiddenInformation(string name)
    {
        var state = StateCodec.Read(File.ReadAllText(Path.Combine(Folder, Case(name).File)));
        foreach (var viewer in Enum.GetValues<Side>())
            Assert.Equal(Json(Views.For(state, viewer)), Json(Views.For(HiddenInformation.Perturbed(state, viewer), viewer)));
    }

    private static CorpusCase Case(string name) => Cases.Single(c => c.Name == name);

    private static string Json(object value) => JsonSerializer.Serialize(value, BoardCatalog.Json);

    private sealed class CorpusManifest
    {
        public List<CorpusCase> Cases { get; set; } = [];
    }

    private sealed class CorpusCase
    {
        public string Name { get; set; } = "";
        public int Seed { get; set; }
        public string File { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public int SchemaVersion { get; set; }
        public MatchConfiguration Configuration { get; set; } = new();
        public DecisionKind Decision { get; set; }
        public Side Owner { get; set; }
        public GameCommand Command { get; set; } = new();
        public DecisionKind? ExpectedDecision { get; set; }
        public Side? ExpectedOwner { get; set; }
    }
}
