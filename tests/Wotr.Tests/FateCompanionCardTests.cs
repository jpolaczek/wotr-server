using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.FateCardsFixture;

namespace Wotr.Tests;

public class FateCompanionCardTests
{
    internal static MatchState There(string region = "carrock", string companion = "base.character.legolas")
    {
        var state = State(FateCards.ThereAndBack);
        CharacterFixture.Put(state, companion, region);
        foreach (var nation in state.Nations.Where(n => n.Id is "dwarves" or "elves" or "north"))
        {
            nation.StepsToWar = 3;
            nation.Active = false;
        }
        return state;
    }

    [Theory]
    [InlineData("carrock", "base.character.gimli", true)]
    [InlineData("iron-hills", "base.character.gimli", true)]
    [InlineData("woodland-realm", "base.character.legolas", true)]
    [InlineData("erebor", "base.character.gimli", false)]
    [InlineData("dale", "base.character.legolas", false)]
    [InlineData("ered-luin", "base.character.gimli", false)]
    [InlineData("carrock", "base.character.boromir", false)]
    public void ThereChecksOnlyThePrintedCompanionsAndNewRegionsEvenWithoutSeparation(string region, string companion, bool advances)
    {
        var state = There(region, companion);
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state.Control[region] = Side.Shadow;
        state = Play(state, FateCards.ThereAndBack);
        AssertPolitics(state, advances);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void ThereAllowsOneExtraRegionAndWaitsForTheGuideBeforeAdvancingPolitics()
    {
        var state = There();
        state.Fellowship.Guide = CharacterHuntTests.Merry;
        var payload = Cost(state, FateCards.ThereAndBack);
        state = KingsFixture.Do(state, CommandKind.PlayEvent, payload);
        var travel = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single().Travel;
        Assert.Equal(2, travel.Single(t => t.Id == CharacterHuntTests.Merry).Range);
        AssertPolitics(state, false);
        state = KingsFixture.Do(state, CommandKind.SeparateCompanions, new()
        {
            Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "trollshaws"]
        });
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == payload.DieId).Location);
        AssertPolitics(state, false);
        state = KingsFixture.Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        AssertPolitics(state, true);
        Assert.Equal("trollshaws", state.Characters.Single(c => c.Id == CharacterHuntTests.Merry).Location);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    private static void AssertPolitics(MatchState state, bool advances)
    {
        foreach (var nation in state.Nations.Where(n => n.Id is "dwarves" or "elves" or "north"))
        {
            Assert.Equal(advances ? 2 : 3, nation.StepsToWar);
            Assert.Equal(advances && nation.Id == "north", nation.Active);
        }
    }

    internal static MatchState Book(string region = "iron-hills", bool second = false)
    {
        var state = State(FateCards.Book);
        CharacterFixture.Put(state, "base.character.gimli", region);
        if (second)
            CharacterFixture.Put(state, "base.character.legolas", "ered-luin");
        state.Nations.Single(n => n.Id == "dwarves").StepsToWar = 3;
        return state;
    }

    [Theory]
    [InlineData("iron-hills", UnitKind.Regular)]
    [InlineData("iron-hills", UnitKind.Elite)]
    [InlineData("ered-luin", UnitKind.Regular)]
    [InlineData("ered-luin", UnitKind.Elite)]
    public void BookChoosesEitherCompanionRegionAndEitherUnitAfterPolitics(string region, UnitKind kind)
    {
        var state = Play(Book(second: true), FateCards.Book);
        var options = new FellowshipOptions().GetOptions(state, Side.FreePeoples).ToArray();
        Assert.Single(options);
        Assert.Equal(CommandKind.MoveCompanions, options[0].Kind);
        Assert.True(options[0].AllowEmptySelection);
        Assert.Equal(0, options[0].MaximumGroups);
        LordsFixture.Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterRules.Strider], Route = ["rivendell"] });
        state = KingsFixture.Do(state, CommandKind.MoveCompanions);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        Assert.Equal(new[] { "ered-luin", "iron-hills" }, Option(state).EligibleIds.Order());
        state = Choose(state, region);
        var candidate = Option(state).EligibleIds.Select(id => state.Units.Single(u => u.Id == id)).Single(u => u.Kind == kind);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [candidate.Id] }));
        Assert.Equal(region, preview.Placements.Single().RegionId);
        Assert.Equal(before, StateCodec.Write(state));
        state = Choose(state, candidate.Id);
        Assert.Equal(region, state.Units.Single(u => u.Id == candidate.Id).Location);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("iron-hills", true)]
    [InlineData("ered-luin", true)]
    [InlineData("erebor", false)]
    [InlineData("dale", false)]
    public void BookDoesNotRequireFriendlyControlForPoliticsAndNeverRecruitsUnderAnEnemyArmy(string region, bool advances)
    {
        var state = Book(region);
        FateFixture.Clear(state, region);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, region);
        state.Control[region] = Side.Shadow;
        state = Play(state, FateCards.Book);
        state = KingsFixture.Do(state, CommandKind.MoveCompanions);
        Assert.Equal(advances ? 0 : 3, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        Assert.DoesNotContain(state.Units, u => u.Location == region && u.Nation == "dwarves");
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void BookAllowsIndependentMovementButRejectsDuplicateCompanionsAndStillAdvancesWithoutReserve()
    {
        var state = Book("dale", second: true);
        Reserve(state, "dwarves", UnitKind.Regular, 0);
        Reserve(state, "dwarves", UnitKind.Elite, 0);
        state = Play(state, FateCards.Book);
        var move = new CompanionMoveChoice { Ids = ["base.character.gimli"], Route = ["dale", "iron-hills"] };
        LordsFixture.Reject(state, CommandKind.MoveCompanions, new() { CompanionMoves = [move, move] });
        state = KingsFixture.Do(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [move, new() { Ids = ["base.character.legolas"], Route = ["ered-luin", "grey-havens"] }]
        });
        Assert.Equal("iron-hills", state.Characters.Single(c => c.Id == "base.character.gimli").Location);
        Assert.Equal("grey-havens", state.Characters.Single(c => c.Id == "base.character.legolas").Location);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "dwarves").StepsToWar);
        Assert.Empty(state.Resolution.Frames);
    }
}
