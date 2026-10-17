using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.ActionLogFixture;
using static Wotr.Tests.ActionLogPersistenceTests;

namespace Wotr.Tests;

public class ActionLogPagingTests
{
    [PostgresFact]
    public async Task OlderPagesStayStableWhileANewStepUpdatesAnOldGroup()
    {
        await using var fixture = await CorrespondenceFixture.Create();
        var state = LongHistory();
        await fixture.Save(state);
        var expected = Records(state, Side.FreePeoples).OrderByDescending(r => r.Position).Select(r => r.Id).ToArray();
        var page = await Page(fixture.Fp, state.Id);
        Assert.Equal(50, page.Steps.Count);
        var firstHead = page.LatestCursor;
        var observed = page.Steps.Select(s => s.Id).ToList();
        var finish = BattleFixture.Command(state, CommandKind.FinishActionSequence);
        await CorrespondenceFixture.Send(fixture.Sh, finish);
        while (page.NextCursor.Length > 0)
        {
            page = await Page(fixture.Fp, state.Id, "before=" + page.NextCursor);
            observed.AddRange(page.Steps.Select(s => s.Id));
        }
        Assert.Equal(expected, observed);
        Assert.Equal(observed.Count, observed.Distinct().Count());
        Assert.True(observed.Count >= 120);
        var increment = await Page(fixture.Fp, state.Id, "after=" + firstHead);
        var step = Assert.Single(increment.Steps);
        Assert.Equal(CommandKind.FinishActionSequence, step.Command);
        Assert.Equal(state.Resolution.ActionLog!.Action.Id, step.ActionId);
        Assert.Equal(ActionLogStatus.Completed, Assert.Single(increment.Actions).Status);
        Assert.Empty((await Page(fixture.Fp, state.Id, "after=" + increment.LatestCursor)).Steps);
        var action = await CorrespondenceFixture.Read<ActionLogView>(fixture.Fp, $"/api/matches/{state.Id}/actions/{step.ActionId}");
        AssertArmy(Assert.Single(action.Steps.SelectMany(s => s.Movements)));
        Assert.Single(action.Action.Costs);
        Assert.DoesNotContain("private-sentinel", JsonSerializer.Serialize(action, BoardCatalog.Json));
    }

    [PostgresFact]
    public async Task ForwardPagingAndActorFiltersNeverCountHiddenOrLegacySteps()
    {
        await using var fixture = await CorrespondenceFixture.Create();
        var state = LongHistory();
        await fixture.Save(state);
        foreach (var filter in Enum.GetValues<ActionLogFilter>())
        {
            var expected = Records(state, Side.FreePeoples).Where(r => ActionLogProjection.Matches(r.Event, Side.FreePeoples, filter)).ToList();
            var ids = new List<string>();
            var oldest = await Page(fixture.Fp, state.Id, $"filter={filter}&limit=1");
            while (oldest.NextCursor.Length > 0)
                oldest = await Page(fixture.Fp, state.Id, $"filter={filter}&limit=50&before=" + oldest.NextCursor);
            var lower = oldest.Steps.Last();
            // Obtain a cursor for the oldest visible item, then walk forward in pages of 17.
            var cursor = Cursor(state.Id, Side.FreePeoples, filter, lower.Id);
            ids.Add(lower.Id);
            do
            {
                var page = await Page(fixture.Fp, state.Id, $"filter={filter}&limit=17&after=" + cursor);
                ids.AddRange(page.Steps.Select(s => s.Id));
                cursor = page.NextCursor;
                Assert.DoesNotContain("private-sentinel", JsonSerializer.Serialize(page, BoardCatalog.Json));
                Assert.All(page.Actions, a => Assert.True(a.StepCount > 0));
                if (filter != ActionLogFilter.All)
                    Assert.All(page.Steps, s => { Assert.False(s.Legacy); Assert.NotNull(s.Actor); });
            } while (cursor.Length > 0);
            Assert.Equal(expected.Select(r => r.Id), ids);
        }
    }

    [PostgresFact]
    public async Task InvalidLimitsForeignCursorsAndPrivateActionIdsFailClosed()
    {
        await using var fixture = await CorrespondenceFixture.Create();
        var state = LongHistory();
        await fixture.Save(state);
        var page = await Page(fixture.Fp, state.Id);
        var path = $"/api/matches/{state.Id}/actions";
        foreach (var query in new[] { "limit=0", "limit=51", "filter=99", "before=garbage", "before=&after=",
            "before=" + new string('a', 513), "filter=You&after=" + page.LatestCursor })
            Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Fp.GetAsync(path + "?" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Sh.GetAsync(path + "?after=" + page.LatestCursor)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Other.GetAsync(path)).StatusCode);
        using var anonymous = fixture.Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        var privatePosition = state.Events.FindIndex(e => e.OnlyFor == Side.Shadow) + 1;
        var privateId = EventIdentity.For(state.Id, privatePosition);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Fp.GetAsync(path + "/" + privateId)).StatusCode);
        var forged = Cursor(state.Id, Side.FreePeoples, ActionLogFilter.All, privateId);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Fp.GetAsync(path + "?after=" + forged)).StatusCode);
        var otherState = ActionSequenceFixture.Board();
        otherState.Id = Guid.NewGuid().ToString("N");
        await fixture.Save(otherState);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Fp.GetAsync($"/api/matches/{otherState.Id}/actions?after=" + page.LatestCursor)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Fp.GetAsync($"/api/matches/{otherState.Id}/actions/" + page.Actions[0].Id)).StatusCode);
    }

    [PostgresFact]
    public async Task LegacyReadIsUnchangedAndNextWriteBuildsTheSameIndexAtomically()
    {
        await using var host = new TestHost();
        using var viewer = host.Client("alice");
        var store = host.Services.GetRequiredService<MatchStore>();
        var database = host.Services.GetRequiredService<Database>();
        var state = LongHistory();
        await using (var connection = await database.Source.OpenConnectionAsync())
        await using (var insert = Database.Command(connection, null,
            "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ($1,'alice','bob',$2,$3::jsonb)",
            state.Id, state.Revision, StateCodec.Write(state)))
            await insert.ExecuteNonQueryAsync();
        var before = await Page(viewer, state.Id);
        Assert.All(before.Steps, s => Assert.Equal(EventIdentity.For(state.Id, state.Events.FindIndex(e => e.Text == s.Text) + 1), s.Id));
        CorrespondenceFixture.SameState(state, (await store.Read(state.Id, "alice"))!.State);
        var finish = BattleFixture.Command(state, CommandKind.FinishActionSequence);
        await Assert.ThrowsAsync<IOException>(() => store.Execute(finish, "bob", beforeCommit: () => throw new IOException("Rollback index")));
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(await Page(viewer, state.Id)));
        Assert.True((await store.Execute(finish, "bob"))!.Accepted);
        var after = await Page(viewer, state.Id, "before=" + before.LatestCursor);
        Assert.Equal(before.Steps.Skip(1).Select(s => s.Id), after.Steps.Take(49).Select(s => s.Id));
        await using var check = database.Source.CreateCommand("SELECT events_indexed FROM matches WHERE id=$1");
        check.Parameters.AddWithValue(state.Id);
        Assert.Equal(true, await check.ExecuteScalarAsync());
    }

    internal static MatchState LongHistory(int count = 120)
    {
        var (state, first, _) = Army();
        state = Execute(state, first);
        for (var i = 0; i < count; i++)
        {
            var root = new ActionIdentity
            {
                Id = EventIdentity.For(state.Id, state.Events.Count + 1),
                Initiator = i % 2 == 0 ? Side.Shadow : Side.FreePeoples,
                Command = CommandKind.Pass, Turn = i / 30 + 1
            };
            state.Events.Add(new()
            {
                Revision = i / 4 + 2, Kind = "Passed", Text = "Public step " + i,
                Activity = new() { Action = root, Actor = root.Initiator, Turn = root.Turn, Command = CommandKind.Pass, Status = ActionLogStatus.Completed }
            });
            if (i % 3 == 0)
                state.Events.Add(new() { Revision = i / 4 + 2, Kind = "Private", OnlyFor = Side.Shadow, Text = "private-sentinel " + i });
        }
        state.Events.Add(new() { Revision = count + 2, Kind = "Legacy", Text = "Uninterpreted historical text." });
        state.Revision = count + 2;
        return state;
    }

    private static string Cursor(string match, Side viewer, ActionLogFilter filter, string id) =>
        WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { Version = 1, Match = match, Viewer = viewer, Filter = filter, Event = id }));
}
