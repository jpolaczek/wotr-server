using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class KernelTests
{
    public static MatchState NewGame() => MatchFactory.Create("test", new SequenceRandom(new int[92]));
    public static GameCommand Command(MatchState state, CommandKind kind) => new()
    {
        MatchId = state.Id,
        CommandId = Guid.NewGuid().ToString("N"),
        ExpectedRevision = state.Revision,
        DecisionId = state.Pending!.Id,
        Kind = kind
    };
    [Fact]
    public void NestedReactionRestoresExactChoiceAndPaidCostAfterRestart()
    {
        var s = NewGame();
        s.Phase = GamePhase.ActionResolution;
        s.Open(DecisionKind.HuntDamage, Side.FreePeoples, []);
        var finalDecision = s.Pending!.Id;
        s.Dice[0].Location = DieLocation.Resolving;
        s.Resolution.PaidDieIds.Add(s.Dice[0].Id);
        s.Resolution.Suspend(s, "hunt-damage", 2);
        s.Open(DecisionKind.Reaction, Side.Shadow, [CommandKind.DeclineReaction]);
        s.Resolution.Suspend(s, "nested-reaction", 1);
        s.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        var restarted = StateCodec.Read(StateCodec.Write(s));
        var engine = new GameEngine(new DeclineReactionHandler());
        var noRandom = new SequenceRandom();
        var command = Command(s, CommandKind.DeclineReaction);
        var direct = engine.Execute(s, Side.FreePeoples, command, noRandom).State;
        var restored = engine.Execute(restarted, Side.FreePeoples, command, noRandom).State;
        Assert.Equal(StateCodec.Write(direct), StateCodec.Write(restored));
        Assert.Equal(Side.Shadow, restored.Pending!.Owner);
        restored = engine.Execute(restored, Side.Shadow, Command(restored, CommandKind.DeclineReaction), noRandom).State;
        Assert.Equal(finalDecision, restored.Pending!.Id);
        Assert.Single(restored.Resolution.PaidDieIds);
        Assert.Equal(DieLocation.Resolving, restored.Dice[0].Location);
        Assert.Equal(0, noRandom.Calls);
    }
    [Theory]
    [InlineData("actor")]
    [InlineData("revision")]
    [InlineData("decision")]
    [InlineData("choice")]
    public void InvalidCommandDoesNotMutateOrReadRandom(string invalid)
    {
        var s = NewGame();
        s.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        var before = StateCodec.Write(s);
        var command = Command(s, CommandKind.DeclineReaction);
        var side = Side.FreePeoples;
        if (invalid == "actor")
            side = Side.Shadow;
        if (invalid == "revision")
            command.ExpectedRevision++;
        if (invalid == "decision")
            command.DecisionId = "expired";
        if (invalid == "choice")
            command.Kind = CommandKind.Pass;
        var rng = new SequenceRandom();
        var result = new GameEngine(new DeclineReactionHandler()).Execute(s, side, command, rng);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(s));
        Assert.Equal(0, rng.Calls);
    }
    [Fact]
    public void ShuffleIsDeterministicAndRecordedWithoutDuplicatedCards()
    {
        var first = NewGame();
        var second = NewGame();
        Assert.Equal(StateCodec.Write(first), StateCodec.Write(second));
        Assert.Equal(92, first.RandomSamples.Count);
        Assert.All(first.Players, p => Assert.Empty(p.Hand));
        Assert.Equal(96, first.Players.SelectMany(p => p.Hand.Concat(p.Decks.Values.SelectMany(d => d))).Distinct().Count());
        Assert.Equal(7, first.Fellowship.Companions.Count);
        Assert.Equal("rivendell", first.Fellowship.RegionId);
        Assert.Equal(3, first.Rings.Count(r => r.Owner == Side.FreePeoples));
    }
    [Fact]
    public void UnsupportedSaveFailsClosed()
    {
        var state = NewGame();
        state.SchemaVersion = 900;
        Assert.Throws<InvalidDataException>(() => StateCodec.Read(StateCodec.Write(state)));
    }
}
