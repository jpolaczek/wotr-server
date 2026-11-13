using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// Lords: Smeagol leaves a Fellowship declared in an unconquered City or Stronghold, and the Free
// Peoples choose a new Guide before the declaration's reactions and Hunts.
public sealed class SmeagolDeclarationBoundaryTests
{
    private static readonly Projection Projection = GameRuntime.Projection(EffectCatalog.All);

    private static MatchState Declared()
    {
        var state = LordsSmeagolTests.State(true);
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.RegionId = "lorien";
        state.Control["lorien"] = Side.FreePeoples;
        TurnFlow.OpenFellowshipPhase(state);
        return LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["lorien"] });
    }

    [Fact]
    public void TheGuideAfterSmeagolLeavesIsMarkedWithTheDeclaration()
    {
        var state = Declared();
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.False(SmeagolRules.Present(state));

        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
        {
            var facts = Projection.For(state, side).ActionBoundary?.FellowshipDeclaration;
            Assert.Equal(["lorien"], facts?.Route);
            Assert.Empty(facts!.RemainingStrongholds);
        }
    }

    // The check runs for every view: once Smeagol has left, a Fellowship on the Mordor Track,
    // which has no board region, must still project.
    [Fact]
    public void AFellowshipOnTheMordorTrackAfterSmeagolLeftStillProjects()
    {
        var state = Declared();
        state = LordsFixture.Do(state, CommandKind.SelectGuide, new() { GuideId = CompanionRules.Guides(state)[0] });
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 0;

        Assert.True(state.Lords!.SmeagolLeft);
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            Assert.NotNull(Projection.For(state, side));
    }

    [Theory]
    [InlineData("queue")]
    [InlineData("left")]
    [InlineData("revealed-effects")]
    public void AnInconsistentGuideAfterTheDeclarationIsNotMarked(string change)
    {
        var state = Declared();
        var declaration = state.Resolution.Frames[0].Fellowship!;
        switch (change)
        {
            case "queue": declaration.AdditionalHuntRegions = ["dol-guldur"]; break;
            case "left": state.Lords!.SmeagolLeft = false; break;
            case "revealed-effects": declaration.RevealEffectsPending = false; break;
        }

        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.FellowshipDeclaration);
    }
}
