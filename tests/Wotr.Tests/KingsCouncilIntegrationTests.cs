using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsIntegrationFixture;

namespace Wotr.Tests;

public class KingsCouncilIntegrationTests
{
    [Theory]
    [InlineData(KingsChieftains.Serpent, "near-harad", CouncilTokens.Politics)]
    [InlineData(KingsChieftains.Serpent, "near-harad", CouncilTokens.Travel)]
    [InlineData(KingsChieftains.Mirkwood, "barad-dur", CouncilTokens.Politics)]
    [InlineData(KingsChieftains.Mirkwood, "barad-dur", CouncilTokens.Travel)]
    [InlineData(KingsChieftains.Ugluk, "orthanc", CouncilTokens.Politics)]
    [InlineData(KingsChieftains.Ugluk, "orthanc", CouncilTokens.Travel)]
    public void V13TokenAndEligibleDieBypassOnlyTheChieftainPrerequisite(string id, string region, string token)
    {
        var state = Actions();
        Assert.False(KingsChieftains.Prerequisite(state, id));
        var p = Payload(state, id, region, token);
        var command = BattleFixture.Command(state, CommandKind.EnterCharacter, p);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Equal(new[] { p.DieId, token }, preview.Costs);
        Assert.Contains(preview.Placements, x => x.UnitId == id && x.RegionId == region);
        Assert.Equal(before, StateCodec.Write(state));
        var option = new CharacterOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.EnterCharacter)
            .CharacterActions.Single(c => c.CharacterId == id);
        Assert.True(option.RequiresCouncilToken);
        Assert.Contains(token, option.CouncilTokens);
        Assert.Contains(region, option.Targets);
        state = KingsFixture.Do(StateCodec.Copy(state), command.Kind, p);
        Assert.Equal(DieLocation.Spent, LordsFixture.Die(state, p.DieId).Location);
        Assert.Contains(token, state.Lords!.Council!.SpentTokens);
        Assert.Equal(state.Turn, state.Lords.Council.LastTokenTurn);
        Assert.Equal(region, state.Characters.Single(c => c.Id == id).Location);
        Assert.False(state.MinionEverEntered);
        Assert.Equal(DieLocation.Removed, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Empty(KingsCouncilEntry.Tokens(state));
        Assert.Empty(KingsCouncilEntry.Candidates(state, Side.Shadow));
    }

    [Theory]
    [InlineData("missing-token")]
    [InlineData("spent-token")]
    [InlineData("used-this-turn")]
    [InlineData("wrong-region")]
    [InlineData("no-die")]
    [InlineData("character-die")]
    [InlineData("wrong-character")]
    [InlineData("already-entered")]
    [InlineData("council-disabled")]
    [InlineData("wrong-command")]
    public void InvalidEntryLeavesBothResourcesIntact(string reason)
    {
        var state = Actions();
        var p = Payload(state);
        var kind = CommandKind.EnterCharacter;
        switch (reason)
        {
            case "missing-token": p.CouncilTokenId = ""; break;
            case "spent-token": state.Lords!.Council!.SpentTokens.Add(p.CouncilTokenId); break;
            case "used-this-turn": state.Lords!.Council!.LastTokenTurn = state.Turn; break;
            case "wrong-region": p.RegionId = "rivendell"; break;
            case "no-die": p.DieId = ""; break;
            case "character-die": LordsFixture.Die(state, p.DieId).Face = DieFace.Character; break;
            case "wrong-character": p.CharacterId = CharacterRules.Mouth; break;
            case "already-entered": KingsDiceTests.Chieftain(state); break;
            case "council-disabled": state.Configuration.Council = false; break;
            case "wrong-command": kind = CommandKind.DiscardDie; break;
        }
        LordsFixture.Reject(state, kind, p);
    }

    [Fact]
    public void CouncilTokenCanAccompanyARulerButCannotReplaceTheEligibleDie()
    {
        var state = Actions();
        var ruler = KingsDice.Ruler(state, Side.Shadow)!;
        ruler.Location = DieLocation.Available;
        ruler.Face = DieFace.Character;
        var payload = Payload(state);
        payload.DieId = ruler.Id;
        state = KingsFixture.Do(state, CommandKind.EnterCharacter, payload);
        Assert.Equal(DieLocation.Spent, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Single(state.Lords!.Council!.SpentTokens);
    }

    [Theory]
    [InlineData("boromir", SovereignCatalog.Denethor, "minas-tirith")]
    [InlineData("gimli", SovereignCatalog.Dain, "erebor")]
    [InlineData("merry", SovereignCatalog.Brand, "dale")]
    [InlineData("pippin", SovereignCatalog.Brand, "dale")]
    public void V14CompanionWithoutNationIconCannotRemoveCorruptionOrCorruptedSovereign(string identity, string target, string region)
    {
        var state = Actions();
        var definition = CharacterCatalog.Lords["lords.character." + identity];
        var companion = new CharacterState { Id = "base.character." + identity, Location = region };
        CharacterLifecycle.ApplyVersion(companion, definition);
        state.Characters.Add(companion);
        state.Fellowship.Companions.Remove(companion.Id);
        var sovereign = KingsFixture.Sovereign(state, target);
        Assert.Empty(companion.NationIcon);
        Assert.False(SovereignAwakening.AppropriateCompanion(state, sovereign));
        CorruptionRules.Attach(state, sovereign, KingsCorruptionTests.One);
        KingsPolitics.Record(state, SovereignCatalog.All[target].Nation);
        Assert.Empty(state.Kings!.PendingCorruptionRemoval);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = region;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var die = KingsFixture.Die(state);
        die.Face = DieFace.Character;
        LordsFixture.Reject(state, CommandKind.RemoveCorruptedSovereign, new() { CharacterId = target, DieId = die.Id });
        companion.NationIcon = "free-peoples";
        Assert.True(SovereignAwakening.CanRemove(state, target));
    }

    internal static CommandPayload Payload(MatchState state, string id = KingsChieftains.Serpent,
        string region = "near-harad", string token = CouncilTokens.Politics) => new()
    {
        CharacterId = id, RegionId = region, CouncilTokenId = token,
        DieId = KingsFixture.Die(state, Side.Shadow).Id
    };
}
