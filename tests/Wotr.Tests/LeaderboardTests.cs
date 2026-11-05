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

// RANK-08 (R35, docs/RANKED_PLAY.md D13): the top 25 and a player's own place, on a database of its own, since every other
// test's finished ranked match would stand on it. Each test starts with an empty board.
public sealed class LeaderboardTests(ClockDatabase database) : IClassFixture<ClockDatabase>
{
    [PostgresFact]
    public async Task ThirtyPlayersGiveTwentyFiveRowsInOrderWithTheirTies()
    {
        using var host = await Host();
        await Clear();
        await Seed("a", 2000, 5);
        await Seed("tied-more-games", 1900, 9);
        await Seed("tied-id-1", 1900, 4);
        await Seed("tied-id-2", 1900, 4);
        for (var i = 0; i < 26; i++)
            await Seed($"p{i:00}", 1800 - i, 1);
        var player = MenuAccounts.Client(host, await MenuAccounts.Register(host));

        var board = await Read(player);
        Assert.Equal(Leaderboard.Size, board.Rows.Count);
        Assert.Equal(Enumerable.Range(1, 25), board.Rows.Select(row => row.Rank));
        Assert.Equal(new[] { "a", "tied-more-games", "tied-id-1", "tied-id-2", "p00" }, board.Rows.Take(5).Select(row => row.Name));
        Assert.Equal("p20", board.Rows[^1].Name);
        var first = board.Rows[0];
        Assert.Equal((2000, 5, 3, 2), (first.Rating, first.Games, first.Wins, first.Losses));
    }

    [PostgresFact]
    public async Task APlayerOutsideTheTopSeesTheirTrueRankAndOneWithoutAGameIsNotListed()
    {
        using var host = await Host();
        await Clear();
        for (var i = 0; i < 30; i++)
            await Seed($"p{i:00}", 2000 - i, 3);
        var low = await MenuAccounts.Register(host);
        await Rate(low.AccountId, 1000, 7);
        var low2 = await MenuAccounts.Register(host);
        await Rate(low2.AccountId, 1000.4, 7);

        var board = await Read(MenuAccounts.Client(host, low));
        Assert.Equal((32, low.Username, 1000, 7), (board.You!.Rank, board.You.Name, board.You.Rating, board.You.Games));
        Assert.DoesNotContain(board.Rows, row => row.Name == low.Username);
        Assert.Equal(31, (await Read(MenuAccounts.Client(host, low2))).You!.Rank);

        var fresh = await MenuAccounts.Register(host);
        var seen = await Read(MenuAccounts.Client(host, fresh));
        Assert.Null(seen.You);
        Assert.DoesNotContain(seen.Rows, row => row.Name == fresh.Username);
    }

    // A rating row exists from the first ranked start; until that game ends the player is not on the board.
    [PostgresFact]
    public async Task AFirstRankedGameStillRunningAndOtherGamesAddNothing()
    {
        using var host = await Host();
        await Clear();
        var fp = await MenuAccounts.Register(host);
        var sh = await MenuAccounts.Register(host);
        var start = await host.Services.GetRequiredService<RankedMatches>().Start(fp.AccountId, sh.AccountId, new(), RankedPreset.Standard);
        Assert.Equal("", start.Error);
        var state = TestScenarioCatalog.Find("sh-military")!.Create(Guid.NewGuid().ToString("N"));
        await host.Services.GetRequiredService<MatchStore>().Create(state, fp.AccountId, sh.AccountId);

        var running = await Read(MenuAccounts.Client(host, fp));
        Assert.Empty(running.Rows);
        Assert.Null(running.You);

        var conceded = await host.Services.GetRequiredService<MatchStore>().Execute(RankedMatches.Concession(start.MatchId, Guid.NewGuid().ToString("N")), sh.AccountId);
        Assert.True(conceded!.Accepted, conceded.Error);
        var ended = await Read(MenuAccounts.Client(host, sh));
        Assert.Equal(new[] { fp.Username, sh.Username }, ended.Rows.Select(row => row.Name));
        Assert.Equal((2, 1338, 1, 0, 1), (ended.You!.Rank, ended.You.Rating, ended.You.Games, ended.You.Wins, ended.You.Losses));
    }

    [PostgresFact]
    public async Task TheBoardReadsTheIndexAndNeedsASignedInPlayer()
    {
        using var host = await Host();
        await Clear();
        await using var data = NpgsqlDataSource.Create(database.Connection);
        await using (var seed = data.CreateCommand(
            """
            WITH made AS (
                INSERT INTO accounts(id, username, normalized_name, password_hash)
                SELECT 'bulk-' || g, 'bulk-' || g, 'bulk-' || g || '-' || gen_random_uuid(), '' FROM generate_series(1, 5000) g
                RETURNING id
            )
            INSERT INTO player_ratings(account_id, rating, deviation, volatility, games)
            SELECT id, random() * 3000, 100, 0.06, 1 FROM made
            """))
            await seed.ExecuteNonQueryAsync();
        await using (var analyze = data.CreateCommand("ANALYZE player_ratings"))
            await analyze.ExecuteNonQueryAsync();
        await using var explain = data.CreateCommand("EXPLAIN " + Leaderboard.TopQuery);
        var plan = new List<string>();
        await using (var reader = await explain.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                plan.Add(reader.GetString(0));
        Assert.Contains(plan, line => line.Contains("player_ratings_order"));
        using var anonymous = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var refused = await anonymous.GetAsync("/api/leaderboard");
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
    }

    private async Task<AccountHost> Host()
    {
        var host = new AccountHost(database.Connection, extra: TestBudgets.Stress);
        await MenuAccounts.Register(host);
        return host;
    }

    private static async Task<LeaderboardView> Read(HttpClient client) =>
        (await client.GetFromJsonAsync<LeaderboardView>("/api/leaderboard", BoardCatalog.Json))!;

    private async Task Clear()
    {
        await using var data = NpgsqlDataSource.Create(database.Connection);
        await using var clear = data.CreateCommand("TRUNCATE rating_changes, ranked_seats, player_ratings");
        await clear.ExecuteNonQueryAsync();
    }

    private async Task Seed(string name, double rating, int games)
    {
        await using var data = NpgsqlDataSource.Create(database.Connection);
        var id = name + "-" + Guid.NewGuid().ToString("N");
        await using var account = data.CreateCommand("INSERT INTO accounts(id, username, normalized_name, password_hash) VALUES ($1, $2, $1, '')");
        account.Parameters.Add(new NpgsqlParameter { Value = id });
        account.Parameters.Add(new NpgsqlParameter { Value = name });
        await account.ExecuteNonQueryAsync();
        await Rate(id, rating, games);
    }

    private async Task Rate(string account, double rating, int games)
    {
        await using var data = NpgsqlDataSource.Create(database.Connection);
        await using var command = data.CreateCommand(
            "INSERT INTO player_ratings(account_id, rating, deviation, volatility, games, wins, losses) VALUES ($1, $2, 100, 0.06, $3, $4, $5)");
        command.Parameters.Add(new NpgsqlParameter { Value = account });
        command.Parameters.Add(new NpgsqlParameter { Value = rating });
        command.Parameters.Add(new NpgsqlParameter { Value = games });
        command.Parameters.Add(new NpgsqlParameter { Value = (games + 1) / 2 });
        command.Parameters.Add(new NpgsqlParameter { Value = games / 2 });
        await command.ExecuteNonQueryAsync();
    }
}
