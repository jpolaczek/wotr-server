using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class CompanionTests
{
    [Fact]
    public void TravelOptionsSeparateBlockedPassagesStopsAndRevealDestinations()
    {
        var state = FellowshipTests.Phase("lorien", 3);
        var declarable = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.DeclareFellowship);
        Assert.Empty(declarable.Travel.Single().BlockedRegions);
        Assert.Empty(declarable.Travel.Single().StopRegions);
        FellowshipFlow.BeginSeparation(state, "fixture");
        foreach (var unit in state.Units.Where(u => u.Location == "minas-tirith"))
            unit.Garrison = true;
        var enemy = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular);
        enemy.Location = "minas-tirith";
        var separation = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.SeparateCompanions);
        foreach (var travel in separation.Travel)
        {
            Assert.Contains("moria", travel.StopRegions);
            Assert.Contains("minas-tirith", travel.BlockedRegions);
            Assert.DoesNotContain("minas-tirith", travel.Targets);
        }
        Assert.Empty(new FellowshipOptions().GetOptions(state, Side.Shadow));
    }

    private const string Grey = "base.character.gandalf-grey";
    private const string Strider = "base.character.strider";
    private const string Gimli = "base.character.gimli";
    private const string Merry = "base.character.merry";

    [Fact]
    public void ASeparatedGroupUsesProgressPlusItsHighestLevelAndGuideChoiceKeepsTheCostPaid()
    {
        var state = TurnTests.Actions();
        state.Fellowship.Progress = 5;
        var route = new List<string> { "rivendell", "fords-of-bruinen", "high-pass", "goblins-gate", "old-ford", "carrock", "northern-mirkwood", "woodland-realm" };
        var payload = new CommandPayload { DieId = state.Dice[0].Id, Ids = [Gimli, Merry], Route = route };
        state = FellowshipTests.Do(state, CommandKind.SeparateCompanions, payload);
        Assert.Equal(2, state.Characters.Count);
        Assert.All(state.Characters, c => Assert.Equal("woodland-realm", c.Location));
        Assert.Equal(5, state.Fellowship.Progress);
        Assert.Equal("rivendell", state.Fellowship.RegionId);
        Assert.Equal(5, state.Fellowship.Companions.Count);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        state = FellowshipTests.Do(StateCodec.Copy(state), CommandKind.SelectGuide, new() { GuideId = Strider });
        Assert.Equal(Strider, state.Fellowship.Guide);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("range")]
    [InlineData("mordor")]
    [InlineData("empty")]
    public void InvalidSeparationNeverConsumesTheDieOrChangesComposition(string reason)
    {
        var state = TurnTests.Actions();
        var p = new CommandPayload { DieId = state.Dice[0].Id, Ids = [Merry], Route = ["rivendell", "fords-of-bruinen"] };
        if (reason == "duplicate")
            p.Ids.Add(Merry);
        if (reason == "range")
            p.Route.Add("hollin");
        if (reason == "mordor")
            state.Fellowship.MordorStep = 0;
        if (reason == "empty")
            p.Ids.Clear();
        FellowshipTests.Reject(state, CommandKind.SeparateCompanions, p);
    }

    [Fact]
    public void EmptyFellowshipUsesGollumAndDoesNotCreateACompanionForHuntAllocation()
    {
        var state = TurnTests.Actions();
        state = FellowshipTests.Do(state, CommandKind.SeparateCompanions, new()
        {
            DieId = state.Dice[0].Id,
            Ids = state.Fellowship.Companions.ToList(),
            Route = ["rivendell"]
        });
        Assert.Empty(state.Fellowship.Companions);
        Assert.Equal("base.character.gollum", state.Fellowship.Guide);
        Assert.Equal(7, state.Characters.Count);
        Assert.DoesNotContain(state.Characters, c => c.Id == state.Fellowship.Guide);
        var recovered = StateCodec.Copy(state);
        DiceRules.Recover(recovered);
        Assert.Equal(1, DiceRules.HuntMaximum(recovered));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        FellowshipTests.Reject(state, CommandKind.SeparateCompanions, new() { DieId = state.Dice[1].Id, Ids = [Gimli], Route = ["rivendell"] });
    }

    [Fact]
    public void SeparationIntoFriendlySiegeStaysInsideWhileTheFellowshipMayLeave()
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = "minas-tirith";
        Besiege(state, "minas-tirith");
        FellowshipTests.Reject(state, CommandKind.SeparateCompanions,
            new() { DieId = state.Dice[0].Id, Ids = [Gimli], Route = ["minas-tirith", "osgiliath"] });
        state = FellowshipTests.Do(state, CommandKind.SeparateCompanions,
            new() { DieId = state.Dice[0].Id, Ids = [Gimli], Route = ["minas-tirith"] });
        Assert.True(Assert.Single(state.Characters).Garrison);
        state = FellowshipTests.Do(state, CommandKind.SelectGuide, new() { GuideId = Grey });
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.Progress = 1;
        TurnFlow.OpenFellowshipPhase(state);
        state = FellowshipTests.Do(state, CommandKind.DeclareFellowship, new() { Route = ["minas-tirith", "osgiliath"] });
        Assert.Equal("osgiliath", state.Fellowship.RegionId);
        Assert.Equal("minas-tirith", Assert.Single(state.Characters).Location);
    }

    [Fact]
    public void CompanionsStopAtEnemyStrongholdsButIgnoreEnemyArmiesAndMayLeaveTheStrongholdNextTime()
    {
        var state = TurnTests.Actions();
        Outside(state, Grey, "hollin");
        var die = state.Dice[0].Id;
        FellowshipTests.Reject(state, CommandKind.MoveCompanions, new() { DieId = die, CompanionMoves = [Move([Grey], "hollin", "moria", "dimrill-dale")] });
        state = FellowshipTests.Do(state, CommandKind.MoveCompanions, new() { DieId = die, CompanionMoves = [Move([Grey], "hollin", "moria")] });
        Assert.Equal("moria", Assert.Single(state.Characters).Location);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var enemy = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular);
        enemy.Location = "dimrill-dale";
        state = FellowshipTests.Do(state, CommandKind.MoveCompanions, new() { DieId = state.Dice[1].Id, CompanionMoves = [Move([Grey], "moria", "dimrill-dale", "lorien")] });
        Assert.Equal("lorien", Assert.Single(state.Characters).Location);
    }

    [Fact]
    public void GroupsMaySplitButCannotReuseACharacterOrMeetMidMoveToExtendRange()
    {
        var state = TurnTests.Actions();
        Outside(state, Grey, "rivendell");
        Outside(state, Merry, "rivendell");
        Outside(state, Gimli, "hollin");
        var die = state.Dice[0].Id;
        FellowshipTests.Reject(state, CommandKind.MoveCompanions, new()
        {
            DieId = die,
            CompanionMoves =
            [Move([Grey], "rivendell", "fords-of-bruinen", "hollin"), Move([Grey, Gimli], "hollin", "moria")]
        });
        FellowshipTests.Reject(state, CommandKind.MoveCompanions, new() { DieId = die, CompanionMoves = [Move([Grey, Gimli], "rivendell", "fords-of-bruinen")] });
        FellowshipTests.Reject(state, CommandKind.MoveCompanions, new() { DieId = die, CompanionMoves = [Move([Merry], "rivendell", "fords-of-bruinen", "hollin")] });
        state = FellowshipTests.Do(state, CommandKind.MoveCompanions, new()
        {
            DieId = die,
            CompanionMoves =
            [Move([Grey], "rivendell", "fords-of-bruinen", "hollin"), Move([Merry], "rivendell", "trollshaws")]
        });
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == Grey).Location);
        Assert.Equal("trollshaws", state.Characters.Single(c => c.Id == Merry).Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void AGroupCarriesALowerLevelCompanionAndActivatesOnlyAtItsDestination()
    {
        var state = TurnTests.Actions();
        Outside(state, Grey, "pelargir");
        Outside(state, Merry, "pelargir");
        var moves = new CommandPayload { DieId = state.Dice[0].Id, CompanionMoves = [Move([Grey, Merry], "pelargir", "osgiliath", "minas-tirith")] };
        var preview = new PreviewService(FellowshipTests.Engine).Preview(state, Side.FreePeoples, FellowshipTests.Command(state, CommandKind.MoveCompanions, moves));
        Assert.Equal(2, preview.Placements.Count);
        state = FellowshipTests.Do(state, CommandKind.MoveCompanions, moves);
        Assert.All(state.Characters, c => Assert.Equal("minas-tirith", c.Location));
        Assert.True(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.Equal(2, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
    }

    [Fact]
    public void FriendlySiegesBlockEntryExitAndTraversalForIndependentCompanions()
    {
        var state = TurnTests.Actions();
        Outside(state, Grey, "osgiliath");
        Besiege(state, "minas-tirith");
        FellowshipTests.Reject(state, CommandKind.MoveCompanions,
            new() { DieId = state.Dice[0].Id, CompanionMoves = [Move([Grey], "osgiliath", "minas-tirith")] });
        var options = FellowshipTests.Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.MoveCompanions);
        Assert.DoesNotContain("minas-tirith", options.Travel[0].Targets);
        state.Characters[0].Location = "minas-tirith";
        state.Characters[0].Garrison = true;
        FellowshipTests.Reject(state, CommandKind.MoveCompanions,
            new() { DieId = state.Dice[0].Id, CompanionMoves = [Move([Grey], "minas-tirith", "osgiliath")] });
    }

    [Fact]
    public void LeavingTheGuideCreatesATieAtTheNextHighestLevel()
    {
        var state = TurnTests.Actions();
        state = FellowshipTests.Do(state, CommandKind.SeparateCompanions,
            new() { DieId = state.Dice[0].Id, Ids = [Grey, Strider], Route = ["rivendell"] });
        Assert.Equal(new[] { "base.character.boromir", Gimli, "base.character.legolas" }, state.Pending!.PrivateChoices);
        FellowshipTests.Reject(state, CommandKind.SelectGuide, new() { GuideId = Merry });
        state = FellowshipTests.Do(state, CommandKind.SelectGuide, new() { GuideId = Gimli });
        Assert.Equal(Gimli, state.Fellowship.Guide);
    }

    internal static void Outside(MatchState state, string id, string region)
    {
        var character = CharacterCatalog.Companion(state, id);
        state.Fellowship.Companions.Remove(id);
        character.Location = region;
        state.Characters.Add(character);
        state.Fellowship.Guide = CompanionRules.Guides(state)[0];
    }
    private static CompanionMoveChoice Move(List<string> ids, params string[] route) => new() { Ids = ids, Route = [.. route] };
    private static void Besiege(MatchState state, string region)
    {
        foreach (var unit in state.Units.Where(u => u.Location == region))
            unit.Garrison = true;
        state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular).Location = region;
    }
}
