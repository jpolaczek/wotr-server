using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class FateGimliBattleTests
{
    [Theory]
    [InlineData(FateGimli.Standard, "iron-hills", 2)]
    [InlineData(FateGimli.Standard, "ered-luin", 2)]
    [InlineData(FateGimli.Standard, "erebor", 2)]
    [InlineData(FateGimli.Standard, "dale", 2)]
    [InlineData(FateGimli.Alternate, "iron-hills", 2)]
    [InlineData(FateGimli.Alternate, "ered-luin", 2)]
    [InlineData(FateGimli.Alternate, "erebor", 1)]
    [InlineData(FateGimli.Alternate, "dale", 1)]
    public void PrintedCombatBonusUsesParticipationDefendersNationCancellationAndFiveDiceCap(string definition, string region, int expected)
    {
        foreach (var fpAttacks in new[] { false, true })
        {
            var fpRegion = fpAttacks ? "hollin" : region;
            var state = FateGimliFixture.Actions(definition, fpRegion);
            foreach (var unit in state.Units)
                BoardRules.ReturnToReserve(unit);
            var fp = new BattleForce { Side = Side.FreePeoples, Region = fpRegion,
                Participants = [BattleFixture.Put(state, "dwarves", UnitKind.Regular, fpRegion).Id, FateGimli.Figure] };
            var sh = new BattleForce { Side = Side.Shadow, Region = region };
            var battle = new BattleState { Region = region, Attacker = fpAttacks ? fp : sh, Defender = fpAttacks ? sh : fp };
            state.Resolution.Frames.Add(new() { Battle = battle });
            Assert.Equal(expected, BattleRoll.DiceCount(state, fp));
            fp.Participants.Remove(FateGimli.Figure);
            Assert.Equal(1, BattleRoll.DiceCount(state, fp));
            fp.Participants.Add(FateGimli.Figure);
            fp.Round.CancelledAbilities.Add(FateGimli.Figure);
            Assert.Equal(1, BattleRoll.DiceCount(state, fp));
            fp.Round = new();
            Assert.Equal(expected, BattleRoll.DiceCount(state, fp));
            for (var i = 0; i < 4; i++)
                fp.Participants.Add(BattleFixture.Put(state, "dwarves", UnitKind.Regular, fpRegion).Id);
            Assert.Equal(5, BattleRoll.DiceCount(state, fp));
        }
    }
}
