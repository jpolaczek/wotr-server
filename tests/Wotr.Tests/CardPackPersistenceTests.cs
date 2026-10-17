using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards01;

namespace Wotr.Tests;

public class CardPackPersistenceTests
{
    [PostgresFact]
    public async Task HuntReplacementAndProtectionRestoreEachWindowAndRetryNeverDrawsAgain()
    {
        var state = HuntProtectionCardTests.ShelobReplacement();
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal("hunt.table", state.Pending!.Window);
        Assert.Equal(HuntTests.Three, HuntRuntime.Active(state)!.TileId);
        Assert.Equal("return-after-action", state.HuntTiles.Single(t => t.Id.Contains("special-sh.die")).Location);
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction }, new SequenceRandom());
        Assert.Equal(2, HuntRuntime.Active(state)!.Damage);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(2, state.Player(Side.FreePeoples).Discard.Count);
        Assert.Equal(2, state.RandomSamples.Count);
    }

    [PostgresFact]
    public async Task MightySelectionRestoresBothGroupsAndCommitsLeadershipAndHitOnlyOnce()
    {
        var state = CompanionCombatCardTests.MightyChoice();
        var die = BattleRuntime.Active(state)!.Attacker.Round.Dice[0].Id;
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider, die] }, new SequenceRandom());
        var round = BattleRuntime.Active(state)!.Attacker.Round;
        Assert.Equal(2, round.ForfeitedLeadership[CharacterRules.Strider]);
        Assert.Single(round.Dice, d => d.ConvertedHit);
        Assert.Single(state.Events, e => e.Kind == "CombatMissConverted");
    }

    [PostgresFact]
    public async Task BladeSelectionRestoresMinionsAndCannotSpendTheSameHitTwice()
    {
        var state = await RestartDecision(CompanionCombatCardTests.BladeChoice(), CommandKind.ChooseEffect,
            new() { Ids = [CharacterRules.Mouth] }, new SequenceRandom());
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Mouth).Location);
        Assert.Equal(1, BattleRuntime.Active(state)!.Attacker.Round.SpentHits);
        Assert.Single(state.Events, e => e.Kind == "MinionEliminated");
    }

    internal static async Task<MatchState> RestartDecision(MatchState state, CommandKind kind, CommandPayload payload, SequenceRandom random)
    {
        state.Id = Guid.NewGuid().ToString("N");
        var chooser = state.Pending!.Owner == Side.FreePeoples ? "alice" : "bob";
        var observer = chooser == "alice" ? "bob" : "alice";
        PlayerView before;
        PlayerView otherBefore;
        await using (var first = new TestHost())
        {
            await first.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var player = first.Client(chooser);
            using var other = first.Client(observer);
            before = await Read(player, state.Id);
            otherBefore = await Read(other, state.Id);
        }
        GameCommand command;
        MatchState after;
        await using (var second = new TestHost(random))
        {
            using var player = second.Client(chooser);
            using var other = second.Client(observer);
            var view = await Read(player, state.Id);
            Assert.Equal(JsonSerializer.Serialize(before, BoardCatalog.Json), JsonSerializer.Serialize(view, BoardCatalog.Json));
            var otherView = await Read(other, state.Id);
            Assert.Null(otherView.Decision);
            Assert.Equal(JsonSerializer.Serialize(otherBefore, BoardCatalog.Json), JsonSerializer.Serialize(otherView, BoardCatalog.Json));
            command = new()
            {
                MatchId = state.Id,
                ExpectedRevision = view.Revision,
                DecisionId = view.Decision!.Id,
                CommandId = Guid.NewGuid().ToString("N"),
                Kind = kind,
                Payload = payload
            };
            Assert.Equal(view.Revision + 1, (await Send(player, command)).Revision);
            var samples = random.Calls;
            Assert.Equal(view.Revision + 1, (await Send(player, command)).Revision);
            Assert.Equal(samples, random.Calls);
            after = (await second.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        }
        var noRandom = new SequenceRandom();
        await using var third = new TestHost(noRandom);
        using var returned = third.Client(chooser);
        Assert.Equal(after.Revision, (await Send(returned, command)).Revision);
        var saved = (await third.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
        Assert.Equal(StateCodec.Write(after), StateCodec.Write(saved));
        Assert.Equal(0, noRandom.Calls);
        return saved;
    }

    private static async Task<PlayerView> Read(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<PlayerView>($"/api/matches/{id}", BoardCatalog.Json))!;

    private static async Task<CommandReceipt> Send(HttpClient client, GameCommand command)
    {
        using var response = await client.PostAsJsonAsync($"/api/matches/{command.MatchId}/commands", command, BoardCatalog.Json);
        response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json))!;
        Assert.True(receipt.Accepted, receipt.Error);
        return receipt;
    }
}
