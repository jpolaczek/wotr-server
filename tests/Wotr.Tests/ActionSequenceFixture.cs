using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

internal static class ActionSequenceFixture
{
    public static MatchState Board(Side side = Side.FreePeoples)
    {
        var state = TurnTests.Actions(actor: side);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        foreach (var die in state.Dice.Where(d => d.Owner == side))
            die.Face = DieFace.Army;
        return state;
    }

    public static UnitState Put(MatchState state, string nation, string region, UnitKind kind = UnitKind.Regular)
    {
        var unit = state.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
        unit.Location = region;
        return unit;
    }

    public static CommandPayload Move(MatchState state, string from, string to, params string[] ids) => new()
    {
        Stepwise = true,
        DieId = state.Resolution.ActionSequence?.DieId ?? state.Dice.First(d => d.Owner == state.Pending!.Owner).Id,
        Moves = [new() { From = from, To = to, UnitIds = [.. ids] }]
    };

    public static MatchState Do(MatchState state, CommandKind kind, CommandPayload? payload = null) =>
        LordsFixture.Do(state, kind, payload);

    public static void Reject(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var result = LordsFixture.Engine().Execute(state, state.Pending!.Owner, BattleFixture.Command(state, kind, payload), random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, random.Calls);
    }
}
