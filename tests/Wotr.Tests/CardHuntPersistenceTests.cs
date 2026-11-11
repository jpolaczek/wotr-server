using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class CardHuntPersistenceTests
{
    [PostgresTheory]
    [InlineData(ShadowCharacterCards01.OrcPatrol, "first")]
    [InlineData(ShadowCharacterCards01.IsildursBane, "first")]
    [InlineData(ShadowCharacterCards02.FoulThing, "guide")]
    [InlineData(ShadowCharacterCards02.Strike, "first")]
    [InlineData(ShadowCharacterCards02.Strike, "guide")]
    [InlineData(ShadowCharacterCards02.Strike, "reroll")]
    [InlineData(ShadowCharacterCards02.Strike, "roll")]
    [InlineData(ShadowCharacterCards02.Strike, "draw")]
    [InlineData(ShadowCharacterCards02.Strike, "palantir")]
    public async Task RestartsKeepTheOriginalCardPaymentAndReplayEachAcceptedChoice(string card, string window)
    {
        var state = CardHuntProjectionTests.At(card, window);
        var paid = Assert.Single(state.Resolution.PaidDieIds);
        var initialHuntDice = state.Dice.Where(d => d.Location == DieLocation.Hunt).Select(d => d.Id).ToArray();
        await using (var host = new TestHost(new SequenceRandom()))
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        GameCommand? previous = null;
        var previousOwner = Side.FreePeoples;
        PlayerView? last = null;
        for (var step = 0; step < 8; step++)
        {
            var random = new SequenceRandom(0);
            await using var host = new TestHost(random);
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            if (previous is not null)
            {
                Assert.Equal(previous.ExpectedRevision + 1,
                    (await Send(previousOwner == Side.FreePeoples ? alice : bob, previous)).Revision);
                Assert.Equal(0, random.Calls);
            }
            var view = (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            if (last is not null)
                Assert.Equal(JsonSerializer.Serialize(last), JsonSerializer.Serialize(view));
            var opposing = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            if (view.Decision?.Kind == DecisionKind.Action || opposing.Decision?.Kind == DecisionKind.Action)
            {
                var final = (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
                Assert.Empty(final.Resolution.PaidDieIds);
                Assert.Empty(final.Resolution.Frames);
                Assert.Equal(DieLocation.Spent, final.Dice.Single(d => d.Id == paid).Location);
                Assert.Equal(initialHuntDice, final.Dice.Where(d => d.Location == DieLocation.Hunt).Select(d => d.Id));
                Assert.Equal(1, final.Player(Side.Shadow).Discard.Count(id => id == card));
                Assert.True(final.HuntReport!.Completed);
                return;
            }
            var acting = view.Decision is not null ? view : opposing;
            var waiting = view.Decision is not null ? opposing : view;
            Assert.Null(waiting.Decision);
            if (acting.Decision!.Kind != DecisionKind.EffectChoice)
            {
                Assert.Equal(JsonSerializer.Serialize(view.ActionBoundary!.CardHunt),
                    JsonSerializer.Serialize(opposing.ActionBoundary!.CardHunt));
                Assert.Equal(card, view.ActionBoundary.CardHunt!.CardId);
            }
            Assert.Equal(DieLocation.Resolving, view.Dice.Single(d => d.Id == paid).Location);
            var kind = acting.Decision.Kind switch
            {
                DecisionKind.Guide => CommandKind.SelectGuide,
                DecisionKind.HuntReroll => CommandKind.RerollHunt,
                DecisionKind.Reaction => CommandKind.DeclineReaction,
                DecisionKind.EffectChoice => CommandKind.ChooseEffect,
                _ => CommandKind.ResolveHuntDamage
            };
            var payload = new CommandPayload { HuntResponse = HuntResponse.Ring };
            if (kind == CommandKind.SelectGuide)
                payload.GuideId = acting.Options.Single(o => o.Kind == kind).EligibleIds[0];
            if (kind == CommandKind.ChooseEffect)
                payload.Ids = ["character"];
            var command = new GameCommand
            {
                MatchId = state.Id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
                DecisionId = acting.Decision.Id, Kind = kind, Payload = payload
            };
            var client = acting.You == Side.FreePeoples ? alice : bob;
            Assert.Equal(view.Revision + 1, (await Send(client, command)).Revision);
            var consumed = random.Calls;
            Assert.Equal(view.Revision + 1, (await Send(client, command)).Revision);
            Assert.Equal(consumed, random.Calls);
            last = (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            previous = command;
            previousOwner = acting.You;
        }
        Assert.Fail("The reviewed card Hunt did not finish within its fixture bound.");
    }

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        return receipt;
    }
}
