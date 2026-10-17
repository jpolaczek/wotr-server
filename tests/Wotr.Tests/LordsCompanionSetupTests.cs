using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class LordsCompanionSetupTests
{
    private static MatchState Setup(bool council = false)
    {
        var state = KernelTests.NewGame();
        state.Configuration.Lords = true;
        state.Configuration.Council = council;
        TurnFlow.Start(state);
        return state;
    }

    private static MatchState Choose(MatchState state, string definition, string guide) =>
        LordsFixture.Do(state, CommandKind.ChooseCompanionVersions, new() { Ids = [definition], GuideId = guide });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GreyVersionWithStriderGuideIsPrivateEvenWhenCouncilIsOff(bool council)
    {
        var original = Setup(council);
        Assert.Equal(0, original.Turn);
        Assert.Empty(original.Player(Side.FreePeoples).Hand);
        Assert.Null(LordsFixture.Views().For(original, Side.Shadow).Decision);
        Assert.Empty(LordsFixture.Views().For(original, Side.Shadow).Options);
        Assert.Equal(council ? 14 : 2, LordsFixture.Views().For(original, Side.FreePeoples).Options
            .Single(o => o.Kind == CommandKind.ChooseCompanionVersions).EligibleIds.Count);
        var grey = Choose(original, CharacterRules.Grey, CharacterRules.Strider);
        var keeper = Choose(original, LordsCharacters.Gandalf, CharacterRules.Strider);
        var view = LordsFixture.Views().For(keeper, Side.Shadow);
        Assert.Equal(JsonSerializer.Serialize(LordsFixture.Views().For(grey, Side.Shadow)), JsonSerializer.Serialize(view));
        Assert.DoesNotContain(LordsCharacters.Gandalf, JsonSerializer.Serialize(view));
        Assert.Empty(view.OwnCompanionVersions);
        Assert.Equal(CharacterRules.Strider, Assert.Single(view.Fellowship.RevealedCompanionVersions).Value);
        Assert.Equal(LordsCharacters.Gandalf, Assert.Single(LordsFixture.Views().For(keeper, Side.FreePeoples).OwnCompanionVersions));
        Assert.Equal(7, keeper.Fellowship.Companions.Count);
        Assert.Equal(DieLocation.Removed, LordsFixture.Die(keeper, LordsDiceCatalog.Narya).Location);
        Assert.Equal(1, keeper.Turn);
    }

    [Fact]
    public void InitialKeeperGuideIsPublicBeforeFirstRecoveryAndLaterGuideChangeDoesNotRerunRecovery()
    {
        var keeper = Choose(Setup(), LordsCharacters.Gandalf, CharacterRules.Grey);
        Assert.Equal(LordsCharacters.Gandalf, LordsFixture.Views().For(keeper, Side.Shadow)
            .Fellowship.RevealedCompanionVersions[CharacterRules.Grey]);
        Assert.Equal(DieLocation.Unrolled, LordsFixture.Die(keeper, LordsDiceCatalog.Narya).Location);
        keeper = LordsFixture.Do(keeper, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(DieLocation.Unrolled, LordsFixture.Die(keeper, LordsDiceCatalog.Narya).Location);
        Assert.Equal(2, keeper.Lords!.RevealedCompanionVersions.Count);

        var later = Choose(Setup(), LordsCharacters.Gandalf, CharacterRules.Strider);
        later = LordsFixture.Do(later, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Grey });
        Assert.Equal(DieLocation.Removed, LordsFixture.Die(later, LordsDiceCatalog.Narya).Location);
        Assert.Equal(LordsCharacters.Gandalf, later.Lords!.RevealedCompanionVersions[CharacterRules.Grey]);
        Assert.Equal(GamePhase.HuntAllocation, later.Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenGreyIsRevealedBySeparationOrElimination(bool casualty)
    {
        var state = Choose(Setup(), LordsCharacters.Gandalf, CharacterRules.Strider);
        if (casualty)
            HobbitDeparture.EliminateFromFellowship(state, CharacterRules.Grey);
        else
        {
            state.Phase = GamePhase.ActionResolution;
            foreach (var die in state.Dice.Where(d => d.Source == "standard"))
            {
                die.Location = DieLocation.Available;
                die.Face = DieFace.Character;
            }
            TurnFlow.SelectNext(state, Side.FreePeoples);
            var payload = CharacterFixture.Cost(state, DieFace.Character);
            payload.Ids = [CharacterRules.Grey];
            payload.Route = ["rivendell"];
            state = LordsFixture.Do(state, CommandKind.SeparateCompanions, payload);
        }
        Assert.Equal(LordsCharacters.Gandalf, LordsFixture.Views().For(state, Side.Shadow)
            .Fellowship.RevealedCompanionVersions[CharacterRules.Grey]);
        Assert.DoesNotContain(CharacterRules.Grey, state.Fellowship.Companions);
        Assert.Equal(casualty ? "casualties" : "rivendell", state.Characters.Single(c => c.Id == CharacterRules.Grey).Location);
        var restored = StateCodec.Read(StateCodec.Write(state));
        Assert.Equal(JsonSerializer.Serialize(LordsFixture.Views().For(state, Side.Shadow)), JsonSerializer.Serialize(LordsFixture.Views().For(restored, Side.Shadow)));
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("guide")]
    [InlineData("cost")]
    public void InvalidSetupSelectionDoesNotChangeStateOrDrawCards(string invalid)
    {
        var state = Setup();
        var payload = new CommandPayload { Ids = [LordsCharacters.Gandalf], GuideId = CharacterRules.Strider };
        switch (invalid)
        {
            case "duplicate": payload.Ids.Add(LordsCharacters.Gandalf); break;
            case "missing": payload.Ids.Clear(); break;
            case "foreign": payload.Ids = [LordsCharacters.WitchKing]; break;
            case "guide": payload.GuideId = LordsCharacters.Gandalf; break;
            case "cost": payload.DieId = state.Dice[0].Id; break;
        }
        LordsFixture.Reject(state, CommandKind.ChooseCompanionVersions, payload);
    }

    [Fact]
    public void SetupHasNoRandomCostPreviewDoesNotRevealSelectionAndRestartUsesExactChoice()
    {
        var state = Setup();
        var command = BattleFixture.Command(state, CommandKind.ChooseCompanionVersions,
            new() { Ids = [LordsCharacters.Gandalf], GuideId = CharacterRules.Strider });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Empty(preview.Costs);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.False(new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command).Legal);
        var random = new SequenceRandom();
        var direct = LordsFixture.Engine().Execute(state, Side.FreePeoples, command, random);
        var restored = LordsFixture.Engine().Execute(StateCodec.Read(before), Side.FreePeoples, command, random);
        Assert.True(direct.Receipt.Accepted);
        Assert.Equal(StateCodec.Write(direct.State), StateCodec.Write(restored.State));
        Assert.Equal(0, random.Calls);
        var wrongActor = LordsFixture.Engine().Execute(state, Side.Shadow, command, random);
        Assert.False(wrongActor.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(wrongActor.State));
        Assert.Throws<InvalidOperationException>(() => TurnFlow.Start(state));
    }
}
