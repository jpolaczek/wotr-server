using System.Net;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.CorrespondenceFixture;

namespace Wotr.Tests;

public class CorrespondenceListTests
{
    [PostgresFact]
    public async Task PagesAreBoundedStableAndFilteredForEachAccountsActualSide()
    {
        await using var server = await Create();
        var prefix = Guid.NewGuid().ToString("N");
        for (var index = 0; index < 55; index++)
        {
            var state = TurnTests.Actions(actor: index % 2 == 0 ? Side.FreePeoples : Side.Shadow);
            state.Id = prefix + index.ToString("D2");
            if (index % 5 == 0)
            {
                state.Phase = GamePhase.Finished;
                state.Winner = Side.FreePeoples;
                state.Victory = VictoryCondition.RingDestroyed;
                state.Pending = null;
            }
            await server.Save(state, reverse: index % 3 == 0);
        }
        var first = await Read<MatchListView>(server.Fp, "/api/matches?status=All");
        Assert.Equal(20, first.Matches.Count);
        Assert.Equal(prefix + "19", first.NextCursor);
        var second = await Read<MatchListView>(server.Fp, "/api/matches?status=All&cursor=" + first.NextCursor);
        var third = await Read<MatchListView>(server.Fp, "/api/matches?status=All&cursor=" + second.NextCursor);
        Assert.Equal(20, second.Matches.Count);
        Assert.Equal(15, third.Matches.Count);
        Assert.Empty(third.NextCursor);
        var all = first.Matches.Concat(second.Matches).Concat(third.Matches).ToArray();
        Assert.Equal(Enumerable.Range(0, 55).Select(i => prefix + i.ToString("D2")), all.Select(m => m.MatchId));
        for (var index = 0; index < all.Length; index++)
            Assert.Equal(index % 3 == 0 ? Side.Shadow : Side.FreePeoples, all[index].You);
        foreach (var status in Enum.GetValues<MatchStatus>())
        {
            var filtered = await Read<MatchListView>(server.Fp, $"/api/matches?status={status}&limit=50");
            Assert.Equal(all.Where(m => m.Status == status).Select(m => m.MatchId), filtered.Matches.Select(m => m.MatchId));
            Assert.Empty(filtered.NextCursor);
            Assert.All(filtered.Matches, m => Assert.Equal(status, m.Status));
        }
        var finished = all.Where(m => m.Status == MatchStatus.Finished).ToArray();
        Assert.Equal(11, finished.Length);
        Assert.All(finished, m =>
        {
            Assert.Null(m.DecisionOwner);
            Assert.Null(m.YourDecision);
            Assert.Equal(VictoryCondition.RingDestroyed, m.Victory);
            Assert.Equal(Side.FreePeoples, m.Winner);
        });
        Assert.Equal(50, (await Read<MatchListView>(server.Fp, "/api/matches?status=All&limit=50")).Matches.Count);
        Assert.Empty((await Read<MatchListView>(server.Other, "/api/matches?status=All")).Matches);
        foreach (var query in new[] { "limit=0", "limit=-1", "limit=51", "status=99", "cursor=%0A", "cursor=" + new string('a', 101) })
            Assert.Equal(HttpStatusCode.BadRequest, (await server.Fp.GetAsync("/api/matches?" + query)).StatusCode);
    }

    [PostgresFact]
    public async Task EveryProtocolDecisionReportsOnlyItsOwnerAndNoPrivateParameters()
    {
        await using var server = await Create();
        foreach (var kind in Enum.GetValues<DecisionKind>())
        {
            var state = TurnTests.Actions();
            state.Id = Guid.NewGuid().ToString("N");
            state.Pending!.Kind = kind;
            state.Pending.SourceId = "private-source";
            state.Pending.PrivateChoices = ["private-choice"];
            await server.Save(state);
        }
        var mine = (await Read<MatchListView>(server.Fp, "/api/matches?limit=50")).Matches;
        var theirs = (await Read<MatchListView>(server.Sh, "/api/matches?limit=50")).Matches;
        Assert.Equal(Enum.GetValues<DecisionKind>().Order(), mine.Select(m => m.YourDecision!.Value).Order());
        Assert.All(mine, m => Assert.Equal(MatchStatus.YourDecision, m.Status));
        Assert.All(theirs, m => { Assert.Equal(MatchStatus.Waiting, m.Status); Assert.Null(m.YourDecision); });
        Assert.DoesNotContain("private-", await server.Sh.GetStringAsync("/api/matches?limit=50"));
    }

    // LOBBY-05: My games (the list without a status) leaves the finished games out, page by page.
    [PostgresFact]
    public async Task MyGamesListsOnlyTheGamesStillGoingInStablePages()
    {
        await using var server = await Create();
        var prefix = Guid.NewGuid().ToString("N");
        var going = new List<string>();
        for (var index = 0; index < 30; index++)
        {
            var state = TurnTests.Actions();
            state.Id = prefix + index.ToString("D2");
            if (index % 3 == 0)
            {
                state.Phase = GamePhase.Finished;
                state.Winner = Side.Shadow;
                state.Victory = VictoryCondition.ShadowMilitary;
                state.Pending = null;
            }
            else
                going.Add(state.Id);
            await server.Save(state);
        }

        var first = await Read<MatchListView>(server.Fp, "/api/matches?limit=15");
        var second = await Read<MatchListView>(server.Sh, "/api/matches?limit=15");
        var rest = await Read<MatchListView>(server.Fp, "/api/matches?limit=15&cursor=" + first.NextCursor);

        Assert.Equal(going.Take(15), first.Matches.Select(m => m.MatchId));
        Assert.Equal(going.Skip(15), rest.Matches.Select(m => m.MatchId));
        Assert.Empty(rest.NextCursor);
        Assert.Equal(going.Take(15), second.Matches.Select(m => m.MatchId));
        Assert.DoesNotContain(first.Matches.Concat(rest.Matches), m => m.Status == MatchStatus.Finished);
        Assert.Equal(10, (await Read<MatchListView>(server.Fp, "/api/matches?status=Finished&limit=50")).Matches.Count);
        Assert.Equal(30, (await Read<MatchListView>(server.Fp, "/api/matches?status=All&limit=50")).Matches.Count);
    }

    // LOBBY-05: a game between two people is named after both, Free Peoples first, the same for both players.
    [PostgresFact]
    public async Task AGameBetweenTwoPeopleNamesBothPlayersForBoth()
    {
        await using var server = await Create();
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        await server.Save(state);
        var expected = server.FreePeoples.Username + " vs " + server.Shadow.Username;

        var mine = Assert.Single((await Read<MatchListView>(server.Fp, "/api/matches")).Matches);
        var theirs = Assert.Single((await Read<MatchListView>(server.Sh, "/api/matches")).Matches);

        Assert.Equal(expected, mine.Name);
        Assert.Equal(expected, theirs.Name);
        Assert.Equal(server.Shadow.Username, mine.Opponent);
        Assert.Equal(server.FreePeoples.Username, theirs.Opponent);
        Assert.Equal("Friday · Ann vs Bob", MatchIndexStore.Title("Friday", "Ann", "Bob"));
    }
}
