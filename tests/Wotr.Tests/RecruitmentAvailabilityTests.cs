using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionSequenceFixture;

namespace Wotr.Tests;

// UI-129: every own nation gets a "recruit" availability entry whose targets and conditions come from the same
// rule as Muster itself, so the client can say why a settlement is closed.
public class RecruitmentAvailabilityTests
{
    [Fact]
    public void ANationOneStepFromWarIsExplainedAndOffersNoSettlement()
    {
        var state = Muster();
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        rohan.Active = true;
        rohan.StepsToWar = 1;
        var entry = Recruit(state, "rohan");
        Assert.False(entry.CanAct);
        Assert.Empty(entry.Targets);
        var war = entry.Conditions.Single(c => c.Code == RecruitmentBlock.AtWar);
        Assert.False(war.Met);
        Assert.Equal("Rohan is 1 step from war.", war.Description);
        Assert.Equal("settlement:helms-deep:at-war", Settlement(entry, "helms-deep").Code);
        Assert.NotEmpty(entry.EligibleDice);
    }

    [Fact]
    public void ClosedSettlementsNameTheirRuleAndOpenOnesAreTheTargets()
    {
        var state = Muster();
        state.Control["pelargir"] = Side.Shadow;
        Put(state, "sauron", "minas-tirith");
        var entry = Recruit(state, "gondor");
        Assert.True(entry.CanAct);
        Assert.Equal("settlement:pelargir:captured", Settlement(entry, "pelargir").Code);
        Assert.Equal("settlement:minas-tirith:besieged", Settlement(entry, "minas-tirith").Code);
        Assert.False(Settlement(entry, "pelargir").Met);
        Assert.Equal(["dol-amroth", "lamedon", "lossarnach"], entry.Targets.Order().ToArray());
        Assert.True(Settlement(entry, "lossarnach").Met);
        Assert.Equal("settlement:lossarnach", Settlement(entry, "lossarnach").Code);
        var expected = BoardRules.CurrentRegions(state).Where(r => r.Nation == "gondor" && BoardRules.IsSettlement(r) &&
            state.Units.Where(u => u.Nation == "gondor" && u.Location == "reserve")
                .Any(u => RecruitmentRules.CanPlaceMuster(state, Side.FreePeoples, u, r.Id))).Select(r => r.Id).Order();
        Assert.Equal(expected, entry.Targets.Order());
    }

    [Fact]
    public void DuringTheActionAUsedSettlementAndTheEliteBudgetAreNamed()
    {
        var state = Muster();
        state = Do(state, CommandKind.Recruit, RecruitSequenceTests.Placement(state, UnitKind.Regular, "minas-tirith"));
        Assert.Equal(1, state.Resolution.ActionSequence!.Remaining);
        var entry = Recruit(state, "gondor");
        Assert.True(entry.CanAct, "The open settlements can still take the second figure.");
        Assert.Equal("settlement:minas-tirith:used-this-action", Settlement(entry, "minas-tirith").Code);
        Assert.DoesNotContain("minas-tirith", entry.Targets);
        Assert.Contains("pelargir", entry.Targets);
        Assert.False(entry.Conditions.Single(c => c.Code == RecruitmentBlock.EliteBudget).Met);
        Assert.Equal([state.Resolution.ActionSequence.DieId], entry.EligibleDice);
    }

    [Fact]
    public void NazgulEnterOnlyAStrongholdWhileOtherFiguresStillTakeATown()
    {
        var state = Muster(Side.Shadow);
        var nazgul = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Nazgul && u.Location == "reserve");
        Assert.Equal(RecruitmentBlock.NazgulStronghold, RecruitmentRules.MusterBlocker(state, Side.Shadow, nazgul, "nurn"));
        Assert.Null(RecruitmentRules.MusterBlocker(state, Side.Shadow, nazgul, "barad-dur"));
        Assert.Contains("nurn", Recruit(state, "sauron", Side.Shadow).Targets);
    }

    [Fact]
    public void EachSideSeesOnlyItsOwnNations()
    {
        var state = Muster();
        var free = InGameProjectionTests.ProjectionViews().For(state, Side.FreePeoples).Availability.Where(a => a.ActionId == "recruit")
            .Select(a => a.SubjectId).Order();
        var shadow = InGameProjectionTests.ProjectionViews().For(state, Side.Shadow).Availability.Where(a => a.ActionId == "recruit")
            .Select(a => a.SubjectId).Order();
        Assert.Equal(["dwarves", "elves", "gondor", "north", "rohan"], free);
        Assert.Equal(["isengard", "sauron", "southrons"], shadow);
    }

    private static ActionAvailabilityView Recruit(MatchState state, string nation, Side viewer = Side.FreePeoples) =>
        InGameProjectionTests.Availability(state, viewer, nation, "recruit");

    private static AvailabilityConditionView Settlement(ActionAvailabilityView entry, string region) =>
        entry.Conditions.Single(c => c.Code == "settlement:" + region || c.Code.StartsWith("settlement:" + region + ":"));

    private static MatchState Muster(Side side = Side.FreePeoples)
    {
        var state = Board(side);
        foreach (var die in state.Dice.Where(d => d.Owner == side))
            die.Face = DieFace.Muster;
        return state;
    }
}
