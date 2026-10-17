using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattlePersistenceTests
{
    [PostgresFact]
    public async Task RestartAtRetreatRestoresTargetsAndRetryKeepsAdvanceAndOnePaidDie()
    {
        var state = State();
        state.Id = Guid.NewGuid().ToString("N");
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [attacker.Id]), 1, 1);
        state = Execute(state, CommandKind.ContinueBattle);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        string decision;
        await using (var first = new TestHost(new SequenceRandom()))
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var bob = first.Client("bob");
            decision = (await Read(bob, state.Id)).Decision!.Id;
        }
        GameCommand retreat;
        var rng = new SequenceRandom();
        await using (var second = new TestHost(rng))
        {
            using var bob = second.Client("bob");
            var restored = await Read(bob, state.Id);
            Assert.Equal(decision, restored.Decision!.Id);
            Assert.Contains("osgiliath", restored.Options.Single(o => o.Kind == CommandKind.RetreatBattle).Targets);
            retreat = Choice(restored, CommandKind.RetreatBattle, new() { RegionId = "osgiliath" });
            await Send(bob, retreat);
        }
        await using var third = new TestHost(rng);
        using var returned = third.Client("bob");
        using var alice = third.Client("alice");
        Assert.Equal(retreat.ExpectedRevision + 1, (await Send(returned, retreat)).Revision);
        var advance = await Read(alice, state.Id);
        Assert.Equal(DecisionKind.BattleAdvance, advance.Decision!.Kind);
        Assert.Equal("osgiliath", advance.Units.Single(u => u.Id == defender.Id).Location);
        await Send(alice, Choice(advance, CommandKind.AdvanceBattle, new() { Ids = [attacker.Id] }));
        var final = await Read(alice, state.Id);
        Assert.Null(final.Battle);
        Assert.Equal(Target, final.Units.Single(u => u.Id == attacker.Id).Location);
        Assert.Single(final.Dice, d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent);
        Assert.Equal(0, rng.Calls);
    }

    [PostgresFact]
    public async Task RestartAfterSecretCommitAndAttackerLossKeepsPrivacyResultsAndOneCost()
    {
        var catalog = Catalog([new(3, "test.fp")], [new(3, "test.sh")], new("test.fp", []), new("test.sh", []));
        var state = State();
        state.Id = Guid.NewGuid().ToString("N");
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Elite, Target);
        state.Player(Side.FreePeoples).Hand.Add(FpCard);
        state.Player(Side.Shadow).Hand.Add(ShCard);
        GameCommand committed;
        await using (var first = new TestHost(new SequenceRandom(), catalog))
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            var attack = Command(state, CommandKind.AttackArmy, AttackPayload(state, [attacker.Id]));
            Assert.Equal(1, (await Send(alice, attack)).Revision);
            var view = await Read(alice, state.Id);
            committed = Choice(view, CommandKind.CommitCombatCard, new() { CardId = FpCard });
            Assert.Equal(2, (await Send(alice, committed)).Revision);
        }

        GameCommand attackerLoss;
        var rolls = new SequenceRandom(5, 5);
        await using (var second = new TestHost(rolls, catalog))
        {
            using var alice = second.Client("alice");
            using var bob = second.Client("bob");
            Assert.Equal(2, (await Send(alice, committed)).Revision);
            Assert.Equal(0, rolls.Calls);
            Assert.DoesNotContain(FpCard, await bob.GetStringAsync($"/api/matches/{state.Id}"));
            var defenderView = await Read(bob, state.Id);
            Assert.Null(defenderView.Battle!.Attacker.CardId);
            Assert.Equal("strategy", defenderView.Battle.Attacker.CardBack);
            Assert.Null((await Read(alice, state.Id)).Decision);
            var card = Choice(defenderView, CommandKind.CommitCombatCard, new() { CardId = ShCard });
            Assert.Equal(3, (await Send(bob, card)).Revision);
            Assert.Equal(3, (await Send(bob, card)).Revision);
            Assert.Equal(2, rolls.Calls);
            var losses = await Read(alice, state.Id);
            Assert.Equal(DecisionKind.Losses, losses.Decision!.Kind);
            Assert.Equal(ShCard, losses.Battle!.Defender.CardId);
            Assert.Equal(6, losses.Battle.Attacker.Dice[0].Result);
            attackerLoss = Choice(losses, CommandKind.TakeBattleLosses, new() { Losses = [Loss(attacker.Id)] });
            Assert.Equal(4, (await Send(alice, attackerLoss)).Revision);
        }

        var noRandom = new SequenceRandom();
        await using var third = new TestHost(noRandom, catalog);
        using var returnedAlice = third.Client("alice");
        using var returnedBob = third.Client("bob");
        Assert.Equal(4, (await Send(returnedAlice, attackerLoss)).Revision);
        var restored = await Read(returnedBob, state.Id);
        Assert.Equal(DecisionKind.Losses, restored.Decision!.Kind);
        Assert.Equal(1, restored.Decision.RequiredCount);
        Assert.Equal(DieLocation.Resolving, restored.Dice[0].Location);
        Assert.Equal(0, restored.Battle!.Attacker.Strength);
        Assert.Equal(6, restored.Battle.Attacker.Dice[0].Result);
        var downgrade = Choice(restored, CommandKind.TakeBattleLosses, new() { Losses = [Loss(defender.Id, BattleLossKind.Downgrade)] });
        Assert.Equal(5, (await Send(returnedBob, downgrade)).Revision);
        Assert.Equal(5, (await Send(returnedBob, downgrade)).Revision);
        Assert.Equal(0, noRandom.Calls);
        var final = (await third.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Empty(final.Resolution.Frames);
        Assert.Equal(2, final.RandomSamples.Count);
        Assert.Equal(DieLocation.Spent, final.Dice[0].Location);
        Assert.Contains(FpCard, final.Player(Side.FreePeoples).Discard);
        Assert.Contains(ShCard, final.Player(Side.Shadow).Discard);
        Assert.Equal("reserve", final.Units.Single(u => u.Id == defender.Id).Location);
        Assert.Single(final.Units, u => u.Location == Target && u.Kind == UnitKind.Regular);
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
