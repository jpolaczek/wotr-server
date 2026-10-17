using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.AutoDecisionTests;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class AutoDecisionPersistenceTests
{
    [PostgresFact]
    public async Task BothAttackerRolesRestoreCommittedSkipAndCancelFutureAnswersAfterHostRestart()
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            var state = Battle(side);
            var account = side == Side.FreePeoples ? "alice" : "bob";
            var other = side == Side.FreePeoples ? "bob" : "alice";
            var enable = Change(state, side, true);
            GameCommand cancel;
            await using (var host = new TestHost())
            {
                var store = host.Services.GetRequiredService<MatchStore>();
                await store.Create(state, "alice", "bob");
                using var client = host.Client(account);
                Assert.Equal(state.Revision + 1, (await Send(client, enable)).Revision);
                state = (await store.Read(state.Id, account))!.State;
                cancel = Change(state, side, false);
            }
            var rng = new SequenceRandom(0, 0);
            await using var restarted = new TestHost(rng);
            using var returned = restarted.Client(account);
            using var opponent = restarted.Client(other);
            Assert.Equal(state.Revision, (await Send(returned, enable)).Revision);
            Assert.Equal(0, rng.Calls);
            await Send(opponent, Command(state, CommandKind.CommitCombatCard));
            var result = await Send(returned, cancel);
            Assert.Equal(1, result.AutoDecision!.LastSkippedRound);
            Assert.Null(result.AutoDecision.Policy);
            Assert.False(result.PublicStateChanged);
            Assert.Equal(2, rng.Calls);
            var final = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, account))!.State;
            Assert.Equal(DecisionKind.BattleContinue, final.Pending!.Kind);
            Assert.Equal(2, final.Events.Count(e => e.Kind == "CombatCardCommitted"));
        }
    }

    [PostgresFact]
    public async Task PrivatePreferenceAndReceiptSurviveRestartAndNeverNotifyOrExposeTheOpponent()
    {
        var state = Battle();
        var change = Change(state, Side.Shadow, true);
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            using var bob = first.Client("bob");
            using var outsider = first.Client("mallory");
            var observerBefore = await alice.GetStringAsync($"/api/matches/{state.Id}");
            var hub = first.Services.GetRequiredService<RevisionHub>();
            var subscription = hub.Subscribe(state.Id);
            var ownSubscription = hub.Subscribe(state.Id, "bob");
            try
            {
                var result = await Send(bob, change);
                Assert.False(result.PublicStateChanged);
                Assert.Equal(state.Revision, result.Revision);
                Assert.NotNull(result.AutoDecision!.Policy);
                Assert.False(subscription.Channel.Reader.TryRead(out _));
                Assert.True(ownSubscription.Channel.Reader.TryRead(out var ownRevision));
                Assert.Equal(state.Revision, ownRevision);
                Assert.Equal(new MatchVersion(state.Revision, 1), await first.Services.GetRequiredService<MatchStore>().ReadVersion(state.Id, "bob", default));
                Assert.Equal(new MatchVersion(state.Revision, 0), await first.Services.GetRequiredService<MatchStore>().ReadVersion(state.Id, "alice", default));
                Assert.Equal(observerBefore, await alice.GetStringAsync($"/api/matches/{state.Id}"));
                Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/matches/{state.Id}/receipts/{change.CommandId}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync($"/api/matches/{state.Id}/commands", change, BoardCatalog.Json)).StatusCode);
            }
            finally { hub.Remove(state.Id, subscription.Id); hub.Remove(state.Id, ownSubscription.Id); }
        }
        var rng = new SequenceRandom(0, 0);
        await using var restarted = new TestHost(rng);
        using var returnedBob = restarted.Client("bob");
        using var returnedAlice = restarted.Client("alice");
        Assert.Equal(1, (await Send(returnedBob, change)).AutoDecision!.Version);
        var commit = Command(state, CommandKind.CommitCombatCard);
        Assert.True((await Send(returnedAlice, commit)).PublicStateChanged);
        Assert.True((await Send(returnedAlice, commit)).Accepted);
        Assert.Equal(2, rng.Calls);
        var restored = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "bob"))!.State;
        Assert.Equal(DecisionKind.BattleContinue, restored.Pending!.Kind);
        Assert.Equal(1, restored.Player(Side.Shadow).AutoDecision.LastSkippedRound);
        Assert.Equal(2, restored.Events.Count(e => e.Kind == "CombatCardCommitted"));
        var originalReceipt = await Send(returnedBob, change);
        Assert.Equal(0, originalReceipt.AutoDecision!.LastSkippedRound);
        change.Payload.AutoDecision!.Enabled = false;
        using var conflict = await returnedBob.PostAsJsonAsync($"/api/matches/{state.Id}/commands", change, BoardCatalog.Json);
        Assert.Equal("CommandIdConflict", (await conflict.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Error);
    }

    [PostgresFact]
    public async Task CancellationRacingWithOpponentCommitHasOnlyTheTwoSerialOutcomes()
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var db = new Database(data);
        await db.Migrate();
        foreach (var cancelFirst in new[] { true, false })
        {
            var state = Set(Battle(), Side.Shadow, true);
            var rng = new SequenceRandom(0, 0);
            var store = new MatchStore(db, Engine(), rng);
            await store.Create(state, "alice", "bob");
            var cancel = Change(state, Side.Shadow, false);
            var commit = Command(state, CommandKind.CommitCombatCard);
            var firstLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = cancelFirst == false ? store.Execute(commit, "alice", beforeCommit: Pause) : store.Execute(cancel, "bob", beforeCommit: Pause);
            await firstLocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var second = cancelFirst == false ? store.Execute(cancel, "bob") : store.Execute(commit, "alice");
            release.SetResult();
            var results = await Task.WhenAll(first, second);
            Assert.All(results, r => Assert.True(r!.Accepted, r.Error));
            var restored = (await store.Read(state.Id, "bob"))!.State;
            Assert.Null(restored.Player(Side.Shadow).AutoDecision.Policy);
            Assert.Equal(state.Revision + 1, restored.Revision);
            Assert.Equal(cancelFirst == false ? DecisionKind.BattleContinue : DecisionKind.CombatCard, restored.Pending!.Kind);
            Assert.Equal(cancelFirst == false ? 1 : 0, restored.Player(Side.Shadow).AutoDecision.LastSkippedRound);
            Assert.Equal(cancelFirst == false ? 2 : 0, rng.Calls);
            Assert.True((await store.Execute(cancel, "bob"))!.Accepted);
            Assert.Equal(StateCodec.Write(restored), StateCodec.Write((await store.Read(state.Id, "bob"))!.State));
            async Task Pause()
            { firstLocked.SetResult(); await release.Task; }
        }
    }

    [PostgresFact]
    public async Task RollbackKeepsThePreferenceDecisionAndRandomResultsAtomic()
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        var db = new Database(data);
        await db.Migrate();
        var state = Set(Battle(), Side.Shadow, true);
        var store = new MatchStore(db, Engine(), new SequenceRandom(0, 0));
        await store.Create(state, "alice", "bob");
        var change = Change(state, Side.FreePeoples, true);
        await Assert.ThrowsAsync<IOException>(() => store.Execute(change, "alice", beforeCommit: () => throw new IOException("Injected process loss")));
        Assert.Null(await store.Receipt(state.Id, change.CommandId, "alice"));
        var restored = (await store.Read(state.Id, "alice"))!.State;
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(StateCodec.Write(state)), System.Text.Json.Nodes.JsonNode.Parse(StateCodec.Write(restored))));
        var restarted = new MatchStore(db, Engine(), new SequenceRandom(0, 0));
        Assert.True((await restarted.Execute(change, "alice"))!.Accepted);
        Assert.Equal(2, (await restarted.Read(state.Id, "alice"))!.State.RandomSamples.Count);
    }

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        return receipt;
    }
}
