using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

internal static class ActionLogFixture
{
    public static (MatchState State, GameCommand First, string SecondUnit) Army(DieFace face = DieFace.ArmyMuster)
    {
        var state = ActionSequenceFixture.Board(Side.Shadow);
        state.Id = Guid.NewGuid().ToString("N");
        state.Dice.First(d => d.Owner == Side.Shadow).Face = face;
        var units = Enumerable.Range(0, 2).Select(_ => ActionSequenceFixture.Put(state, "sauron", "morannon").Id)
            .Concat(Enumerable.Range(0, 3).Select(_ => ActionSequenceFixture.Put(state, "sauron", "morannon", UnitKind.Elite).Id)).ToArray();
        var other = ActionSequenceFixture.Put(state, "isengard", "orthanc").Id;
        var first = BattleFixture.Command(state, CommandKind.MoveArmies,
            ActionSequenceFixture.Move(state, "morannon", "dagorlad", units));
        return (state, first, other);
    }

    public static MatchState Execute(MatchState state, GameCommand command)
    {
        var random = new SequenceRandom();
        var result = LordsFixture.Engine().Execute(state, state.Pending!.Owner, command, random);
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        Assert.Equal(0, random.Calls);
        return result.State;
    }

    public static List<PositionedEvent> Records(MatchState state, Side viewer)
    {
        var order = new Dictionary<long, int>();
        return state.Events.Select((item, i) => new { item, position = i + 1 })
            .Where(e => Visibility.CanRead(viewer, e.item.OnlyFor)).Select(e =>
            {
                order[e.item.Revision] = order.GetValueOrDefault(e.item.Revision) + 1;
                return new PositionedEvent(EventIdentity.For(state.Id, e.position, e.item), e.position, order[e.item.Revision], e.item);
            }).ToList();
    }

    public static ActionLogView Action(MatchState state, string id, Side viewer = Side.FreePeoples, ActionLogFilter filter = ActionLogFilter.All) =>
        ActionLogProjection.Action(state.Id, Records(state, viewer).Where(r => r.ActionId == id), viewer, filter)!;

    public static void AssertArmy(ActionMovement move)
    {
        Assert.Equal("morannon", move.From);
        Assert.Equal("dagorlad", move.To);
        Assert.Equal(new[] { "morannon", "dagorlad" }, move.Route);
        Assert.Equal(2, move.Participants.Count(p => p.UnitKind == UnitKind.Regular));
        Assert.Equal(3, move.Participants.Count(p => p.UnitKind == UnitKind.Elite));
        Assert.All(move.Participants, p => { Assert.Equal("sauron", p.NationId); Assert.Equal(Side.Shadow, p.Side); });
    }
}
