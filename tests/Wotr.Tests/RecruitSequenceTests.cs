using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

public class RecruitSequenceTests
{
    [Fact]
    public void SecondRecruitRequiresAnotherSettlementAndCannotBeAnElite()
    {
        var state = Board();
        state.Dice[0].Face = DieFace.Muster;
        var first = Placement(state, UnitKind.Regular, "minas-tirith");
        state = Do(state, CommandKind.Recruit, first);
        state = StateCodec.Copy(state);
        Assert.Equal(1, state.Resolution.ActionSequence!.Remaining);
        var option = ActionSequenceOptions.For(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.Recruit);
        Assert.DoesNotContain("minas-tirith", option.Targets);
        Assert.DoesNotContain(option.Recruitment, r => r.Kind == UnitKind.Elite);
        Reject(state, CommandKind.Recruit, Placement(state, UnitKind.Regular, "minas-tirith"));
        Reject(state, CommandKind.Recruit, Placement(state, UnitKind.Elite, "pelargir"));
        state = Do(state, CommandKind.Recruit, Placement(state, UnitKind.Regular, "pelargir"));
        Assert.Null(state.Resolution.ActionSequence);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "UnitRecruited"));
        Assert.Equal(1, state.Dice.Count(d => d.Location == DieLocation.Spent));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void EliteConsumesTheWholeBudgetAndUnaccompaniedLeaderStillDies()
    {
        var state = Board();
        state.Dice[0].Face = DieFace.Muster;
        var elite = Do(state, CommandKind.Recruit, Placement(state, UnitKind.Elite, "minas-tirith"));
        Assert.Null(elite.Resolution.ActionSequence);
        Assert.Equal(DieLocation.Spent, elite.Dice[0].Location);
        var leader = Placement(state, UnitKind.Leader, "minas-tirith");
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.Recruit, leader));
        Assert.Equal(leader.Placements[0].UnitId, Assert.Single(preview.RemovedIds));
        state = Do(state, CommandKind.Recruit, leader);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader.Placements[0].UnitId).Location);
        Assert.Equal(DecisionKind.ActionSequence, state.Pending!.Kind);
        state = Do(StateCodec.Copy(state), CommandKind.FinishActionSequence);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }

    internal static CommandPayload Placement(MatchState state, UnitKind kind, string region) => new()
    {
        Stepwise = true,
        DieId = state.Resolution.ActionSequence?.DieId ?? state.Dice[0].Id,
        Placements = [new() { UnitId = state.Units.First(u => u.Nation == "gondor" && u.Kind == kind && u.Location == "reserve").Id, RegionId = region }]
    };
}
