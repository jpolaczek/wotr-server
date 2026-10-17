using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.ActionLogPersistenceTests;

namespace Wotr.Tests;

public class HuntActivityPersistenceTests
{
    [PostgresFact]
    public async Task RealHostRestartKeepsAtomicHuntFactsSeparateFromActorFilteredNoticesAndNewChains()
    {
        await using var postgres = new IsolatedPostgres();
        await postgres.Start();
        var initial = HuntTests.State(0);
        initial.Id = Guid.NewGuid().ToString("N");
        var command = FellowshipTests.Command(initial, CommandKind.MoveFellowship, new() { DieId = initial.Dice[0].Id });
        SessionView fp;
        SessionView sh;
        await using (var seed = new AccountHost(postgres.Connection))
        {
            var accounts = seed.Services.GetRequiredService<AccountService>();
            fp = await Register(accounts);
            sh = await Register(accounts);
            await seed.Services.GetRequiredService<MatchStore>().Create(initial, fp.AccountId, sh.AccountId);
        }
        ActionLogView recorded;
        string chain;
        await using (var server = await KestrelProcess.Start(postgres.Connection))
        {
            using var free = server.Client(fp);
            using var shadow = server.Client(sh);
            var receipt = await CorrespondenceFixture.Send(free, command);
            Assert.True(receipt.Accepted);
            var notices = await Page(shadow, initial.Id, "filter=Opponent");
            var step = Assert.Single(notices.Steps);
            Assert.Equal(Side.FreePeoples, step.Actor);
            Assert.Equal(CommandKind.MoveFellowship, step.Command);
            recorded = await Detail(shadow, initial.Id, step.ActionId);
            chain = recorded.Action.Context.HuntChainId;
            Assert.NotEmpty(chain);
            Assert.NotEqual(step.Id, recorded.Action.Version);
            Assert.All(recorded.Steps, s => Assert.Equal(receipt.Revision, s.Revision));
            var rolled = Assert.Single(recorded.Steps, s => s.Facts.Hunt?.Entry.Kind == HuntReportKind.Rolled);
            Assert.Null(rolled.Actor);
            Assert.True(rolled.Automatic);
            Assert.Empty(rolled.Facts.Hunt!.Entry.Hunt!.Dice);
            Assert.Contains(recorded.Steps, s => s.Facts.Hunt?.Completed == true);
            Assert.DoesNotContain(recorded.Steps, s => s.Facts.Hunt?.Entry.Kind == HuntReportKind.TileDrawn);
            Assert.Single(recorded.Steps.SelectMany(s => s.Costs), c => c.Kind == ActionCostKind.Die);
            Assert.Empty((await Page(free, initial.Id, "filter=Opponent")).Steps);
        }
        await postgres.Restart();
        await using (var server = await KestrelProcess.Start(postgres.Connection))
        {
            using var free = server.Client(fp);
            using var shadow = server.Client(sh);
            Assert.Equal(1, (await CorrespondenceFixture.Send(free, command)).Revision);
            Assert.Equal(JsonSerializer.Serialize(recorded), JsonSerializer.Serialize(await Detail(shadow, initial.Id, recorded.Action.Id)));
            var shadowView = await View(shadow, initial.Id);
            await CorrespondenceFixture.Send(shadow, Choice(shadowView, CommandKind.DiscardDie,
                new() { DieId = shadowView.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id }));
            var freeView = await View(free, initial.Id);
            await CorrespondenceFixture.Send(free, Choice(freeView, CommandKind.MoveFellowship,
                new() { DieId = freeView.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id }));
            Assert.NotEqual(chain, (await View(free, initial.Id)).HuntReport!.Id);
        }
        await postgres.Restart();
        await using (var server = await KestrelProcess.Start(postgres.Connection))
        {
            using var free = server.Client(fp);
            using var shadow = server.Client(sh);
            Assert.Equal(JsonSerializer.Serialize(recorded), JsonSerializer.Serialize(await Detail(shadow, initial.Id, recorded.Action.Id)));
            Assert.Equal(JsonSerializer.Serialize(recorded), JsonSerializer.Serialize(await Detail(free, initial.Id, recorded.Action.Id)));
            var latest = await View(free, initial.Id);
            Assert.NotEqual(chain, latest.HuntReport!.Id);
            Assert.Equal(DieLocation.Hunt, latest.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        }
        await using var inspect = new AccountHost(postgres.Connection);
        var final = (await inspect.Services.GetRequiredService<MatchStore>().Read(initial.Id, fp.AccountId))!.State;
        Assert.Equal(initial.RandomSamples.Count, final.RandomSamples.Count);
    }

    private static Task<ActionLogView> Detail(HttpClient client, string match, string action) =>
        CorrespondenceFixture.Read<ActionLogView>(client, $"/api/matches/{match}/actions/{action}");

    private static Task<PlayerView> View(HttpClient client, string match) =>
        CorrespondenceFixture.Read<PlayerView>(client, $"/api/matches/{match}");

    private static GameCommand Choice(PlayerView view, CommandKind kind, CommandPayload payload) => new()
    {
        MatchId = view.MatchId, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
        DecisionId = view.Decision!.Id, Kind = kind, Payload = payload
    };

    private static async Task<SessionView> Register(AccountService accounts)
    {
        return await TestAccounts.Register(accounts, "hunt" + Guid.NewGuid().ToString("N")[..12], "Hunt activity restart test password!");
    }
}
