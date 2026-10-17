using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.ActionLogFixture;

namespace Wotr.Tests;

public class ActionLogPersistenceTests
{
    [PostgresFact]
    public async Task RealAccountsReadBothMovesAndFinishAcrossHostRestartsWithoutMutationOrDuplicateCost()
    {
        foreach (var finish in new[] { false, true })
        {
            await using var fixture = await CorrespondenceFixture.Create();
            var (state, first, secondUnit) = Army(DieFace.WillOfTheWest);
            await fixture.Save(state);
            var initial = await Page(fixture.Sh, state.Id);
            Assert.Empty(initial.Steps);
            using (var preview = await fixture.Sh.PostAsJsonAsync($"/api/matches/{state.Id}/preview", first, BoardCatalog.Json))
                Assert.True((await preview.Content.ReadFromJsonAsync<ActionPreview>(BoardCatalog.Json))!.Legal);
            Assert.Empty((await Page(fixture.Sh, state.Id)).Steps);
            await CorrespondenceFixture.Send(fixture.Sh, first);
            var page = await Page(fixture.Sh, state.Id);
            if (!finish && Environment.GetEnvironmentVariable("WOTR_ACTION_LOG_WIRE_PATH") is { Length: > 0 } wirePath)
                await File.WriteAllTextAsync(wirePath, JsonSerializer.Serialize(page, BoardCatalog.Json));
            var before = JsonSerializer.Serialize(page, BoardCatalog.Json);
            var root = Assert.Single(page.Actions).Id;
            AssertArmy(Assert.Single(page.Steps.SelectMany(s => s.Movements)));
            await fixture.Restart();
            Assert.Equal(before, JsonSerializer.Serialize(await Page(fixture.Sh, state.Id), BoardCatalog.Json));
            Assert.Equal(1, (await CorrespondenceFixture.Send(fixture.Sh, first)).Revision);
            state = (await fixture.Matches.Read(state.Id, fixture.Shadow.AccountId))!.State;
            var second = BattleFixture.Command(state, finish ? CommandKind.FinishActionSequence : CommandKind.MoveArmies,
                finish ? new() : ActionSequenceFixture.Move(state, "orthanc", "fords-of-isen", secondUnit));
            await CorrespondenceFixture.Send(fixture.Sh, second);
            await fixture.Restart();
            var result = await CorrespondenceFixture.Read<ActionLogView>(fixture.Fp, $"/api/matches/{state.Id}/actions/{root}");
            Assert.Equal(ActionLogStatus.Completed, result.Action.Status);
            Assert.Single(result.Steps.SelectMany(s => s.Costs));
            Assert.Equal(finish ? 1 : 2, result.Steps.SelectMany(s => s.Movements).Count());
            Assert.Equal(DieFace.WillOfTheWest, Assert.Single(result.Action.Costs).Face);
            var increment = await Page(fixture.Sh, state.Id, "after=" + page.LatestCursor);
            Assert.All(increment.Steps, s => Assert.Equal(root, s.ActionId));
            Assert.Single(increment.Steps, s => s.Command == second.Kind);
            Assert.Equal(2, (await CorrespondenceFixture.Send(fixture.Sh, second)).Revision);
            var final = (await fixture.Matches.Read(state.Id, fixture.Shadow.AccountId))!.State;
            Assert.Equal(state.RandomSamples.Count, final.RandomSamples.Count);
            await Page(fixture.Fp, state.Id);
            await Page(fixture.Sh, state.Id);
            CorrespondenceFixture.SameState(final, (await fixture.Matches.Read(state.Id, fixture.Shadow.AccountId))!.State);
            var activity = await CorrespondenceFixture.Read<MatchActivityView>(fixture.Fp, $"/api/matches/{state.Id}/activity");
            Assert.Equal(-1, activity.LastSeenRevision);
            Assert.Equal(HttpStatusCode.NotFound, (await fixture.Other.GetAsync($"/api/matches/{state.Id}/actions/{root}")).StatusCode);
        }
    }

    [PostgresFact]
    public async Task RollbackRejectAndPrivatePreferenceDoNotPublishAnAction()
    {
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        var store = host.Services.GetRequiredService<MatchStore>();
        using var shadow = host.Client("bob");
        var (state, command, _) = Army();
        await store.Create(state, "alice", "bob");
        await Assert.ThrowsAsync<IOException>(() => store.Execute(command, "bob", beforeCommit: () => throw new IOException("Before commit")));
        Assert.Null(await store.Receipt(state.Id, command.CommandId, "bob"));
        Assert.Empty((await Page(shadow, state.Id)).Steps);
        Assert.Equal("NotAuthorized", (await store.Execute(command, "alice"))!.Error);
        Assert.Empty((await Page(shadow, state.Id)).Steps);
        Assert.True((await store.Execute(command, "bob"))!.Accepted);
        var page = JsonSerializer.Serialize(await Page(shadow, state.Id), BoardCatalog.Json);
        Assert.True((await store.Execute(command, "bob"))!.Accepted);
        Assert.Equal(page, JsonSerializer.Serialize(await Page(shadow, state.Id), BoardCatalog.Json));
        Assert.Equal(0, random.Calls);

        var battle = AutoDecisionTests.Battle();
        await store.Create(battle, "alice", "bob");
        var change = AutoDecisionTests.Change(battle, Side.Shadow, true);
        var old = JsonSerializer.Serialize(await Page(shadow, battle.Id), BoardCatalog.Json);
        Assert.False((await store.Execute(change, "bob"))!.PublicStateChanged);
        Assert.Equal(old, JsonSerializer.Serialize(await Page(shadow, battle.Id), BoardCatalog.Json));
    }

    internal static Task<ActionLogPage> Page(HttpClient client, string id, string query = "") =>
        CorrespondenceFixture.Read<ActionLogPage>(client, $"/api/matches/{id}/actions?{query}");
}
