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

// DEMO-03 (DM06–DM08): the server plays the CPU's moves in demo matches as ordinary commands of the CPU account and
// hands the decision back to the player; opening a demo resumes the CPU; a removed demo or a regular match never
// gets a CPU move.
public sealed class DemoRunnerTests
{
    private static readonly IReadOnlyDictionary<string, string?> Instant =
        new Dictionary<string, string?>(TestBudgets.Stress) { [DemoOptions.DelayKey] = "0" };

    [PostgresFact]
    public async Task TheCpuPlaysTheFellowshipPhaseBeforeTheShadowPlayersAllocation()
    {
        using var host = new AccountHost(extra: Instant);
        var (client, account) = await Player(host);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(account, Side.Shadow);

        var demo = await Start(client, room.Id);

        var view = await Until(client, demo.MatchId, v => v.DecisionOwner == Side.Shadow);
        Assert.Equal(DecisionKind.AllocateHunt, view.Decision!.Kind);
        Assert.True(await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1 AND account_id='demo-cpu' AND accepted", demo.MatchId) > 0);
        var log = (await client.GetFromJsonAsync<ActionLogPage>(Route(demo.MatchId) + "/actions", BoardCatalog.Json))!;
        Assert.Contains(log.Steps, step => step.Actor == Side.FreePeoples && step.Command == CommandKind.ContinueFellowship);
    }

    // The player of the Free Peoples and the CPU take turns over real commands until the second turn.
    [PostgresFact]
    public async Task ThePlayerAndTheCpuTakeTurnsUntilTheNextTurn()
    {
        using var host = new AccountHost(extra: Instant);
        var (client, account) = await Player(host);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(account, Side.FreePeoples);
        var demo = await Start(client, room.Id);
        var bot = host.Services.GetRequiredService<DemoBot>();
        var store = host.Services.GetRequiredService<MatchStore>();

        for (var move = 0; move < 60; move++)
        {
            var view = await Until(client, demo.MatchId, v => v.DecisionOwner == Side.FreePeoples || v.Winner.HasValue);
            if (view.Turn > 1 || view.Winner.HasValue)
                break;
            var state = (await store.Read(demo.MatchId, account))!.State;
            var command = bot.Next(state, Side.FreePeoples)!;
            using var sent = await client.PostAsJsonAsync(Route(demo.MatchId) + "/commands", command, BoardCatalog.Json);
            Assert.True((await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        }

        var last = await Until(client, demo.MatchId, v => v.DecisionOwner == Side.FreePeoples || v.Winner.HasValue);
        Assert.True(last.Turn > 1, "Both sides acted through the first turn.");
        Assert.Contains(last.Dice, die => die.Owner == Side.Shadow);
    }

    [PostgresFact]
    public async Task OpeningADemoResumesItsCpuAndARegularMatchHasNone()
    {
        using var host = new AccountHost(extra: Instant);
        var (client, account) = await Player(host);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(account, Side.Shadow);
        // As after a restart: the demo exists, but nothing has woken its CPU yet.
        var demo = (await host.Services.GetRequiredService<DemoSessionStore>().Start(account, room.Id)).Demo!;
        Assert.Equal(0, await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1", demo.MatchId));

        var view = await Until(client, demo.MatchId, v => v.DecisionOwner == Side.Shadow);

        Assert.Equal(DecisionKind.AllocateHunt, view.Decision!.Kind);
        var opponent = await MenuAccounts.Register(host);
        var regular = await RegularMatch(host, account, opponent.AccountId);
        using var read = await client.GetAsync(Route(regular));
        read.EnsureSuccessStatusCode();
        await Task.Delay(300);
        Assert.Equal(0, await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1", regular));
    }

    [PostgresFact]
    public async Task ADemoRemovedWhileTheCpuPlaysLeavesNothingAndANewDemoPlays()
    {
        using var host = new AccountHost(extra: new Dictionary<string, string?>(TestBudgets.Stress) { [DemoOptions.DelayKey] = "150" });
        var (client, account) = await Player(host);
        var room = await host.Services.GetRequiredService<RoomStore>().Create(account, Side.Shadow);
        var first = await Start(client, room.Id);

        using var deleted = await client.DeleteAsync(Route(first.MatchId));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await Task.Delay(400);
        Assert.Equal(0, await Count("SELECT count(*) FROM command_receipts WHERE match_id=$1", first.MatchId));

        var second = await Start(client, room.Id);
        var view = await Until(client, second.MatchId, v => v.DecisionOwner == Side.Shadow);
        Assert.Equal(DecisionKind.AllocateHunt, view.Decision!.Kind);
    }

    private static async Task<(HttpClient Client, string Account)> Player(AccountHost host)
    {
        var session = await MenuAccounts.Register(host);
        return (MenuAccounts.Client(host, session), session.AccountId);
    }

    private static async Task<DemoMatchView> Start(HttpClient client, string room)
    {
        using var answer = await client.PostAsync("/api/rooms/" + room + "/demo", null);
        answer.EnsureSuccessStatusCode();
        return (await answer.Content.ReadFromJsonAsync<DemoMatchView>(BoardCatalog.Json))!;
    }

    // Reads the player's view until the condition holds; each read also wakes the CPU, as opening the board does.
    private static async Task<PlayerView> Until(HttpClient client, string match, Func<PlayerView, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var view = (await client.GetFromJsonAsync<PlayerView>(Route(match), BoardCatalog.Json))!;
            if (condition(view))
                return view;
            Assert.True(DateTime.UtcNow < deadline, $"Still waiting at revision {view.Revision}: {view.Decision?.Kind} of {view.DecisionOwner}.");
            await Task.Delay(50);
        }
    }

    private static async Task<string> RegularMatch(AccountHost host, string first, string second)
    {
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var room = await rooms.Create(first, Side.FreePeoples);
        await rooms.Join(room.JoinCode, second);
        await rooms.Confirm(room.Id, first, 1);
        await rooms.Confirm(room.Id, second, 1);
        return (await rooms.Start(room.Id, first, 1)).Room!.MatchId;
    }

    private static string Route(string match) => "/api/matches/" + match;

    private static async Task<long> Count(string sql, string key)
    {
        await using var data = NpgsqlDataSource.Create(PostgresTests.Connection);
        await using var command = data.CreateCommand(sql);
        command.Parameters.AddWithValue(key);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
