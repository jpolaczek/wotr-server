using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards03;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsRevealedTests
{
    [Fact]
    public void RequiresAragornOrAwakenedDenethorNotStriderOrCorruptedDenethor()
    {
        var state = State(Side.Shadow);
        var payload = Cost(state, King);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        Corrupt(state, SovereignCatalog.Denethor);
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        var aragorn = LordsFixture.Put(state, CharacterRules.Aragorn, "lossarnach");
        Assert.True(new KingsRevealedCondition().CanPlay(state, Side.Shadow));
        aragorn.Location = "casualties";
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        Assert.True(new KingsRevealedCondition().CanPlay(Ready(King), Side.Shadow));
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Nazgul)]
    public void RecruitsFiveRegularsOneNazgulAndChoosesBonusPerAwakenedSovereign(UnitKind kind)
    {
        var state = Ready(King);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Dain), "erebor");
        state = Play(state, King);
        Assert.Equal(5, state.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular));
        Assert.Single(state.Units, u => u.Location == "minas-morgul" && u.Kind == UnitKind.Nazgul);
        var option = new EffectOptions().GetOptions(state, Side.Shadow).Single();
        Assert.Equal(2, option.Minimum);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [option.EligibleIds[0]] });
        var ids = option.EligibleIds.Where(id => state.Units.Single(u => u.Id == id).Kind == kind).ToList();
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ids });
        Assert.Equal(8, state.Units.Count(u => u.Location == "minas-morgul"));
        Assert.All(ids, id => Assert.Equal("minas-morgul", state.Units.Single(u => u.Id == id).Location));
        Assert.Contains(King, state.Player(Side.Shadow).Discard);
    }

    [Fact]
    public void OccupiedDestinationAndMissingReservesProduceOnlyPossibleRecruitment()
    {
        var occupied = Ready(King);
        occupied.Control["minas-morgul"] = Side.FreePeoples;
        BattleFixture.Put(occupied, "gondor", UnitKind.Regular, "minas-morgul");
        occupied = Play(occupied, King);
        Assert.DoesNotContain(occupied.Events, e => e.Kind == "EventRecruited");
        var state = Ready(King);
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular).Skip(3))
            unit.Location = "removed";
        foreach (var unit in state.Units.Where(u => u.Kind == UnitKind.Nazgul).Skip(2))
            unit.Location = "removed";
        state = Play(state, King);
        Assert.Equal(3, state.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular));
        Assert.Equal(2, state.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Nazgul));
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Fact]
    public void StackLimitIsResolvedAfterTheBonusSelection()
    {
        var state = Ready(King);
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-morgul");
        state = Play(state, King);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        var id = state.Pending.PrivateChoices.First(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Regular);
        state = Choose(state, id);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
    }
}
