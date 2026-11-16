using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.BotProtocol;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// BOT-225: the public protocol seat asks every decision of a base game as the protocol's questions, built only from the
// server's own options and judged by the engine. A random protocol player answers them on one side; every command it
// completes must be accepted, and every decision without a question is counted by kind (the seat's open gaps).
public sealed class BotProtocolSeatTests(ITestOutputHelper output)
{
    private const int TurnLimit = 40;
    private const int StepLimit = 6000;
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly DemoBot Demo = new(Engine, Views);

    [Theory]
    [InlineData(1, Side.Shadow)]
    [InlineData(2, Side.FreePeoples)]
    [InlineData(3, Side.Shadow)]
    [InlineData(4, Side.FreePeoples)]
    [InlineData(5, Side.Shadow)]
    [InlineData(6, Side.FreePeoples)]
    [InlineData(7, Side.Shadow)]
    [InlineData(8, Side.FreePeoples)]
    public void EveryAnswerOfARandomProtocolPlayerIsALegalCommand(int seed, Side seat) => Play(seed, new[] { seat });

    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void TwoRandomProtocolPlayersPlayWholeGamesThroughTheSeat(int seed) => Play(seed, new[] { Side.FreePeoples, Side.Shadow });

    private void Play(int seed, Side[] seats)
    {
        var random = new SeededRandom(seed);
        var chooser = new Random(seed);
        var state = NewGame("protocol-seat-" + seed, seed);
        var memories = seats.ToDictionary(s => s, _ => new ProtocolMemory());
        var questions = new Dictionary<string, int>();
        var gaps = new Dictionary<string, int>();
        var times = new List<double>();
        var validations = new List<int>();
        var steps = 0;
        for (; steps < StepLimit && state.Winner is null && state.Turn <= TurnLimit; steps++)
        {
            var side = state.Pending!.Owner;
            GameCommand? command = null;
            if (seats.Contains(side))
            {
                var view = Views.For(state, side);
                var snapshot = state;
                var judged = 0;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var root = ProtocolQuestions.For(view, c => { judged++; return Engine.Validate(snapshot, side, c) is null; });
                if (root is null)
                {
                    Count(gaps, view.Decision!.Kind.ToString());
                    output.WriteLine("GAP " + System.Text.Json.JsonSerializer.Serialize(new { view.Decision, state.Pending.Commands,
                        options = view.Options.Where(o => o.Enabled), demo = Demo.Next(state, side) }));
                }
                else
                {
                    var decision = ProtocolDecision.Start(root);
                    _ = ProtocolView.Write(view, memories[side]);
                    while (decision.Question is { } question)
                    {
                        Count(questions, question.Question);
                        Assert.NotEmpty(question.Branches);
                        Assert.True(decision.Answer(chooser.Next(question.Branches.Count)));
                    }
                    command = decision.Command;
                    Assert.NotNull(command);
                    times.Add(watch.Elapsed.TotalMilliseconds);
                    validations.Add(judged);
                }
            }
            command ??= Demo.Next(state, side);
            Assert.NotNull(command);
            var result = Engine.Execute(state, side, command!, random);
            Assert.True(result.Receipt.Accepted, $"{command!.Kind}: {result.Receipt.Error} " +
                System.Text.Json.JsonSerializer.Serialize(new { command.Payload, state.Pending.Kind, state.Pending.Owner, seat = seats.Contains(side),
                    validate = Engine.Validate(state, side, command) }));
            state = result.State;
        }
        output.WriteLine($"seed {seed} seats {string.Join("+", seats)}: turn {state.Turn}, {steps} steps, winner {state.Winner?.ToString() ?? "none"}");
        output.WriteLine("questions: " + string.Join(", ", questions.OrderBy(q => q.Key).Select(q => $"{q.Key} {q.Value}")));
        output.WriteLine("gaps: " + string.Join(", ", gaps.OrderBy(q => q.Key).Select(q => $"{q.Key} {q.Value}")));
        Assert.Empty(gaps);
        times.Sort();
        validations.Sort();
        if (times.Count > 0)
            output.WriteLine($"seat ms p50 {times[times.Count / 2]:F1} p95 {times[(int)(times.Count * 0.95)]:F1} max {times[^1]:F1}; " +
                $"validations p50 {validations[validations.Count / 2]} max {validations[^1]}");
    }

    private static void Count(Dictionary<string, int> counts, string key) => counts[key] = counts.GetValueOrDefault(key) + 1;

    private static MatchState NewGame(string id, int seed)
    {
        var state = MatchFactory.Create(id, new SeededRandom(seed), ConfigurationRules.Normalize(new()));
        TurnFlow.Start(state);
        return state;
    }
}
