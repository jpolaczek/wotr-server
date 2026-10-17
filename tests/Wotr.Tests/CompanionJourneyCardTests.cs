using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public class CompanionJourneyCardTests
{
    [Theory]
    [InlineData(Gwaihir)]
    [InlineData(Swifter)]
    public void NoRemainingCompanionsDoesNotInventAPlayConditionOrOpenAnEmptyDecision(string card)
    {
        var state = TurnTests.Actions();
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state = Play(state, card);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }

    [Theory]
    [InlineData(Gwaihir, 6, 6)]
    [InlineData(Swifter, 5, 7)]
    public void SeparationUsesProgressAndCardRangeForTheChosenGroup(string card, int hobbitRange, int striderRange)
    {
        var state = TurnTests.Actions();
        state.Fellowship.Progress = 2;
        state = Play(state, card);
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(hobbitRange, option.Travel.Single(t => t.Id == CharacterHuntTests.Merry).Range);
        Assert.Equal(striderRange, option.Travel.Single(t => t.Id == CharacterRules.Strider).Range);
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "lorien"] });
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin", "moria", "dimrill-dale"] });
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell", "fords-of-bruinen", "hollin", "north-dunland"] });
        Assert.Equal("north-dunland", state.Characters.Single().Location);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }

    internal static MatchState BesiegedDestination(string card = Gwaihir)
    {
        var state = BattleFixture.State();
        Put(state, "base.character.gimli", "dimrill-dale");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        return Play(state, card);
    }

    [Theory]
    [InlineData(Gwaihir)]
    [InlineData(Swifter)]
    public void JourneyMayEndInSiegeButCannotCrossOrLeaveIt(string card)
    {
        var state = BesiegedDestination(card);
        var move = new CommandPayload { CompanionMoves = [new() { Ids = ["base.character.gimli"], Route = ["dimrill-dale", "lorien"] }] };
        var options = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.MoveCompanions);
        Assert.Equal(1, options.MaximumGroups);
        Assert.Contains("lorien", options.Travel.Single().Targets);
        Assert.Contains("lorien", options.Travel.Single().StopRegions);
        Assert.DoesNotContain("lorien", options.Travel.Single().BlockedRegions);
        move.CompanionMoves[0].Route.Add("parth-celebrant");
        Reject(state, CommandKind.MoveCompanions, move);
        move.CompanionMoves[0].Route.RemoveAt(2);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.MoveCompanions, move));
        Assert.True(preview.Legal);
        Assert.Empty(preview.Costs);
        state = Do(StateCodec.Copy(state), CommandKind.MoveCompanions, move);
        Assert.True(state.Characters.Single().Garrison);
        Assert.Equal("lorien", state.Characters.Single().Location);
        Assert.Empty(state.Resolution.Frames);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Play(state, card);
        move.CompanionMoves[0].Route = ["lorien", "dimrill-dale"];
        Reject(state, CommandKind.MoveCompanions, move);
        move.CompanionMoves[0].Route = ["lorien"];
        state = Do(state, CommandKind.MoveCompanions, move);
        Assert.True(state.Characters.Single().Garrison);
    }

    [Theory]
    [InlineData(Gwaihir)]
    [InlineData(Swifter)]
    public void SeparationCanEnterButCannotDepartFromBesiegedFellowship(string card)
    {
        var state = BesiegedDestination(card);
        state.Fellowship.RegionId = "dimrill-dale";
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["dimrill-dale", "lorien"] });
        Assert.True(state.Characters.Single(c => c.Id == CharacterHuntTests.Merry).Garrison);
        state = BesiegedDestination(card);
        state.Fellowship.RegionId = "lorien";
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["lorien", "dimrill-dale"] });
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["lorien"] });
        Assert.True(state.Characters.Single(c => c.Id == CharacterHuntTests.Merry).Garrison);
    }

    [Theory]
    [InlineData(Gwaihir, 4, 4)]
    [InlineData(Swifter, 6, 5)]
    public void OneOutsideGroupUsesShadowfaxOrFixedLevelAndCannotSplit(string card, int small, int large)
    {
        var state = BattleFixture.State();
        var gandalf = Put(state, CharacterRules.Grey, "rivendell");
        CharacterLifecycle.ApplyVersion(gandalf, CharacterCatalog.Base[CharacterRules.White]);
        Put(state, CharacterHuntTests.Merry, "rivendell");
        Put(state, "base.character.pippin", "rivendell");
        state = Play(state, card);
        var group = state.Characters.ToArray();
        Assert.Equal(small, CompanionRules.Range(state, group.Take(2).ToArray(), false));
        Assert.Equal(large, CompanionRules.Range(state, group, false));
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.MoveCompanions);
        var white = option.Travel.Single(t => t.Id == CharacterRules.Grey);
        Assert.Equal(card == Swifter ? small : 0, white.SmallGroupRange);
        Reject(state, CommandKind.MoveCompanions, new()
        {
            CompanionMoves = [
            new() { Ids = [CharacterRules.Grey], Route = ["rivendell"] },
            new() { Ids = [CharacterHuntTests.Merry], Route = ["rivendell"] }]
        });
        var route = new List<string> { "rivendell", "fords-of-bruinen", "hollin", "north-dunland", "south-dunland", "gap-of-rohan", "fords-of-isen" };
        var move = new CommandPayload { CompanionMoves = [new() { Ids = [CharacterRules.Grey, CharacterHuntTests.Merry], Route = route }] };
        move.CompanionMoves[0].Ids.Add("base.character.pippin");
        Reject(state, CommandKind.MoveCompanions, move);
        move.CompanionMoves[0].Ids.Remove("base.character.pippin");
        if (card == Gwaihir)
            Reject(state, CommandKind.MoveCompanions, move);
        else
            Assert.Equal("fords-of-isen", Do(state, CommandKind.MoveCompanions, move).Characters[0].Location);
    }

    [Theory]
    [InlineData(Gwaihir)]
    [InlineData(Swifter)]
    public void MordorStillAllowsOutsideTravelOrRemovalOfOneSeparatedGroup(string card)
    {
        var state = TurnTests.Actions();
        Put(state, "base.character.gimli", "dale");
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 0;
        state = Play(state, card);
        Assert.Equal(2, new FellowshipOptions().GetOptions(state, Side.FreePeoples).Count());
        Reject(state, CommandKind.SeparateCompanions, new() { Ids = [CharacterHuntTests.Merry], Route = ["dale"] });
        state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [.. state.Fellowship.Companions] });
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Equal("dale", state.Characters.Single(c => c.Id == "base.character.gimli").Location);
        Assert.All(state.Characters.Where(c => c.Id != "base.character.gimli"), c => Assert.Equal("casualties", c.Location));
    }
}
