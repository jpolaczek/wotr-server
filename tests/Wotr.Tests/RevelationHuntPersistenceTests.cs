using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class RevelationHuntPersistenceTests
{
    public static IEnumerable<object[]> Choices() => RevelationHuntProjectionTests.Roots()
        .SelectMany(root => new[] { false, true }.Select(respond => new[] { root[0], root[1], respond }));

    [PostgresTheory]
    [MemberData(nameof(Choices))]
    public async Task EitherAnswerSurvivesRestartAndCompletesTheSamePayment(string parent, string window, bool respond)
    {
        var state = BalrogHuntProjectionTests.At(parent, window, offer: true);
        var payment = Assert.Single(state.Resolution.PaidDieIds);
        await using (var host = new TestHost(new SequenceRandom()))
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        GameCommand? previous = null;
        var previousOwner = Side.Shadow;
        PlayerView? last = null;
        for (var step = 0; step < 10; step++)
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
            var fp = (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            var shadow = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            if (last is not null)
                Assert.Equal(JsonSerializer.Serialize(last), JsonSerializer.Serialize(shadow));
            var stored = (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
            if (stored.Pending?.Kind == DecisionKind.Action)
            {
                Assert.Empty(stored.Resolution.Frames);
                Assert.Empty(stored.Resolution.PaidDieIds);
                Assert.Equal(parent == BalrogHuntProjectionTests.Movement ? DieLocation.Hunt : DieLocation.Spent,
                    stored.Dice.Single(d => d.Id == payment).Location);
                Assert.Equal(respond ? 1 : 0, stored.Player(Side.Shadow).Discard.Count(id => id == ShadowCharacterCards03.Balrog));
                Assert.Equal(!respond, stored.Player(Side.Shadow).Table.Contains(ShadowCharacterCards03.Balrog));
                Assert.True(stored.HuntReport!.Completed);
                var strongholds = window == "none" ? 0 : window == "single" ? 1 : 2;
                Assert.Equal(1 + strongholds + (respond ? 1 : 0),
                    stored.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.Started));
                return;
            }
            var acting = fp.Decision is not null ? fp : shadow;
            var waiting = fp.Decision is not null ? shadow : fp;
            Assert.Null(waiting.Decision);
            Assert.Empty(waiting.Options);
            Assert.Equal(JsonSerializer.Serialize(acting.ActionBoundary!.FellowshipMove), JsonSerializer.Serialize(waiting.ActionBoundary!.FellowshipMove));
            Assert.Equal(JsonSerializer.Serialize(acting.ActionBoundary.CardHunt), JsonSerializer.Serialize(waiting.ActionBoundary.CardHunt));
            Assert.Equal(DieLocation.Resolving, acting.Dice.Single(d => d.Id == payment).Location);
            var kind = acting.Decision!.Kind switch
            {
                DecisionKind.Reaction => respond ? CommandKind.RespondEffect : CommandKind.DeclineReaction,
                DecisionKind.Guide => CommandKind.SelectGuide,
                _ => CommandKind.ResolveHuntDamage
            };
            var payload = new CommandPayload { HuntResponse = HuntResponse.Ring };
            if (kind == CommandKind.RespondEffect)
                payload.ReactionId = ShadowCharacterCards03.BalrogReaction;
            if (kind == CommandKind.SelectGuide)
                payload.GuideId = acting.Options.Single(o => o.Kind == kind).EligibleIds[0];
            var command = new GameCommand
            {
                MatchId = state.Id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = acting.Revision,
                DecisionId = acting.Decision.Id, Kind = kind, Payload = payload
            };
            var client = acting.You == Side.FreePeoples ? alice : bob;
            Assert.Equal(acting.Revision + 1, (await Send(client, command)).Revision);
            var consumed = random.Calls;
            if (step == 0 && !respond && window == "none")
                Assert.Equal(0, consumed);
            Assert.Equal(acting.Revision + 1, (await Send(client, command)).Revision);
            Assert.Equal(consumed, random.Calls);
            last = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            previous = command;
            previousOwner = acting.You;
        }
        Assert.Fail("Revelation and its remaining Hunts did not finish within the fixture bound.");
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
