using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class PhaseWornPersistenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var window in PhaseBoundaryProjectionTests.Windows)
            yield return [window, ""];
        foreach (var parent in new[] { "movement", "stronghold", "balrog", ShadowCharacterCards01.OrcPatrol,
            ShadowCharacterCards02.FoulThing, ShadowCharacterCards02.Strike })
            foreach (var choice in new[] { WornDiscardStep.Decline, WornDiscardStep.RandomHand })
                yield return [parent, choice];
    }

    [PostgresTheory]
    [MemberData(nameof(Cases))]
    public async Task RestartAndReplayPreserveThePhaseOrNestedHuntReturn(string root, string choice)
    {
        var state = choice == "" ? PhaseBoundaryProjectionTests.At(root) : WornHuntProjectionTests.At(root);
        var payment = state.Resolution.PaidDieIds.SingleOrDefault();
        var paymentOwner = payment is null ? Side.FreePeoples : state.Dice.Single(d => d.Id == payment).Owner;
        await using (var host = new TestHost(new SequenceRandom()))
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        GameCommand? previous = null;
        var previousOwner = Side.FreePeoples;
        PlayerView? last = null;
        for (var step = 0; step < 16; step++)
        {
            var random = new SequenceRandom(new int[200]);
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
            var acting = fp.Decision is not null ? fp : shadow;
            var waiting = fp.Decision is not null ? shadow : fp;
            if (acting.Decision!.Kind == DecisionKind.Action)
            {
                var final = (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
                Assert.Empty(final.Resolution.PaidDieIds);
                Assert.Empty(final.Resolution.Frames);
                Assert.All(final.Players, p => Assert.True(p.Hand.Count <= 6));
                if (payment is not null)
                    Assert.Equal(choice != "" && paymentOwner == Side.FreePeoples ? DieLocation.Hunt : DieLocation.Spent,
                        final.Dice.Single(d => d.Id == payment).Location);
                if (choice != "")
                    Assert.Contains(ShadowCharacterCards03.Worn, final.Player(Side.Shadow).Table);
                return;
            }
            Assert.NotNull(acting.ActionBoundary);
            Assert.NotNull(waiting.ActionBoundary);
            Assert.Null(waiting.Decision);
            Assert.Empty(waiting.Options);
            var kind = acting.Decision.Kind switch
            {
                DecisionKind.FellowshipPhase => CommandKind.ContinueFellowship,
                DecisionKind.AllocateHunt => CommandKind.AllocateHunt,
                DecisionKind.DiscardCards => CommandKind.DiscardCards,
                DecisionKind.EffectChoice => CommandKind.ChooseEffect,
                DecisionKind.Guide => CommandKind.SelectGuide,
                _ => CommandKind.ResolveHuntDamage
            };
            var payload = new CommandPayload { HuntResponse = HuntResponse.Ring };
            if (kind == CommandKind.DiscardCards)
                payload.Ids = [acting.Hand[0]];
            if (kind == CommandKind.ChooseEffect)
                payload.Ids = [choice];
            if (kind == CommandKind.SelectGuide)
                payload.GuideId = acting.Options.Single(o => o.Kind == kind).EligibleIds[0];
            var command = new GameCommand
            {
                MatchId = state.Id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = acting.Revision,
                DecisionId = acting.Decision.Id, Kind = kind, Payload = payload
            };
            var client = acting.You == Side.FreePeoples ? alice : bob;
            Assert.Equal(acting.Revision + 1, (await Send(client, command)).Revision);
            var calls = random.Calls;
            Assert.Equal(acting.Revision + 1, (await Send(client, command)).Revision);
            Assert.Equal(calls, random.Calls);
            previous = command;
            previousOwner = acting.You;
            last = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
        }
        Assert.Fail("The phase or nested Hunt did not finish within the fixture bound.");
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
