using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BalrogHuntPersistenceTests
{
    [PostgresTheory]
    [InlineData(BalrogHuntProjectionTests.Movement, "damage")]
    [InlineData(BalrogHuntProjectionTests.Movement, "guide")]
    [InlineData(BalrogHuntProjectionTests.Movement, "none")]
    [InlineData(ShadowCharacterCards01.OrcPatrol, "damage")]
    [InlineData(ShadowCharacterCards01.IsildursBane, "damage")]
    [InlineData(ShadowCharacterCards02.FoulThing, "guide")]
    [InlineData(ShadowCharacterCards02.Strike, "damage")]
    public async Task RestartsRetainTheBalrogReactionAndThenCompleteTheOriginalPayment(string parent, string window)
    {
        var state = BalrogHuntProjectionTests.At(parent, window);
        var payment = Assert.Single(state.Resolution.PaidDieIds);
        await using (var host = new TestHost(new SequenceRandom()))
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        GameCommand? previous = null;
        PlayerView? last = null;
        for (var step = 0; step < 8; step++)
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
            var stored = (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
            if (stored.Pending?.Kind == DecisionKind.Action)
            {
                Assert.Empty(stored.Resolution.Frames);
                Assert.Empty(stored.Resolution.PaidDieIds);
                Assert.Equal(parent == BalrogHuntProjectionTests.Movement ? DieLocation.Hunt : DieLocation.Spent,
                    stored.Dice.Single(d => d.Id == payment).Location);
                Assert.True(stored.HuntReport!.Completed);
                Assert.Equal(1, stored.Player(Side.Shadow).Discard.Count(id => id == ShadowCharacterCards03.Balrog));
                Assert.Equal(window == "none" ? 2 : 4,
                    stored.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.Started));
                return;
            }
            var waiting = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            Assert.Null(waiting.Decision);
            Assert.Equal(JsonSerializer.Serialize(view.ActionBoundary!.FellowshipMove), JsonSerializer.Serialize(waiting.ActionBoundary!.FellowshipMove));
            Assert.Equal(JsonSerializer.Serialize(view.ActionBoundary.CardHunt), JsonSerializer.Serialize(waiting.ActionBoundary.CardHunt));
            Assert.Equal(DieLocation.Resolving, view.Dice.Single(d => d.Id == payment).Location);
            var kind = view.Decision!.Kind == DecisionKind.Guide ? CommandKind.SelectGuide : CommandKind.ResolveHuntDamage;
            var payload = new CommandPayload { HuntResponse = HuntResponse.Ring };
            if (kind == CommandKind.SelectGuide)
                payload.GuideId = view.Options.Single(o => o.Kind == kind).EligibleIds[0];
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
        Assert.Fail("Balrog and the original paid Hunt did not finish within the fixture bound.");
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
