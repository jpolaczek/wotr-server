using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// RANK-05 (R35, docs/RANKED_PLAY.md D5): the ranked queue on a real database. Most tests pair by hand on a test clock;
// one lets the pairing service run as in production.
public sealed class RankedQueueTests
{
    [PostgresFact]
    public async Task TwoCompatiblePlayersOpenTheSameMatchAtOnce()
    {
        using var host = new AccountHost(expansions: true, extra: Settings(automatic: true));
        var (a, b) = (await Player.Register(host), await Player.Register(host));
        Assert.Equal(RankedSearchStatus.Searching, (await a.Search(new())).Status);
        Assert.Equal(RankedSearchStatus.Searching, (await b.Search(new())).Status);

        var (seenByA, seenByB) = (await a.UntilMatched(), await b.UntilMatched());
        Assert.Equal(seenByA.MatchId, seenByB.MatchId);
        Assert.Equal((b.Name, 1500, a.Name, 1500), (seenByA.Opponent, seenByA.OpponentRating, seenByB.Opponent, seenByB.OpponentRating));
        Assert.Equal("Standard", await Scalar("SELECT preset FROM ranked_matches WHERE match_id=$1", seenByA.MatchId));
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM ranked_tickets WHERE account_id=$1", a.Account));
    }

    [PostgresFact]
    public async Task ASearchWhoseClientWentQuietIsNeverPairedAndIsToldItStopped()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        using var host = new AccountHost(expansions: true, extra: Settings(), services: s => s.AddSingleton<TimeProvider>(clock));
        var (a, b) = (await Player.Register(host), await Player.Register(host));
        var queue = host.Services.GetRequiredService<RankedQueue>();
        await a.Search(new());
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal((RankedSearchStatus.Searching, 4), Pick(await a.Poll()));
        clock.Advance(TimeSpan.FromSeconds(7));
        await b.Search(new());

        Assert.Equal(0, await queue.PairOnce());
        Assert.Equal(RankedSearchStatus.Stopped, (await a.Poll()).Status);
        Assert.Equal(RankedSearchStatus.Idle, (await a.Poll()).Status);
        Assert.Equal(RankedSearchStatus.Searching, (await b.Poll()).Status);
    }

    // A cancel that loses the race to a pairing opens the match rather than leaving it running unseen; one that wins
    // leaves the other player searching.
    [PostgresFact]
    public async Task ACancelEitherWinsOrAnswersWithTheMatch()
    {
        using var host = new AccountHost(expansions: true, extra: Settings());
        var (a, b, c) = (await Player.Register(host), await Player.Register(host), await Player.Register(host));
        var queue = host.Services.GetRequiredService<RankedQueue>();
        await a.Search(new());
        await b.Search(new());
        Assert.Equal(1, await queue.PairOnce());
        var late = await a.Cancel();
        Assert.Equal(RankedSearchStatus.Matched, late.Status);
        Assert.Equal(late.MatchId, (await b.Poll()).MatchId);
        Assert.Equal((RankedSearchStatus.Matched, late.MatchId), (Pick2(await a.Search(new()))));

        var d = await Player.Register(host);
        await c.Search(new());
        await d.Search(new());
        Assert.Equal(RankedSearchStatus.Idle, (await c.Cancel()).Status);
        Assert.Equal(0, await queue.PairOnce());
        Assert.Equal(RankedSearchStatus.Searching, (await d.Poll()).Status);
    }

    [PostgresFact]
    public async Task TenSearchesMakeFiveMatchesEvenWhenTwoPassesRunTogether()
    {
        using var host = new AccountHost(expansions: true, extra: Settings());
        var players = new List<Player>();
        for (var i = 0; i < 10; i++)
            players.Add(await Player.Register(host));
        await Task.WhenAll(players.Select(p => p.Search(new())));
        var queue = host.Services.GetRequiredService<RankedQueue>();
        var passes = await Task.WhenAll(queue.PairOnce(), queue.PairOnce());
        Assert.Equal(5, passes.Sum() + await queue.PairOnce());
        var matches = (await Task.WhenAll(players.Select(p => p.Poll()))).Select(v => v.MatchId).ToList();
        Assert.All(matches, id => Assert.NotEmpty(id));
        Assert.Equal(5, matches.Distinct().Count());
        Assert.All(matches.GroupBy(id => id), group => Assert.Equal(2, group.Count()));
    }

    [PostgresFact]
    public async Task IncompatibleSearchesWaitAndAPairingPlaysTheirAgreedGame()
    {
        using var host = new AccountHost(expansions: true, extra: Settings());
        var (a, b) = (await Player.Register(host), await Player.Register(host));
        var queue = host.Services.GetRequiredService<RankedQueue>();
        await a.Search(new() { Kings = RankedChoice.Enforce, Faction = Side.Shadow, FactionChoice = RankedChoice.Enforce });
        await b.Search(new() { Kings = RankedChoice.Off });
        Assert.Equal(0, await queue.PairOnce());
        await b.Search(new() { Lords = RankedChoice.Off, Long = RankedChoice.Allow });
        Assert.Equal(1, await queue.PairOnce());
        var match = (await a.Poll()).MatchId;
        var state = (await host.Services.GetRequiredService<MatchStore>().Read(match, a.Account))!;
        Assert.Equal(Side.Shadow, state.Side);
        Assert.Equal((false, true, true), (state.State.Configuration.Lords, state.State.Configuration.Kings, state.State.Configuration.Fate));
        Assert.Equal("Standard", await Scalar("SELECT preset FROM ranked_matches WHERE match_id=$1", match));
    }

    [PostgresFact]
    public async Task ASearchIsRefusedForAnUnreleasedModuleAnOlderClientAndIllegalChoices()
    {
        using (var closed = new AccountHost(expansions: false, extra: Settings()))
        {
            var player = await Player.Register(closed);
            Assert.Equal(RankedRules.ExpansionsUnavailable, await player.Refused(new(), HttpStatusCode.Conflict));
            Assert.Equal(RankedSearchStatus.Searching, (await player.Search(new()
            {
                Lords = RankedChoice.Off, Kings = RankedChoice.Off, Fate = RankedChoice.Off
            })).Status);
        }
        var gated = new Dictionary<string, string?>(Settings()) { [ModuleClientPolicy.Key] = "kings=0.11.0" };
        using var host = new AccountHost(expansions: true, extra: gated);
        var old = await Player.Register(host, "0.10.0");
        Assert.Equal(ClientCompatibility.ClientUpdateRequired, await old.Refused(new(), HttpStatusCode.Conflict));
        Assert.Equal(RankedSearchStatus.Searching, (await old.Search(new() { Kings = RankedChoice.Off })).Status);
        Assert.Equal(RankedRules.IllegalSearch, await old.Refused(new() { Kings = RankedChoice.Off, Standard = RankedChoice.Off },
            HttpStatusCode.BadRequest));
    }

    private static (RankedSearchStatus, int) Pick(RankedSearchView view) => (view.Status, view.ElapsedSeconds);

    private static (RankedSearchStatus, string) Pick2(RankedSearchView view) => (view.Status, view.MatchId);

    // Each test starts from an empty queue: the tests of this class share one database, and a test clock writes times
    // that others would read as live.
    private static IReadOnlyDictionary<string, string?> Settings(bool automatic = false)
    {
        using (var data = NpgsqlDataSource.Create(PostgresTests.Connection))
        using (var clear = data.CreateCommand("DELETE FROM ranked_tickets"))
        {
            try
            {
                clear.ExecuteNonQuery();
            }
            catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                // The first host of the run has not migrated yet; the queue is empty anyway.
            }
        }
        return new Dictionary<string, string?>(TestBudgets.Stress) { [RankedOptions.PairingIntervalKey] = automatic ? "200" : "0" };
    }

    private static async Task<object?> Scalar(string sql, object value)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        return await command.ExecuteScalarAsync();
    }

    private sealed class Player
    {
        public string Account { get; private init; } = "";
        public string Name { get; private init; } = "";
        private HttpClient client = null!;

        public static async Task<Player> Register(AccountHost host, string? version = null)
        {
            var session = await MenuAccounts.Register(host);
            var client = MenuAccounts.Client(host, session);
            if (version is not null)
            {
                client.DefaultRequestHeaders.Add(ClientCompatibility.ClientVersionHeader, version);
                client.DefaultRequestHeaders.Add(ClientCompatibility.ProtocolVersionHeader, Protocol.Version.ToString());
            }
            return new() { Account = session.AccountId, Name = session.Username, client = client };
        }

        public async Task<RankedSearchView> Search(RankedSearchRequest search)
        {
            using var response = await client.PostAsJsonAsync("/api/ranked/queue", search, BoardCatalog.Json);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<RankedSearchView>(BoardCatalog.Json))!;
        }

        public async Task<string> Refused(RankedSearchRequest search, HttpStatusCode status)
        {
            using var response = await client.PostAsJsonAsync("/api/ranked/queue", search, BoardCatalog.Json);
            Assert.Equal(status, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;
        }

        public async Task<RankedSearchView> Poll() =>
            (await client.GetFromJsonAsync<RankedSearchView>("/api/ranked/queue", BoardCatalog.Json))!;

        public async Task<RankedSearchView> Cancel()
        {
            using var response = await client.DeleteAsync("/api/ranked/queue");
            return (await response.Content.ReadFromJsonAsync<RankedSearchView>(BoardCatalog.Json))!;
        }

        public async Task<RankedSearchView> UntilMatched()
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var view = await Poll();
                if (view.Status == RankedSearchStatus.Matched)
                    return view;
                await Task.Delay(200);
            }
            throw new TimeoutException("No pairing within 10 s.");
        }
    }
}
