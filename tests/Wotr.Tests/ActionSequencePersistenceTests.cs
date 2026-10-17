using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

public class ActionSequencePersistenceTests
{
    [PostgresFact]
    public Task ArmyStepsSurviveEveryHostRestart() => RoundTrip(CommandKind.MoveArmies);
    [PostgresFact]
    public Task RecruitmentStepsSurviveEveryHostRestart() => RoundTrip(CommandKind.Recruit);
    [PostgresFact]
    public Task CompanionGroupsSurviveEveryHostRestart() => RoundTrip(CommandKind.MoveCompanions);
    [PostgresFact]
    public Task ShadowGroupsSurviveEveryHostRestart() => RoundTrip(CommandKind.MoveShadowCharacters);
    [PostgresFact]
    public Task EarlyFinishSurvivesEveryHostRestart() => RoundTrip(CommandKind.MoveArmies, finish: true);

    private static async Task RoundTrip(CommandKind kind, bool finish = false)
    {
        var (state, first, second) = Scenario(kind, finish);
        var owner = state.Pending!.Owner == Side.FreePeoples ? "alice" : "bob";
        var opponent = owner == "alice" ? "bob" : "alice";
        var secret = state.Player(state.Pending.Owner).Decks["character"][0];
        state.Player(state.Pending.Owner).Hand.Add(secret);
        await using (var host = new TestHost())
        {
            using var client = host.Client(owner);
            using var other = host.Client(opponent);
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            Assert.Equal("NotAuthorized", (await Send(other, first)).Error);
            Assert.True((await Send(client, first)).Accepted);
        }
        await using (var host = new TestHost())
        {
            using var client = host.Client(owner);
            using var other = host.Client(opponent);
            var restored = await View(client, state.Id);
            Assert.Equal(1, restored.Revision);
            Assert.Equal(DecisionKind.ActionSequence, restored.Decision!.Kind);
            Assert.Equal(1, restored.ActionSequence!.CompletedSteps);
            Assert.Equal(DieLocation.Resolving, restored.Dice.Single(d => d.Id == first.Payload.DieId).Location);
            Assert.Contains(restored.Options, o => o.Kind == CommandKind.FinishActionSequence);
            Assert.Equal(1, (await Send(client, first)).Revision);
            var recovered = await client.GetFromJsonAsync<CommandReceipt>($"/api/matches/{state.Id}/receipts/{first.CommandId}", BoardCatalog.Json);
            Assert.True(recovered!.Accepted);
            Assert.Equal(1, recovered.Revision);
            var otherView = await View(other, state.Id);
            Assert.Null(otherView.Decision);
            Assert.Empty(otherView.Options);
            Assert.NotNull(otherView.ActionSequence);
            var raw = await other.GetStringAsync($"/api/matches/{state.Id}");
            Assert.DoesNotContain(secret, raw);
            Assert.DoesNotContain("initialFigures", raw);
            var stale = BattleFixture.Command(state, second.Kind, second.Payload);
            Assert.Equal("StaleRevision", (await Send(client, stale)).Error);
            var changed = BattleFixture.Command(state, first.Kind, first.Payload);
            changed.CommandId = first.CommandId;
            changed.Payload = new() { Stepwise = true, DieId = "different" };
            Assert.Equal("CommandIdConflict", (await Send(client, changed)).Error);
            using var previewResponse = await client.PostAsJsonAsync($"/api/matches/{state.Id}/preview", second, BoardCatalog.Json);
            var preview = await previewResponse.Content.ReadFromJsonAsync<ActionPreview>(BoardCatalog.Json);
            Assert.True(preview!.Legal);
            Assert.Empty(preview.Costs);
            Assert.True((await Send(client, second)).Accepted);
        }
        await using (var host = new TestHost())
        {
            using var client = host.Client(owner);
            var final = await View(client, state.Id);
            Assert.Equal(2, final.Revision);
            Assert.Null(final.ActionSequence);
            Assert.Equal(TurnFlow.Other(state.Pending!.Owner), final.DecisionOwner);
            Assert.Equal(DieLocation.Spent, final.Dice.Single(d => d.Id == first.Payload.DieId).Location);
            Assert.Equal(1, (await Send(client, first)).Revision);
            Assert.Equal(2, (await Send(client, second)).Revision);
            Assert.Equal(2, (await View(client, state.Id)).Revision);
            var saved = await host.Services.GetRequiredService<MatchStore>().Read(state.Id, owner, CancellationToken.None);
            Assert.Equal(state.RandomSamples.Count, saved!.State.RandomSamples.Count);
            Assert.Empty(saved.State.Resolution.PaidDieIds);
        }
    }

    private static (MatchState State, GameCommand First, GameCommand Second) Scenario(CommandKind kind, bool finish)
    {
        var state = Board(kind == CommandKind.MoveShadowCharacters ? Side.Shadow : Side.FreePeoples);
        state.Id = Guid.NewGuid().ToString("N");
        CommandPayload first;
        Func<MatchState, CommandPayload> next;
        if (kind == CommandKind.MoveArmies)
        {
            var army = Put(state, "gondor", "minas-tirith").Id;
            var second = Put(state, "north", "dale").Id;
            first = Move(state, "minas-tirith", "osgiliath", army);
            next = s => Move(s, "dale", "erebor", second);
        }
        else if (kind == CommandKind.Recruit)
        {
            state.Dice[0].Face = DieFace.Muster;
            first = RecruitSequenceTests.Placement(state, UnitKind.Regular, "minas-tirith");
            next = s => RecruitSequenceTests.Placement(s, UnitKind.Regular, "pelargir");
        }
        else
        {
            state.Dice.First(d => d.Owner == state.Pending!.Owner).Face = DieFace.Character;
            var one = kind == CommandKind.MoveCompanions
                ? CharacterFixture.Put(state, "base.character.gimli", "rivendell").Id
                : Put(state, "sauron", "barad-dur", UnitKind.Nazgul).Id;
            var two = kind == CommandKind.MoveCompanions
                ? CharacterFixture.Put(state, "base.character.legolas", "rivendell").Id
                : Put(state, "sauron", "barad-dur", UnitKind.Nazgul).Id;
            var origin = kind == CommandKind.MoveCompanions ? "rivendell" : "barad-dur";
            first = TravelSequenceTests.Travel(state, one, origin, kind == CommandKind.MoveCompanions ? "trollshaws" : "hollin");
            next = s => TravelSequenceTests.Travel(s, two, origin, kind == CommandKind.MoveCompanions ? "fords-of-bruinen" : "moria");
        }
        var command = BattleFixture.Command(state, kind, first);
        var after = Do(state, kind, first);
        return (state, command, BattleFixture.Command(after, finish ? CommandKind.FinishActionSequence : kind, finish ? new() : next(after)));
    }

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
    }

    private static async Task<PlayerView> View(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;
}
