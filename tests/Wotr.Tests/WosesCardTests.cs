using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;
using static Wotr.Tests.Strategy02Fixture;

namespace Wotr.Tests;

public class WosesCardTests
{
    [Fact]
    public void WholeGarrisonCanLeaveWithLeaderLossAndCompanionRemaining()
    {
        var state = Play(Passage(), PathsOfWoses);
        var payload = Move(state, "helms-deep", "minas-tirith");
        var leader = state.Units.Single(u => u.Location == "helms-deep" && u.Kind == UnitKind.Leader).Id;
        payload.Moves[0].UnitIds.Remove(leader);
        payload.Moves[0].CharacterIds.Clear();
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.MoveArmies, payload));
        Assert.True(preview.Legal);
        Assert.Contains(leader, preview.RemovedIds);
        Assert.Equal(Side.Shadow, preview.ControlChanges["helms-deep"]);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Equal(Side.Shadow, state.Control["helms-deep"]);
        Assert.Equal("helms-deep", state.Characters.Single().Location);
        Assert.False(state.Characters.Single().Garrison);
        Assert.False(state.Units.Single(u => u.Location == "minas-tirith").Garrison);
    }

    [Fact]
    public void PartialGarrisonExitKeepsSiegeAndFieldCannotAbandonLeader()
    {
        var state = Passage();
        var rear = BattleFixture.Put(state, "rohan", UnitKind.Regular, "helms-deep");
        rear.Garrison = true;
        state = Play(state, PathsOfWoses);
        var payload = Move(state, "helms-deep", "minas-tirith");
        payload.Moves[0].UnitIds.Remove(rear.Id);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.True(BoardRules.Besieged(state, "helms-deep", Side.FreePeoples));
        Assert.Equal(Side.FreePeoples, state.Control["helms-deep"]);
        state = Play(Passage(false), PathsOfWoses);
        payload = Move(state, "helms-deep", "minas-tirith");
        payload.Moves[0].UnitIds.Remove(state.Units.Single(u => u.Kind == UnitKind.Leader && u.Location == "helms-deep").Id);
        Reject(state, CommandKind.MoveArmies, payload);
    }

    [Theory]
    [InlineData("captured")]
    [InlineData("fp-besieged")]
    [InlineData("sh-besieged")]
    public void UnavailableMinasTirithRequiresAdjacentFreeArmyDestination(string situation)
    {
        var state = Passage();
        state.Control["minas-tirith"] = situation == "fp-besieged" ? Side.FreePeoples : Side.Shadow;
        if (situation != "captured")
        {
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = situation == "fp-besieged";
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "minas-tirith").Garrison = situation == "sh-besieged";
        }
        state.Control["lossarnach"] = Side.Shadow;
        state = Play(state, PathsOfWoses);
        var option = new MovementOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.DoesNotContain("minas-tirith", option.Targets);
        Assert.Contains("lossarnach", option.Targets);
        Reject(state, CommandKind.MoveArmies, Move(state, "helms-deep", "minas-tirith"));
        state = Do(state, CommandKind.MoveArmies, Move(state, "helms-deep", "lossarnach"));
        Assert.Equal(Side.FreePeoples, state.Control["lossarnach"]);
    }

    [Fact]
    public void MustUseMinasTirithWhenAvailableAndHonorRohanAndBorders()
    {
        var state = Passage();
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 1;
        Assert.False(EffectCatalog.Base.Events[PathsOfWoses].Condition.CanPlay(state, Side.FreePeoples));
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 0;
        var elves = BattleFixture.Put(state, "elves", UnitKind.Regular, "helms-deep");
        elves.Garrison = true;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 1;
        state = Play(state, PathsOfWoses);
        Reject(state, CommandKind.MoveArmies, Move(state, "helms-deep", "lossarnach"));
        Reject(state, CommandKind.MoveArmies, Move(state, "helms-deep", "minas-tirith"));
        var payload = Move(state, "helms-deep", "minas-tirith");
        payload.Moves[0].UnitIds.Remove(elves.Id);
        state = Do(state, CommandKind.MoveArmies, payload);
        Assert.Equal("helms-deep", state.Units.Single(u => u.Id == elves.Id).Location);
    }

    [Fact]
    public void CardIsUnavailableWhenEveryDestinationIsBlocked()
    {
        var state = Passage();
        state.Control["minas-tirith"] = Side.Shadow;
        foreach (var region in BoardRules.Regions["minas-tirith"].Neighbors)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
        Assert.False(EffectCatalog.Base.Events[PathsOfWoses].Condition.CanPlay(state, Side.FreePeoples));
        Assert.Empty(ArmyEventOptions.Armies(state, Side.FreePeoples, ArmyEventOrder.RohanPassage));
    }
}
