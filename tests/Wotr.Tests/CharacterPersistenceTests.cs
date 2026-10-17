using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class CharacterPersistenceTests
{
    [PostgresFact]
    public async Task TransformationKeepsFigureAndHistoricalMinionAcrossRestartAndRetry()
    {
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        CharacterFixture.Put(state, CharacterRules.Grey, "casualties");
        state.MinionEverEntered = true;
        state.Dice[0].Face = DieFace.WillOfTheWest;
        GameCommand command;
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            var view = await Read(alice, state.Id);
            command = Choice(view, CommandKind.EnterCharacter, new() { DieId = state.Dice[0].Id, CharacterId = CharacterRules.White, RegionId = "fangorn" });
            Assert.Equal(1, (await Send(alice, command)).Revision);
        }
        await using var restarted = new TestHost();
        using var returned = restarted.Client("alice");
        Assert.Equal(1, (await Send(returned, command)).Revision);
        var saved = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.True(saved.MinionEverEntered);
        Assert.Equal(1, saved.Player(Side.FreePeoples).ExtraDiceNextRecovery);
        Assert.Equal(CharacterRules.Grey, Assert.Single(saved.Characters).Id);
        Assert.Equal(CharacterRules.White, saved.Characters[0].DefinitionId);
        Assert.Single(saved.Events, e => e.Kind == "CharacterEntered");
        Assert.Single(saved.Dice, d => d.Location == DieLocation.Spent);
    }

    [PostgresFact]
    public async Task GreyOptionalDrawRestartsBeforeAndAfterResponseWithoutLeakingTheCard()
    {
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        HandRules.Add(state.Player(Side.FreePeoples), CharacterReactionTests.Bilbo);
        var p = CharacterFixture.Cost(state, DieFace.Event);
        p.CardId = CharacterReactionTests.Bilbo;
        state = CharacterFixture.Do(state, CommandKind.PlayEvent, p);
        PlayerView before;
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            before = await Read(alice, state.Id);
        }
        await using var restarted = new TestHost();
        using var returned = restarted.Client("alice");
        using var bob = restarted.Client("bob");
        var view = await Read(returned, state.Id);
        Assert.Equal(JsonSerializer.Serialize(before, BoardCatalog.Json), JsonSerializer.Serialize(view, BoardCatalog.Json));
        Assert.Equal("event.completed", view.Decision!.Window);
        Assert.Null((await Read(bob, state.Id)).Decision);
        var top = state.Player(Side.FreePeoples).Decks["character"][0];
        var command = Choice(view, CommandKind.RespondEffect, new() { ReactionId = "character.grey-draw" });
        var receipt = await Send(returned, command);
        Assert.Equal(receipt.Revision, (await Send(returned, command)).Revision);
        Assert.Contains(top, (await Read(returned, state.Id)).Hand);
        var enemy = JsonSerializer.Serialize(await Read(bob, state.Id), BoardCatalog.Json);
        Assert.DoesNotContain(top, enemy);
        Assert.DoesNotContain(Wotr.Domain.Content.CardCatalog.Title(top), enemy);
        var saved = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Single(saved.Events, e => e.Kind == "CardDrawn" && e.Text.Contains(Wotr.Domain.Content.CardCatalog.Title(top)));
        Assert.Empty(saved.Resolution.Frames);
    }

    [PostgresFact]
    public async Task FixedHobbitGroupRestartsWithOriginalOriginAndRetryDoesNotSeparateTwice()
    {
        var state = HuntTests.Move(CharacterHuntTests.Hobbits(), new SequenceRandom(5, 0));
        state = CharacterFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.merry-guide" });
        state = CharacterFixture.Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.pippin-guide" });
        state = CharacterFixture.Do(state, CommandKind.DeclineReaction);
        state.Id = Guid.NewGuid().ToString("N");
        await using (var first = new TestHost())
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        await using var restarted = new TestHost();
        using var alice = restarted.Client("alice");
        var view = await Read(alice, state.Id);
        Assert.Equal(DecisionKind.CompanionSeparation, view.Decision!.Kind);
        var option = Assert.Single(view.Options);
        Assert.All(option.Travel, t => Assert.Equal(new[] { CharacterHuntTests.Merry, CharacterHuntTests.Pippin }, t.RequiredGroup));
        var command = Choice(view, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry, CharacterHuntTests.Pippin], Route = ["hollin", "north-dunland"] });
        var receipt = await Send(alice, command);
        Assert.Equal(receipt.Revision, (await Send(alice, command)).Revision);
        var saved = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.All(saved.Characters, c => Assert.Equal("north-dunland", c.Location));
        Assert.Single(saved.Events, e => e.Kind == "CompanionsSeparated");
        Assert.Equal(1, saved.Fellowship.Corruption);
    }

    private static async Task<PlayerView> Read(HttpClient client, string id) => (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;
    private static GameCommand Choice(PlayerView view, CommandKind kind, CommandPayload payload) => new()
    {
        MatchId = view.MatchId,
        ExpectedRevision = view.Revision,
        DecisionId = view.Decision!.Id,
        CommandId = Guid.NewGuid().ToString("N"),
        Kind = kind,
        Payload = payload
    };
    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        return receipt;
    }
}
