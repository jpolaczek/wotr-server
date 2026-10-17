using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CouncilSetupTests
{
    internal static MatchState Setup()
    {
        var state = KernelTests.NewGame();
        state.Configuration.Lords = true;
        state.Configuration.Council = true;
        TurnFlow.Start(state);
        return state;
    }

    internal static MatchState Versions(params string[] alternatives) => LordsFixture.Do(Setup(),
        CommandKind.ChooseCompanionVersions, new() { Ids = [CharacterRules.Grey, .. alternatives], GuideId = CharacterRules.Grey });

    internal static MatchState AllVersions() => Versions(CouncilCompanions.Versions.Skip(1).Select(v => v.AlternateId).ToArray());

    internal static List<UnitPlacementChoice> Homelands(MatchState state, int count) =>
        CouncilCompanions.CanStartOutside(state).Take(count).Select(c => new UnitPlacementChoice
        {
            UnitId = c.Id,
            RegionId = CouncilCompanions.Homelands(c).Single()
        }).ToList();

    [Fact]
    public void AllHiddenVersionsHaveIdenticalShadowPayloadAndAlwaysTheSameHomelandDecision()
    {
        var original = Versions();
        var alternate = AllVersions();
        Assert.Equal(DecisionKind.CouncilHomelands, original.Pending!.Kind);
        Assert.Equal(DecisionKind.CouncilHomelands, alternate.Pending!.Kind);
        Assert.Equal(JsonSerializer.Serialize(LordsFixture.Views().For(original, Side.Shadow)),
            JsonSerializer.Serialize(LordsFixture.Views().For(alternate, Side.Shadow)));
        Assert.Empty(LordsFixture.Views().For(original, Side.FreePeoples).Options.Single().EligibleIds);
        Assert.Equal(5, LordsFixture.Views().For(alternate, Side.FreePeoples).Options.Single().EligibleIds.Count);
        Assert.All(LordsFixture.Views().For(alternate, Side.FreePeoples).OwnCompanions.Where(c => c.Level == 1),
            c => Assert.StartsWith("lords.", c.DefinitionId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void HomelandsGrantExactlyZeroOneChosenOrBothTokensAndRevealOnlyDepartures(int count)
    {
        var state = AllVersions();
        state.Nations.Single(n => n.Id == "elves").Active = false;
        var placements = Homelands(state, count);
        state = LordsFixture.Do(state, CommandKind.ChooseCouncilHomelands, new() { Placements = placements });
        Assert.Equal(7 - count, state.Fellowship.Companions.Count);
        Assert.Equal(count + 1, state.Lords!.RevealedCompanionVersions.Count);
        Assert.Equal(count == 1 ? DecisionKind.CouncilToken : DecisionKind.FellowshipPhase, state.Pending!.Kind);
        Assert.Equal(count > 1 ? 2 : 0, state.Lords.Council!.GrantedTokens.Count);
        foreach (var placement in placements)
        {
            var character = state.Characters.Single(c => c.Id == placement.UnitId);
            Assert.Equal(placement.RegionId, character.Location);
            Assert.Equal(character.DefinitionId, state.Lords.RevealedCompanionVersions[character.Id]);
        }
        Assert.Equal(count >= 3, state.Nations.Single(n => n.Id == "elves").Active);
        Assert.False(state.Nations.Single(n => n.Id == "gondor").Active);
        Assert.False(state.Nations.Single(n => n.Id == "dwarves").Active);
        Assert.False(state.Nations.Single(n => n.Id == "north").Active);
    }

    [Theory]
    [InlineData(CouncilTokens.Politics)]
    [InlineData(CouncilTokens.Travel)]
    public void ShadowChoosesEitherTokenAfterOneDepartureAndRestart(string token)
    {
        var state = AllVersions();
        var command = BattleFixture.Command(state, CommandKind.ChooseCouncilHomelands, new() { Placements = Homelands(state, 1) });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Single(preview.Placements);
        Assert.Empty(preview.Costs);
        Assert.Equal(before, StateCodec.Write(state));
        state = LordsFixture.Do(StateCodec.Read(before), command.Kind, command.Payload);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Empty(LordsFixture.Views().For(state, Side.FreePeoples).Options);
        state = LordsFixture.Do(StateCodec.Read(StateCodec.Write(state)), CommandKind.ChooseCouncilToken, new() { Ids = [token] });
        Assert.Equal(token, Assert.Single(state.Lords!.Council!.GrantedTokens));
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
    }

    [Theory]
    [InlineData("base")]
    [InlineData("guide")]
    [InlineData("duplicate")]
    [InlineData("wrong-region")]
    [InlineData("die")]
    public void IllegalHomelandsAreRejectedWithoutMutation(string invalid)
    {
        var state = AllVersions();
        var p = new CommandPayload { Placements = Homelands(state, 1) };
        switch (invalid)
        {
            case "base": state.Characters.Single(c => c.Id == p.Placements[0].UnitId).DefinitionId = p.Placements[0].UnitId; break;
            case "guide": p.Placements[0].UnitId = CharacterRules.Strider; break;
            case "duplicate": p.Placements.Add(p.Placements[0]); break;
            case "wrong-region": p.Placements[0].RegionId = "orthanc"; break;
            case "die": p.DieId = state.Dice[0].Id; break;
        }
        LordsFixture.Reject(state, CommandKind.ChooseCouncilHomelands, p);
    }

    [Fact]
    public void DuplicateVersionsAndCouncilAlternatesWithoutVariantAreRejected()
    {
        var p = new CommandPayload { Ids = [CharacterRules.Grey, CouncilCompanions.Strider, CharacterRules.Strider], GuideId = CharacterRules.Grey };
        LordsFixture.Reject(Setup(), CommandKind.ChooseCompanionVersions, p);
        p.Ids.Remove(CharacterRules.Strider);
        var state = Setup();
        state.Configuration.Council = false;
        LordsFixture.Reject(state, CommandKind.ChooseCompanionVersions, p);
    }
}
