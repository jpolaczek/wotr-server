using System.Net;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class UI10PrivacyTests
{
    [PostgresFact]
    public async Task DrawPreviewDoesNotRevealDeckOrderOrMutateState()
    {
        var original = TurnTests.Actions();
        original.RandomSamples.Clear();
        original.Dice[0].Face = DieFace.Event;
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        using var player = host.Client("alice");
        using var opponent = host.Client("bob");
        var previews = new List<string>();
        var views = new List<string>();
        foreach (var reversed in new[] { false, true })
        {
            var state = StateCodec.Copy(original);
            if (reversed)
                state.Player(Side.FreePeoples).Decks["character"].Reverse();
            var store = await UI10ProtocolFixture.Save(host, state);
            var before = StateCodec.Write((await store.Read(state.Id, "alice"))!.State);
            var command = BattleFixture.Command(state, CommandKind.DrawCard,
                new() { DieId = state.Dice[0].Id, Deck = "character" });
            using var response = await UI10ProtocolFixture.Post(player, state.Id, "preview", UI10ProtocolFixture.Json(command));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var raw = await response.Content.ReadAsStringAsync();
            var preview = JsonNode.Parse(raw)!;
            Assert.True(preview["legal"]!.GetValue<bool>());
            Assert.True(preview["hasUnknownResult"]!.GetValue<bool>());
            foreach (var card in state.Player(Side.FreePeoples).Decks["character"])
                Assert.DoesNotContain(card, raw);
            previews.Add(raw);
            views.Add((await opponent.GetStringAsync($"/api/matches/{state.Id}")).Replace(state.Id, "match", StringComparison.Ordinal));
            Assert.Equal(before, StateCodec.Write((await store.Read(state.Id, "alice"))!.State));
            using var receipt = await player.GetAsync($"/api/matches/{state.Id}/receipts/{command.CommandId}");
            Assert.Equal(HttpStatusCode.NotFound, receipt.StatusCode);
        }
        Assert.Equal(previews[0], previews[1]);
        Assert.Equal(views[0], views[1]);
        Assert.Equal(0, random.Calls);
    }

    [PostgresFact]
    public async Task RealSecretCombatCommitKeepsOpponentViewPreviewAndErrorsIdentical()
    {
        var original = CardPackBattleFixture.BeforeCards("base.fp.character.01", fpAttacks: false);
        var cards = new[] { ShadowStrategyCards03.NewPower, ShadowStrategyCards03.ManyKings };
        foreach (var card in cards)
            EventTests.GiveCard(original, Side.Shadow, card);
        var random = new SequenceRandom();
        await using var host = new TestHost(random);
        using var shadow = host.Client("bob");
        using var free = host.Client("alice");
        var views = new List<string>();
        var previews = new List<string>();
        var errors = new List<string>();
        foreach (var card in cards)
        {
            var state = StateCodec.Copy(original);
            var store = await UI10ProtocolFixture.Save(host, state);
            var commit = BattleFixture.Command(state, CommandKind.CommitCombatCard, new() { CardId = card });
            var receipt = await UI10ProtocolFixture.Response<CommandReceipt>(await UI10ProtocolFixture.Post(
                shadow, state.Id, "commands", UI10ProtocolFixture.Json(commit)));
            Assert.True(receipt.Accepted, receipt.Error);
            var raw = await free.GetStringAsync($"/api/matches/{state.Id}");
            var view = JsonNode.Parse(raw)!;
            Assert.Null(view["battle"]!["attacker"]!["cardId"]);
            Assert.Equal("strategy", view["battle"]!["attacker"]!["cardBack"]!.GetValue<string>());
            foreach (var hidden in cards)
                Assert.DoesNotContain(hidden, raw);
            views.Add(raw.Replace(state.Id, "match", StringComparison.Ordinal));
            var after = (await store.Read(state.Id, "alice"))!.State;
            var valid = BattleFixture.Command(after, CommandKind.CommitCombatCard);
            using (var preview = await UI10ProtocolFixture.Post(free, state.Id, "preview", UI10ProtocolFixture.Json(valid)))
            {
                Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
                var body = await preview.Content.ReadAsStringAsync();
                Assert.True(JsonNode.Parse(body)!["legal"]!.GetValue<bool>());
                previews.Add(body);
            }
            valid.Payload.CardId = cards[0];
            using (var invalid = await UI10ProtocolFixture.Post(free, state.Id, "preview", UI10ProtocolFixture.Json(valid)))
            {
                Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
                var body = await invalid.Content.ReadAsStringAsync();
                Assert.Equal("IllegalChoice", JsonNode.Parse(body)!["error"]!.GetValue<string>());
                Assert.DoesNotContain(cards[0], body);
                errors.Add(body);
            }
            await UI10ProtocolFixture.RejectedWithoutMutation(host, free, after, UI10ProtocolFixture.Json(valid));
        }
        Assert.Equal(views[0], views[1]);
        Assert.Equal(previews[0], previews[1]);
        Assert.Equal(errors[0], errors[1]);
        Assert.Equal(0, random.Calls);
    }
}
