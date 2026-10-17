using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsRulerRecruitmentTests
{
    internal static MatchState Ready(string sovereign, string? region = null)
    {
        var state = Actions();
        var definition = SovereignCatalog.All[sovereign];
        state.Nations.Single(n => n.Id == definition.Nation).StepsToWar = 0;
        SovereignLifecycle.Awaken(state, Sovereign(state, sovereign), region ?? definition.Home);
        Ruler(state, DieFace.Muster);
        return state;
    }

    internal static CommandPayload Recruit(MatchState state, string sovereign, params (UnitKind Kind, string Region)[] choices)
    {
        var option = KingsRulerRecruitment.Options(state, Side.FreePeoples).Single(o => o.CharacterId == sovereign);
        var used = new HashSet<string>();
        var placements = new List<UnitPlacementChoice>();
        foreach (var choice in choices)
        {
            var unit = state.Units.First(u => u.Location == "reserve" && u.Nation == SovereignCatalog.All[sovereign].Nation &&
                u.Kind == choice.Kind && !used.Contains(u.Id));
            used.Add(unit.Id);
            placements.Add(new() { UnitId = unit.Id, RegionId = choice.Region });
        }
        return new() { CharacterId = sovereign, AbilityId = option.AbilityId, DieId = option.Dice.Single(), Placements = placements };
    }

    [Theory]
    [InlineData(SovereignCatalog.Brand, "dale", "bree")]
    [InlineData(SovereignCatalog.Theoden, "edoras", "helms-deep")]
    public void CallRecruitsInTwoDifferentSettlementsAndMayAddTheLocalRecruitToOneOfThem(string id, string home, string other)
    {
        var state = Ready(id);
        var payload = Recruit(state, id, (UnitKind.Regular, home), (UnitKind.Regular, other), (UnitKind.Regular, home));
        var duplicateRegion = new CommandPayload { CharacterId = id, AbilityId = payload.AbilityId, DieId = payload.DieId,
            Placements = payload.Placements.Select(p => new UnitPlacementChoice { UnitId = p.UnitId, RegionId = home }).ToList() };
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, duplicateRegion);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload));
        Assert.True(preview.Legal);
        Assert.Equal(3, preview.Placements.Count);
        var option = KingsRulerRecruitment.Options(state, Side.FreePeoples).Single();
        Assert.Equal(3, option.TargetCount);
        Assert.Equal(home, option.RulerRecruitment!.LocalRegion);
        Assert.DoesNotContain(option.Recruitment, r => r.Kind == UnitKind.Elite);
        state = Do(StateCodec.Copy(state), CommandKind.UseCharacterAbility, payload);
        foreach (var placement in payload.Placements)
            Assert.Equal(placement.RegionId, state.Units.Single(u => u.Id == placement.UnitId).Location);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [Fact]
    public void DainCanRecruitAnEliteIntoSiegeButTheOtherRecruitMustUseADifferentFreeSettlement()
    {
        var state = Ready(SovereignCatalog.Dain);
        foreach (var unit in state.Units.Where(u => u.Location == "erebor"))
            unit.Garrison = true;
        state.Characters.Single(c => c.Id == SovereignCatalog.Dain).Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "erebor");
        var payload = Recruit(state, SovereignCatalog.Dain, (UnitKind.Elite, "erebor"), (UnitKind.Regular, "iron-hills"));
        var original = payload.Placements[1].RegionId;
        payload.Placements[1].RegionId = "erebor";
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
        payload.Placements[1].RegionId = original;
        var regular = payload.Placements[1].UnitId;
        payload.Placements[1].UnitId = state.Units.First(u => u.Location == "reserve" && u.Nation == "dwarves" && u.Kind == UnitKind.Elite && u.Id != payload.Placements[0].UnitId).Id;
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
        payload.Placements[1].UnitId = regular;
        state = Do(state, CommandKind.UseCharacterAbility, payload);
        Assert.True(state.Units.Single(u => u.Id == payload.Placements[0].UnitId).Garrison);
        Assert.False(state.Units.Single(u => u.Id == payload.Placements[1].UnitId).Garrison);
    }

    [Theory]
    [InlineData("standard-muster")]
    [InlineData("standard-will")]
    [InlineData("ruler-character")]
    [InlineData("peace")]
    public void RulerRecruitmentRejectsOrdinaryDiceWrongResultsAndPeace(string reason)
    {
        var state = Ready(SovereignCatalog.Brand);
        var payload = Recruit(state, SovereignCatalog.Brand,
            (UnitKind.Regular, "dale"), (UnitKind.Regular, "bree"), (UnitKind.Regular, "dale"));
        var die = state.Dice.Single(d => d.Id == payload.DieId);
        if (reason.StartsWith("standard"))
        {
            die.Source = "standard";
            die.Face = reason == "standard-will" ? DieFace.WillOfTheWest : DieFace.Muster;
        }
        if (reason == "ruler-character")
            die.Face = DieFace.Character;
        if (reason == "peace")
            state.Nations.Single(n => n.Id == "north").StepsToWar = 1;
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
    }

    [Fact]
    public void ShortReserveAndAConqueredHomeLeaveThePlayerAChoiceAmongRemainingSettlements()
    {
        var state = Ready(SovereignCatalog.Brand);
        var available = state.Units.First(u => u.Nation == "north" && u.Kind == UnitKind.Regular && u.Location == "reserve").Id;
        foreach (var unit in state.Units.Where(u => u.Nation == "north" && u.Location == "reserve" && u.Id != available))
            unit.Location = "casualties";
        state.Control["dale"] = Side.Shadow;
        var option = KingsRulerRecruitment.Options(state, Side.FreePeoples).Single();
        Assert.Equal(1, option.TargetCount);
        Assert.DoesNotContain("dale", option.Targets);
        Assert.Contains("bree", option.Targets);
        Assert.Contains("the-shire", option.Targets);
        var payload = Recruit(state, SovereignCatalog.Brand, (UnitKind.Regular, "bree"));
        state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.WillOfTheWest;
        state = Do(state, CommandKind.UseCharacterAbility, payload);
        Assert.Equal("bree", state.Units.Single(u => u.Id == available).Location);
    }

    [Fact]
    public void RecruitmentPreservesTheStackLimitDecisionAndItsResolvingCost()
    {
        var state = Ready(SovereignCatalog.Dain);
        foreach (var unit in state.Units.Where(u => u.Location == "erebor"))
            unit.Garrison = true;
        while (state.Units.Count(u => u.Location == "erebor" && u.Garrison && BoardRules.IsArmyUnit(u)) < 5)
            BattleFixture.Put(state, "north", UnitKind.Regular, "erebor").Garrison = true;
        state.Characters.Single(c => c.Id == SovereignCatalog.Dain).Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "erebor");
        var payload = Recruit(state, SovereignCatalog.Dain, (UnitKind.Elite, "erebor"), (UnitKind.Regular, "iron-hills"));
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload));
        Assert.Single(preview.Excess, e => e.Garrison && e.Count == 1);
        state = Do(state, CommandKind.UseCharacterAbility, payload);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }
}
