using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WOTR_TEST_DB")))
            Skip = "Set WOTR_TEST_DB to run real PostgreSQL integration tests.";
    }
}
public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WOTR_TEST_DB")))
            Skip = "Set WOTR_TEST_DB to run real PostgreSQL integration tests.";
    }
}
public class PostgresTests
{
    public static string Connection => Environment.GetEnvironmentVariable("WOTR_TEST_DB")!;
    private static MatchState AwaitingReaction()
    {
        var state = KernelTests.NewGame();
        state.Id = Guid.NewGuid().ToString("N");
        state.Open(DecisionKind.Action, Side.Shadow, []);
        state.Resolution.Suspend(state, "test", 1);
        state.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        return state;
    }
    private sealed class RandomReaction : ICommandHandler
    {
        public bool Handles(CommandKind kind) => kind == CommandKind.DeclineReaction;
        public string? Validate(MatchState state, Side actor, GameCommand command) => null;
        public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
        {
            state.Dice[0].Location = DieLocation.Spent;
            var result = random.Next(6);
            state.Resolution.Resume(state);
            state.Events.Add(new()
            {
                Revision = state.Revision,
                Kind = "TestRandom",
                OnlyFor = actor,
                Text = result.ToString()
            });
        }
    }
    [PostgresFact]
    public async Task RaceRetryConflictAndReloadPreserveOneSpendAndRandomSample()
    {
        await using var data = NpgsqlDataSource.Create(Connection);
        var db = new Database(data);
        await db.Migrate();
        var rng = new SequenceRandom(4);
        var engine = new GameEngine(new RandomReaction());
        var store = new MatchStore(db, engine, rng);
        var state = AwaitingReaction();
        await store.Create(state, "alice", "bob");
        var one = KernelTests.Command(state, CommandKind.DeclineReaction);
        var two = KernelTests.Command(state, CommandKind.DeclineReaction);
        var results = await Task.WhenAll(store.Execute(one, "alice"), store.Execute(two, "alice"));
        Assert.Single(results, r => r!.Accepted);
        Assert.Single(results, r => r!.Error == "StaleRevision");
        var winner = results[0]!.Accepted ? one : two;
        Assert.Equal(1, rng.Calls);
        var committed = (await store.Read(state.Id, "alice"))!.State;
        // A fresh pool/store has no in-memory state from the original service.
        await using var restartedData = NpgsqlDataSource.Create(Connection);
        var restarted = new MatchStore(new Database(restartedData), engine, new SequenceRandom());
        var receipt = await restarted.Execute(winner, "alice");
        Assert.True(receipt!.Accepted);
        Assert.Equal(1, receipt.Revision);
        Assert.Equal(StateCodec.Write(committed), StateCodec.Write((await restarted.Read(state.Id, "alice"))!.State));
        Assert.Equal(DieLocation.Spent, committed.Dice[0].Location);
        Assert.Equal(93, committed.RandomSamples.Count);
        Assert.Equal(Side.Shadow, committed.Pending!.Owner);
        var projection = new Projection(new CoreOptions());
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(projection.For(committed, side)), System.Text.Json.JsonSerializer.Serialize(projection.For((await restarted.Read(state.Id, side == Side.FreePeoples ? "alice" : "bob"))!.State, side)));
        winner.Payload.DieId = "different";
        Assert.Equal("CommandIdConflict", (await restarted.Execute(winner, "alice"))!.Error);
        Assert.Null(await restarted.Read(state.Id, "mallory"));
        await using var count = data.CreateCommand("SELECT count(*) FROM match_events WHERE match_id=$1");
        count.Parameters.Add(new NpgsqlParameter { Value = state.Id });
        Assert.Single(committed.Events, e => e.Kind == "TestRandom");
        Assert.Single(committed.Events, e => e.Kind == "ActionDecision");
        Assert.Equal((long)committed.Events.Count, await count.ExecuteScalarAsync());
    }
    [PostgresFact]
    public async Task CrashBeforeCommitLeavesNoPartialStateReceiptOrEvent()
    {
        await using var data = NpgsqlDataSource.Create(Connection);
        var db = new Database(data);
        await db.Migrate();
        var store = new MatchStore(db, new GameEngine(new RandomReaction()), new SequenceRandom(2));
        var state = AwaitingReaction();
        await store.Create(state, "alice", "bob");
        var request = KernelTests.Command(state, CommandKind.DeclineReaction);
        await Assert.ThrowsAsync<IOException>(() => store.Execute(request, "alice", beforeCommit: () => throw new IOException("Injected loss of process before commit")));
        Assert.Null(await store.Receipt(state.Id, request.CommandId, "alice"));
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(StateCodec.Write(state)),
            System.Text.Json.Nodes.JsonNode.Parse(StateCodec.Write((await store.Read(state.Id, "alice"))!.State))));
        await using var count = data.CreateCommand("SELECT count(*) FROM match_events WHERE match_id=$1");
        count.Parameters.Add(new NpgsqlParameter { Value = state.Id });
        Assert.Equal(0L, await count.ExecuteScalarAsync());
    }
    [PostgresFact]
    public async Task WrongRoleCannotResolveDecision()
    {
        await using var data = NpgsqlDataSource.Create(Connection);
        var db = new Database(data);
        await db.Migrate();
        var rng = new SequenceRandom();
        var store = new MatchStore(db, new GameEngine(new DeclineReactionHandler()), rng);
        var state = AwaitingReaction();
        await store.Create(state, "alice", "bob");
        var request = KernelTests.Command(state, CommandKind.DeclineReaction);
        Assert.Equal("NotAuthorized", (await store.Execute(request, "bob"))!.Error);
        Assert.Equal(0, rng.Calls);
        Assert.Equal(0, (await store.Read(state.Id, "alice"))!.State.Revision);
        Assert.Null(await store.Execute(request, "mallory"));
    }
}
