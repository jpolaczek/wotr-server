using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class LordsPersistenceTests
{
    [PostgresFact]
    public async Task SpecialDieChoiceRestartsWithItsOrderAndRetriesWithoutRollingAgain()
    {
        var state = LordsDiceTests.BothSidesRolling();
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(Enumerable.Repeat(5, 15).ToArray()));
        await Restart(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Gothmog });
    }

    [PostgresFact]
    public Task NaryaTransfersExactlyOneRingAndPersistsItsPoliticalChange()
    {
        var state = LordsNaryaTests.Ready();
        return Restart(state, CommandKind.UseNarya, LordsNaryaTests.Cost(state));
    }

    [PostgresFact]
    public async Task VilyaRestartsBeforeCompletingThePaidMovementWithoutRepeatingIt()
    {
        var state = LordsVilyaTests.Ready();
        state = LordsFixture.Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
        await Restart(state, CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
    }

    [PostgresFact]
    public Task NenyaPersistsRemovedTileAndReplacementBeforeRetry() =>
        Restart(LordsNenyaTests.Window(), CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id }, 0);

    internal static async Task Restart(MatchState state, CommandKind kind, CommandPayload payload, params int[] samples)
    {
        state.Id = Guid.NewGuid().ToString("N");
        var command = BattleFixture.Command(state, kind, payload);
        var actor = state.Pending!.Owner == Side.FreePeoples ? "alice" : "bob";
        var execution = LordsFixture.Engine().Execute(state, state.Pending.Owner, command, new SequenceRandom(samples));
        Assert.True(execution.Receipt.Accepted, execution.Receipt.Error);
        var expected = execution.State;
        string[] before;
        await using (var host = new TestHost())
        {
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            before = await ReadBoth(host, state.Id);
        }
        var entropy = new SequenceRandom(samples);
        string[] after;
        await using (var restarted = new TestHost(entropy))
        {
            Assert.Equal(before, await ReadBoth(restarted, state.Id));
            using var opponent = restarted.Client(actor == "alice" ? "bob" : "alice");
            var rejected = await Send(opponent, command);
            Assert.False(rejected.Accepted);
            Assert.Equal("NotAuthorized", rejected.Error);
            Assert.Equal(0, entropy.Calls);
            using var player = restarted.Client(actor);
            using var response = await player.PostAsJsonAsync($"/api/matches/{state.Id}/preview", command, BoardCatalog.Json);
            response.EnsureSuccessStatusCode();
            Assert.True((await response.Content.ReadFromJsonAsync<ActionPreview>(BoardCatalog.Json))!.Legal);
            Assert.Equal(0, entropy.Calls);
            Assert.Equal(before, await ReadBoth(restarted, state.Id));
            var receipt = await Send(player, command);
            Assert.True(receipt.Accepted, receipt.Error);
            Assert.Equal(state.Revision + 1, receipt.Revision);
            Assert.Equal(samples.Length, entropy.Calls);
            var saved = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, actor))!.State;
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(StateCodec.Write(expected)), JsonNode.Parse(StateCodec.Write(saved))));
            after = await ReadBoth(restarted, state.Id);
        }
        var noEntropy = new SequenceRandom();
        await using var returned = new TestHost(noEntropy);
        using var client = returned.Client(actor);
        var retry = await Send(client, command);
        Assert.True(retry.Accepted);
        Assert.Equal(state.Revision + 1, retry.Revision);
        Assert.Equal(0, noEntropy.Calls);
        Assert.Equal(after, await ReadBoth(returned, state.Id));
        var restored = (await returned.Services.GetRequiredService<MatchStore>().Read(state.Id, actor))!.State;
        Assert.Equal(expected.Resolution.PaidDieIds, restored.Resolution.PaidDieIds);
        Assert.Equal(expected.RandomSamples.Count, restored.RandomSamples.Count);
        Assert.Equal(expected.Rings.Select(r => r.Owner), restored.Rings.Select(r => r.Owner));
        Assert.Equal(expected.Dice.Select(d => d.Location), restored.Dice.Select(d => d.Location));
    }

    private static async Task<string[]> ReadBoth(TestHost host, string id)
    {
        using var fp = host.Client("alice");
        using var sh = host.Client("bob");
        return [await fp.GetStringAsync($"/api/matches/{id}"), await sh.GetStringAsync($"/api/matches/{id}")];
    }

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }
}
