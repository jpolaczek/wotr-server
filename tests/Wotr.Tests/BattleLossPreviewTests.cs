using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleLossPreviewTests
{
    [Theory]
    [InlineData(CasualtyCause.Event, false)]
    [InlineData(CasualtyCause.DestructiveEvent, true)]
    public void EventPreviewKeepsItsCauseAndDoesNotImportBattleCommanderDeaths(CasualtyCause cause, bool killsCharacter)
    {
        var state = State();
        var unit = Put(state, "gondor", UnitKind.Regular, Origin);
        var leader = Put(state, "gondor", UnitKind.Leader, Origin);
        var companion = Character(state, "test.companion", Side.FreePeoples, Origin);
        var target = ArmyTarget.At(state, Side.FreePeoples, Origin)!;
        state.Resolution.Frames.Add(new() { EventLoss = new(target, 3, "test.event-loss", [6, 6, 6], cause) });
        state.Open(DecisionKind.Losses, Side.FreePeoples, [CommandKind.TakeEventLosses], 3);
        var command = Command(state, CommandKind.TakeEventLosses, new() { Losses = [Loss(unit.Id)] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal(3, preview.Losses!.RequiredHits);
        Assert.Equal(1, preview.Losses.AppliedHits);
        Assert.Equal(unit.Id, Assert.Single(preview.Losses.Operations).Figure.Id);
        Assert.Contains(leader.Id, preview.RemovedIds);
        Assert.Equal(killsCharacter, preview.RemovedIds.Contains(companion.Id));
        var final = Execute(state, CommandKind.TakeEventLosses, command.Payload);
        Assert.Equal(killsCharacter ? "casualties" : Origin, final.Characters.Single(c => c.Id == companion.Id).Location);
        Assert.Equal(1, final.Events.Last(e => e.Kind == "EventLosses").Activity!.Facts.Hits);
    }
}
