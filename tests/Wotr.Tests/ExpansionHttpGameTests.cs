using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// EXP-03: one game with every expansion and variant, start to end, through the real command route and PostgreSQL.
// Each move is the demo CPU's choice for the side to act, sent by that player over HTTP; the server restarts every
// 120 moves, and after each restart both players read exactly the projections they read before it.
public sealed class ExpansionHttpGameTests(ITestOutputHelper output)
{
    private const int MoveLimit = 3000;
    private const int RestartEvery = 120;

    [PostgresFact]
    public async Task AGameWithEveryExpansionFinishesOverHttpAcrossRestarts()
    {
        var id = "exp03-http-" + Guid.NewGuid().ToString("N")[..12];
        var state = MatchFactory.Create(id, new SeededRandom(7), ConfigurationRules.Normalize(new()
        {
            Fate = true, Lords = true, Kings = true, NewCities = true, Council = true, VisibleCorruption = true,
            SequentialCorruption = true, RemovingRulerTokens = true
        }));
        TurnFlow.Start(state);
        // The per-account request budgets are for players, not for a CPU sending every move at once.
        await using (var created = new TestHost(new SeededRandom(1), settings: TestBudgets.Stress))
            await created.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");

        var moves = 0;
        var restarts = 0;
        string[]? before = null;
        PlayerView? view = null;
        while (moves < MoveLimit && view?.Winner is null)
        {
            await using var host = new TestHost(new SeededRandom(100 + restarts), settings: TestBudgets.Stress);
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            if (before is not null)
                Assert.Equal(before, await ReadBoth(alice, bob, id));
            var store = host.Services.GetRequiredService<MatchStore>();
            var bot = host.Services.GetRequiredService<DemoBot>();
            for (var batch = 0; batch < RestartEvery && moves < MoveLimit; batch++, moves++)
            {
                var current = (await store.Read(id, "alice"))!.State;
                if (current.Winner is not null)
                    break;
                var side = current.Pending!.Owner;
                var command = bot.Next(current, side);
                Assert.True(command is not null, $"No move for {current.Pending.Kind} at revision {current.Revision}.");
                var receipt = await Send(side == Side.FreePeoples ? alice : bob, command);
                Assert.True(receipt.Accepted, $"{command.Kind} at revision {current.Revision}: {receipt.Error}");
            }
            before = await ReadBoth(alice, bob, id);
            view = await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json);
            restarts++;
        }
        output.WriteLine($"{moves} moves, {restarts} server runs, turn {view!.Turn}, winner {view.Winner} ({view.Victory})");
        Assert.True(view.Winner.HasValue, $"No end after {moves} moves.");
        Assert.True(restarts > 1, "The game crossed at least one restart.");
    }

    private static async Task<string[]> ReadBoth(HttpClient alice, HttpClient bob, string id) =>
        [await alice.GetStringAsync($"/api/matches/{id}"), await bob.GetStringAsync($"/api/matches/{id}")];

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }
}
