using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsChieftainEntryTests
{
    [Theory]
    [InlineData(KingsChieftains.Serpent, "gondor", "southrons", "near-harad", 2, 2)]
    [InlineData(KingsChieftains.Mirkwood, "north", "sauron", "barad-dur", 3, 1)]
    [InlineData(KingsChieftains.Ugluk, "rohan", "isengard", "orthanc", 2, 1)]
    public void EntryRequiresItsPrintedEnemyWarAndMatchingArmyAndNeverEnablesWhite(string id, string enemy, string nation,
        string region, int level, int leadership)
    {
        var state = Actions(Side.Shadow);
        BattleFixture.Put(state, nation, UnitKind.Regular, region);
        var payload = new CommandPayload { CharacterId = id, RegionId = region, DieId = Die(state, Side.Shadow).Id };
        state.Nations.Single(n => n.Id == nation).StepsToWar = 0;
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        state.Nations.Single(n => n.Id == enemy).StepsToWar = 0;
        payload.RegionId = "rivendell";
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        payload.RegionId = region;
        var option = new CharacterOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.EnterCharacter);
        Assert.Contains(option.CharacterActions, c => c.CharacterId == id && c.Targets.Contains(region));
        state = Do(state, CommandKind.EnterCharacter, payload);
        var character = state.Characters.Single(c => c.Id == id);
        Assert.Equal(level, character.Level);
        Assert.Equal(leadership, character.Leadership);
        Assert.Equal(nation, character.NationIcon);
        Assert.False(state.MinionEverEntered);
        Assert.Equal(0, state.Player(Side.Shadow).ExtraDiceNextRecovery);
        Assert.Equal(DieLocation.Removed, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Equal("Minion", CharacterCatalog.All[id].Role);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == id);
        CharacterLifecycle.Eliminate(state, character);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), c => c.DefinitionId == id);
        var grey = CharacterCatalog.Companion(state, CharacterRules.Grey);
        grey.Location = "casualties";
        state.Characters.Add(grey);
        state.Fellowship.Companions.Remove(CharacterRules.Grey);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.FreePeoples), c => c.DefinitionId == CharacterRules.White);
    }

    [Theory]
    [InlineData("dwarves", true)]
    [InlineData("elves", true)]
    [InlineData("north", true)]
    [InlineData("rohan", false)]
    [InlineData("gondor", false)]
    public void MirkwoodRequiresOneOfTheThreeNorthernNations(string nation, bool allowed)
    {
        var state = Actions(Side.Shadow);
        state.Nations.Single(n => n.Id == nation).StepsToWar = 0;
        Assert.Equal(allowed, CharacterEntry.Candidates(state, Side.Shadow).Any(p => p.DefinitionId == KingsChieftains.Mirkwood));
    }

    [Theory]
    [InlineData(DieFace.Character, true)]
    [InlineData(DieFace.Army, true)]
    [InlineData(DieFace.ArmyMuster, true)]
    [InlineData(DieFace.Muster, true)]
    [InlineData(DieFace.Event, true)]
    [InlineData(DieFace.Eye, false)]
    public void AnyAvailableRulerResultExceptEyeCanEnterAChieftain(DieFace face, bool legal)
    {
        var state = Actions(Side.Shadow);
        BattleFixture.Put(state, "southrons", UnitKind.Regular, "near-harad");
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 0;
        var die = KingsDice.Ruler(state, Side.Shadow)!;
        die.Location = DieLocation.Available;
        die.Face = face;
        var payload = new CommandPayload { CharacterId = KingsChieftains.Serpent, RegionId = "near-harad", DieId = die.Id };
        if (!legal)
        {
            LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
            return;
        }
        state = Do(state, CommandKind.EnterCharacter, payload);
        Assert.Equal(DieLocation.Spent, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Contains(state.Characters, c => c.Id == KingsChieftains.Serpent && c.Location == "near-harad");
    }

    [Fact]
    public void UgluksFreeFellowshipLocationsRequireRevealEvenWhenRohanIsAtWar()
    {
        var state = Actions(Side.Shadow);
        state.Fellowship.RegionId = "trollshaws";
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 0;
        Assert.DoesNotContain("trollshaws", KingsChieftains.Regions(state, KingsChieftains.Ugluk));
        Assert.Contains("orthanc", KingsChieftains.Regions(state, KingsChieftains.Ugluk));
        state.Fellowship.Hidden = false;
        Assert.Contains("trollshaws", KingsChieftains.Regions(state, KingsChieftains.Ugluk));
        Assert.Contains("hollin", KingsChieftains.Regions(state, KingsChieftains.Ugluk));
        Assert.DoesNotContain("rivendell", KingsChieftains.Regions(state, KingsChieftains.Ugluk));
        BattleFixture.Put(state, "north", UnitKind.Regular, "hollin");
        Assert.DoesNotContain("hollin", KingsChieftains.Regions(state, KingsChieftains.Ugluk));
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = 1;
        state = Do(state, CommandKind.EnterCharacter, new()
        {
            CharacterId = KingsChieftains.Ugluk, RegionId = "trollshaws", DieId = Die(state, Side.Shadow).Id
        });
        Assert.Equal("trollshaws", state.Characters.Single(c => c.Id == KingsChieftains.Ugluk).Location);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void FellowshipOnMordorTrackOffersOnlyIsengardArmyEntry(bool hidden, bool rohanAtWar, bool allowed)
    {
        var state = Actions(Side.Shadow);
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 1;
        state.Fellowship.Hidden = hidden;
        state.Nations.Single(n => n.Id == "rohan").StepsToWar = rohanAtWar ? 0 : 1;
        foreach (var unit in state.Units.Where(u => u.Nation == "isengard"))
            unit.Location = "reserve";
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");

        var views = new Projection(new CharacterOptions());
        Assert.Empty(views.For(state, Side.FreePeoples).Options);
        var entry = views.For(state, Side.Shadow).Options.Single(o => o.Kind == CommandKind.EnterCharacter);
        var ugluk = entry.CharacterActions.SingleOrDefault(c => c.CharacterId == KingsChieftains.Ugluk);
        Assert.Equal(allowed, ugluk is not null);
        if (ugluk is not null)
            Assert.Equal(new[] { "orthanc" }, ugluk.Targets);
        foreach (var region in new[] { MordorRules.Track, "gorgoroth", "morannon" })
            LordsFixture.Reject(state, CommandKind.EnterCharacter, new()
            {
                CharacterId = KingsChieftains.Ugluk, RegionId = region, DieId = Die(state, Side.Shadow).Id
            });
    }

    [PostgresFact]
    public async Task RevealingFellowshipInMordorRestoresBothViewsAndDoesNotRepeatDamage()
    {
        var state = Actions();
        state.Fellowship.RegionId = MordorRules.Track;
        state.Fellowship.MordorStep = 0;
        state.Fellowship.Corruption = 3;
        Pool(state, "base.hunt.standard.2.1.1");
        var die = Die(state);
        die.Face = DieFace.Character;
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = die.Id }, 0);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        var payload = new CommandPayload { HuntResponse = HuntResponse.Ring };
        var resolved = Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, payload);
        Assert.False(resolved.Fellowship.Hidden);
        Assert.Equal(1, resolved.Fellowship.MordorStep);
        Assert.Equal(5, resolved.Fellowship.Corruption);
        Assert.Equal(Side.Shadow, resolved.Pending!.Owner);
        await LordsPersistenceTests.Restart(state, CommandKind.ResolveHuntDamage, payload);
    }

    [Theory]
    [InlineData(KingsChieftains.Serpent, 2)]
    [InlineData(KingsChieftains.Mirkwood, 3)]
    [InlineData(KingsChieftains.Ugluk, 2)]
    public void ChieftainsUseNonFlyingRoutesAndRespectBothKindsOfStronghold(string id, int range)
    {
        var state = Actions(Side.Shadow);
        var chieftain = KingsDiceTests.Chieftain(state, id, "dimrill-dale");
        var option = ShadowTravel.Options(state).Single(o => o.Id == id);
        Assert.False(option.Flight);
        Assert.Equal(range, option.Range);
        CompanionMoveChoice move = new() { Ids = [id], Route = ["dimrill-dale", "lorien"] };
        Assert.Null(ShadowTravel.Plan(state, [move]));
        foreach (var unit in state.Units.Where(u => u.Location == "lorien"))
            unit.Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        Assert.NotNull(ShadowTravel.Plan(state, [move]));
        move.Route = ["dimrill-dale", "barad-dur"];
        Assert.Null(ShadowTravel.Plan(state, [move]));
        chieftain.Location = "morannon";
        chieftain.Garrison = true;
        foreach (var unit in state.Units.Where(u => u.Location == "morannon"))
            unit.Garrison = true;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "morannon");
        move.Route = ["morannon", "dagorlad"];
        Assert.Null(ShadowTravel.Plan(state, [move]));
    }

    [PostgresFact]
    public Task EntryRestartsWithOneFigureAndNoRepeatedCostOrStandardDie()
    {
        var state = Actions(Side.Shadow);
        BattleFixture.Put(state, "southrons", UnitKind.Regular, "near-harad");
        state.Nations.Single(n => n.Id == "gondor").StepsToWar = 0;
        return LordsPersistenceTests.Restart(state, CommandKind.EnterCharacter, new()
        {
            CharacterId = KingsChieftains.Serpent, RegionId = "near-harad", DieId = Die(state, Side.Shadow).Id
        });
    }
}
