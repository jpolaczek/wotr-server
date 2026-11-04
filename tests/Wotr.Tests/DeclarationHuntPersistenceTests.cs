using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class DeclarationHuntPersistenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var window in DeclarationHuntProjectionTests.Windows.Where(w => w != "placement"))
            yield return [window, false];
        yield return ["offer", true];
    }

    [PostgresTheory]
    [MemberData(nameof(Cases))]
    public async Task RestartAndReceiptReplayReturnToTheOriginalFellowshipPhase(string window, bool respond)
    {
        var state = DeclarationHuntProjectionTests.At(window);
        var returnToken = state.Resolution.Frames[0].ReturnDecision!.Id;
        await using (var host = new TestHost(new SequenceRandom()))
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
        GameCommand? previous = null;
        var previousOwner = Side.FreePeoples;
        string? previousViews = null;
        for (var step = 0; step < 20; step++)
        {
            var random = new SequenceRandom(new int[100]);
            await using var host = new TestHost(random);
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            if (previous is not null)
            {
                await Send(previousOwner == Side.FreePeoples ? alice : bob, previous);
                Assert.Equal(0, random.Calls);
            }
            var fp = (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            var sh = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
            if (previousViews is not null)
                Assert.Equal(previousViews, JsonSerializer.Serialize(new[] { fp, sh }));
            var view = fp.Decision is not null ? fp : sh;
            var kind = view.Decision!.Kind;
            if (kind == DecisionKind.FellowshipPhase)
            {
                Assert.Equal(returnToken, view.Decision.Id);
                var final = (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
                Assert.Empty(final.Resolution.Frames);
                Assert.Empty(final.Resolution.PaidDieIds);
                Assert.Equal(final.Turn, final.Fellowship.LastDeclarationTurn);
                var resume = Command(view, CommandKind.ContinueFellowship);
                await Send(alice, resume);
                var next = (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!;
                Assert.Equal(DecisionKind.AllocateHunt, next.Decision!.Kind);
                return;
            }
            Assert.NotNull(fp.ActionBoundary!.FellowshipDeclaration);
            Assert.NotNull(sh.ActionBoundary!.FellowshipDeclaration);
            var command = kind switch
            {
                DecisionKind.Reaction when respond && view.Decision.Window == "fellowship.declared" =>
                    Command(view, CommandKind.RespondEffect,
                        new() { ReactionId = ShadowCharacterCards03.BalrogReaction + ".declaration" }),
                DecisionKind.Reaction => Command(view, CommandKind.DeclineReaction),
                DecisionKind.Guide => Command(view, CommandKind.SelectGuide,
                    new() { GuideId = view.Options.Single(o => o.Kind == CommandKind.SelectGuide).EligibleIds[0] }),
                DecisionKind.EffectChoice => Command(view, CommandKind.ChooseEffect, new() { Ids = [WornDiscardStep.Decline] }),
                _ => Command(view, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring })
            };
            var client = view.You == Side.FreePeoples ? alice : bob;
            await Send(client, command);
            var calls = random.Calls;
            await Send(client, command);
            Assert.Equal(calls, random.Calls);
            previous = command;
            previousOwner = view.You;
            previousViews = JsonSerializer.Serialize(new[]
            {
                (await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!,
                (await bob.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json))!
            });
        }
        Assert.Fail("The declaration did not return within the fixture bound.");
    }

    private static GameCommand Command(PlayerView view, CommandKind kind, CommandPayload? payload = null) => new()
    {
        MatchId = view.MatchId, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
        DecisionId = view.Decision!.Id, Kind = kind, Payload = payload ?? new()
    };

    private static async Task Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        Assert.Equal(command.ExpectedRevision + 1, receipt.Revision);
    }
}
