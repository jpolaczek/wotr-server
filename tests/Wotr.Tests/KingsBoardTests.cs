using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsBoardTests
{
    internal static MatchState Captured()
    {
        var state = Actions(Side.Shadow);
        var sovereign = Sovereign(state, SovereignCatalog.Theoden);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = "edoras";
        state.Units.RemoveAll(u => u.Id == sovereign.Id);
        foreach (var unit in state.Units.Where(u => u.Location == KingsBoard.HelmsDeep))
            BoardRules.ReturnToReserve(unit);
        state.Control[KingsBoard.HelmsDeep] = Side.Shadow;
        return state;
    }

    [Fact]
    public void WhiteHandEnablesIsengardRecruitmentVoiceAndAdditionalHuntWhilePreservingVictoryPoints()
    {
        var state = Captured();
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 0;
        var region = BoardRules.Region(state, KingsBoard.HelmsDeep);
        Assert.Equal("isengard", region.Nation);
        Assert.Equal(Side.Shadow, region.OriginalOwner);
        Assert.Equal("rohan", BoardRules.Region(state, "edoras").Nation);
        Assert.Equal("rohan", BoardRules.Regions[KingsBoard.HelmsDeep].Nation);
        var regular = state.Units.First(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        Assert.True(RecruitmentRules.CanPlaceMuster(state, Side.Shadow, regular, region.Id));
        Assert.Contains(new MusterOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.Recruit).Recruitment,
            r => r.Nation == "isengard" && r.Targets.Contains(region.Id));
        var saruman = new CharacterState { Id = CharacterRules.Saruman, Location = "orthanc" };
        CharacterLifecycle.ApplyVersion(saruman, CharacterCatalog.All[CharacterRules.Saruman]);
        state.Characters.Add(saruman);
        Assert.Contains(region.Id, CharacterAbilities.Options(state, Side.Shadow).Single(o => o.AbilityId == CharacterAbilities.VoiceRecruit).Targets);
        Assert.Equal(new[] { region.Id }, TravelRules.AdditionalHunts(state, [region.Id]));
        Assert.Equal(2, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Equal(0, MilitaryVictory.Points(state, Side.FreePeoples));
        Assert.Single(LordsFixture.Views().For(state, Side.FreePeoples).RegionOverrides);
        state = Do(state, CommandKind.Recruit, new() { DieId = Die(state, Side.Shadow).Id,
            Placements = [new() { UnitId = regular.Id, RegionId = region.Id }] });
        Assert.Equal(region.Id, state.Units.Single(u => u.Id == regular.Id).Location);
        SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Theoden));
        Assert.Equal("rohan", BoardRules.Region(state, region.Id).Nation);
        Assert.Empty(TravelRules.AdditionalHunts(state, [region.Id]));
        Assert.Equal(2, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Empty(LordsFixture.Views().For(state, Side.Shadow).RegionOverrides);
    }

    [Fact]
    public void MovementPreviewUsesTheOverlayWithoutMutatingItsSourceAndRecaptureRestoresRohan()
    {
        var state = Captured();
        var regular = BattleFixture.Put(state, "isengard", UnitKind.Regular, "fords-of-isen");
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 1;
        var cost = Die(state, Side.Shadow);
        cost.Face = DieFace.Army;
        var command = BattleFixture.Command(state, CommandKind.MoveArmies, new()
        {
            DieId = cost.Id, MovementCost = DieFace.Army,
            Moves = [new() { From = regular.Location, To = KingsBoard.HelmsDeep, UnitIds = [regular.Id] }]
        });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, command.Kind, command.Payload);
        Assert.Equal(KingsBoard.HelmsDeep, state.Units.Single(u => u.Id == regular.Id).Location);
        BoardRules.ReturnToReserve(state.Units.Single(u => u.Id == regular.Id));
        TerritoryRules.Capture(state, Side.FreePeoples, KingsBoard.HelmsDeep);
        Assert.Equal("rohan", BoardRules.Region(state, KingsBoard.HelmsDeep).Nation);
        Assert.Equal(0, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Equal(0, MilitaryVictory.Points(state, Side.FreePeoples));
        Assert.Contains(state.PoliticalReactions, r => r.Trigger == PoliticalTrigger.SettlementCapture && r.NationId == "isengard");
    }
}
