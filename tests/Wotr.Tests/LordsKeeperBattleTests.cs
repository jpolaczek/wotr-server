using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCharacterFixture;

namespace Wotr.Tests;

public class LordsKeeperBattleTests
{
    [Fact]
    public void HouseholdGivesElvenElitesCombatLeadershipAndCombatCardTargetsButNoMovementCommand()
    {
        var state = State(emptyBoard: true);
        var elrond = Put(state, LordsCharacters.Elrond, "rivendell");
        var elite = BattleFixture.Put(state, "elves", UnitKind.Elite, "rivendell");
        var force = new BattleForce { Side = Side.FreePeoples, Region = "rivendell", Participants = [elrond.Id, elite.Id] };
        Assert.Equal(3, BattleLeadership.Total(state, force));
        Assert.Contains(CombatCardFigures.Leaders(state, force), u => u.Id == elite.Id);
        Assert.False(CharacterRules.Leads(state, elite));
        force.Round.CancelledAbilities.Add(elrond.Id);
        Assert.Equal(2, BattleLeadership.Total(state, force));
        force.Round.CancelledAbilities.Clear();
        BoardRules.ReturnToReserve(elite);
        var regular = BattleFixture.Put(state, "elves", UnitKind.Regular, "rivendell");
        force.Participants.Add(regular.Id);
        Assert.Equal(2, BattleLeadership.Total(state, force));
        Assert.Empty(CombatCardFigures.Leaders(state, force));
    }

    [Theory]
    [InlineData(LordsCharacters.Elrond)]
    [InlineData(LordsCharacters.Galadriel)]
    public void KeeperHasCombatCardLevelThreeForHeroicDeathAndBlackBreath(string id)
    {
        var state = State(emptyBoard: true);
        var keeper = Put(state, id, "lorien");
        var battle = new BattleState
        {
            Region = "lorien",
            Attacker = new() { Side = Side.Shadow, Region = "lorien" },
            Defender = new() { Side = Side.FreePeoples, Region = "lorien", Participants = [keeper.Id] }
        };
        BattleRuntime.Begin(state, battle);
        for (var i = 0; i < 3; i++)
            battle.Attacker.Round.Dice.Add(new() { Id = "hit-" + i, RerollHit = i < 2, ConvertedHit = i == 2 });
        var heroic = new HeroicDeathStep();
        var fp = new EffectExecution { Owner = Side.FreePeoples };
        Assert.True(heroic.ValidSelection(state, fp, [keeper.Id, HeroicDeathStep.HitPrefix + "3"]));
        var breath = new BlackBreathStep();
        var sh = new EffectExecution { Owner = Side.Shadow };
        Assert.DoesNotContain(keeper.Id, breath.Candidates(state, sh));
        battle.Attacker.Round.Dice[2].RerollHit = true;
        Assert.Contains(keeper.Id, breath.Candidates(state, sh));
        Assert.Equal(0, keeper.Level);
    }

    [Fact]
    public void GaladrielRecruitsInsideSiegeWithRealCostWarReservesAndGarrisonLimit()
    {
        var state = State(emptyBoard: true);
        Put(state, LordsCharacters.Galadriel, "lorien").Garrison = true;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        for (var i = 0; i < 5; i++)
            BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien").Garrison = true;
        var reserve = state.Units.First(u => u.Nation == "elves" && u.Kind == UnitKind.Elite && u.Location == "reserve");
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.Placements = [new() { UnitId = reserve.Id, RegionId = "lorien" }];
        LordsFixture.Reject(state, CommandKind.Recruit, payload);
        War(state, "elves");
        state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.Character;
        LordsFixture.Reject(state, CommandKind.Recruit, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Face = DieFace.Muster;
        state = LordsFixture.Do(state, CommandKind.Recruit, payload);
        Assert.True(state.Units.Single(u => u.Id == reserve.Id).Garrison);
        Assert.False(RecruitmentRules.CanPlaceMuster(state, Side.FreePeoples, state.Units.Single(u => u.Id == reserve.Id), "lorien"));
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(1, state.Pending.RequiredCount);
        state = LordsFixture.Do(state, CommandKind.RemoveExcess, new() { Ids = [reserve.Id] });
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == reserve.Id).Location);
        CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, LordsCharacters.Galadriel)!);
        var available = state.Units.First(u => u.Nation == "elves" && u.Kind == UnitKind.Elite && u.Location == "reserve");
        Assert.False(RecruitmentRules.CanPlaceMuster(state, Side.FreePeoples, available, "lorien"));
    }

    [Fact]
    public void KeeperGreyHasNoCaptainBonusAndFlameAddsTwoWithinFiveDiceLimit()
    {
        var state = State(emptyBoard: true);
        var grey = Put(state, LordsCharacters.Gandalf, "fangorn");
        var fp = new BattleForce { Side = Side.FreePeoples, Region = "fangorn", Participants = [grey.Id] };
        Assert.Equal(0, BattleRules.CaptainBonus(state, fp));
        var balrog = Put(state, LordsCharacters.Balrog, "moria");
        var sh = new BattleForce { Side = Side.Shadow, Region = "moria", Participants = [balrog.Id] };
        sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria").Id);
        Assert.Equal(3, BattleRoll.DiceCount(state, sh));
        for (var i = 0; i < 4; i++)
            sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria").Id);
        Assert.Equal(5, BattleRoll.DiceCount(state, sh));
        sh.Participants = sh.Participants.Take(2).ToList();
        sh.Round.CancelledAbilities.Add(balrog.Id);
        Assert.Equal(1, BattleRoll.DiceCount(state, sh));
    }
}
