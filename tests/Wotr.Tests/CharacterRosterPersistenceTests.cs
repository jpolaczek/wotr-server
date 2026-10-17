using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.CharacterRosterFixture;

namespace Wotr.Tests;

public class CharacterRosterPersistenceTests
{
    [PostgresFact]
    public async Task BothHttpRostersAndPreviewArePureAcrossLegacySnapshotAndRestart()
    {
        var state = Actions(7, Side.Shadow, council: true);
        state.Id = Guid.NewGuid().ToString("N");
        state.MinionEverEntered = true;
        Put(state, LordsCharacters.Gandalf, "casualties", CharacterRules.Grey);
        Put(state, FateGimli.Alternate, "iron-hills", FateGimli.Figure);
        state.Lords!.Council!.GrantedTokens.Add(CouncilTokens.Travel);
        var legacy = JsonNode.Parse(StateCodec.Write(state))!;
        legacy["schemaVersion"] = 1;
        state = StateCodec.Read(legacy.ToJsonString());
        var before = StateCodec.Write(state);
        var entropy = new SequenceRandom();
        string[] first;
        string stored;
        await using (var host = new TestHost(entropy))
        {
            await ImportLegacy(host, state.Id, state.Revision, legacy.ToJsonString());
            stored = await Snapshot(host, state.Id);
            Assert.Equal(1, JsonNode.Parse(stored)!["schemaVersion"]!.GetValue<int>());
            first = await Both(host, state.Id);
            var fp = JsonSerializer.Deserialize<PlayerView>(first[0], BoardCatalog.Json)!;
            var grey = fp.OwnCharacterRoster.Single(r => r.FigureId == CharacterRules.Grey);
            Assert.Equal(CharacterRosterLifecycle.EliminatedWithReplacement, grey.Lifecycle);
            Assert.Equal(LordsCharacters.Gandalf, grey.CurrentDefinitionId);
            Assert.True(grey.FutureVersions.Single().Entry.ConditionsMet);
            Assert.False(grey.FutureVersions.Single().Entry.CanAct);
            Assert.DoesNotContain(CharacterRules.Grey, JsonSerializer.Deserialize<PlayerView>(first[1], BoardCatalog.Json)!
                .OwnCharacterRoster.Select(r => r.FigureId));
            using var client = host.Client("bob");
            var command = BattleFixture.Command(state, CommandKind.DiscardDie,
                new() { DieId = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id });
            using var preview = await client.PostAsJsonAsync($"/api/matches/{state.Id}/preview", command, BoardCatalog.Json);
            preview.EnsureSuccessStatusCode();
            Assert.True((await preview.Content.ReadFromJsonAsync<ActionPreview>(BoardCatalog.Json))!.Legal);
            Assert.Equal(first, await Both(host, state.Id));
            Assert.Equal(stored, await Snapshot(host, state.Id));
            AssertState(before, (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State);
            Assert.Equal(0, entropy.Calls);
            await Capture("WOTR_CHARACTER_ROSTER_WIRE", first[0]);
            await Capture("WOTR_CHARACTER_ROSTER_SHADOW_WIRE", first[1]);
        }
        var restartedEntropy = new SequenceRandom();
        await using var restarted = new TestHost(restartedEntropy);
        Assert.Equal(first, await Both(restarted, state.Id));
        Assert.Equal(stored, await Snapshot(restarted, state.Id));
        AssertState(before, (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "bob"))!.State);
        Assert.Equal(0, restartedEntropy.Calls);
    }

    [PostgresFact]
    public async Task PrivateCouncilVersionsHaveIdenticalCompleteShadowHttpViewAndErrorAfterRestart()
    {
        var standard = CouncilSetupTests.Versions();
        var alternate = CouncilSetupTests.AllVersions();
        standard.Id = alternate.Id = Guid.NewGuid().ToString("N");
        var command = BattleFixture.Command(standard, CommandKind.EnterCharacter,
            new() { CharacterId = CharacterRules.Aragorn, RegionId = "minas-tirith", DieId = standard.Dice[0].Id });
        string shadowBefore;
        string errorBefore;
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(standard, "alice", "bob");
            shadowBefore = (await Both(first, standard.Id))[1];
            errorBefore = await RejectedPreview(first, command);
            await ReplaceSnapshot(first, alternate);
        }
        var entropy = new SequenceRandom();
        await using var restarted = new TestHost(entropy);
        var before = await Snapshot(restarted, alternate.Id);
        var views = await Both(restarted, alternate.Id);
        Assert.Equal(shadowBefore, views[1]);
        Assert.Equal(errorBefore, await RejectedPreview(restarted, command));
        var own = JsonSerializer.Deserialize<PlayerView>(views[0], BoardCatalog.Json)!;
        Assert.Equal(CouncilCompanions.Strider, own.OwnCharacterRoster.Single(r => r.FigureId == CharacterRules.Strider).CurrentDefinitionId);
        Assert.Equal(before, await Snapshot(restarted, alternate.Id));
        Assert.Equal(0, entropy.Calls);
    }

    [PostgresFact]
    public Task WhiteReturnRestartsBeforeAndAfterOneTransformationAndOneCost()
    {
        var state = Actions(1);
        Put(state, LordsCharacters.Gandalf, "casualties", CharacterRules.Grey);
        state.MinionEverEntered = true;
        return LordsPersistenceTests.Restart(state, CommandKind.EnterCharacter,
            new() { CharacterId = CharacterRules.White, RegionId = "fangorn", DieId = state.Dice[0].Id });
    }

    [PostgresFact]
    public Task WitchKingReplacementKeepsOneIdentityAndItsPermanentLimitAcrossRestart()
    {
        var state = Actions(1, Side.Shadow);
        Put(state, LordsCharacters.WitchKing, "hollin", CharacterRules.WitchKing);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        return LordsPersistenceTests.Restart(state, CommandKind.EnterCharacter, new()
        {
            CharacterId = CharacterRules.WitchKing, RegionId = "hollin",
            DieId = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id
        });
    }

    [PostgresFact]
    public Task ExplicitHobbitReturnPreservesTheQueuedSecondFigureAcrossRestart()
    {
        var state = CouncilHobbitBattleTests.ReturnDecision(both: true);
        var target = new EffectOptions().GetOptions(state, Side.FreePeoples).Single().EligibleIds.First();
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [target] });
    }

    private static async Task Capture(string variable, string json)
    {
        if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } path)
            await File.WriteAllTextAsync(path, json);
    }

    private static async Task<string[]> Both(TestHost host, string id)
    {
        using var fp = host.Client("alice");
        using var sh = host.Client("bob");
        return [await fp.GetStringAsync($"/api/matches/{id}"), await sh.GetStringAsync($"/api/matches/{id}")];
    }

    private static async Task<string> RejectedPreview(TestHost host, GameCommand command)
    {
        using var client = host.Client("bob");
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/preview", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(JsonSerializer.Deserialize<ActionPreview>(body, BoardCatalog.Json)!.Legal);
        return body;
    }

    private static void AssertState(string expected, MatchState actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(StateCodec.Write(actual))));

    private static async Task ImportLegacy(TestHost host, string id, long revision, string json)
    {
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var insert = Database.Command(connection, null,
            "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ($1,'alice','bob',$2,$3::jsonb)", id, revision, json);
        Assert.Equal(1, await insert.ExecuteNonQueryAsync());
    }

    private static async Task ReplaceSnapshot(TestHost host, MatchState state)
    {
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var update = Database.Command(connection, null,
            "UPDATE matches SET state=$2::jsonb WHERE id=$1", state.Id, StateCodec.Write(state));
        Assert.Equal(1, await update.ExecuteNonQueryAsync());
    }

    private static async Task<string> Snapshot(TestHost host, string id)
    {
        var database = host.Services.GetRequiredService<Database>();
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var read = Database.Command(connection, null, "SELECT state::text FROM matches WHERE id=$1", id);
        return (string)(await read.ExecuteScalarAsync())!;
    }
}
