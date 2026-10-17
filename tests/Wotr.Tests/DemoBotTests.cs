using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// DEMO-02 (DM04, DM05): the demo CPU plays only its own decisions, every move it makes passes the engine, the same
// state gives the same move, and two CPUs play the base game from its start to the end without a decision they
// cannot answer.
public sealed class DemoBotTests(ITestOutputHelper output)
{
    private const int TurnLimit = 40;
    private const int StepLimit = 6000;
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Bot = new(Engine, Views);

    [Fact]
    public void TheBotPlaysOnlyItsOwnDecisionAndTheSameStateGivesTheSameMove()
    {
        var state = NewGame("demo-bot-own", 1);
        var owner = state.Pending!.Owner;
        Assert.Null(Bot.Next(state, Other(owner)));
        var command = Bot.Next(state, owner)!;
        Assert.Equal(JsonSerializer.Serialize(command), JsonSerializer.Serialize(Bot.Next(state, owner)));
        Assert.True(Engine.Execute(state, owner, command, new SeededRandom(1)).Receipt.Accepted);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void TwoBotsPlayTheBaseGameWithoutAnUnansweredDecision(int seed)
    {
        var random = new SeededRandom(seed);
        var state = NewGame("demo-bot-" + seed, seed);
        var decisions = new Dictionary<DecisionKind, int>();
        var steps = 0;
        for (; steps < StepLimit && state.Winner is null && state.Turn <= TurnLimit; steps++)
        {
            var side = state.Pending!.Owner;
            decisions[state.Pending.Kind] = decisions.GetValueOrDefault(state.Pending.Kind) + 1;
            var command = Bot.Next(state, side);
            Assert.True(command is not null, Describe(state, side));
            var result = Engine.Execute(state, side, command, random);
            Assert.True(result.Receipt.Accepted, $"{command.Kind}: {result.Receipt.Error}");
            state = result.State;
        }
        output.WriteLine($"seed {seed}: turn {state.Turn}, {steps} steps, winner {state.Winner?.ToString() ?? "none"} ({state.Victory})");
        output.WriteLine(string.Join(", ", decisions.OrderBy(d => d.Key.ToString()).Select(d => $"{d.Key} {d.Value}")));
        Assert.True(state.Winner is not null || state.Turn > TurnLimit, $"Stopped at step {steps} in turn {state.Turn}.");
        if (state.Winner is not null)
            Assert.All(new[] { Side.FreePeoples, Side.Shadow }, side => Assert.Null(Bot.Next(state, side)));
    }

    private static MatchState NewGame(string id, int seed)
    {
        var state = MatchFactory.Create(id, new SeededRandom(seed), ConfigurationRules.Normalize(new()));
        TurnFlow.Start(state);
        return state;
    }

    private static Side Other(Side side) => side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;

    private static string Describe(MatchState state, Side side)
    {
        var view = Views.For(state, side);
        var options = view.Options.Select(o => $"{o.Kind}{(o.Enabled ? "" : " (off: " + o.Reason + ")")} " +
            $"[{o.Minimum}-{o.Maximum}] ids {string.Join("/", o.EligibleIds)} targets {string.Join("/", o.Targets)} " +
            $"armies {JsonSerializer.Serialize(o.Armies)} costs {JsonSerializer.Serialize(o.MovementCosts)} " +
            $"choices {JsonSerializer.Serialize(o.EffectChoices)} event {JsonSerializer.Serialize(o.ArmyEvent)} recruit {JsonSerializer.Serialize(o.EventRecruitment)}");
        return $"{side} has no move for {state.Pending!.Kind} {state.Pending.Presentation} from {state.Pending.SourceId} " +
            $"(turn {state.Turn}, commands {string.Join("/", state.Pending.Commands)}): " + string.Join(", ", options);
    }
}
