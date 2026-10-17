using System.Net;
using System.Net.Http.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CorrespondenceFixture;

namespace Wotr.Tests;

public class CorrespondenceSeenTests
{
    [PostgresFact]
    public async Task SeenIsDurableMonotonicAndCannotHideAConcurrentNewDecision()
    {
        await using var server = await Create();
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        state.Revision = 11;
        await server.Save(state);
        Assert.Equal(3, (await Seen(server.Fp, state.Id, 3)).LastSeenRevision);
        Assert.Equal(3, (await Seen(server.Fp, state.Id, 1)).LastSeenRevision);
        await Task.WhenAll(Enumerable.Range(0, 12).Select(revision => Seen(server.Fp, state.Id, revision)));
        Assert.Equal(11, (await Seen(server.Fp, state.Id, 0)).LastSeenRevision);
        SameState(state, (await server.Matches.Read(state.Id, server.FreePeoples.AccountId))!.State);
        await server.Restart();
        var fp = Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches);
        var sh = Assert.Single((await Read<MatchListView>(server.Sh, "/api/matches")).Matches);
        Assert.Equal(11, fp.LastSeenRevision);
        Assert.False(fp.HasUnseenChanges);
        Assert.Equal(-1, sh.LastSeenRevision);
        Assert.True(sh.HasUnseenChanges);

        var command = BattleFixture.Command(state, CommandKind.DiscardDie, new() { DieId = state.Dice[0].Id });
        await Task.WhenAll(Send(server.Fp, command), Seen(server.Fp, state.Id, 11));
        fp = Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches);
        Assert.Equal(12, fp.Revision);
        Assert.Equal(11, fp.LastSeenRevision);
        Assert.True(fp.HasUnseenChanges);
        await Seen(server.Fp, state.Id, 12);
        Assert.Equal(12, (await Send(server.Fp, command)).Revision);
        Assert.False(Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches).HasUnseenChanges);
    }

    [PostgresFact]
    public async Task AuthenticationOwnershipAndRevisionBoundsApplyToEveryEndpoint()
    {
        await using var server = await Create();
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        await server.Save(state);
        var path = $"/api/matches/{state.Id}";
        using var anonymous = server.Anonymous();
        foreach (var endpoint in new[] { "/api/matches", path + "/activity" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Put(anonymous, path, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await server.Other.GetAsync(path + "/activity")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(server.Other, path, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Put(server.Other, path, 999)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await server.Fp.GetAsync("/api/matches/missing/activity")).StatusCode);
        foreach (var revision in new long?[] { null, -1, state.Revision + 1, long.MaxValue })
            Assert.Equal(HttpStatusCode.BadRequest, (await Put(server.Fp, path, revision)).StatusCode);
        foreach (var revision in new[] { -2L, state.Revision + 1, long.MaxValue })
            Assert.Equal(HttpStatusCode.BadRequest, (await server.Fp.GetAsync(path + "/activity?afterRevision=" + revision)).StatusCode);
        Assert.Equal(-1, Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches).LastSeenRevision);
        SameState(state, (await server.Matches.Read(state.Id, server.FreePeoples.AccountId))!.State);
    }

    private static Task<HttpResponseMessage> Put(HttpClient client, string path, long? revision) =>
        client.PutAsJsonAsync(path + "/seen", new SeenRevisionRequest { Revision = revision }, BoardCatalog.Json);
}
