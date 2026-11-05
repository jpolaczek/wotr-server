using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// RANK-06 (R35, docs/RANKED_PLAY.md D9): every decision runs on the big or the small limit, exactly as D9 lists.
public class MoveClockTests
{
    private static readonly DecisionKind[] Big =
    [
        DecisionKind.FellowshipPhase, DecisionKind.AllocateHunt, DecisionKind.Action, DecisionKind.ActionSequence,
        DecisionKind.ArmyEvent, DecisionKind.CharacterEvent, DecisionKind.CouncilVersions, DecisionKind.CouncilHomelands,
        DecisionKind.CouncilToken
    ];

    // A kind added without a clock fails here, before it can reach a ranked match.
    [Fact]
    public void EveryDecisionKindRunsOnTheLimitD9Gives()
    {
        foreach (var kind in Enum.GetValues<DecisionKind>())
            Assert.Equal(Big.Contains(kind) ? MoveClockKind.Big : MoveClockKind.Small, MoveClock.Kind(kind));
        Assert.Equal(22, Enum.GetValues<DecisionKind>().Count(kind => MoveClock.Kind(kind) == MoveClockKind.Small));
        Assert.Throws<ArgumentOutOfRangeException>(() => MoveClock.Kind((DecisionKind)int.MaxValue));
    }

    [Theory]
    [InlineData(RankedPreset.Fast, 90, 20)]
    [InlineData(RankedPreset.Standard, 180, 30)]
    [InlineData(RankedPreset.Long, 300, 60)]
    public void ThePresetsGiveD4sLimits(RankedPreset preset, int big, int small)
    {
        Assert.Equal(TimeSpan.FromSeconds(big), MoveClock.Limit(preset, MoveClockKind.Big));
        Assert.Equal(TimeSpan.FromSeconds(small), MoveClock.Limit(preset, MoveClockKind.Small));
    }

    // D10: with a 30 s limit the warning comes 20 s in.
    [Fact]
    public void TheWarningComesTenSecondsBeforeTheLimit() =>
        Assert.Equal(TimeSpan.FromSeconds(20), MoveClock.Limit(RankedPreset.Standard, MoveClockKind.Small) - MoveClock.Warning);
}
