using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsCharacterFixture;

namespace Wotr.Tests;

public class LordsShadowTravelTests
{
    [Theory]
    [InlineData(LordsCharacters.Gothmog, 3)]
    [InlineData(LordsCharacters.Mouth, 3)]
    [InlineData(LordsCharacters.Balrog, 2)]
    public void NonFlyingMinionsUseEdgesAndRangeIgnoreEnemyArmiesAndCannotCrossOwnSiege(string id, int range)
    {
        var state = State(Side.Shadow, emptyBoard: true);
        var minion = Put(state, id, "minas-morgul");
        minion.Level = range;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "south-ithilien");
        var route = new[] { "minas-morgul", "south-ithilien", "osgiliath", "north-ithilien", "morannon" };
        var move = new CompanionMoveChoice { Ids = [minion.Id], Route = route.Take(range + 1).ToList() };
        Assert.NotNull(ShadowTravel.Plan(state, [move]));
        move.Route = route.Take(range + 2).ToList();
        Assert.Null(ShadowTravel.Plan(state, [move]));
        move.Route = ["minas-morgul", "rivendell"];
        Assert.Null(ShadowTravel.Plan(state, [move]));
        move.Route = ["minas-morgul", "south-ithilien"];
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-morgul");
        Assert.Null(ShadowTravel.Plan(state, [move]));
        Assert.False(ShadowTravel.Options(state).Single(t => t.Id == minion.Id).Flight);
    }

    [Fact]
    public void ChiefFliesAndCountsAsNazgulForHuntCardsAndWhiteRiderButDoesNotInheritSorcerer()
    {
        var state = State(Side.Shadow, emptyBoard: true);
        var chief = Put(state, LordsCharacters.WitchKing, "barad-dur", CharacterRules.WitchKing);
        state.Fellowship.RegionId = "barad-dur";
        Assert.True(NazgulEventTravel.WithFellowship(state));
        Assert.True(new MinionPresentCondition(CharacterRules.WitchKing).CanPlay(state, Side.Shadow));
        Assert.True(ShadowTravel.Options(state, nazgulOnly: true).Single(t => t.Id == chief.Id).Flight);
        var move = new CompanionMoveChoice { Ids = [chief.Id], Route = ["barad-dur", "rivendell"] };
        Assert.Null(ShadowTravel.Plan(state, [move], nazgulOnly: true));
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "rivendell");
        Assert.NotNull(ShadowTravel.Plan(state, [move], nazgulOnly: true));
        var battle = new BattleState
        {
            Region = "barad-dur",
            Attacker = new() { Side = Side.Shadow, Region = "barad-dur", Participants = [chief.Id] },
            Defender = new() { Side = Side.FreePeoples, Region = "barad-dur" }
        };
        BattleRuntime.Begin(state, battle);
        Assert.Equal(1, BattleLeadership.Nazgul(state, battle.Attacker)[chief.Id]);
        Assert.True(new NazgulInBattle().CanPlay(state, battle, Side.Shadow));
        Assert.False(CharacterRules.Has(chief, "draw-after-first-round-combat-card"));
        battle.WhiteRiderUsed = true;
        Assert.Equal(0, BattleLeadership.Total(state, battle.Attacker));
        Assert.Empty(BattleLeadership.Nazgul(state, battle.Attacker));
        Assert.Equal(CharacterCatalog.Lords[LordsCharacters.WitchKing].Leadership, chief.Leadership);
    }
}
