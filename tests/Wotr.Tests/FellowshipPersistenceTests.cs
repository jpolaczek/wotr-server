using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class FellowshipPersistenceTests
{
    [PostgresFact]
    public async Task AcceptedRouteRestartsBetweenAdditionalTilesAndKeepsItsReceiptAndCost()
    {
        var state = PaidAction();
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Progress = 3;
        HuntTests.Pool(state, "base.hunt.standard.2.0.1", "base.hunt.standard.2.0.2");
        FellowshipFlow.BeginReveal(state, "test-hunt", FellowshipReturn.FinishAction);
        GameCommand route;
        PlayerView firstTile;
        await using (var first = new TestHost(new SequenceRandom(0)))
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            route = Choice(await Read(alice, state.Id), CommandKind.RevealFellowship,
                new() { Route = ["moria", "dimrill-dale", "north-anduin-vale", "dol-guldur"] });
            Assert.Equal(1, (await Send(alice, route)).Revision);
            firstTile = await Read(alice, state.Id);
            Assert.Equal(DecisionKind.HuntDamage, firstTile.Decision!.Kind);
            Assert.Equal("dol-guldur", firstTile.Fellowship.RegionId);
            Assert.Equal(DieLocation.Resolving, firstTile.Dice[0].Location);
        }

        PlayerView secondTile;
        var extraRandom = new SequenceRandom(0);
        await using (var second = new TestHost(extraRandom))
        {
            using var alice = second.Client("alice");
            using var bob = second.Client("bob");
            var restored = await Read(alice, state.Id);
            Assert.Equal(JsonSerializer.Serialize(firstTile, BoardCatalog.Json), JsonSerializer.Serialize(restored, BoardCatalog.Json));
            Assert.Equal(1, (await Send(alice, route)).Revision);
            Assert.Equal(0, extraRandom.Calls);
            await AssertPrivateDecision(bob, state.Id);
            await Send(alice, Choice(restored, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }));
            secondTile = await Read(alice, state.Id);
            Assert.Equal(DecisionKind.HuntDamage, secondTile.Decision!.Kind);
            Assert.NotEqual(firstTile.Hunt!.TileId, secondTile.Hunt!.TileId);
            Assert.Equal(1, extraRandom.Calls);
        }

        var finalRandom = new SequenceRandom();
        await using var third = new TestHost(finalRandom);
        using var returned = third.Client("alice");
        var pending = await Read(returned, state.Id);
        Assert.Equal(JsonSerializer.Serialize(secondTile, BoardCatalog.Json), JsonSerializer.Serialize(pending, BoardCatalog.Json));
        Assert.Equal(1, (await Send(returned, route)).Revision);
        await Send(returned, Choice(pending, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }));
        var saved = (await third.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Equal(4, saved.Fellowship.Corruption);
        Assert.Equal(DieLocation.Spent, saved.Dice[0].Location);
        Assert.Empty(saved.Resolution.Frames);
        Assert.Single(saved.Events, e => e.Kind == "FellowshipRevealed");
        Assert.Equal(2, saved.Events.Count(e => e.Kind == "HuntTileDrawn"));
        Assert.Equal(0, finalRandom.Calls);
    }

    [PostgresFact]
    public async Task HttpRestartsDuringRevelationAndRetriesDoNotRelocateOrPayTwice()
    {
        var state = PaidAction();
        state.Fellowship.Progress = 2;
        FellowshipFlow.BeginReveal(state, "test-hunt", FellowshipReturn.FinishAction);
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
        var restored = await Read(returned, state.Id);
        Assert.Equal(JsonSerializer.Serialize(before, BoardCatalog.Json), JsonSerializer.Serialize(restored, BoardCatalog.Json));
        Assert.Equal(DecisionKind.FellowshipRoute, restored.Decision!.Kind);
        Assert.Equal(DieLocation.Resolving, restored.Dice[0].Location);
        await AssertPrivateDecision(bob, state.Id);

        var command = Choice(restored, CommandKind.RevealFellowship,
            new() { Route = ["rivendell", "fords-of-bruinen", "hollin"] });
        Assert.Equal(1, (await Send(returned, command)).Revision);
        Assert.Equal(1, (await Send(returned, command)).Revision);
        var after = await Read(returned, state.Id);
        Assert.Equal("hollin", after.Fellowship.RegionId);
        Assert.Equal(0, after.Fellowship.Progress);
        Assert.False(after.Fellowship.Hidden);
        Assert.Equal(Side.Shadow, after.DecisionOwner);
        Assert.Single(after.Dice, d => d.Location == DieLocation.Spent);
        var persisted = (await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Single(persisted.Events, e => e.Kind == "FellowshipRevealed");
        Assert.Empty(persisted.Resolution.Frames);
    }

    [PostgresFact]
    public async Task HttpRestartsAtForcedGroupThenGuideAndKeepsTheOriginalPaidContinuation()
    {
        var state = PaidAction();
        FellowshipFlow.BeginSeparation(state, "test-effect", FellowshipReturn.FinishAction);
        PlayerView before;
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            before = await Read(alice, state.Id);
        }

        GameCommand separate;
        PlayerView guide;
        await using (var second = new TestHost())
        {
            using var alice = second.Client("alice");
            using var bob = second.Client("bob");
            var restored = await Read(alice, state.Id);
            Assert.Equal(JsonSerializer.Serialize(before, BoardCatalog.Json), JsonSerializer.Serialize(restored, BoardCatalog.Json));
            Assert.Equal(DecisionKind.CompanionSeparation, restored.Decision!.Kind);
            Assert.Empty(Assert.Single(restored.Options).EligibleDice);
            await AssertPrivateDecision(bob, state.Id);
            separate = Choice(restored, CommandKind.SeparateCompanions,
                new() { Ids = ["base.character.gimli"], Route = ["rivendell", "fords-of-bruinen"] });
            Assert.Equal(1, (await Send(alice, separate)).Revision);
            guide = await Read(alice, state.Id);
            Assert.Equal(DecisionKind.Guide, guide.Decision!.Kind);
            Assert.Equal(DieLocation.Resolving, guide.Dice[0].Location);
        }

        await using var third = new TestHost();
        using var returned = third.Client("alice");
        var pending = await Read(returned, state.Id);
        Assert.Equal(JsonSerializer.Serialize(guide, BoardCatalog.Json), JsonSerializer.Serialize(pending, BoardCatalog.Json));
        Assert.Equal(1, (await Send(returned, separate)).Revision);
        var choice = Choice(pending, CommandKind.SelectGuide, new() { GuideId = "base.character.strider" });
        Assert.Equal(2, (await Send(returned, choice)).Revision);
        Assert.Equal(2, (await Send(returned, choice)).Revision);
        var persisted = (await third.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Equal("fords-of-bruinen", Assert.Single(persisted.Characters).Location);
        Assert.Single(persisted.Events, e => e.Kind == "CompanionsSeparated");
        Assert.Single(persisted.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal("base.character.strider", persisted.Fellowship.Guide);
        Assert.Equal(Side.Shadow, persisted.Pending!.Owner);
        Assert.Empty(persisted.Resolution.Frames);
    }

    private static MatchState PaidAction()
    {
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        DiceRules.Pay(state, Side.FreePeoples, state.Dice[0].Id, DieFace.Character);
        return state;
    }

    private static async Task<PlayerView> Read(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;

    private static async Task AssertPrivateDecision(HttpClient enemy, string id)
    {
        var view = await Read(enemy, id);
        Assert.Null(view.Decision);
        Assert.Empty(view.Options);
        Assert.Empty(view.OwnCompanions);
    }

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
