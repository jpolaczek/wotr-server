using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCharacterFixture;

namespace Wotr.Tests;

public class LordsGothmogTests
{
    [Fact]
    public void MouthCannotUseGothmogsRecruitmentAbility()
    {
        var state = State(Side.Shadow, emptyBoard: true);
        var mouth = Put(state, LordsCharacters.Mouth, "gap-of-rohan", CharacterRules.Mouth);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, mouth.Location);
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.CharacterId = mouth.Id;
        payload.AbilityId = LordsCharacterAbilities.GothmogRecruit;
        payload.Ids = [mouth.Location];
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
        Assert.DoesNotContain(LordsCharacterAbilities.Options(state, Side.Shadow), o => o.AbilityId == payload.AbilityId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FlingRecruitsOrUpgradesSauronWithAnyShadowArmyWithoutNationalSettlement(bool upgrade)
    {
        var state = State(Side.Shadow, emptyBoard: true);
        var gothmog = Put(state, LordsCharacters.Gothmog, "gap-of-rohan");
        var regular = BattleFixture.Put(state, upgrade ? "sauron" : "isengard", UnitKind.Regular, gothmog.Location);
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.CharacterId = gothmog.Id;
        payload.AbilityId = upgrade ? LordsCharacterAbilities.GothmogUpgrade : LordsCharacterAbilities.GothmogRecruit;
        payload.Ids = [gothmog.Location];
        var options = new CharacterOptions().GetOptions(state, Side.Shadow).Single(o => o.Kind == CommandKind.UseCharacterAbility);
        Assert.Contains(options.CharacterActions, o => o.AbilityId == payload.AbilityId);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload));
        var placed = Assert.Single(preview.Placements).UnitId;
        state = LordsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        var unit = state.Units.Single(u => u.Id == placed);
        Assert.Equal("sauron", unit.Nation);
        Assert.Equal(upgrade ? UnitKind.Elite : UnitKind.Regular, unit.Kind);
        Assert.Equal(gothmog.Location, unit.Location);
        Assert.Equal(upgrade ? "reserve" : gothmog.Location, state.Units.Single(u => u.Id == regular.Id).Location);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [Fact]
    public void FlingWorksForBesiegersButNotFromOwnBesiegedStrongholdOrWithoutArmyAndReserve()
    {
        var state = State(Side.Shadow, emptyBoard: true);
        var gothmog = Put(state, LordsCharacters.Gothmog, "lorien");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien").Garrison = true;
        Assert.Empty(LordsCharacterAbilities.Options(state, Side.Shadow));
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        Assert.Contains(LordsCharacterAbilities.Options(state, Side.Shadow), o => o.AbilityId == LordsCharacterAbilities.GothmogRecruit);
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve"))
            unit.Location = "morannon";
        Assert.DoesNotContain(LordsCharacterAbilities.Options(state, Side.Shadow), o => o.AbilityId == LordsCharacterAbilities.GothmogRecruit);
        gothmog.Location = "minas-morgul";
        gothmog.Garrison = true;
        BattleFixture.Put(state, "isengard", UnitKind.Regular, "minas-morgul").Garrison = true;
        BattleFixture.Put(state, "elves", UnitKind.Regular, "minas-morgul");
        Assert.Empty(LordsCharacterAbilities.Options(state, Side.Shadow));
    }
}
