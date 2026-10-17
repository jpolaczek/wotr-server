using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.BaseCorpusFixture;

namespace Wotr.Tests;

public class BaseCorpusPersistenceTests
{
    [PostgresFact]
    public Task FrozenSecretCombatRestartsAndRetriesThroughProductionHandlers() => Restart("secret-combat");

    [PostgresFact]
    public Task FrozenHuntRestartsAndRetriesThroughProductionHandlers() => Restart("hunt-reroll");

    [PostgresFact]
    public Task FrozenRingIntermediateStateRestartsWithoutPayingTwice() => Restart("ring-paid");

    private static async Task Restart(string name)
    {
        var entry = Case(name);
        var baseline = Load(entry);
        var id = Guid.NewGuid().ToString("N");
        var state = StateCodec.Read(StateCodec.Write(baseline).Replace(baseline.Id, id, StringComparison.Ordinal));
        var command = JsonSerializer.Deserialize<GameCommand>(Json(entry.Command!).Replace(baseline.Id, id, StringComparison.Ordinal), BoardCatalog.Json)!;
        var actor = state.Pending!.Owner == Side.FreePeoples ? "alice" : "bob";
        var expected = CharacterFixture.Engine().Execute(state, state.Pending.Owner, command, new ZeroRandom()).State;
        string[] before;
        await using (var host = new TestHost())
        {
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            before = await ReadBoth(host, id);
        }
        var entropy = new ZeroRandom();
        string[] after;
        await using (var restarted = new TestHost(entropy))
        {
            Assert.Equal(before, await ReadBoth(restarted, id));
            using var wrongPlayer = restarted.Client(actor == "alice" ? "bob" : "alice");
            var rejected = await Send(wrongPlayer, command);
            Assert.False(rejected.Accepted);
            Assert.Equal("NotAuthorized", rejected.Error);
            Assert.Equal(0, entropy.Calls);
            using var player = restarted.Client(actor);
            var receipt = await Send(player, command);
            Assert.True(receipt.Accepted, receipt.Error);
            Assert.Equal(state.Revision + 1, receipt.Revision);
            var saved = (await restarted.Services.GetRequiredService<MatchStore>().Read(id, actor))!.State;
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(StateCodec.Write(expected)), JsonNode.Parse(StateCodec.Write(saved))));
            after = await ReadBoth(restarted, id);
        }
        var noEntropy = new ZeroRandom();
        await using var returned = new TestHost(noEntropy);
        using var client = returned.Client(actor);
        var repeated = await Send(client, command);
        Assert.True(repeated.Accepted);
        Assert.Equal(state.Revision + 1, repeated.Revision);
        Assert.Equal(0, noEntropy.Calls);
        Assert.Equal(after, await ReadBoth(returned, id));
        var restored = (await returned.Services.GetRequiredService<MatchStore>().Read(id, actor))!.State;
        Assert.Equal(expected.RandomSamples.Count, restored.RandomSamples.Count);
        Assert.Equal(expected.Resolution.PaidDieIds, restored.Resolution.PaidDieIds);
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
