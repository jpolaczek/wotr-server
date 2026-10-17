using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsWeaknessTests
{
    [Theory]
    [InlineData(SovereignCatalog.Brand, "north", "the-shire", UnitKind.Elite, true)]
    [InlineData(SovereignCatalog.Brand, "north", "dale", UnitKind.Leader, true)]
    [InlineData(SovereignCatalog.Brand, "gondor", "dale", UnitKind.Elite, false)]
    [InlineData(SovereignCatalog.Brand, "north", "dale", UnitKind.Regular, false)]
    [InlineData(SovereignCatalog.Theoden, "rohan", "helms-deep", UnitKind.Elite, true)]
    [InlineData(SovereignCatalog.Theoden, "rohan", "edoras", UnitKind.Leader, true)]
    [InlineData(SovereignCatalog.Theoden, "north", "edoras", UnitKind.Leader, true)]
    [InlineData(SovereignCatalog.Theoden, "rohan", "dale", UnitKind.Leader, false)]
    [InlineData(SovereignCatalog.Dain, "dwarves", "iron-hills", UnitKind.Elite, false)]
    [InlineData(SovereignCatalog.Dain, "elves", "erebor", UnitKind.Elite, true)]
    [InlineData(SovereignCatalog.Dain, "dwarves", "erebor", UnitKind.Leader, true)]
    [InlineData(SovereignCatalog.Denethor, "gondor", "dol-amroth", UnitKind.Elite, false)]
    [InlineData(SovereignCatalog.Denethor, "dwarves", "minas-tirith", UnitKind.Elite, true)]
    [InlineData(SovereignCatalog.Denethor, "gondor", "minas-tirith", UnitKind.Leader, true)]
    [InlineData(SovereignCatalog.Thranduil, "elves", "woodland-realm", UnitKind.Elite, true)]
    [InlineData(SovereignCatalog.Thranduil, "north", "woodland-realm", UnitKind.Elite, true)]
    [InlineData(SovereignCatalog.Thranduil, "elves", "woodland-realm", UnitKind.Leader, false)]
    [InlineData(SovereignCatalog.Thranduil, "elves", "lorien", UnitKind.Elite, false)]
    public void RecruitmentWeaknessHasThePrintedNationRegionAndUnitScope(string id, string nation, string region, UnitKind kind, bool blocked)
    {
        var state = Actions();
        var sovereign = Sovereign(state, id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = SovereignCatalog.All[id].Home;
        var unit = state.Units.First(u => u.Nation == nation && u.Kind == kind && !u.Id.StartsWith("kings."));
        BoardRules.ReturnToReserve(unit);
        Assert.Equal(blocked, KingsWeaknesses.BlocksRecruit(state, unit, region));
        SovereignLifecycle.Remove(state, sovereign);
        Assert.False(KingsWeaknesses.BlocksRecruit(state, unit, region));
    }

    [Fact]
    public void MusterCannotRecruitAnEliteWhileAnEventCanAndThranduilDoesNotBlockLeaders()
    {
        var state = Actions();
        Sovereign(state, SovereignCatalog.Thranduil).Status = SovereignStatus.Corrupted;
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        var elite = state.Units.First(u => u.Nation == "elves" && u.Kind == UnitKind.Elite && u.Location == "reserve");
        LordsFixture.Reject(state, CommandKind.Recruit, new()
        {
            DieId = Die(state).Id, Placements = [new() { UnitId = elite.Id, RegionId = "woodland-realm" }]
        });
        var step = new RecruitEffectStep("elves", "woodland-realm", RequiredKind: UnitKind.Elite);
        var frame = new ResolutionFrame { Effect = new() { Owner = Side.FreePeoples, SourceId = "test-event" } };
        Assert.Contains(elite.Id, step.Candidates(state, frame.Effect));
        step.Choose(new(state, frame, new SequenceRandom()), elite.Id);
        Assert.Equal("woodland-realm", elite.Location);
        var leader = state.Units.First(u => u.Nation == "elves" && u.Kind == UnitKind.Leader && !u.Id.StartsWith("kings."));
        BoardRules.ReturnToReserve(leader);
        Assert.True(RecruitmentRules.CanPlaceMuster(state, Side.FreePeoples, leader, "woodland-realm"));
    }

    internal static MatchState Siege(string sovereign)
    {
        var state = KingsBattleTests.Battle(sovereign, false, Side.Shadow);
        var battle = BattleRuntime.Active(state)!;
        var home = SovereignCatalog.All[sovereign].Home;
        foreach (var force in new[] { battle.Attacker, battle.Defender })
        {
            foreach (var unit in BattleRules.Units(state, force).ToArray())
            {
                unit.Location = home;
                unit.Garrison = force.Side == Side.FreePeoples;
            }
            force.Region = home;
            force.Garrison = force.Side == Side.FreePeoples;
        }
        battle.Region = home;
        battle.Mode = BattleMode.Siege;
        battle.Stage = BattleStage.RoundEnd;
        battle.CardsRevealed = true;
        state.Pending = null;
        new BattleRuntime().Continue(state, new SequenceRandom());
        return state;
    }

    [Theory]
    [InlineData(SovereignCatalog.Dain)]
    [InlineData(SovereignCatalog.Denethor)]
    [InlineData(SovereignCatalog.Thranduil)]
    public void SiegeWeaknessRemovesOneParticipatingRegularAndCannotBeUsedInTheNextRound(string sovereign)
    {
        var state = Siege(sovereign);
        var battle = BattleRuntime.Active(state)!;
        var costs = KingsWeaknesses.SiegeRegulars(state, battle).ToArray();
        Assert.Equal(2, costs.Length);
        var rear = BattleFixture.Put(state, "sauron", UnitKind.Regular, battle.Region);
        LordsFixture.Reject(state, CommandKind.ExtendSiege, new() { Ids = [rear.Id] });
        var command = BattleFixture.Command(state, CommandKind.ExtendSiege, new() { Ids = [costs[0]] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Contains(costs[0], preview.RemovedIds);
        Assert.Empty(preview.Placements);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal("reserve", state.Units.Single(u => u.Id == costs[0]).Location);
        Assert.Equal(2, battle.Round);
        Assert.True(battle.Kings!.SiegeWeaknessUsed);
        Assert.Empty(KingsWeaknesses.SiegeRegulars(state, battle));
        state = Do(state, CommandKind.CommitCombatCard);
        state = Do(state, CommandKind.CommitCombatCard, null, 0, 0, 0);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal(SovereignStatus.Corrupted, Sovereign(state, sovereign).Status);
    }

    [PostgresFact]
    public Task SiegeWeaknessPersistsItsFigureCostAndOncePerBattleLimit()
    {
        var state = Siege(SovereignCatalog.Dain);
        var cost = KingsWeaknesses.SiegeRegulars(state, BattleRuntime.Active(state)!).First();
        return LordsPersistenceTests.Restart(state, CommandKind.ExtendSiege, new() { Ids = [cost] });
    }
}
