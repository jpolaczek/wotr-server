using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-07 (SIGNALS_UX TE01): each Game end scenario starts where it promises, and its one command ends the game with
// the promised winner and reason through the engine and projection the server uses, whatever the server's dice.
public class GameEndScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData("fp-ring-destroyed", Side.FreePeoples, VictoryCondition.RingDestroyed)]
    [InlineData("sh-ring-corrupted", Side.Shadow, VictoryCondition.RingCorrupted)]
    [InlineData("sh-military", Side.Shadow, VictoryCondition.ShadowMilitary)]
    [InlineData("fp-military", Side.FreePeoples, VictoryCondition.FreePeoplesMilitary)]
    public void OneCommandEndsTheGameWithTheWinnerAndReason(string id, Side winner, VictoryCondition victory)
    {
        foreach (var seed in Enumerable.Range(1, 8))
        {
            var scenario = TestScenarioCatalog.Find(id)!;
            var state = scenario.Create(id);
            Assert.Equal("Game end", scenario.Group);
            Assert.Null(state.Winner);
            Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
            Assert.Equal(DecisionKind.Action, state.Pending?.Kind);
            var random = new SeededRandom(seed);
            var end = Play(state, scenario.StartingSide, id, random);
            Assert.Equal(winner, end.Winner);
            Assert.Equal(victory, end.Victory);
            Assert.Null(end.Pending);
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                var view = Views.For(end, side);
                Assert.Equal(winner, view.Winner);
                Assert.Equal(victory, view.Victory);
            }
        }
    }

    [Fact]
    public void TheRingScenariosStandOnTheTrackWithACertainHuntPool()
    {
        var destroyed = TestScenarioCatalog.Find("fp-ring-destroyed")!.Create("destroyed");
        Assert.Equal(4, destroyed.Fellowship.MordorStep);
        Assert.Equal(6, destroyed.Fellowship.Corruption);
        Assert.All(Pool(destroyed), tile => Assert.Equal("0", tile.Value));
        var corrupted = TestScenarioCatalog.Find("sh-ring-corrupted")!.Create("corrupted");
        Assert.Equal(1, corrupted.Fellowship.MordorStep);
        Assert.Equal(11, corrupted.Fellowship.Corruption);
        Assert.All(Pool(corrupted), tile => Assert.True(int.Parse(tile.Value) > 0, tile.Id));
        Assert.Contains(corrupted.Events, e => e.Kind == "MordorEntered");
    }

    [Fact]
    public void TheMilitaryScenariosHoldThePointsAndLeaveOneActionDie()
    {
        var shadow = TestScenarioCatalog.Find("sh-military")!.Create("shadow");
        Assert.Equal(10, MilitaryVictory.Points(shadow, Side.Shadow));
        Assert.Equal(0, MilitaryVictory.Points(shadow, Side.FreePeoples));
        Assert.Equal(1, Available(shadow, Side.Shadow));
        Assert.Equal(0, Available(shadow, Side.FreePeoples));
        Assert.All(new[] { "minas-tirith", "dol-amroth", "helms-deep", "erebor", "lorien" },
            region => Assert.Contains(shadow.Units, u => u.Location == region && u.Nation == "sauron"));
        var free = TestScenarioCatalog.Find("fp-military")!.Create("free");
        Assert.Equal(4, MilitaryVictory.Points(free, Side.FreePeoples));
        Assert.Equal(0, MilitaryVictory.Points(free, Side.Shadow));
        Assert.Equal(1, Available(free, Side.FreePeoples));
        Assert.Equal(0, Available(free, Side.Shadow));
    }

    // The ring endings move the Fellowship with a Character die (and take any damage with the Ring); the military
    // endings use the last action die without effect.
    private static MatchState Play(MatchState state, Side side, string id, IRandomSource random)
    {
        var military = id.EndsWith("military", StringComparison.Ordinal);
        var die = state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available &&
            (military || d.Face == DieFace.Character));
        var current = Execute(state, side, military ? CommandKind.DiscardDie : CommandKind.MoveFellowship, new() { DieId = die.Id }, random);
        if (current.Pending?.Kind == DecisionKind.HuntDamage)
            current = Execute(current, Side.FreePeoples, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, random);
        return current;
    }

    private static MatchState Execute(MatchState state, Side side, CommandKind kind, CommandPayload payload, IRandomSource random)
    {
        var result = Engine.Execute(state, side, new GameCommand
        {
            MatchId = state.Id,
            CommandId = kind + "-" + state.Revision,
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = kind,
            Payload = payload
        }, random);
        Assert.True(result.Receipt.Accepted, kind + ": " + result.Receipt.Error);
        return result.State;
    }

    private static IEnumerable<HuntTileDefinition> Pool(MatchState state) =>
        state.HuntTiles.Where(t => t.Location == "pool").Select(t => HuntPool.Definitions[t.Id]);

    private static int Available(MatchState state, Side side) =>
        state.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Available);
}
