using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class VictoryPersistenceTests
{
    [PostgresFact]
    public async Task HttpRestartsBeforeAndAfterBothRingEndingsAndKeepsTheResultImmutable()
    {
        foreach (var corruption in new[] { 10, 11 })
        {
            var state = MordorTests.OnTrack(4, corruption);
            state.Id = Guid.NewGuid().ToString("N");
            HuntTests.Pool(state, "base.hunt.standard.1.0.1");
            var move = FellowshipTests.Command(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
            await using (var first = new TestHost(new SequenceRandom(0)))
            {
                await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
                using var alice = first.Client("alice");
                Assert.False((await Read(alice, state.Id)).Fellowship.MordorAttemptedThisTurn);
                Assert.True((await Send(alice, move)).Accepted);
                var before = await Read(alice, state.Id);
                Assert.Equal(4, before.Fellowship.MordorStep);
                Assert.Null(before.Winner);
                Assert.True(before.Fellowship.MordorAttemptedThisTurn);
            }

            GameCommand damage;
            await using (var second = new TestHost(new SequenceRandom()))
            {
                using var alice = second.Client("alice");
                Assert.Equal(1, (await Send(alice, move)).Revision);
                var restored = await Read(alice, state.Id);
                Assert.True(restored.Fellowship.MordorAttemptedThisTurn);
                Assert.Equal(DecisionKind.HuntDamage, restored.Decision!.Kind);
                Assert.Equal(DieLocation.Resolving, restored.Dice[0].Location);
                damage = new()
                {
                    MatchId = state.Id,
                    CommandId = Guid.NewGuid().ToString("N"),
                    ExpectedRevision = restored.Revision,
                    DecisionId = restored.Decision.Id,
                    Kind = CommandKind.ResolveHuntDamage,
                    Payload = new() { HuntResponse = HuntResponse.Ring }
                };
                Assert.True((await Send(alice, damage)).Accepted);
            }

            await using var third = new TestHost(new SequenceRandom());
            using var returned = third.Client("alice");
            using var bob = third.Client("bob");
            var result = await Read(returned, state.Id);
            var enemy = await Read(bob, state.Id);
            Assert.Equal(corruption == 10 ? Side.FreePeoples : Side.Shadow, result.Winner);
            Assert.Equal(corruption == 10 ? VictoryCondition.RingDestroyed : VictoryCondition.RingCorrupted, result.Victory);
            Assert.Equal(result.Victory, enemy.Victory);
            Assert.Equal(result.Winner, enemy.Winner);
            Assert.True(result.Fellowship.MordorAttemptedThisTurn);
            Assert.True(enemy.Fellowship.MordorAttemptedThisTurn);
            Assert.Null(result.Decision);
            Assert.Empty(result.Options);
            Assert.Equal(2, (await Send(returned, damage)).Revision);
            var mutate = new GameCommand
            {
                MatchId = state.Id,
                ExpectedRevision = result.Revision,
                CommandId = Guid.NewGuid().ToString("N"),
                Kind = CommandKind.HideFellowship,
                Payload = new() { DieId = state.Dice[1].Id }
            };
            var rejected = await Send(returned, mutate);
            Assert.False(rejected.Accepted);
            Assert.Equal("GameFinished", rejected.Error);
            var persisted = (await third.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
            Assert.Equal(2, persisted.Revision);
            Assert.Equal(state.RandomSamples.Count + 1, persisted.RandomSamples.Count);
            Assert.Single(persisted.Events, e => e.Kind == result.Victory.ToString());
            Assert.Empty(persisted.Resolution.Frames);
            Assert.Equal(DieLocation.Hunt, persisted.Dice[0].Location);
        }
    }

    private static async Task<PlayerView> Read(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }
}
