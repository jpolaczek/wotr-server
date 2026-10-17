using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsBattleTests
{
    internal static MatchState Battle(string sovereignId, bool awakened, Side attacker = Side.FreePeoples)
    {
        var state = Actions(attacker);
        foreach (var unit in state.Units.Where(u => !u.Id.StartsWith("kings.")))
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        var sovereign = Sovereign(state, sovereignId);
        if (awakened)
            SovereignLifecycle.Awaken(state, sovereign, "lossarnach");
        else
        {
            sovereign.Status = SovereignStatus.Corrupted;
            sovereign.CorruptedRegion = SovereignCatalog.All[sovereignId].Home;
            state.Units.RemoveAll(u => u.Id == sovereignId);
        }
        var fp = new BattleForce { Side = Side.FreePeoples, Region = "lossarnach" };
        var sh = new BattleForce { Side = Side.Shadow, Region = "pelargir" };
        for (var i = 0; i < 2; i++)
        {
            fp.Participants.Add(BattleFixture.Put(state, "north", UnitKind.Regular, fp.Region).Id);
            sh.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, sh.Region).Id);
        }
        if (awakened)
            fp.Participants.Add(sovereignId);
        var battle = new BattleState
        {
            Attacker = attacker == Side.FreePeoples ? fp : sh, Defender = attacker == Side.FreePeoples ? sh : fp,
            Region = attacker == Side.FreePeoples ? sh.Region : fp.Region, Stage = BattleStage.AttackerCard
        };
        BattleRuntime.Begin(state, battle);
        new BattleRuntime().Continue(state, new SequenceRandom());
        return state;
    }

    internal static MatchState Cards(MatchState state, string fpCard, string shCard, params int[] samples)
    {
        state.Player(Side.FreePeoples).Hand = [fpCard];
        state.Player(Side.Shadow).Hand = [shCard];
        var first = state.Pending!.Owner == Side.FreePeoples ? fpCard : shCard;
        var second = state.Pending.Owner == Side.FreePeoples ? shCard : fpCard;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = first });
        Assert.NotEqual("CombatReplacement", state.Pending!.Presentation);
        return Do(state, CommandKind.CommitCombatCard, new() { CardId = second }, samples);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrandReplacesHisOwnersCardOnlyAfterRevealAndOnlyOnceForTheEntireBattle(bool awakened)
    {
        var state = Cards(Battle(SovereignCatalog.Brand, awakened), BattleFixture.FpCard, KingsCards03.Valinor);
        var owner = awakened ? Side.FreePeoples : Side.Shadow;
        Assert.Equal("CombatReplacement", state.Pending!.Presentation);
        Assert.Equal(owner, state.Pending.Owner);
        Assert.True(BattleRuntime.Active(state)!.CardsRevealed);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["replace"] }, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Kings!.BrandUsed);
        Assert.True(battle.Opponent(owner).Round.CardCancelled);
        Assert.False(battle.Force(owner).Round.CardCancelled);
        Assert.Single(battle.Work.Where(w => w.Owner == owner), w => w.ProgramId == KingsBattle.BrandCancel);
        Assert.NotEqual("", battle.Force(owner).Round.CardId);
        Assert.True(state.Player(owner).CombatCardCommitted);
        Assert.Equal(0, battle.Force(owner).Round.CombatModifier);
        Assert.Equal(5, battle.Force(owner).Round.CombatDiceLimit);
        battle.Round++;
        battle.Stage = BattleStage.BeforeRoll;
        Assert.False(KingsBattle.OfferBrand(state, battle));
        if (!awakened)
            Assert.Equal("dale", SovereignLifecycle.Region(state, Sovereign(state, SovereignCatalog.Brand)));
    }

    [Theory]
    [InlineData(Side.FreePeoples, true)]
    [InlineData(Side.Shadow, false)]
    public void DefenderWinsInitiativeZeroSoSwarmCanCancelBrandsReplacement(Side attacker, bool brandCancelled)
    {
        var state = Cards(Battle(SovereignCatalog.Brand, true, attacker), BattleFixture.FpCard, ShadowStrategyCards02.Moving);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["replace"] }, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(brandCancelled, battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Equal(!brandCancelled, battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
    }

    [Theory]
    [InlineData("no-north")]
    [InlineData("not-participating")]
    [InlineData("no-own-card")]
    [InlineData("initial")]
    public void BrandRequiresTheActualParticipantsAndALegallyCommittedCard(string scenario)
    {
        var state = Battle(SovereignCatalog.Brand, true);
        var battle = BattleRuntime.Active(state)!;
        battle.CardsRevealed = true;
        battle.Stage = BattleStage.BeforeRoll;
        battle.Force(Side.FreePeoples).Round.CardId = scenario == "no-own-card" ? "" : BattleFixture.FpCard;
        if (scenario == "no-north")
            foreach (var unit in BattleRules.Units(state, battle.Force(Side.FreePeoples)))
                unit.Nation = "gondor";
        if (scenario == "not-participating")
            battle.Force(Side.FreePeoples).Participants.Remove(SovereignCatalog.Brand);
        if (scenario == "initial")
            Sovereign(state, SovereignCatalog.Brand).Status = SovereignStatus.Initial;
        Assert.False(KingsBattle.OfferBrand(state, battle));
        Assert.Null(battle.Kings);
    }

    [Fact]
    public void TheodenHasThreeLeadershipOnlyInTheFirstRoundAndDefianceForfeitsAllThree()
    {
        var state = Battle(SovereignCatalog.Theoden, true);
        var battle = BattleRuntime.Active(state)!;
        var fp = battle.Force(Side.FreePeoples);
        Assert.Equal(3, BattleLeadership.Total(state, fp));
        fp.Round.CardCancelled = true;
        Assert.Equal(3, BattleLeadership.Total(state, fp));
        fp.Round.CancelledAbilities.Add(SovereignCatalog.Theoden);
        Assert.Equal(1, BattleLeadership.Total(state, fp));
        fp.Round.CancelledAbilities.Clear();
        fp.Round.CancelledLeadership.Add(SovereignCatalog.Theoden);
        Assert.Equal(0, BattleLeadership.Total(state, fp));
        BattleRuntime.NextRound(battle);
        Assert.Equal(1, BattleLeadership.Total(state, fp));
        battle.Round = 1;
        state = Cards(state, FreePeoplesCharacterCards02.Alone, KingsCards03.Valinor, 0, 0, 0, 0);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(3, battle.Force(Side.FreePeoples).Round.ForfeitedLeadership[SovereignCatalog.Theoden]);
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Equal(0, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
    }
}
