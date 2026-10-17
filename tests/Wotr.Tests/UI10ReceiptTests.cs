using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class UI10ReceiptTests
{
    [PostgresFact]
    public async Task TwoHttpDevicesRaceRealEventAndRecoverOneCommittedRollAfterRestart()
    {
        var state = Shadow01Fixture.BeforeEvent(ShadowCharacterCards02.Candles);
        var payload = Shadow01Fixture.EventCost(state, ShadowCharacterCards02.Candles);
        var random = new SequenceRandom(2, 3, 5);
        GameCommand winner;
        CommandReceipt winningReceipt;
        string committed;
        await using (var first = new TestHost(random))
        {
            using var deviceOne = first.Client("bob");
            using var deviceTwo = first.Client("bob");
            var store = await UI10ProtocolFixture.Save(first, state);
            var requests = new[]
            {
                BattleFixture.Command(state, CommandKind.PlayEvent, payload),
                BattleFixture.Command(state, CommandKind.PlayEvent, payload)
            };
            var responses = await Task.WhenAll(
                UI10ProtocolFixture.Post(deviceOne, state.Id, "commands", UI10ProtocolFixture.Json(requests[0])),
                UI10ProtocolFixture.Post(deviceTwo, state.Id, "commands", UI10ProtocolFixture.Json(requests[1])));
            var receipts = await Task.WhenAll(responses.Select(UI10ProtocolFixture.Response<CommandReceipt>));
            winningReceipt = Assert.Single(receipts, r => r.Accepted);
            Assert.Equal("StaleRevision", Assert.Single(receipts, r => !r.Accepted).Error);
            winner = requests.Single(r => r.CommandId == winningReceipt.CommandId);
            Assert.Equal(state.Revision + 1, winningReceipt.Revision);
            Assert.Equal(3, random.Calls);
            var after = (await store.Read(state.Id, "bob"))!.State;
            Assert.Equal(3, after.RandomSamples.Count);
            Assert.Equal(state.Fellowship.Corruption + 2, after.Fellowship.Corruption);
            Assert.Equal(DieLocation.Spent, after.Dice.Single(d => d.Id == payload.DieId).Location);
            Assert.Single(after.Player(Side.Shadow).Discard, c => c == ShadowCharacterCards02.Candles);
            committed = StateCodec.Write(after);
        }

        var noRandom = new SequenceRandom();
        await using var restarted = new TestHost(noRandom);
        using var returnedDevice = restarted.Client("bob");
        using var opponent = restarted.Client("alice");
        var path = $"/api/matches/{state.Id}/receipts/{winner.CommandId}";
        var recovered = await UI10ProtocolFixture.Response<CommandReceipt>(await returnedDevice.GetAsync(path));
        var repeated = await UI10ProtocolFixture.Response<CommandReceipt>(await UI10ProtocolFixture.Post(
            returnedDevice, state.Id, "commands", UI10ProtocolFixture.Json(winner)));
        Assert.Equal(JsonSerializer.Serialize(winningReceipt), JsonSerializer.Serialize(recovered));
        Assert.Equal(JsonSerializer.Serialize(winningReceipt), JsonSerializer.Serialize(repeated));
        using (var privateReceipt = await opponent.GetAsync(path))
            Assert.Equal(HttpStatusCode.NotFound, privateReceipt.StatusCode);
        winner.Payload.DieId = "private-untrusted-marker";
        var conflict = await UI10ProtocolFixture.Response<CommandReceipt>(await UI10ProtocolFixture.Post(
            returnedDevice, state.Id, "commands", UI10ProtocolFixture.Json(winner)));
        Assert.Equal("CommandIdConflict", conflict.Error);
        Assert.False(conflict.Accepted);
        Assert.Equal(0, noRandom.Calls);
        var restored = await restarted.Services.GetRequiredService<MatchStore>().Read(state.Id, "bob");
        Assert.Equal(committed, StateCodec.Write(restored!.State));
    }
}
