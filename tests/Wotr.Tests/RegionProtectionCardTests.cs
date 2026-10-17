using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.FreePeoplesStrategyCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Strategy01Fixture;

namespace Wotr.Tests;

public class RegionProtectionCardTests
{
    [Theory]
    [InlineData(PowerTooGreat, "elves", "lorien", "dimrill-dale")]
    [InlineData(TomBombadil, "north", "the-shire", "buckland")]
    public void AdvancesInactiveNationAndBlocksArmyEntryAttackAndRetreat(string card, string nation, string region, string from)
    {
        var state = BattleFixture.State();
        Assert.True(BoardRules.Regions.ContainsKey(region));
        Assert.True(MovementRules.CanEnter(state, Side.Shadow, from, region));
        var politics = state.Nations.Single(n => n.Id == nation);
        politics.Active = false;
        politics.StepsToWar = 2;
        state = Play(state, card);
        Assert.False(state.Nations.Single(n => n.Id == nation).Active);
        Assert.Equal(1, state.Nations.Single(n => n.Id == nation).StepsToWar);
        Assert.Equal(3, state.ActiveEffects.Count(e => e.SourceId == card));
        Assert.All(state.ActiveEffects, effect => Assert.True(BoardRules.Regions.ContainsKey(effect.TargetId)));
        var unit = BattleFixture.Put(state, "sauron", UnitKind.Regular, from);
        var payload = BattleFixture.AttackPayload(state, [unit.Id], from, region);
        Assert.Null(MovementRules.Plan(state, Side.Shadow, payload));
        Assert.False(MovementRules.CanEnter(state, Side.Shadow, from, region));
        Assert.False(BattleMovement.IsFree(state, Side.Shadow, region));
        BattleFixture.Put(state, nation, UnitKind.Regular, region);
        Assert.Null(BattleRules.Attack(state, Side.Shadow, payload.Moves[0], DieFace.Army));
        Assert.True(RegionProtection.Blocks(state, Side.Shadow, region));
        Assert.False(RegionProtection.Blocks(state, Side.FreePeoples, region));
    }

    [Fact]
    public void ProtectedStrongholdCannotBeAssaultedButShadowCanLeaveAndMoveCharacters()
    {
        var state = Play(BattleFixture.State(), PowerTooGreat);
        var attacker = BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien").Garrison = true;
        var move = new ArmyMoveChoice { From = "lorien", To = "lorien", UnitIds = [attacker.Id] };
        Assert.Null(BattleRules.Attack(state, Side.Shadow, move, DieFace.Army));
        Assert.True(MovementRules.CanEnter(state, Side.Shadow, "lorien", "dimrill-dale"));
        Assert.False(RegionProtection.Blocks(state, Side.Shadow, "dimrill-dale"));
        Assert.NotNull(RecruitmentRules.EventGarrison(state, Side.FreePeoples, "lorien"));
    }

    internal static (MatchState State, CommandPayload Payload) Removal()
    {
        var state = Play(BattleFixture.State(), PowerTooGreat);
        foreach (var id in new[] { "base.sh.strategy.01", "base.sh.character.01", "base.sh.strategy.05" })
            EventTests.GiveCard(state, Side.Shadow, id);
        var payload = Cost(state, DieFace.Muster);
        payload.CardId = PowerTooGreat;
        payload.Ids = ["base.sh.strategy.01", "base.sh.character.01"];
        return (state, payload);
    }

    [Fact]
    public void RemovalIsAtomicAndAnyDiePaysButTwoExactHandTypesAreRequired()
    {
        var (state, payload) = Removal();
        payload.Ids[0] = "base.sh.strategy.05"; // Muster Strategy is not an Army card.
        Reject(state, CommandKind.DiscardProtection, payload);
        payload.Ids[0] = payload.Ids[1];
        Reject(state, CommandKind.DiscardProtection, payload);
        payload.Ids[0] = "base.sh.strategy.01";
        state.Player(Side.Shadow).Table.Add(payload.Ids[0]);
        HandRules.Remove(state.Player(Side.Shadow), payload.Ids[0]);
        Reject(state, CommandKind.DiscardProtection, payload);
        state.Player(Side.Shadow).Table.Remove(payload.Ids[0]);
        HandRules.Add(state.Player(Side.Shadow), payload.Ids[0]);
        state = Do(state, CommandKind.DiscardProtection, payload);
        Assert.Empty(state.ActiveEffects);
        Assert.Contains(PowerTooGreat, state.Player(Side.FreePeoples).Discard);
        Assert.All(payload.Ids, id => Assert.Contains(id, state.Player(Side.Shadow).Discard));
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.DoesNotContain(state.Events.Where(e => e.OnlyFor != Side.Shadow), e => payload.Ids.Any(e.Text.Contains));
        var publicSteps = ActionLogFamilyTests.Steps(state);
        var removal = publicSteps.Single(s => s.Command == CommandKind.DiscardProtection);
        Assert.Equal(2, removal.Costs.Single(c => c.Kind == ActionCostKind.Card).Count);
        Assert.Empty(removal.Costs.Single(c => c.Kind == ActionCostKind.Card).Id);
        Assert.Single(removal.Costs, c => c.Kind == ActionCostKind.Die);
        foreach (var id in payload.Ids)
        {
            Assert.DoesNotContain(id, ActionLogFamilyTests.Json(publicSteps));
            Assert.Contains(ActionLogFamilyTests.Steps(state, Side.Shadow).SelectMany(s => s.Facts.Cards), c => c.Id == id);
        }
    }

    [Fact]
    public void RemovingOneSourceLeavesOtherProtectionAndNeverRevealsPrivateOptions()
    {
        var (state, payload) = Removal();
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Play(state, TomBombadil);
        Assert.Equal(2, RegionProtection.Option(state, Side.Shadow)!.Targets.Count);
        Assert.Null(RegionProtection.Option(state, Side.FreePeoples));
        state = Do(state, CommandKind.DiscardProtection, payload);
        Assert.True(RegionProtection.Blocks(state, Side.Shadow, "the-shire"));
        Assert.False(RegionProtection.Blocks(state, Side.Shadow, "lorien"));
    }
}
