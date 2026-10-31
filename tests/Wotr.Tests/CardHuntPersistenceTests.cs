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
    public async Task RestartsKeepTheOriginalCardPaymentAndReplayEachAcceptedChoice(string card, string window)
    {
        var state = CardHuntProjectionTests.At(card, window);
        var paid = Assert.Single(state.Resolution.PaidDieIds);
        var initialHuntDice = state.Dice.Where(d => d.Location == DieLocation.Hunt).Select(d => d.Id).ToArray();
        await using (var host = new TestHost(new SequenceRandom()))
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        GameCommand? previous = null;
        PlayerView? last = null;
        for (var step = 0; step < 5; step++)
        {
            var random = new SequenceRandom(0);
            await using var host = new TestHost(random);
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            if (previous is not null)
            {
                Assert.Equal(previous.ExpectedRevision + 1, (await Send(alice, previous)).Revision);
                Assert.Equal(0, random.Calls);
            }
            var view = (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            if (last is not null)
                Assert.Equal(JsonSerializer.Serialize(last), JsonSerializer.Serialize(view));
            if (view.Decision?.Kind == DecisionKind.Action)
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
            var opposing = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            Assert.Null(opposing.Decision);
            Assert.Equal(JsonSerializer.Serialize(view.ActionBoundary!.CardHunt),
                JsonSerializer.Serialize(opposing.ActionBoundary!.CardHunt));
            Assert.Equal(card, view.ActionBoundary.CardHunt!.CardId);
            Assert.Equal(DieLocation.Resolving, view.Dice.Single(d => d.Id == paid).Location);
            var kind = view.Decision!.Kind == DecisionKind.Guide ? CommandKind.SelectGuide : CommandKind.ResolveHuntDamage;
            var payload = kind == CommandKind.SelectGuide
                ? new CommandPayload { GuideId = view.Options.Single(o => o.Kind == kind).EligibleIds[0] }
                : new CommandPayload { HuntResponse = HuntResponse.Ring };
            var command = new GameCommand
            {
                MatchId = state.Id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
                DecisionId = view.Decision.Id, Kind = kind, Payload = payload
            };
            Assert.Equal(view.Revision + 1, (await Send(alice, command)).Revision);
            var consumed = random.Calls;
            Assert.Equal(view.Revision + 1, (await Send(alice, command)).Revision);
            Assert.Equal(consumed, random.Calls);
            last = (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            previous = command;
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
