using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards04;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsEnemyCombatTests
{
    internal static MatchState Combat() => KingsCards03CombatTests.Combat(KingsCards03.Palantir);

    [Fact]
    public void CombatRequiresDefendersRegionSovereignAndValidatesModuleHandOwnerAndWindow()
    {
        var state = Combat();
        state.Player(Side.Shadow).Hand = [Enemy];
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = Enemy });
        state = Do(state, CommandKind.CommitCombatCard, new());
        var payload = new CommandPayload { CardId = Enemy };
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, BattleFixture.Command(state, CommandKind.CommitCombatCard, payload)));
        Assert.DoesNotContain(Enemy, Json(state, Side.FreePeoples));
        state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = BattleRuntime.Active(state)!.Attacker.Region;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
        state.Units.Single(u => u.Id == SovereignCatalog.Denethor).Location = BattleRuntime.Active(state)!.Region;
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
        state.Configuration.Kings = true;
        state.Player(Side.Shadow).Hand.Clear();
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
        state.Player(Side.Shadow).Hand.Add(Enemy);
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Theory]
    [InlineData(SovereignStatus.Initial, 1)]
    [InlineData(SovereignStatus.Awakened, 1)]
    [InlineData(SovereignStatus.Corrupted, 0)]
    public void PrintedPowerModifiesBothRollTypesAccordingToSovereignAndExpiresNextRound(SovereignStatus status, int fpBonus)
    {
        var state = Combat();
        var battle = BattleRuntime.Active(state)!;
        if (status == SovereignStatus.Awakened)
            SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), battle.Region);
        if (status == SovereignStatus.Corrupted)
        {
            Corrupt(state, SovereignCatalog.Denethor);
            Sovereign(state, SovereignCatalog.Denethor).CorruptedRegion = battle.Region;
        }
        state = KingsChieftainBattleFixture.Cards(state, Enemy);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(fpBonus, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(fpBonus, battle.Force(Side.FreePeoples).Round.RerollModifier);
        Assert.DoesNotContain(SovereignCatalog.Denethor, battle.Attacker.Participants.Concat(battle.Defender.Participants));
        state = KingsChieftainBattleFixture.SkipRerolls(state);
        state = Do(state, CommandKind.ContinueBattle);
        if (state.Pending!.Kind == DecisionKind.BattleRetreat)
            state = Do(state, CommandKind.HoldBattle);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Round);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.RerollModifier);
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
    }

    [Fact]
    public void DaringDefianceCancelsPowerAndChieftainReplacesEffectWithoutErasingPlayedCard()
    {
        var state = Combat();
        KingsCards01CombatTests.AddCompanion(state, Merry);
        state = KingsChieftainBattleFixture.Cards(state, Enemy, KingsChieftainBattleFixture.Daring);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
        state = Combat();
        KingsChieftainBattleFixture.Add(state, KingsChieftains.Serpent);
        state = KingsChieftainBattleFixture.Cards(state, Enemy);
        Assert.False(BattleRuntime.Active(state)!.CardsRevealed);
        state = KingsChieftainBattleFixture.Choose(state, KingsChieftains.Serpent);
        state = KingsChieftainBattleFixture.PayWrath(state);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(Enemy, battle.Force(Side.Shadow).Round.CardId);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatModifier);
    }
}
