using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class InGameProjectionPersistenceTests
{
    [PostgresFact]
    public async Task BothHttpViewsAndPreviewArePureAcrossRestartAndLegacySnapshot()
    {
        var state = KingsFixture.Actions(Side.Shadow);
        state.Id = Guid.NewGuid().ToString("N");
        state.Nations.Single(n => n.Id == "dwarves").Active = true;
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards01.Mithril);
        var legacy = JsonNode.Parse(StateCodec.Write(state))!;
        legacy["schemaVersion"] = 1;
        state = StateCodec.Read(legacy.ToJsonString());
        var before = StateCodec.Write(state);
        var command = BattleFixture.Command(state, CommandKind.DiscardDie,
            new() { DieId = KingsFixture.Die(state, Side.Shadow).Id });
        string[] firstViews;
        string storedBefore;
        var entropy = new SequenceRandom();
        await using (var host = new TestHost(entropy))
        {
            var store = host.Services.GetRequiredService<MatchStore>();
            await ImportLegacy(host, state.Id, state.Revision, legacy.ToJsonString());
            storedBefore = await RawSnapshot(host, state.Id);
            Assert.Equal(1, JsonNode.Parse(storedBefore)!["schemaVersion"]!.GetValue<int>());
            firstViews = await Both(host, state.Id);
            using var shadow = host.Client("bob");
            using var preview = await shadow.PostAsJsonAsync($"/api/matches/{state.Id}/preview", command, BoardCatalog.Json);
            preview.EnsureSuccessStatusCode();
            Assert.True((await preview.Content.ReadFromJsonAsync<ActionPreview>(BoardCatalog.Json))!.Legal);
            Assert.Equal(firstViews, await Both(host, state.Id));
            AssertState(before, (await store.Read(state.Id, "alice"))!.State);
            Assert.Equal(storedBefore, await RawSnapshot(host, state.Id));
            Assert.Equal(0, entropy.Calls);
            var fp = JsonSerializer.Deserialize<PlayerView>(firstViews[0], BoardCatalog.Json)!;
            Assert.True(fp.Availability.Single(a => a.SubjectId == SovereignCatalog.Dain && a.ActionId == "awaken").ConditionsMet);
            Assert.False(fp.Availability.Single(a => a.SubjectId == SovereignCatalog.Dain && a.ActionId == "awaken").CanAct);
            Assert.Null(fp.Sovereigns.Single(s => s.Id == SovereignCatalog.Dain).Corruption);
            if (Environment.GetEnvironmentVariable("WOTR_INGAME_WIRE") is { Length: > 0 } path)
                await File.WriteAllTextAsync(path, firstViews[0]);
        }
        await using var restarted = new TestHost(new SequenceRandom());
        Assert.Equal(firstViews, await Both(restarted, state.Id));
        var restored = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "bob"))!.State;
        AssertState(before, restored);
        Assert.Equal(storedBefore, await RawSnapshot(restarted, state.Id));
    }

    [PostgresFact]
    public Task AvailabilityAndPointsFollowOneCommittedActionAndItsRecoveredReceipt()
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment();
        return LordsPersistenceTests.Restart(state, CommandKind.UseCharacterAbility, payload);
    }

    private static async Task<string[]> Both(TestHost host, string id)
    {
        using var fp = host.Client("alice");
        using var sh = host.Client("bob");
        return [await fp.GetStringAsync($"/api/matches/{id}"), await sh.GetStringAsync($"/api/matches/{id}")];
    }

    private static void AssertState(string expected, MatchState actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(StateCodec.Write(actual))),
            "Every persisted state field must remain unchanged; JSON object key order is not state.");

    private static async Task ImportLegacy(TestHost host, string id, long revision, string json)
    {
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var insert = Database.Command(connection, null,
            "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ($1,'alice','bob',$2,$3::jsonb)", id, revision, json);
        Assert.Equal(1, await insert.ExecuteNonQueryAsync());
    }

    private static async Task<string> RawSnapshot(TestHost host, string id)
    {
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var read = Database.Command(connection, null, "SELECT state::text FROM matches WHERE id=$1", id);
        return (string)(await read.ExecuteScalarAsync())!;
    }
}
