using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class TurnPersistenceTests
{
    [PostgresFact]
    public async Task ReloadAfterRingPreservesActionOwnerAndAllowsDifferentDieCost()
    {
        await using var source = NpgsqlDataSource.Create(PostgresTests.Connection);
        var database = new Database(source);
        await database.Migrate();
        var state = TurnTests.Actions();
        state.Id = Guid.NewGuid().ToString("N");
        state.Dice[1].Face = DieFace.Event;
        var store = new MatchStore(database, new GameEngine(new TurnCommands()), new SequenceRandom());
        await store.Create(state, "ring-alice", "ring-bob");
        var command = KernelTests.Command(state, CommandKind.UseRing);
        command.Payload = new()
        {
            RingId = state.Rings[0].Id,
            DieId = state.Dice[0].Id,
            Face = DieFace.Muster
        };
        Assert.True((await store.Execute(command, "ring-alice"))!.Accepted);
        await using var restartedSource = NpgsqlDataSource.Create(PostgresTests.Connection);
        var restarted = new MatchStore(new Database(restartedSource), new GameEngine(new TurnCommands()), new SequenceRandom());
        var restored = (await restarted.Read(state.Id, "ring-alice"))!.State;
        Assert.Equal(Side.FreePeoples, restored.Pending!.Owner);
        Assert.Equal(state.Pending!.Id, restored.Pending.Id);
        Assert.True(restored.Player(Side.FreePeoples).RingActionPending);
        Assert.Equal(Side.Shadow, restored.Rings[0].Owner);
        var repeat = await restarted.Execute(command, "ring-alice");
        Assert.True(repeat!.Accepted);
        Assert.Equal(1, repeat.Revision);
        var next = KernelTests.Command(restored, CommandKind.DrawCard);
        next.Payload = new()
        {
            DieId = restored.Dice[1].Id,
            Deck = "character"
        };
        Assert.True((await restarted.Execute(next, "ring-alice"))!.Accepted);
        var after = (await restarted.Read(state.Id, "ring-alice"))!.State;
        Assert.Equal(DieFace.Muster, after.Dice[0].Face);
        Assert.Equal(DieLocation.Available, after.Dice[0].Location);
        Assert.Equal(DieLocation.Spent, after.Dice[1].Location);
        Assert.Equal(2, after.Revision);
        Assert.Equal(Side.Shadow, after.Pending!.Owner);
    }
}
