using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-06: the Mordor Track scenarios stand where they promise, and the track is reached by the real entry.
public class MordorTrackScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);

    [Fact]
    public void TheFellowshipWaitsAtMinasMorgulInTheFellowshipPhase()
    {
        var state = TestScenarioCatalog.Find("fp-mordor-enter")!.Create("mordor-enter");

        Assert.Equal(GamePhase.Fellowship, state.Phase);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending?.Kind);
        Assert.Equal("minas-morgul", state.Fellowship.RegionId);
        Assert.Null(state.Fellowship.MordorStep);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.True(MordorRules.CanEnter(state));
    }

    [Fact]
    public void EnteringFromTheScenarioPutsTheFellowshipOnStepZero()
    {
        var state = TestScenarioCatalog.Find("fp-mordor-enter")!.Create("mordor-enter");
        var result = Engine.Execute(state, Side.FreePeoples, new GameCommand
        {
            MatchId = state.Id,
            CommandId = "enter",
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = CommandKind.EnterMordor
        }, new SeededRandom(3));

        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        Assert.Equal(MordorRules.Track, result.State.Fellowship.RegionId);
        Assert.Equal(0, result.State.Fellowship.MordorStep);
    }

    [Theory]
    [InlineData("fp-mordor-track", 0, 3, 3)]
    [InlineData("fp-mordor-crack", 4, 6, 2)]
    public void TheTrackScenariosStartTheActionsOnTheirStep(string id, int step, int corruption, int characterDice)
    {
        var state = TestScenarioCatalog.Find(id)!.Create(id);

        Assert.Equal(GamePhase.ActionResolution, state.Phase);
        Assert.Equal(Side.FreePeoples, state.Pending?.Owner);
        Assert.Equal(MordorRules.Track, state.Fellowship.RegionId);
        Assert.Equal(step, state.Fellowship.MordorStep);
        Assert.Equal(corruption, state.Fellowship.Corruption);
        Assert.Contains(state.Events, e => e.Kind == "MordorEntered");
        Assert.Equal(characterDice, state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character));
    }
}
