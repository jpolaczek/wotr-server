using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

// EXP-03: the expansion save corpus: frozen states from real games of two demo CPUs, taken at the first time each
// expansion decision comes up within the first turns (the Corruption tile under each corruption variant too), so the
// files stay small; finished games are covered by ExpansionBotGameTests. Each case keeps the CPU's answer and the
// decision that follows it with a zero random source, so a later change that cannot read these saves, or reads them
// differently, fails ExpansionCorpusTests. Dismay is not here: the CPU never spends the Ruler token that grants it
// (KingsIntegrationPersistenceTests keep its saves).
internal static class ExpansionCorpusExport
{
    // A case is taken by this turn at the latest; a later one would carry a long history.
    private const int TurnLimit = 4;
    private const int StepLimit = 3000;
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, GameRuntime.Projection(EffectCatalog.All));
    private static readonly JsonSerializerOptions Json = new(BoardCatalog.Json) { WriteIndented = true };

    // Seeds tried; of the games that reach the case, the smallest save gives it.
    private const int Seeds = 30;
    // The Council's choices come only before the first actions; a game past them never reaches them.
    private static readonly DecisionKind[] SetupDecisions =
        [DecisionKind.CouncilVersions, DecisionKind.CouncilHomelands, DecisionKind.CouncilToken];

    // Case name, the configuration of its game and the decision it stops at.
    private static readonly (string Name, MatchConfiguration Configuration, DecisionKind Decision)[] Cases =
    [
        ("lords-versions", new() { Lords = true }, DecisionKind.CouncilVersions),
        ("lords-special-die", new() { Lords = true }, DecisionKind.SpecialDieChoice),
        ("lords-vilya", new() { Lords = true, Kings = true }, DecisionKind.KeepActionDie),
        ("council-homelands", new() { Lords = true, Council = true }, DecisionKind.CouncilHomelands),
        ("council-token", new() { Lords = true, Council = true }, DecisionKind.CouncilToken),
        ("kings-ruler-discard", new() { Kings = true }, DecisionKind.RulerDiscard),
        ("kings-corruption", new() { Kings = true }, DecisionKind.CorruptionTile),
        ("kings-corruption-visible", new() { Kings = true, VisibleCorruption = true }, DecisionKind.CorruptionTile),
        ("kings-corruption-sequential", new() { Kings = true, SequentialCorruption = true }, DecisionKind.CorruptionTile),
        ("fate-new-cities-action", new() { Fate = true, NewCities = true }, DecisionKind.Action),
        ("all-variants-action", AllVariants, DecisionKind.Action)
    ];

    private static MatchConfiguration AllVariants => new()
    {
        Fate = true, Lords = true, Kings = true, NewCities = true, Council = true, VisibleCorruption = true,
        SequentialCorruption = true, RemovingRulerTokens = true
    };

    public static void Write(string destination)
    {
        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
            throw new InvalidOperationException("Export into an empty directory, then review changes before replacing a corpus.");
        var built = Cases.Select(Build).ToArray();
        Directory.CreateDirectory(destination);
        foreach (var entry in built)
            File.WriteAllText(Path.Combine(destination, entry.Name + ".json"), entry.Json, new UTF8Encoding(false));
        var manifest = new
        {
            Format = 1,
            Baseline = "EXP-03 / demo CPU games",
            RulesVersion = Protocol.RulesVersion,
            ContentVersion = Protocol.ContentVersion,
            Cases = built.Select(c => c.Manifest).ToArray()
        };
        File.WriteAllText(Path.Combine(destination, "manifest.json"), JsonSerializer.Serialize(manifest, Json) + "\n");
        Console.WriteLine($"Exported {built.Length} expansion cases to {destination}. Review before committing.");
    }

    private static (string Name, string Json, object Manifest) Build(
        (string Name, MatchConfiguration Configuration, DecisionKind Decision) entry)
    {
        var (reached, seed) = Enumerable.Range(1, Seeds)
            .Select(seed => (State: Reach(entry.Configuration, seed, entry.Decision), Seed: seed))
            .Where(game => game.State is not null)
            .OrderBy(game => StateCodec.Write(game.State!).Length)
            .FirstOrDefault();
        if (reached is null)
            throw new InvalidOperationException($"No game of {Seeds} seeds reached the case {entry.Name}.");
        var state = Normalized(reached, "expansion-corpus-" + entry.Name);
        var json = JsonSerializer.Serialize(state, Json) + "\n";
        GameCommand? command = null;
        MatchState? after = null;
        if (state.Pending is not null)
        {
            command = Bot.Next(state, state.Pending.Owner)
                ?? throw new InvalidOperationException($"Corpus {entry.Name}: the CPU has no answer.");
            var result = Engine.Execute(state, state.Pending.Owner, command, new BaseCorpusSeeds.ZeroRandom());
            if (!result.Receipt.Accepted)
                throw new InvalidOperationException($"Corpus {entry.Name}/{command.Kind}: {result.Receipt.Error}");
            after = result.State;
        }
        return (entry.Name, json, new
        {
            entry.Name,
            Seed = seed,
            File = entry.Name + ".json",
            Sha256 = Hash(json),
            state.SchemaVersion,
            state.Configuration,
            Decision = state.Pending?.Kind,
            Owner = state.Pending?.Owner,
            state.Victory,
            Command = command,
            ExpectedDecision = after?.Pending?.Kind,
            ExpectedOwner = after?.Pending?.Owner,
            ExpectedVictory = after?.Victory
        });
    }

    // Two CPUs play from the start; the state at the first wanted decision (after the first turn for an action), or
    // null when this game does not get there within the first turns.
    private static MatchState? Reach(MatchConfiguration configuration, int seed, DecisionKind decision)
    {
        var random = new SeededRandom(seed);
        // The CPU's choices follow the match's id, so each seed's game has its own.
        var state = MatchFactory.Create("expansion-corpus-game-" + seed, new SeededRandom(seed), ConfigurationRules.Normalize(configuration));
        TurnFlow.Start(state);
        for (var step = 0; step < StepLimit && state.Winner is null && state.Turn <= TurnLimit; step++)
        {
            if (SetupDecisions.Contains(decision) && state.Phase == GamePhase.ActionResolution)
                return null;
            if (state.Pending!.Kind == decision && (decision != DecisionKind.Action || state.Turn > 1))
                return state;
            var side = state.Pending!.Owner;
            var command = Bot.Next(state, side) ?? throw new InvalidOperationException($"No answer for {state.Pending.Kind}.");
            state = Engine.Execute(state, side, command, random).State;
        }
        return null;
    }

    private static MatchState Normalized(MatchState state, string id) =>
        StateCodec.Read(StateCodec.Write(state).Replace(state.Id, id, StringComparison.Ordinal));

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
