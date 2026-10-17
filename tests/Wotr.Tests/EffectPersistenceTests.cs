using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class EffectPersistenceTests
{
    [PostgresFact]
    public async Task HttpRestartsBetweenRecruitAndDrawAndReplaysOnlyOriginalReceipt()
    {
        var state = EventTests.CelebornState();
        state.Id = Guid.NewGuid().ToString("N");
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var draw = state.Player(Side.FreePeoples).Decks["strategy"][0];
        var command = EventTests.Play(state, "base.fp.strategy.15", DieFace.Muster);
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            using var response = await alice.PostAsJsonAsync($"/api/matches/{state.Id}/commands", command, BoardCatalog.Json);
            Assert.True((await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        }

        await using var restarted = new TestHost();
        using var returned = restarted.Client("alice");
        using var bob = restarted.Client("bob");
        var restored = (await returned.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
        Assert.Equal(DecisionKind.EffectChoice, restored.Decision!.Kind);
        Assert.Equal(DieLocation.Resolving, restored.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        Assert.DoesNotContain(draw, restored.Hand);
        using var retry = await returned.PostAsJsonAsync($"/api/matches/{state.Id}/commands", command, BoardCatalog.Json);
        var receipt = (await retry.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted);
        Assert.Equal(1, receipt.Revision);
        var enemy = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
        Assert.Null(enemy.Decision);
        Assert.Empty(enemy.Options);

        var choice = new GameCommand
        {
            MatchId = state.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = restored.Revision,
            DecisionId = restored.Decision.Id,
            Kind = CommandKind.ChooseEffect,
            Payload = new() { Ids = [restored.Decision.Choices[0]] }
        };
        using var result = await returned.PostAsJsonAsync($"/api/matches/{state.Id}/commands", choice, BoardCatalog.Json);
        Assert.True((await result.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Accepted);
        var after = (await returned.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
        Assert.Equal(2, after.Revision);
        Assert.Equal(Side.Shadow, after.DecisionOwner);
        Assert.Equal(draw, Assert.Single(after.Hand));
        Assert.Equal(DieLocation.Spent, after.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        Assert.Equal("lorien", after.Units.Single(u => u.Id == choice.Payload.Ids[0]).Location);
        var enemyJson = await bob.GetStringAsync($"/api/matches/{state.Id}");
        Assert.DoesNotContain(draw, enemyJson);
        using var repeatedChoice = await returned.PostAsJsonAsync($"/api/matches/{state.Id}/commands", choice, BoardCatalog.Json);
        Assert.Equal(2, (await repeatedChoice.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!.Revision);
        var persisted = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Empty(persisted.Resolution.Frames);
        Assert.Single(persisted.Events, e => e.Kind == "CardDrawn");
        Assert.Single(persisted.Events, e => e.Kind == "EventPlayed");
    }
}
