using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CharacterTravelTests
{
    [Fact]
    public void NazgulAndWitchKingFlyAcrossMapAndSiegeButCannotEnterUnbesiegedFreeStronghold()
    {
        var state = BattleFixture.State(Side.Shadow);
        var wk = Put(state, CharacterRules.WitchKing, "barad-dur");
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "barad-dur");
        var garrison = BattleFixture.Put(state, "sauron", UnitKind.Regular, "barad-dur");
        garrison.Garrison = true;
        wk.Garrison = true;
        nazgul.Garrison = true;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "barad-dur");
        var move = new CompanionMoveChoice { Ids = [wk.Id, nazgul.Id], Route = ["barad-dur", "rivendell"] };
        Assert.Null(ShadowTravel.Plan(state, [move]));
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "rivendell");
        Assert.NotNull(ShadowTravel.Plan(state, [move]));
        var p = Cost(state, DieFace.Character);
        p.CompanionMoves = [move];
        state = Do(state, CommandKind.MoveShadowCharacters, p);
        Assert.Equal("rivendell", state.Characters.Single().Location);
        Assert.False(state.Characters.Single().Garrison);
        Assert.Equal("rivendell", state.Units.Single(u => u.Id == nazgul.Id).Location);
    }

    [Fact]
    public void MouthWalksThreeEdgesThroughEnemyArmiesButCannotCrossOwnSiegeAndSarumanNeverMoves()
    {
        var state = BattleFixture.State(Side.Shadow);
        var mouth = Put(state, CharacterRules.Mouth, "minas-morgul");
        var saruman = Put(state, CharacterRules.Saruman, "orthanc");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "south-ithilien");
        var route = new List<string> { "minas-morgul", "south-ithilien", "osgiliath", "north-ithilien" };
        Assert.NotNull(ShadowTravel.Plan(state, [new() { Ids = [mouth.Id], Route = route }]));
        route.Add("morannon");
        Assert.Null(ShadowTravel.Plan(state, [new() { Ids = [mouth.Id], Route = route }]));
        Assert.Null(ShadowTravel.Plan(state, [new() { Ids = [mouth.Id], Route = ["minas-morgul", "rivendell"] }]));
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-morgul");
        Assert.Null(ShadowTravel.Plan(state, [new() { Ids = [mouth.Id], Route = ["minas-morgul", "south-ithilien"] }]));
        Assert.DoesNotContain(ShadowTravel.Options(state), t => t.Id == saruman.Id);
        Assert.False(CharacterRules.Mobile(saruman));
    }

    [Fact]
    public void WitchKingTravelsWithArmyAndSarumanCanAttackButCannotAdvanceOrMoveWithArmy()
    {
        var state = BattleFixture.State(Side.Shadow);
        var saruman = Put(state, CharacterRules.Saruman, "orthanc");
        var wk = Put(state, CharacterRules.WitchKing, "orthanc");
        var unit = BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        var p = Cost(state, DieFace.Character);
        p.MovementCost = DieFace.Character;
        p.Moves = [new() { From = "orthanc", To = "gap-of-rohan", UnitIds = [unit.Id], CharacterIds = [wk.Id] }];
        Assert.NotNull(MovementRules.Plan(state, Side.Shadow, p));
        p.Moves[0].CharacterIds = [saruman.Id];
        Assert.Null(MovementRules.Plan(state, Side.Shadow, p));
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "gap-of-rohan");
        p.Moves[0].CharacterIds = [saruman.Id, wk.Id];
        var battle = BattleRules.PaidAttack(state, Side.Shadow, p)!;
        Assert.NotNull(battle);
        Assert.False(BattleMovement.CanAdvance(state, battle, [unit.Id, saruman.Id]));
        Assert.True(BattleMovement.CanAdvance(state, battle, [unit.Id, wk.Id]));
    }
}
