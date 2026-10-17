using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

// QUAL-04 (review R03): BASE 2.4.1, printed p. 21 — after changing a die with an Elven Ring the player "can also
// skip or pass the action, if eligible to do so". Expected results follow that rule and B03: a pass needs fewer
// unused action dice than the opponent, whether or not an action is awaited after the Ring. The Ring stays used.
public sealed class RingPassTests
{
    private static readonly GameEngine Engine = new(new TurnCommands(), new DeclineReactionHandler());
    private static readonly Projection Projection = new(new TurnOptions(), new CoreOptions());

    [Theory]
    [InlineData(2, 3, true)]
    [InlineData(3, 3, false)]
    [InlineData(4, 3, false)]
    public void FreePeoplesPassAfterTheRingOnlyWithFewerUnusedDice(int free, int shadow, bool legal)
    {
        var state = UseRing(TurnTests.Actions(free, shadow), Side.FreePeoples, DieFace.Muster);
        Assert.True(state.Player(Side.FreePeoples).RingActionPending);
        Assert.Equal(legal, PassOffered(state, Side.FreePeoples));
        var result = Engine.Execute(state, Side.FreePeoples, Command(state, CommandKind.Pass), new SequenceRandom());
        Assert.Equal(legal, result.Receipt.Accepted);
        if (!legal)
        {
            Assert.Equal("IllegalChoice", result.Receipt.Error);
            return;
        }
        var after = result.State;
        Assert.False(after.Player(Side.FreePeoples).RingActionPending);
        Assert.True(after.Player(Side.FreePeoples).RingUsedThisTurn, "The Ring stays used this turn.");
        Assert.Equal(Side.Shadow, after.Rings[0].Owner);
        Assert.Equal(free, TurnFlow.Available(after, Side.FreePeoples));
        Assert.Equal(Side.Shadow, after.Pending!.Owner);
    }

    [Fact]
    public void ShadowsEyeFromTheRingChangesTheCountThatDecidesThePass()
    {
        var state = TurnTests.Actions(3, 3, Side.Shadow);
        state.Rings[0].Owner = Side.Shadow;
        Assert.False(PassOffered(state, Side.Shadow), "Three dice against three: no pass.");
        state = UseRing(state, Side.Shadow, DieFace.Eye);
        Assert.Null(state.Rings[0].Owner);
        Assert.Equal(2, TurnFlow.Available(state, Side.Shadow));
        Assert.True(PassOffered(state, Side.Shadow));
        var after = Execute(state, CommandKind.Pass);
        Assert.False(after.Player(Side.Shadow).RingActionPending);
        Assert.Equal(Side.FreePeoples, after.Pending!.Owner);
        Assert.Single(after.Dice, d => d.Owner == Side.Shadow && d.HuntOrigin == "elven-ring" && d.Location == DieLocation.Hunt);
    }

    [PostgresFact]
    public async Task APassAfterTheRingSurvivesRestartsBeforeAndAfterIt()
    {
        var state = UseRing(TurnTests.Actions(2, 3), Side.FreePeoples, DieFace.Muster);
        await using (var host = new TestHost())
            await UI10ProtocolFixture.Save(host, state);
        await using (var host = new TestHost())
        {
            using var alice = host.Client("alice");
            var view = await alice.GetFromJsonAsync<PlayerView>($"/api/matches/{state.Id}", BoardCatalog.Json);
            Assert.True(view!.Options.Single(o => o.Kind == CommandKind.Pass).Enabled);
            var command = UI10ProtocolFixture.Json(Command(state, CommandKind.Pass));
            var receipt = await UI10ProtocolFixture.Response<CommandReceipt>(
                await UI10ProtocolFixture.Post(alice, state.Id, "commands", command));
            Assert.True(receipt.Accepted, receipt.Error);
        }
        await using (var host = new TestHost())
        {
            var saved = (await host.Services.GetRequiredService<MatchStore>().Read(state.Id, "alice"))!.State;
            Assert.False(saved.Player(Side.FreePeoples).RingActionPending);
            Assert.True(saved.Player(Side.FreePeoples).RingUsedThisTurn);
            Assert.Equal(Side.Shadow, saved.Pending!.Owner);
            Assert.Equal(2, TurnFlow.Available(saved, Side.FreePeoples));
        }
    }

    private static MatchState UseRing(MatchState state, Side actor, DieFace face)
    {
        var die = state.Dice.First(d => d.Owner == actor && d.Location == DieLocation.Available && d.Face != face);
        var ring = state.Rings.First(r => r.Owner == actor);
        return Execute(state, CommandKind.UseRing, new() { RingId = ring.Id, DieId = die.Id, Face = face });
    }

    private static bool PassOffered(MatchState state, Side actor)
    {
        var offered = Projection.For(state, actor).Options.Single(o => o.Kind == CommandKind.Pass).Enabled;
        Assert.Equal(offered, Engine.Validate(state, actor, Command(state, CommandKind.Pass)) is null);
        return offered;
    }

    private static MatchState Execute(MatchState state, CommandKind kind, CommandPayload? payload = null)
    {
        var command = Command(state, kind);
        command.Payload = payload ?? new();
        var result = Engine.Execute(state, state.Pending!.Owner, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    private static GameCommand Command(MatchState state, CommandKind kind)
    {
        var command = KernelTests.Command(state, kind);
        command.Payload = new();
        return command;
    }
}
