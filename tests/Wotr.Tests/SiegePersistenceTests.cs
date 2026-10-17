using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class SiegePersistenceTests
{
    [PostgresFact]
    public async Task RestartAtExtensionKeepsEliteChoiceThenRetryNeverPaysTwice()
    {
        var state = SiegeTests.SetupSiege();
        state.Id = Guid.NewGuid().ToString("N");
        var elite = Put(state, "sauron", UnitKind.Elite, Origin);
        var other = Put(state, "isengard", UnitKind.Elite, Origin);
        var paidDie = state.Dice.First(d => d.Owner == Side.Shadow).Id;
        await using (var first = new TestHost(new SequenceRandom(4, 4, 0)))
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = first.Client("alice");
            using var bob = first.Client("bob");
            await Send(bob, Command(state, CommandKind.AttackArmy, AttackPayload(state, [elite.Id, other.Id], Origin, Origin)));
            await Send(bob, Choice(await Read(bob, state.Id), CommandKind.CommitCombatCard));
            await Send(alice, Choice(await Read(alice, state.Id), CommandKind.CommitCombatCard));
            var view = await Read(bob, state.Id);
            Assert.Equal(DecisionKind.SiegeContinuation, view.Decision!.Kind);
            Assert.Equal(BattleMode.Siege, view.Battle!.Mode);
            Assert.Equal(Side.FreePeoples, view.Control[Origin]);
        }

        GameCommand extend;
        var noRandom = new SequenceRandom();
        await using (var second = new TestHost(noRandom))
        {
            using var bob = second.Client("bob");
            using var alice = second.Client("alice");
            var view = await Read(bob, state.Id);
            Assert.Equal(DecisionKind.SiegeContinuation, view.Decision!.Kind);
            Assert.Equal(DieLocation.Resolving, view.Dice.Single(d => d.Id == paidDie).Location);
            var option = view.Options.Single(o => o.Kind == CommandKind.ExtendSiege);
            Assert.Contains(elite.Id, option.EligibleIds);
            Assert.Contains(other.Id, option.EligibleIds);
            Assert.Null((await Read(alice, state.Id)).Decision);
            extend = Choice(view, CommandKind.ExtendSiege, new() { Ids = [other.Id] });
            Assert.Equal(view.Revision + 1, (await Send(bob, extend)).Revision);
            Assert.Equal(0, noRandom.Calls);
        }

        var secondRound = new SequenceRandom(4, 4, 0);
        await using var third = new TestHost(secondRound);
        using var returned = third.Client("bob");
        using var defender = third.Client("alice");
        Assert.Equal(extend.ExpectedRevision + 1, (await Send(returned, extend)).Revision);
        Assert.Equal(0, secondRound.Calls);
        var resumed = await Read(returned, state.Id);
        Assert.Equal(2, resumed.Battle!.Round);
        Assert.Equal("reserve", resumed.Units.Single(u => u.Id == other.Id).Location);
        Assert.Equal(Origin, resumed.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Single(resumed.Units, u => u.Location == Origin && u.Nation == "isengard" && u.Kind == UnitKind.Regular);
        await Send(returned, Choice(resumed, CommandKind.CommitCombatCard));
        await Send(defender, Choice(await Read(defender, state.Id), CommandKind.CommitCombatCard));
        var stop = Choice(await Read(returned, state.Id), CommandKind.StopBattle);
        await Send(returned, stop);
        await Send(returned, stop);
        var persisted = (await third.Services.GetRequiredService<MatchStore>().Read(state.Id, "bob"))!.State;
        Assert.Empty(persisted.Resolution.Frames);
        Assert.Equal(6, persisted.RandomSamples.Count);
        Assert.Single(persisted.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(Side.FreePeoples, persisted.Control[Origin]);
        Assert.True(BoardRules.Besieged(persisted, Origin, Side.FreePeoples));
    }

    private static async Task<PlayerView> Read(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;

    private static GameCommand Choice(PlayerView view, CommandKind kind, CommandPayload? payload = null) => new()
    {
        MatchId = view.MatchId,
        ExpectedRevision = view.Revision,
        DecisionId = view.Decision!.Id,
        CommandId = Guid.NewGuid().ToString("N"),
        Kind = kind,
        Payload = payload ?? new()
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
