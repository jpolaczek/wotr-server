using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class MusterPersistenceTests
{
    [PostgresFact]
    public async Task HttpMusterRestartsDuringExcessSelectionAndRetryNeverPaysTwice()
    {
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        state.Dice[0].Face = DieFace.Muster;
        var nation = state.Nations.Single(n => n.Id == "gondor");
        nation.Active = true;
        nation.StepsToWar = 0;
        foreach (var unit in state.Units.Where(u => u.Location == "pelargir"))
            unit.Location = "reserve";
        var army = state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular).Take(10).ToList();
        foreach (var unit in army)
            unit.Location = "pelargir";
        var elite = state.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Elite && u.Location == "reserve");
        var command = KernelTests.Command(state, CommandKind.Recruit);
        command.Payload = new() { DieId = state.Dice[0].Id, Placements = [new() { UnitId = elite.Id, RegionId = "pelargir" }] };
        await using (var first = new TestHost())
        {
            using var alice = first.Client("alice");
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            var initial = await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json);
            Assert.Contains(initial!.Options, o => o.Kind == CommandKind.Recruit && o.Recruitment.Any(r => r.ReserveIds.Contains(elite.Id)));
            using var response = await alice.PostAsJsonAsync($"/api/matches/{state.Id}/commands", command, BoardCatalog.Json);
            response.EnsureSuccessStatusCode();
            Assert.True((await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        }
        await using var restarted = new TestHost();
        using var returned = restarted.Client("alice");
        var restored = await returned.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json);
        Assert.Equal(DecisionKind.RemoveExcess, restored!.Decision!.Kind);
        Assert.Equal(DieLocation.Resolving, restored.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        using var repeat = await returned.PostAsJsonAsync($"/api/matches/{state.Id}/commands", command, BoardCatalog.Json);
        Assert.Equal(restored.Revision, (await repeat.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Revision);
        var remove = new GameCommand
        {
            MatchId = state.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = restored.Revision,
            DecisionId = restored.Decision.Id,
            Kind = CommandKind.RemoveExcess,
            Payload = new() { Ids = [army[0].Id] }
        };
        using var finish = await returned.PostAsJsonAsync($"/api/matches/{state.Id}/commands", remove, BoardCatalog.Json);
        Assert.True((await finish.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        var after = await returned.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json);
        Assert.Equal(DieLocation.Spent, after!.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        Assert.Equal("reserve", after.Units.Single(u => u.Id == army[0].Id).Location);
        Assert.Equal(Side.Shadow, after.DecisionOwner);
        Assert.Equal(2, after.Revision);
    }
}
