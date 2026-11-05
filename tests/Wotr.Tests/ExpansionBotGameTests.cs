using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// EXP-03: two demo CPUs play complete games of every expansion family and variant: every decision has a legal answer,
// the saved state reads back to the same state and the same projections, and neither side's projection changes when
// only the other side's hidden information does (hands, deck order, entropy, hidden corruption tiles).
public sealed class ExpansionBotGameTests(ITestOutputHelper output)
{
    private const int TurnLimit = 40;
    private const int StepLimit = 9000;
    // The save and the privacy are checked at every expansion decision and at every n-th other step.
    private const int CheckEvery = 25;
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);

    public static readonly IReadOnlyDictionary<string, MatchConfiguration> Families = new Dictionary<string, MatchConfiguration>
    {
        ["fate"] = new() { Fate = true },
        ["fate-new-cities"] = new() { Fate = true, NewCities = true },
        ["lords"] = new() { Lords = true },
        ["lords-council"] = new() { Lords = true, Council = true },
        ["kings"] = new() { Kings = true },
        ["kings-visible"] = new() { Kings = true, VisibleCorruption = true },
        ["kings-sequential"] = new() { Kings = true, SequentialCorruption = true },
        ["lords-kings"] = new() { Lords = true, Kings = true },
        ["lords-kings-no-dismay"] = new() { Lords = true, Kings = true, Dismay = false },
        ["lords-kings-removing"] = new() { Lords = true, Kings = true, RemovingRulerTokens = true },
        ["fate-lords"] = new() { Fate = true, Lords = true },
        ["fate-kings"] = new() { Fate = true, Kings = true },
        ["all"] = new() { Fate = true, Lords = true, Kings = true },
        ["all-variants"] = new()
        {
            Fate = true, Lords = true, Kings = true, NewCities = true, Council = true, VisibleCorruption = true,
            SequentialCorruption = true, RemovingRulerTokens = true
        }
    };

    // The families whose games between them raise every expansion decision but Dismay (see below).
    private static readonly string[] CoverageFamilies = { "all-variants", "lords-council", "kings-sequential", "lords-kings" };

    private static readonly DecisionKind[] ExpansionKinds =
    {
        DecisionKind.SpecialDieChoice, DecisionKind.RulerDiscard, DecisionKind.KeepActionDie, DecisionKind.Dismay,
        DecisionKind.CouncilVersions, DecisionKind.CouncilHomelands, DecisionKind.CouncilToken, DecisionKind.CorruptionTile
    };

    public static TheoryData<string, int> Games()
    {
        var games = new TheoryData<string, int>();
        foreach (var family in Families.Keys)
            foreach (var seed in new[] { 1, 2 })
                games.Add(family, seed);
        return games;
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void TwoBotsFinishAnExpansionGameWithSavesAndPrivacyIntact(string family, int seed)
    {
        var game = Play(family, seed);
        output.WriteLine($"{family} seed {seed}: turn {game.State.Turn}, {game.Steps} steps, winner " +
            $"{game.State.Winner?.ToString() ?? "none"} ({game.State.Victory}); {Describe(game.Decisions)}");
        Assert.True(game.State.Winner is not null || game.State.Turn > TurnLimit, $"Stopped at step {game.Steps} in turn {game.State.Turn}.");
    }

    // Across the families every expansion decision comes up at least once, except Dismay: the Shadow gets a Dismay
    // token only after the Free Peoples spend a Ruler token, which the simple CPU never does (a sovereign's token
    // action needs a full recruitment plan). The Dismay window below gives the CPU that decision directly.
    [Fact]
    public void EveryExpansionDecisionKindComesUp()
    {
        var seen = new HashSet<DecisionKind>();
        foreach (var family in CoverageFamilies)
            foreach (var seed in new[] { 1, 2, 3 })
                seen.UnionWith(Play(family, seed, check: false).Decisions.Keys);
        var missing = ExpansionKinds.Where(kind => kind != DecisionKind.Dismay && !seen.Contains(kind)).ToList();
        Assert.True(missing.Count == 0, "Never came up: " + string.Join(", ", missing));
    }

    // In a Dismay window the CPU either re-rolls an offered Ruler die with a Dismay token or keeps its tokens; its
    // seeded choice gives both over a few matches, and the engine accepts each.
    [Fact]
    public void TheBotAnswersADismayWindowBothWays()
    {
        var answers = new HashSet<CommandKind>();
        for (var match = 0; match < 12; match++)
        {
            var state = KingsIntegrationFixture.DismayWindow();
            state.Id = "expansion-bot-dismay-" + match;
            Assert.Equal(DecisionKind.Dismay, state.Pending!.Kind);
            var command = Bot.Next(state, state.Pending.Owner);
            Assert.NotNull(command);
            Assert.True(Engine.Execute(state, state.Pending.Owner, command, new SeededRandom(match)).Receipt.Accepted, command.Kind.ToString());
            answers.Add(command.Kind);
        }
        Assert.Contains(CommandKind.UseDismay, answers);
        Assert.Contains(CommandKind.DeclineDismay, answers);
    }

    private static Game Play(string family, int seed, bool check = true)
    {
        var random = new SeededRandom(seed);
        var state = MatchFactory.Create($"expansion-bot-{family}-{seed}", new SeededRandom(seed),
            ConfigurationRules.Normalize(Families[family]));
        TurnFlow.Start(state);
        var decisions = new Dictionary<DecisionKind, int>();
        var steps = 0;
        for (; steps < StepLimit && state.Winner is null && state.Turn <= TurnLimit; steps++)
        {
            var side = state.Pending!.Owner;
            var kind = state.Pending.Kind;
            decisions[kind] = decisions.GetValueOrDefault(kind) + 1;
            if (check && (ExpansionKinds.Contains(kind) || steps % CheckEvery == 0))
                state = Checked(state);
            var command = Bot.Next(state, side);
            Assert.True(command is not null, Unanswered(state, side));
            var result = Engine.Execute(state, side, command, random);
            Assert.True(result.Receipt.Accepted, $"{family} seed {seed}: {command.Kind} for {kind}: {result.Receipt.Error}");
            state = result.State;
        }
        if (state.Winner is not null)
            Assert.All(new[] { Side.FreePeoples, Side.Shadow }, side => Assert.Null(Bot.Next(state, side)));
        return new Game(state, steps, decisions);
    }

    // The saved state reads back to itself and to the same projections; each side's projection ignores the other
    // side's hidden information.
    private static MatchState Checked(MatchState state)
    {
        var saved = StateCodec.Write(state);
        var restored = StateCodec.Read(saved);
        Assert.Equal(saved, StateCodec.Write(restored));
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var original = Json(state, viewer);
            Assert.Equal(original, Json(restored, viewer));
            Assert.Equal(original, Json(HiddenInformation.Perturbed(state, viewer), viewer));
        }
        return restored;
    }

    private static string Json(MatchState state, Side side) => JsonSerializer.Serialize(Views.For(state, side), BoardCatalog.Json);

    private static string Describe(Dictionary<DecisionKind, int> decisions) =>
        string.Join(", ", decisions.OrderBy(d => d.Key.ToString()).Select(d => $"{d.Key} {d.Value}"));

    private static string Unanswered(MatchState state, Side side)
    {
        var view = Views.For(state, side);
        var options = view.Options.Select(o => $"{o.Kind}{(o.Enabled ? "" : " (off: " + o.Reason + ")")} " +
            $"[{o.Minimum}-{o.Maximum}] ids {string.Join("/", o.EligibleIds)} dice {string.Join("/", o.EligibleDice)} " +
            $"targets {string.Join("/", o.Targets)} choices {JsonSerializer.Serialize(o.EffectChoices)}");
        return $"{side} has no move for {state.Pending!.Kind} {state.Pending.Presentation} from {state.Pending.SourceId} " +
            $"(turn {state.Turn}, commands {string.Join("/", state.Pending.Commands)}): " + string.Join(", ", options);
    }

    private sealed record Game(MatchState State, int Steps, Dictionary<DecisionKind, int> Decisions);
}
