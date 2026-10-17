using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class MazarbulCardTests
{
    internal static MatchState BeforeJourney(string region = "iron-hills")
    {
        var state = BattleFixture.State();
        Put(state, "base.character.gimli", region);
        Put(state, "base.character.legolas", "dale");
        state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 3;
        state.Nations.Single(n => n.Id == "dwarves").Active = false;
        return state;
    }

    [Fact]
    public void IndependentGroupsMoveAndPutDwarvesAtWarWithoutSpendingSecondDie()
    {
        var state = Play(BeforeJourney(), BookOfMazarbul);
        Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
        var options = new FellowshipOptions().GetOptions(state, Side.FreePeoples).ToList();
        Assert.DoesNotContain(options, o => o.Kind == CommandKind.SeparateCompanions);
        Assert.Equal(0, options.Single().MaximumGroups);
        Assert.True(options.Single().AllowEmptySelection);
        state = Do(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [
            new() { Ids = ["base.character.gimli"], Route = ["iron-hills", "erebor"] },
            new() { Ids = ["base.character.legolas"], Route = ["dale", "erebor"] }
        ]
        });
        Assert.All(state.Characters, c => Assert.Equal("erebor", c.Location));
        Assert.True(state.Nations.Single(n => n.Id == "dwarves").Active);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData("erebor", true, true)]
    [InlineData("ered-luin", true, true)]
    [InlineData("north-ered-luin", true, false)]
    [InlineData("iron-hills", true, false)]
    [InlineData("erebor", false, false)]
    public void StayAllChecksOnlyExactUnconqueredRegions(string region, bool friendly, bool activates)
    {
        var state = BeforeJourney(region);
        state.Control[region] = friendly ? Side.FreePeoples : Side.Shadow;
        state.Player(Side.Shadow).Table.Add("base.sh.strategy.05");
        state.ActiveEffects.Add(new()
        {
            SourceId = "base.sh.strategy.05",
            Owner = Side.Shadow,
            Modifier = EffectModifier.PreventPassiveMusterAdvance,
            ExpiresWhen = EffectExpiry.FreeNationAdvancedByAttackOrCompanion
        });
        state = Play(state, BookOfMazarbul);
        state = Do(state, CommandKind.MoveCompanions);
        Assert.Equal(activates ? 0 : 3, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        Assert.Equal(activates, state.Nations.Single(n => n.Id == "dwarves").Active);
        Assert.Contains("base.sh.strategy.05", state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void BookCannotSeparateEnterSiegeOrMoveACompanionTwice()
    {
        var state = BeforeJourney();
        BattleFixture.Put(state, "dwarves", UnitKind.Regular, "erebor").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "erebor");
        state = Play(state, BookOfMazarbul);
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Grey], Route = [state.Fellowship.RegionId] });
        var move = new CompanionMoveChoice { Ids = ["base.character.gimli"], Route = ["iron-hills", "erebor"] };
        Reject(state, CommandKind.MoveCompanions, new() { CompanionMoves = [move] });
        move.Route = ["iron-hills"];
        Reject(state, CommandKind.MoveCompanions, new() { CompanionMoves = [move, move] });
    }

    [Fact]
    public void NoOutsideCompanionsCompletesWithoutOpeningAnEmptyDecision()
    {
        var state = Play(BattleFixture.State(), BookOfMazarbul);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Fact]
    public void OrdinaryMoveThatStaysInCityActivatesNationUnlikeRetreat()
    {
        var state = BeforeJourney("erebor");
        var payload = Cost(state, DieFace.Character);
        payload.CompanionMoves = [new() { Ids = ["base.character.gimli"], Route = ["erebor"] }];
        state = Do(state, CommandKind.MoveCompanions, payload);
        Assert.True(state.Nations.Single(n => n.Id == "dwarves").Active);
        Assert.Equal(3, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
    }
}
