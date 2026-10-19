using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using Xunit.Abstractions;

namespace Wotr.Tests;

// DEMO-05 (DM10): a whole demo over the real server, on either side: the player's moves go through the HTTP command
// route (chosen here by the same simple bot, standing in for a person), the CPU answers on the server, and the game
// reaches its end with every decision answered. DEMO-06: also a demo of each expansion, from a room set to it.
public sealed class DemoAcceptanceTests(ITestOutputHelper output)
{
    private const int MoveLimit = 1500;
    private static readonly IReadOnlyDictionary<string, string?> Instant =
        new Dictionary<string, string?>(TestBudgets.Stress) { [DemoOptions.DelayKey] = "0" };

    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public Task AWholeDemoAgainstTheCpuReachesTheEnd(Side side) => Play(side, new());

    [PostgresTheory]
    [InlineData("fate", Side.FreePeoples)]
    [InlineData("lords", Side.Shadow)]
    [InlineData("kings", Side.FreePeoples)]
    public Task AWholeExpansionDemoReachesTheEnd(string module, Side side) => Play(side, new()
    {
        Fate = module == "fate", Lords = module == "lords", Kings = module == "kings"
    });

    private async Task Play(Side side, MatchConfiguration configuration)
    {
        using var host = new AccountHost(extra: Instant);
        var session = await MenuAccounts.Register(host);
        using var client = MenuAccounts.Client(host, session);
        var rooms = host.Services.GetRequiredService<RoomStore>();
        var room = await rooms.Create(session.AccountId, side);
        if (!ConfigurationRules.IsBase(configuration))
            Assert.NotNull((await rooms.Configure(room.Id, session.AccountId,
                new ConfigureRoomRequest { ExpectedVersion = room.Version, Configuration = configuration })).Room);
        using var started = await client.PostAsync("/api/rooms/" + room.Id + "/demo", null);
        var demo = (await started.Content.ReadFromJsonAsync<DemoMatchView>(BoardCatalog.Json))!;
        var bot = host.Services.GetRequiredService<DemoBot>();
        var store = host.Services.GetRequiredService<MatchStore>();

        var moves = 0;
        var view = await Turn(client, demo.MatchId, side);
        for (; moves < MoveLimit && !view.Winner.HasValue; moves++)
        {
            var state = (await store.Read(demo.MatchId, session.AccountId))!.State;
            var command = bot.Next(state, side)!;
            using var sent = await client.PostAsJsonAsync("/api/matches/" + demo.MatchId + "/commands", command, BoardCatalog.Json);
            var receipt = (await sent.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
            Assert.True(receipt.Accepted, $"{command.Kind}: {receipt.Error}");
            view = await Turn(client, demo.MatchId, side);
        }

        output.WriteLine($"{side} {string.Join("+", ExpansionModules.Of(view.Configuration))}: {moves} own moves, turn {view.Turn}, " +
            $"winner {view.Winner} ({view.Victory})");
        Assert.Equal(ExpansionModules.Of(configuration), ExpansionModules.Of(view.Configuration));
        Assert.True(view.Winner.HasValue, $"No end after {moves} moves.");
        var list = (await client.GetFromJsonAsync<MatchListView>("/api/matches?status=Finished", BoardCatalog.Json))!;
        Assert.Contains(list.Matches, match => match.MatchId == demo.MatchId);
    }

    // The player's view once the decision is theirs or the game is over; every read also wakes the CPU.
    private static async Task<PlayerView> Turn(HttpClient client, string match, Side side)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            var view = (await client.GetFromJsonAsync<PlayerView>("/api/matches/" + match, BoardCatalog.Json))!;
            if (view.Winner.HasValue || view.DecisionOwner == side)
                return view;
            Assert.True(DateTime.UtcNow < deadline, $"The CPU did not hand over at revision {view.Revision} ({view.Decision?.Kind}).");
            await Task.Delay(20);
        }
    }
}
