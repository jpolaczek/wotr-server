using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class HuntPersistenceTests
{
    [PostgresFact]
    public async Task HttpRestartsAtRerollDamageGuideAndRouteWithoutRerollingCommittedResults()
    {
        var state = HuntTests.State(1, "moria");
        state.Id = Guid.NewGuid().ToString("N");
        HuntTests.Pool(state, HuntTests.Eye, "base.hunt.standard.eye.1.2");
        var move = FellowshipTests.Command(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
        await using (var first = new TestHost(new SequenceRandom(0)))
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            Assert.Equal(1, (await Send(alice, move)).Revision);
        }

        GameCommand reroll;
        var huntRandom = new SequenceRandom(5, 0);
        await using (var second = new TestHost(huntRandom))
        {
            using var alice = second.Client("alice");
            using var bob = second.Client("bob");
            Assert.Equal(1, (await Send(alice, move)).Revision);
            Assert.Equal(0, huntRandom.Calls);
            var view = await Read(bob, state.Id);
            Assert.Equal(DecisionKind.HuntReroll, view.Decision!.Kind);
            Assert.Equal(1, view.Hunt!.Dice[0].Result);
            Assert.Null((await Read(alice, state.Id)).Decision);
            reroll = Choice(view, CommandKind.RerollHunt, new() { Ids = ["hunt-die-0"] });
            Assert.Equal(2, (await Send(bob, reroll)).Revision);
            Assert.Equal(2, (await Send(bob, reroll)).Revision);
            Assert.Equal(2, huntRandom.Calls);
        }

        GameCommand casualty;
        var casualtyRandom = new SequenceRandom(2); // Boromir, chosen on the server.
        await using (var third = new TestHost(casualtyRandom))
        {
            using var alice = third.Client("alice");
            using var bob = third.Client("bob");
            Assert.Equal(2, (await Send(bob, reroll)).Revision);
            Assert.Equal(0, casualtyRandom.Calls);
            var view = await Read(alice, state.Id);
            Assert.Equal(DecisionKind.HuntDamage, view.Decision!.Kind);
            Assert.Equal(HuntTests.Eye, view.Hunt!.TileId);
            Assert.Equal(1, view.Hunt.Damage);
            Assert.Equal(DieLocation.Resolving, view.Dice[0].Location);
            casualty = Choice(view, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion });
            Assert.Equal(3, (await Send(alice, casualty)).Revision);
            Assert.Equal(3, (await Send(alice, casualty)).Revision);
            Assert.Equal(1, casualtyRandom.Calls);
        }

        await using (var fourth = new TestHost(new SequenceRandom()))
        {
            using var alice = fourth.Client("alice");
            Assert.Equal(3, (await Send(alice, casualty)).Revision);
            var view = await Read(alice, state.Id);
            Assert.Equal(DecisionKind.Guide, view.Decision!.Kind);
            Assert.True(view.Hunt!.CasualtyTaken);
            var guide = Choice(view, CommandKind.SelectGuide, new() { GuideId = "base.character.gandalf-grey" });
            Assert.Equal(4, (await Send(alice, guide)).Revision);
        }

        var extraRandom = new SequenceRandom(0);
        await using var fifth = new TestHost(extraRandom);
        using var returned = fifth.Client("alice");
        var routeView = await Read(returned, state.Id);
        Assert.Equal(DecisionKind.FellowshipRoute, routeView.Decision!.Kind);
        var route = Choice(routeView, CommandKind.RevealFellowship, new() { Route = ["moria"] });
        Assert.Equal(5, (await Send(returned, route)).Revision);
        Assert.Equal(5, (await Send(returned, route)).Revision);
        Assert.Equal(1, extraRandom.Calls);
        var persisted = (await fifth.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Equal(DieLocation.Hunt, persisted.Dice[0].Location);
        Assert.Equal(DiceRules.FellowshipMove, persisted.Dice[0].HuntOrigin);
        Assert.Equal("base.character.boromir", Assert.Single(persisted.Characters).Id);
        Assert.Equal(2, persisted.Events.Count(e => e.Kind == "HuntTileDrawn"));
        Assert.Equal(5, persisted.RandomSamples.Count - state.RandomSamples.Count);
        Assert.Equal(Side.Shadow, persisted.Pending!.Owner);
        Assert.Equal(0, persisted.Fellowship.Corruption);
        Assert.Empty(persisted.Resolution.Frames);
    }

    [PostgresFact]
    public async Task HttpRestoresTableAndNewGuideReactionWithoutPayingAnotherDie()
    {
        var catalog = HuntResolutionTests.GuideCatalog();
        var state = HuntTests.State(5);
        state.Id = Guid.NewGuid().ToString("N");
        state.Player(Side.FreePeoples).Table.Add("test.shield");
        HuntTests.Pool(state, HuntTests.Eye);
        state = HuntTests.Move(state, new SequenceRandom(5, 5, 5, 5, 5, 0), catalog);
        await using (var first = new TestHost(new SequenceRandom(), catalog))
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");

        (DecisionKind Kind, CommandKind Command, CommandPayload Payload)[] steps =
        [
            (DecisionKind.Reaction, CommandKind.RespondEffect, new() { ReactionId = "test.shield" }),
            (DecisionKind.HuntDamage, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide }),
            (DecisionKind.Reaction, CommandKind.RespondEffect, new() { ReactionId = "test.guide" }),
            (DecisionKind.FellowshipRoute, CommandKind.RevealFellowship, new() { Route = ["hollin"] })
        ];
        foreach (var step in steps)
        {
            await using var host = new TestHost(new SequenceRandom(), catalog);
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            var view = await Read(alice, state.Id);
            Assert.Equal(step.Kind, view.Decision!.Kind);
            Assert.Equal(DieLocation.Resolving, view.Dice[0].Location);
            Assert.Equal(0, view.Fellowship.Corruption);
            Assert.Null((await Read(bob, state.Id)).Decision);
            var command = Choice(view, step.Command, step.Payload);
            Assert.Equal(view.Revision + 1, (await Send(alice, command)).Revision);
            Assert.Equal(view.Revision + 1, (await Send(alice, command)).Revision);
        }
        await using var final = new TestHost(new SequenceRandom(), catalog);
        var persisted = (await final.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Equal(state.RandomSamples.Count, persisted.RandomSamples.Count);
        Assert.Equal(DieLocation.Hunt, persisted.Dice[0].Location);
        Assert.Single(persisted.Player(Side.FreePeoples).Discard);
        Assert.Single(persisted.Characters);
        Assert.Equal(0, persisted.Fellowship.Corruption);
        Assert.Empty(persisted.Resolution.Frames);
    }

    private static async Task<PlayerView> Read(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;

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
