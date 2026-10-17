using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.ActionLogPersistenceTests;

namespace Wotr.Tests;

public class ActionLogRestartTests
{
    [PostgresFact]
    public async Task KestrelAndPostgresProcessesRestartBetweenRealPaidSteps()
    {
        await using var postgres = new IsolatedPostgres();
        await postgres.Start();
        var (initial, command, unit) = ActionLogFixture.Army();
        SessionView fp;
        SessionView sh;
        await using (var seed = new AccountHost(postgres.Connection))
        {
            var accounts = seed.Services.GetRequiredService<AccountService>();
            fp = await Register(accounts);
            sh = await Register(accounts);
            await seed.Services.GetRequiredService<MatchStore>().Create(initial, fp.AccountId, sh.AccountId);
        }
        ActionLogPage first;
        GameCommand second;
        await using (var server = await KestrelProcess.Start(postgres.Connection))
        {
            using var shadow = server.Client(sh);
            await CorrespondenceFixture.Send(shadow, command);
            first = await Page(shadow, initial.Id);
            var projection = await CorrespondenceFixture.Read<PlayerView>(shadow, $"/api/matches/{initial.Id}");
            second = new()
            {
                MatchId = initial.Id, CommandId = Guid.NewGuid().ToString("N"),
                ExpectedRevision = projection.Revision, DecisionId = projection.Decision!.Id,
                Kind = CommandKind.MoveArmies,
                Payload = new()
                {
                    Stepwise = true, DieId = projection.ActionSequence!.DieId,
                    Moves = [new() { From = "orthanc", To = "fords-of-isen", UnitIds = [unit] }]
                }
            };
        }
        await postgres.Restart();
        await using (var server = await KestrelProcess.Start(postgres.Connection))
        {
            using var shadow = server.Client(sh);
            Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(await Page(shadow, initial.Id)));
            Assert.Equal(1, (await CorrespondenceFixture.Send(shadow, command)).Revision);
            await CorrespondenceFixture.Send(shadow, second);
        }
        await postgres.Restart();
        await using (var server = await KestrelProcess.Start(postgres.Connection))
        {
            using var freePeoples = server.Client(fp);
            using var shadow = server.Client(sh);
            var action = await CorrespondenceFixture.Read<ActionLogView>(freePeoples,
                $"/api/matches/{initial.Id}/actions/{first.Actions[0].Id}");
            Assert.Equal(ActionLogStatus.Completed, action.Action.Status);
            Assert.Equal(2, action.Steps.SelectMany(s => s.Movements).Count());
            ActionLogFixture.AssertArmy(action.Steps[0].Movements[0]);
            Assert.Single(action.Steps.SelectMany(s => s.Costs));
            Assert.Equal(2, (await CorrespondenceFixture.Send(shadow, second)).Revision);
        }
        await using var inspect = new AccountHost(postgres.Connection);
        var final = (await inspect.Services.GetRequiredService<MatchStore>().Read(initial.Id, sh.AccountId))!.State;
        Assert.Equal(initial.RandomSamples.Count, final.RandomSamples.Count);
        Assert.Equal(DieLocation.Spent, final.Dice.Single(d => d.Id == command.Payload.DieId).Location);
    }

    private static async Task<SessionView> Register(AccountService accounts)
    {
        return await TestAccounts.Register(accounts, "test" + Guid.NewGuid().ToString("N")[..12], "Action log restart test password!");
    }
}
