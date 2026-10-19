using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// EXP-10: the base game and every module play on their own and in every legal combination. Two demo CPUs play a
// complete game of each of the 93 legal configurations. At every step a module that is off has no decision, no offered
// or accepted command, no state, no die, card or character in the match; the pieces stay unique and the physical
// armies keep their identities; the save and both projections read back and ignore the other side's secrets.
// WOTR_SOAK_SEEDS=<n> plays n seeds of every configuration instead of one. One class plays the games one after another
// on one core: split across classes they crowded the suite's timing-sensitive restart and demo tests on 8 cores.
public sealed class ModuleIndependenceGameTests(ITestOutputHelper output)
{
    public static TheoryData<int, int> Games() => ModuleIndependence.Games(_ => true);

    [Theory]
    [MemberData(nameof(Games))]
    public void EveryLegalConfigurationPlaysToTheEndWithOnlyItsOwnModules(int flags, int seed) =>
        ModuleIndependence.Play(flags, seed, output);
}

internal static class ModuleIndependence
{
    private const int TurnLimit = 40;
    private const int StepLimit = 9000;
    private const int CheckEvery = 25;
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);

    private enum Module { Lords, Council, Kings, Dismay, Fate }

    // The handlers each module adds to the runtime (GameRuntime); a command they handle belongs to that module.
    private static readonly (Module Module, ICommandHandler Handler)[] ModuleHandlers =
    {
        (Module.Lords, new LordsDiceCommands()), (Module.Lords, new KeeperRingCommands()),
        (Module.Lords, new LordsSetupCommands()), (Module.Council, new CouncilCommands()),
        (Module.Kings, new SovereignCommands()), (Module.Kings, new CorruptionCommands()),
        (Module.Kings, new KingsDiceCommands()), (Module.Dismay, new DismayCommands())
    };

    // The decisions only a module opens.
    private static readonly Dictionary<DecisionKind, Module> ModuleDecisions = new()
    {
        [DecisionKind.SpecialDieChoice] = Module.Lords,
        [DecisionKind.KeepActionDie] = Module.Lords,
        [DecisionKind.CouncilVersions] = Module.Lords,
        [DecisionKind.CouncilHomelands] = Module.Council,
        [DecisionKind.CouncilToken] = Module.Council,
        [DecisionKind.RulerDiscard] = Module.Kings,
        [DecisionKind.CorruptionTile] = Module.Kings,
        [DecisionKind.Dismay] = Module.Dismay
    };

    public static TheoryData<int, int> Games(Func<MatchConfiguration, bool> part)
    {
        var games = new TheoryData<int, int>();
        var seeds = int.TryParse(Environment.GetEnvironmentVariable("WOTR_SOAK_SEEDS"), out var count) && count > 0 ? count : 1;
        foreach (var flags in ExpansionConfigurations.Legal().Select(row => (int)row[0]))
            if (part(ExpansionConfigurations.FromFlags(flags)))
                for (var seed = 1; seed <= seeds; seed++)
                    games.Add(flags, seed);
        return games;
    }

    public static void Play(int flags, int seed, ITestOutputHelper output)
    {
        var configuration = ConfigurationRules.Normalize(ExpansionConfigurations.FromFlags(flags));
        var name = $"{Name(configuration)} seed {seed}";
        var random = new SeededRandom(seed);
        var state = MatchFactory.Create($"module-independence-{flags}-{seed}", new SeededRandom(seed), configuration);
        TurnFlow.Start(state);
        var pieces = new Pieces(state);
        var decisions = new Dictionary<DecisionKind, int>();
        var steps = 0;
        for (; steps < StepLimit && state.Winner is null && state.Turn <= TurnLimit; steps++)
        {
            var side = state.Pending!.Owner;
            var kind = state.Pending.Kind;
            decisions[kind] = decisions.GetValueOrDefault(kind) + 1;
            var checkpoint = ModuleDecisions.ContainsKey(kind) || steps % CheckEvery == 0;
            OnlyEnabledModules(state, name, checkpoint ? Enum.GetValues<Side>() : [side]);
            pieces.Check(state, name);
            if (checkpoint)
                state = SavedAndPrivate(state);
            var command = Bot.Next(state, side);
            Assert.True(command is not null, $"{name}: {side} has no move for {kind} in turn {state.Turn}.");
            Assert.True(Enabled(OwnerOf(command.Kind), configuration), $"{name}: the CPU sent {command.Kind}.");
            var result = Engine.Execute(state, side, command, random);
            Assert.True(result.Receipt.Accepted, $"{name}: {command.Kind} for {kind}: {result.Receipt.Error}");
            state = result.State;
        }
        pieces.Check(state, name);
        SavedAndPrivate(state);
        output.WriteLine($"{name}: turn {state.Turn}, {steps} steps, winner {state.Winner?.ToString() ?? "none"} " +
            $"({state.Victory}); {string.Join(", ", decisions.OrderBy(d => d.Key.ToString()).Select(d => $"{d.Key} {d.Value}"))}");
        Assert.True(state.Winner is not null || state.Turn > TurnLimit, $"{name}: stopped at step {steps} in turn {state.Turn}.");
        if (state.Winner is not null)
            Assert.All(Enum.GetValues<Side>(), viewer => Assert.Null(Bot.Next(state, viewer)));
    }

    // A module that is off opens no decision, offers no command (to the decision's owner at every step, to both sides
    // at a checkpoint) and keeps no state, die or character.
    private static void OnlyEnabledModules(MatchState state, string name, IEnumerable<Side> viewers)
    {
        var configuration = state.Configuration;
        if (ModuleDecisions.TryGetValue(state.Pending!.Kind, out var decisionModule))
            Assert.True(Enabled(decisionModule, configuration), $"{name}: decision {state.Pending.Kind} in turn {state.Turn}.");
        Assert.All(state.Pending.Commands, kind => Assert.True(Enabled(OwnerOf(kind), configuration),
            $"{name}: {state.Pending.Kind} allows {kind}."));
        foreach (var viewer in viewers)
            Assert.All(Views.For(state, viewer).Options.Where(o => o.Enabled), option => Assert.True(
                Enabled(OwnerOf(option.Kind), configuration), $"{name}: {viewer} is offered {option.Kind} in {state.Pending.Kind}."));
        Assert.True(configuration.Lords || state.Lords is null, $"{name}: Lords state without Lords.");
        Assert.True(configuration.Council || state.Lords?.Council is null, $"{name}: Council state without the Council.");
        Assert.True(configuration.Kings || state.Kings is null, $"{name}: Kings state without Kings.");
        Assert.True(configuration.Lords || state.Dice.All(d => d.Special is null), $"{name}: a special die without Lords.");
        Assert.True(configuration.Kings || state.Dice.All(d => d.Source != "ruler"), $"{name}: a Ruler die without Kings.");
        Assert.All(state.Characters, c => Assert.True(Enabled(ModuleOf(c.DefinitionId), configuration),
            $"{name}: character {c.DefinitionId}."));
    }

    // The match's pieces: every card is in one place only and comes from the decks this configuration built (or is a
    // module's special card); the physical armies keep their identities, except a Kings sovereign that leaves the board
    // as a figure once awakened (it becomes a Companion) or corrupted; dice, tiles and characters are unique.
    private sealed class Pieces(MatchState start)
    {
        private readonly HashSet<string> decks = start.Players.SelectMany(Cards).ToHashSet();
        private readonly List<string> units = start.Units.Select(u => u.Id).Order().ToList();

        public void Check(MatchState state, string name)
        {
            var cards = state.Players.SelectMany(Cards).ToList();
            var twice = cards.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.True(twice.Count == 0, $"{name}: cards in two places in turn {state.Turn}: {string.Join(", ", twice)}.");
            Assert.All(cards, card => Assert.True(decks.Contains(card) || card.Contains(".special.") &&
                Enabled(ModuleOf(card), state.Configuration), $"{name}: card {card} is not in this configuration's decks."));
            var left = state.Kings?.Sovereigns.Where(s => s.Status != SovereignStatus.Initial).Select(s => s.Id).ToHashSet() ?? [];
            var present = state.Units.Select(u => u.Id).ToHashSet();
            var gained = present.Except(units).ToList();
            var lost = units.Where(id => !present.Contains(id) && !left.Contains(id)).ToList();
            Assert.True(gained.Count == 0 && lost.Count == 0,
                $"{name}: armies gained {string.Join(", ", gained)} and lost {string.Join(", ", lost)} in turn {state.Turn}.");
            Unique(state.Dice.Select(d => d.Id), "dice", name);
            Unique(state.HuntTiles.Select(t => t.Id), "hunt tiles", name);
            Unique(state.Characters.Select(c => c.Id), "characters", name);
        }

        private static IEnumerable<string> Cards(PlayerState player) =>
            player.Decks.Values.SelectMany(deck => deck).Concat(player.Hand).Concat(player.Discard).Concat(player.Table);

        private static void Unique(IEnumerable<string> ids, string what, string name)
        {
            var twice = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.True(twice.Count == 0, $"{name}: {what} twice: {string.Join(", ", twice)}.");
        }
    }

    private static MatchState SavedAndPrivate(MatchState state)
    {
        var saved = StateCodec.Write(state);
        var restored = StateCodec.Read(saved);
        Assert.Equal(saved, StateCodec.Write(restored));
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var original = Json(state, viewer);
            SameView(original, Json(restored, viewer), $"{viewer}'s view after reading the save");
            SameView(original, Json(HiddenInformation.Perturbed(state, viewer), viewer),
                $"{viewer}'s view when only the opponent's secrets change, turn {state.Turn}, {state.Pending?.Kind} " +
                $"from {state.Pending?.SourceId} ({state.Pending?.Presentation})");
        }
        return restored;
    }

    private static void SameView(string expected, string actual, string what)
    {
        if (expected != actual)
            Assert.Fail($"{what}: {FirstDifference(JsonNode.Parse(expected), JsonNode.Parse(actual), "$")}");
    }

    // The JSON path of the first difference and both values there.
    private static string FirstDifference(JsonNode? expected, JsonNode? actual, string path)
    {
        if (expected is JsonObject left && actual is JsonObject right)
        {
            foreach (var key in left.Select(p => p.Key).Union(right.Select(p => p.Key)))
                if (!JsonNode.DeepEquals(left[key], right[key]))
                    return FirstDifference(left[key], right[key], path + "." + key);
        }
        else if (expected is JsonArray first && actual is JsonArray second && first.Count == second.Count)
        {
            for (var index = 0; index < first.Count; index++)
                if (!JsonNode.DeepEquals(first[index], second[index]))
                    return FirstDifference(first[index], second[index], $"{path}[{index}]");
        }
        return $"{path}: {expected?.ToJsonString()} became {actual?.ToJsonString()}";
    }

    private static string Json(MatchState state, Side side) => JsonSerializer.Serialize(Views.For(state, side), BoardCatalog.Json);

    private static Module? OwnerOf(CommandKind kind) =>
        ModuleHandlers.Where(m => m.Handler.Handles(kind)).Select(m => (Module?)m.Module).SingleOrDefault();

    // Cards and characters name their module first ("lords.sh.character.17", "kings.character.ugluk"); others are base.
    private static Module? ModuleOf(string id) => id.Split('.')[0] switch
    {
        "lords" => Module.Lords,
        "kings" => Module.Kings,
        "fate" => Module.Fate,
        _ => null
    };

    private static bool Enabled(Module? module, MatchConfiguration configuration) => module switch
    {
        null => true,
        Module.Lords => configuration.Lords,
        Module.Council => configuration.Council,
        Module.Kings => configuration.Kings,
        Module.Dismay => ConfigurationRules.DismayEnabled(configuration),
        Module.Fate => configuration.Fate,
        _ => false
    };

    private static string Name(MatchConfiguration c)
    {
        var parts = new (bool On, string Name)[]
        {
            (c.Lords, "Lords"), (c.Council, "Council"), (c.Kings, "Kings"), (c.Dismay == true, "Dismay"),
            (c.VisibleCorruption, "Visible"), (c.SequentialCorruption, "Sequential"),
            (c.RemovingRulerTokens, "RemovingRulerTokens"), (c.Fate, "Fate"), (c.NewCities, "NewCities")
        }.Where(p => p.On).Select(p => p.Name).ToList();
        return parts.Count == 0 ? "Base" : string.Join("+", parts);
    }
}
