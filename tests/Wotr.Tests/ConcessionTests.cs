using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// RANK-03 (R35, docs/RANKED_PLAY.md): a concession and a timeout end the game from any decision, as a loss for the
// sender, leaving the dice, the Hunt and the resolution as a normal end of game does.
public class ConcessionTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    private static readonly string ExpansionFolder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Expansions");

    [Fact]
    public void TheCasesReachEveryDecisionKind() =>
        Assert.Equal(Enum.GetValues<DecisionKind>().Order(), Decisions().Select(d => d.State.Pending!.Kind).Distinct().Order());

    [Fact]
    public void EitherSideConcedesFromEveryDecision()
    {
        foreach (var (name, state) in Decisions())
            foreach (var side in Enum.GetValues<Side>())
            {
                var result = Engine.Execute(state, side, Command(state, CommandKind.Concede), new BaseCorpusFixture.ZeroRandom());
                Assert.True(result.Receipt.Accepted, $"{name}, {side} concedes: {result.Receipt.Error}");
                Ended(result.State, TurnFlow.Other(side), VictoryCondition.Conceded, name);
            }
    }

    [Fact]
    public void TheDecisionsOwnerLosesOnTimeFromEveryDecisionAndNobodyElseCan()
    {
        foreach (var (name, state) in Decisions())
        {
            var owner = state.Pending!.Owner;
            var result = Engine.Execute(state, owner, Command(state, CommandKind.TimeOut), new BaseCorpusFixture.ZeroRandom());
            Assert.True(result.Receipt.Accepted, $"{name}, {owner} times out: {result.Receipt.Error}");
            Ended(result.State, TurnFlow.Other(owner), VictoryCondition.TimedOut, name);
            var other = Engine.Execute(state, TurnFlow.Other(owner), Command(state, CommandKind.TimeOut), new BaseCorpusFixture.ZeroRandom());
            Assert.Equal("NotAuthorized", other.Receipt.Error);
        }
    }

    // The server checks a deadline against the decision it timed; a timeout that arrives after a move went through
    // must not end the next decision. A concession needs no turn at all.
    [Fact]
    public void ATimeOutNamesItsExactDecisionWhileAConcessionNeedsNoTurn()
    {
        var state = First();
        var owner = state.Pending!.Owner;
        var stale = Command(state, CommandKind.TimeOut);
        stale.ExpectedRevision--;
        Assert.Equal("StaleRevision", Engine.Execute(state, owner, stale, new BaseCorpusFixture.ZeroRandom()).Receipt.Error);
        var moved = Command(state, CommandKind.TimeOut);
        moved.DecisionId = "another-decision";
        Assert.Equal("DecisionExpired", Engine.Execute(state, owner, moved, new BaseCorpusFixture.ZeroRandom()).Receipt.Error);
        var late = Command(state, CommandKind.Concede);
        late.ExpectedRevision--;
        late.DecisionId = "another-decision";
        Assert.True(Engine.Execute(state, TurnFlow.Other(owner), late, new BaseCorpusFixture.ZeroRandom()).Receipt.Accepted);
    }

    [Fact]
    public void AFinishedGameRefusesBothAndNothingRidesOnAConcession()
    {
        var state = First();
        var finished = Engine.Execute(state, Side.Shadow, Command(state, CommandKind.Concede), new BaseCorpusFixture.ZeroRandom()).State;
        Assert.Equal("GameFinished", Engine.Execute(finished, Side.FreePeoples, Command(finished, CommandKind.Concede),
            new BaseCorpusFixture.ZeroRandom()).Receipt.Error);
        var loaded = Command(state, CommandKind.Concede);
        loaded.Payload.RulerTokenId = "any-token";
        Assert.Equal("IllegalChoice", Engine.Execute(state, Side.Shadow, loaded, new BaseCorpusFixture.ZeroRandom()).Receipt.Error);
    }

    [Fact]
    public void TheEndingIsRecordedAndNamed()
    {
        var state = First();
        var after = Engine.Execute(state, Side.Shadow, Command(state, CommandKind.Concede), new BaseCorpusFixture.ZeroRandom()).State;
        Assert.Equal(state.Revision + 1, after.Revision);
        var ending = Assert.Single(after.Events, e => e.Kind == nameof(VictoryCondition.Conceded));
        Assert.Equal("Shadow conceded. FreePeoples wins.", ending.Text);
        Assert.Equal("Conceded the game.", ActionCommandText.Describe(CommandKind.Concede));
        Assert.Equal("Ran out of time.", ActionCommandText.Describe(CommandKind.TimeOut));
    }

    // R11: an ending reveals nothing a player could not already see; each side's hand stays its own.
    [Fact]
    public void AConcededGameRevealsNoHiddenCard()
    {
        foreach (var (name, state) in Decisions())
        {
            var after = Engine.Execute(state, Side.Shadow, Command(state, CommandKind.Concede), new BaseCorpusFixture.ZeroRandom()).State;
            foreach (var viewer in Enum.GetValues<Side>())
            {
                var before = Json(Views.For(state, viewer));
                var hidden = state.Player(TurnFlow.Other(viewer)).Hand.Where(card => !before.Contains(card)).ToList();
                var ended = Json(Views.For(after, viewer));
                Assert.All(hidden, card => Assert.DoesNotContain(card, ended));
            }
        }
    }

    private static void Ended(MatchState state, Side winner, VictoryCondition condition, string name)
    {
        Assert.True(winner == state.Winner, name);
        Assert.True(condition == state.Victory, name);
        Assert.True(GamePhase.Finished == state.Phase, name);
        Assert.True(state.Pending is null, name);
        Assert.True(state.Resolution.Frames.Count == 0, name);
        Assert.True(state.Resolution.PaidDieIds.Count == 0, name);
        Assert.False(state.Dice.Any(d => d.Location == DieLocation.Resolving), name);
        Assert.False(state.HuntTiles.Any(t => t.Location == "drawn"), name);
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(condition, Views.For(state, side).Victory);
    }

    private static GameCommand Command(MatchState state, CommandKind kind) => new()
    {
        CommandId = Guid.NewGuid().ToString("N"),
        MatchId = state.Id,
        DecisionId = state.Pending?.Id ?? "",
        ExpectedRevision = state.Revision,
        Kind = kind,
        Payload = new()
    };

    private static MatchState First() => Decisions().First().State;

    // Every decision kind: the frozen base and expansion saves, and Kings' Dismay window, whose saves the corpora omit.
    private static IEnumerable<(string Name, MatchState State)> Decisions()
    {
        foreach (var entry in BaseCorpusFixture.Cases.Where(c => c.Decision.HasValue))
            yield return ("base " + entry.Name, BaseCorpusFixture.Load(entry));
        foreach (var file in ExpansionFiles())
            yield return ("expansion " + file, StateCodec.Read(File.ReadAllText(Path.Combine(ExpansionFolder, file))));
        var dismay = KingsIntegrationFixture.DismayWindow();
        TurnFlow.BeginActions(dismay);
        yield return ("dismay window", dismay);
    }

    private static IEnumerable<string> ExpansionFiles()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(ExpansionFolder, "manifest.json")));
        return manifest.RootElement.GetProperty("cases").EnumerateArray()
            .Where(c => c.GetProperty("decision").ValueKind == JsonValueKind.String)
            .Select(c => c.GetProperty("file").GetString()!).ToList();
    }

    private static string Json(object value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
}
