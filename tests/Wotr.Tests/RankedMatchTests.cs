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

// RANK-04 (R35, docs/RANKED_PLAY.md): a ranked match starts with its room, seats and ratings; nothing but a concession
// leaves it; its end changes both ratings once, inside the finishing transaction.
public sealed class RankedMatchTests
{
    [PostgresFact]
    public async Task AConcessionFinishesTheMatchAndChangesBothRatingsOnce()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await RankedTable.Start(host);
        var concession = new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") };

        var first = await Concede(table.Shadow, table.Match, concession);
        Assert.True(first.Accepted, first.Error);
        var retried = await Concede(table.Shadow, table.Match, concession);
        Assert.Equal((first.CommandId, first.Revision), (retried.CommandId, retried.Revision));
        Assert.Equal("GameFinished", (await Concede(table.FreePeoples, table.Match, NewConcession())).Error);

        Assert.Equal((1662.3, 1, 1, 0), Rounded(await Rating(table.FreePeoplesAccount)));
        Assert.Equal((1337.7, 1, 0, 1), Rounded(await Rating(table.ShadowAccount)));
        Assert.Equal(2L, await Scalar("SELECT count(*) FROM rating_changes WHERE match_id=$1", table.Match));
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM ranked_seats WHERE match_id=$1", table.Match));
        Assert.Equal("ranked", await Scalar("SELECT kind FROM match_ledger WHERE match_id=$1", table.Match));

        var won = await table.FreePeoples.GetFromJsonAsync<RankedResultView>($"/api/matches/{table.Match}/rating", BoardCatalog.Json);
        var lost = await table.Shadow.GetFromJsonAsync<RankedResultView>($"/api/matches/{table.Match}/rating", BoardCatalog.Json);
        Assert.Equal((1500, 1662, 162), (won!.RatingBefore, won.RatingAfter, won.Change));
        Assert.Equal((1500, 1338, -162), (lost!.RatingBefore, lost.RatingAfter, lost.Change));
        var state = (await host.Services.GetRequiredService<MatchStore>().Read(table.Match, table.ShadowAccount))!.State;
        Assert.Equal((Side.FreePeoples, VictoryCondition.Conceded), (state.Winner!.Value, state.Victory!.Value));
    }

    // R33/D14: observers see both hands, so a ranked match has none; leaving and removing would escape the loss.
    [PostgresFact]
    public async Task NoPathButAConcessionLeavesARankedMatch()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await RankedTable.Start(host);
        var (room, code) = await Room(table.Match);
        var outsider = await MenuAccounts.Register(host);
        var rooms = host.Services.GetRequiredService<RoomStore>();

        Assert.Equal(RoomStore.RankedRefusal, await Refused(table.FreePeoples.DeleteAsync($"/api/matches/{table.Match}")));
        Assert.Equal(RoomStore.RankedRefusal, await Refused(table.FreePeoples.PostAsync($"/api/rooms/{room}/leave", null)));
        Assert.Equal(RoomStore.RankedRefusal, await Refused(table.FreePeoples.PostAsync($"/api/rooms/{room}/invitation", null)));
        Assert.Equal(RoomStore.RankedRefusal, await Refused(table.Shadow.PostAsJsonAsync($"/api/rooms/{room}/role",
            new RoomRoleRequest { Role = RoomRole.Observer }, BoardCatalog.Json)));
        Assert.Equal(RoomStore.RankedRefusal, (await rooms.Join(code, outsider.AccountId, RoomRole.Observer)).Error);
        Assert.Equal(RoomStore.RankedRefusal, (await rooms.Join(code, outsider.AccountId, RoomRole.Player)).Error);
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM matches WHERE id=$1", table.Match));
        Assert.Equal(2L, await Scalar("SELECT count(*) FROM ranked_seats WHERE match_id=$1", table.Match));

        var mine = await table.FreePeoples.GetFromJsonAsync<MatchListView>("/api/matches", BoardCatalog.Json);
        var row = Assert.Single(mine!.Matches, m => m.MatchId == table.Match);
        Assert.True(row.Ranked);
        Assert.StartsWith("Ranked · ", row.Name);
    }

    [PostgresFact]
    public async Task AnAccountHoldsOneActiveRankedMatchUntilItFinishes()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await RankedTable.Start(host);
        var third = await MenuAccounts.Register(host);
        var ranked = host.Services.GetRequiredService<RankedMatches>();

        var refused = await ranked.Start(third.AccountId, table.ShadowAccount, new(), RankedPreset.Standard);
        Assert.Equal((RankedMatches.ActiveRefusal, table.Match), (refused.Error, refused.ActiveMatch));
        Assert.Equal(table.Match, await ranked.Active(table.ShadowAccount));

        Assert.True((await Concede(table.Shadow, table.Match, NewConcession())).Accepted);
        Assert.Null(await ranked.Active(table.ShadowAccount));
        var next = await ranked.Start(third.AccountId, table.ShadowAccount, new(), RankedPreset.Fast);
        Assert.Equal("", next.Error);
        Assert.Equal("Fast", await Scalar("SELECT preset FROM ranked_matches WHERE match_id=$1", next.MatchId));
    }

    // A normal game cannot be conceded and its end changes no rating.
    [PostgresFact]
    public async Task ANormalGameIsNotRankedAndChangesNoRating()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var fp = await MenuAccounts.Register(host);
        var sh = await MenuAccounts.Register(host);
        var state = TestScenarioCatalog.Find("sh-military")!.Create(Guid.NewGuid().ToString("N"));
        var store = host.Services.GetRequiredService<MatchStore>();
        await store.Create(state, fp.AccountId, sh.AccountId);
        var shadow = MenuAccounts.Client(host, sh);
        Assert.Equal(RankedMatches.NotRankedRefusal,
            await Refused(shadow.PostAsJsonAsync($"/api/matches/{state.Id}/concede", NewConcession(), BoardCatalog.Json)));

        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        var receipt = await store.Execute(new GameCommand
        {
            MatchId = state.Id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id, Kind = CommandKind.DiscardDie, Payload = new() { DieId = die.Id }
        }, sh.AccountId);
        Assert.True(receipt!.Accepted, receipt.Error);
        Assert.Equal(Side.Shadow, (await store.Read(state.Id, sh.AccountId))!.State.Winner);
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM player_ratings WHERE account_id = ANY($1)",
            (object)new[] { fp.AccountId, sh.AccountId }));
        Assert.Equal(HttpStatusCode.NotFound, (await shadow.GetAsync($"/api/matches/{state.Id}/rating")).StatusCode);
    }

    // Failure scenario: a rating that cannot be updated rolls the finishing command back, so the match is never
    // finished without its rating.
    [PostgresFact]
    public async Task AFailedRatingUpdateLeavesTheMatchUnfinished()
    {
        using var host = new AccountHost(extra: TestBudgets.Stress);
        var table = await RankedTable.Start(host);
        await Scalar("UPDATE player_ratings SET rating='NaN' WHERE account_id=$1 RETURNING 1", table.ShadowAccount);
        var store = host.Services.GetRequiredService<MatchStore>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.Execute(RankedMatches.Concession(table.Match, Guid.NewGuid().ToString("N")), table.ShadowAccount));
        var state = (await store.Read(table.Match, table.ShadowAccount))!.State;
        Assert.Null(state.Winner);
        Assert.Equal(2L, await Scalar("SELECT count(*) FROM ranked_seats WHERE match_id=$1", table.Match));
        Assert.Null(await Scalar("SELECT finished_at FROM match_ledger WHERE match_id=$1", table.Match));
    }

    private static ConcedeRequest NewConcession() => new() { CommandId = Guid.NewGuid().ToString("N") };

    private static async Task<CommandReceipt> Concede(HttpClient client, string match, ConcedeRequest request)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{match}/concede", request, BoardCatalog.Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }

    private static async Task<string> Refused(Task<HttpResponseMessage> sending)
    {
        using var response = await sending;
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiError>(BoardCatalog.Json))!.Error;
    }

    private static (double, int, int, int) Rounded((double Rating, int Games, int Wins, int Losses) row) =>
        (Math.Round(row.Rating, 1), row.Games, row.Wins, row.Losses);

    private static async Task<(double Rating, int Games, int Wins, int Losses)> Rating(string account)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand("SELECT rating, games, wins, losses FROM player_ratings WHERE account_id=$1");
        command.Parameters.Add(new NpgsqlParameter { Value = account });
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetDouble(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
    }

    private static async Task<(string Id, string Code)> Room(string match)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand("SELECT id, join_code FROM rooms WHERE match_id=$1");
        command.Parameters.Add(new NpgsqlParameter { Value = match });
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1));
    }

    private static async Task<object?> Scalar(string sql, object value)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = value });
        var result = await command.ExecuteScalarAsync();
        return result is DBNull ? null : result;
    }

    private sealed class RankedTable
    {
        public string Match { get; private init; } = "";
        public string FreePeoplesAccount { get; private init; } = "";
        public string ShadowAccount { get; private init; } = "";
        public HttpClient FreePeoples { get; private init; } = null!;
        public HttpClient Shadow { get; private init; } = null!;

        public static async Task<RankedTable> Start(AccountHost host)
        {
            var fp = await MenuAccounts.Register(host);
            var sh = await MenuAccounts.Register(host);
            var start = await host.Services.GetRequiredService<RankedMatches>()
                .Start(fp.AccountId, sh.AccountId, new(), RankedPreset.Standard);
            Assert.Equal("", start.Error);
            return new()
            {
                Match = start.MatchId, FreePeoplesAccount = fp.AccountId, ShadowAccount = sh.AccountId,
                FreePeoples = MenuAccounts.Client(host, fp), Shadow = MenuAccounts.Client(host, sh)
            };
        }
    }
}
