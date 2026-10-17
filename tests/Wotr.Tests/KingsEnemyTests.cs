using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.KingsCards04;
using static Wotr.Tests.KingsCards03Fixture;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsEnemyTests
{
    internal static MatchState Ready(SovereignStatus status = SovereignStatus.Initial)
    {
        var state = State(Side.Shadow);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith");
        BattleFixture.Put(state, "gondor", UnitKind.Elite, "minas-tirith");
        if (status == SovereignStatus.Awakened)
            SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        if (status == SovereignStatus.Corrupted)
            Corrupt(state, SovereignCatalog.Denethor);
        return state;
    }

    [Theory]
    [InlineData(SovereignStatus.Initial, 2)]
    [InlineData(SovereignStatus.Awakened, 2)]
    [InlineData(SovereignStatus.Corrupted, 3)]
    public void ExactlyThreeDiceUseChosenSovereignsPrintedThreshold(SovereignStatus status, int hits)
    {
        var state = Play(Ready(status), Enemy);
        Assert.Empty(state.RandomSamples);
        state = Choose(state, SovereignCatalog.Denethor, 3, 4, 5);
        var loss = EventLosses.Current(state)!;
        Assert.Equal(new[] { 4, 5, 6 }, loss.Rolls);
        Assert.Equal(hits, loss.Hits);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(CasualtyCause.Event, loss.Cause);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.NotEmpty(state.Resolution.PaidDieIds);
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
    }

    [Fact]
    public void EventValidatesModuleSideHandCostAndPhaseWithoutMutation()
    {
        var state = Ready();
        var payload = Cost(state, Enemy, DieFace.Muster);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        var before = StateCodec.Write(state);
        Assert.True(new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command).Legal);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal("NotAuthorized", LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        payload.CardCost = DieFace.Character;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Muster;
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Available;
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Configuration.Kings = true;
        state.Player(Side.Shadow).Hand.Clear();
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
        state.Player(Side.Shadow).Hand.Add(Enemy);
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, CommandKind.PlayEvent, payload);
    }

    [Fact]
    public void RemovedSovereignsAreNotTargetsAndNoTargetStillSpendsCardAndDie()
    {
        var state = Ready();
        foreach (var sovereign in state.Kings!.Sovereigns)
            SovereignLifecycle.Remove(state, sovereign);
        var payload = Cost(state, Enemy, DieFace.Muster);
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Empty(state.RandomSamples);
    }

    [Fact]
    public void RegionWithoutArmyStillRollsButCannotDamageSovereignOrOtherRegion()
    {
        var state = Play(Ready(SovereignStatus.Corrupted), Enemy);
        var before = state.Units.Select(u => (u.Id, u.Location)).ToArray();
        state = Choose(state, SovereignCatalog.Theoden, 5, 5, 5);
        Assert.Equal(before, state.Units.Select(u => (u.Id, u.Location)));
        Assert.Equal(SovereignStatus.Initial, Sovereign(state, SovereignCatalog.Theoden).Status);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Null(EventLosses.Current(state));
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
    }

    [Fact]
    public void SovereignChoiceKeepsMixedThresholdsAndRejectsForgedTargets()
    {
        var state = Ready(SovereignStatus.Corrupted);
        state.Units.Single(u => u.Id == SovereignCatalog.Theoden).Location = "minas-tirith";
        state.Units.Single(u => u.Id == SovereignCatalog.Brand).Location = "minas-tirith";
        state = Play(state, Enemy);
        Assert.Contains(SovereignCatalog.Denethor, state.Pending!.PrivateChoices);
        var choices = new EffectOptions().GetOptions(state, Side.Shadow).Single().EffectChoices;
        Assert.Equal(2, choices.Count(c => c.TargetId == "minas-tirith"));
        Assert.Contains(choices, c => c.TargetId == "minas-tirith" && c.Detail.Contains("4+"));
        Assert.Contains(choices, c => c.TargetId == "minas-tirith" && c.Detail.Contains("5+"));
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["minas-tirith"] });
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Denethor, SovereignCatalog.Theoden] });
        var payload = new CommandPayload { Ids = [SovereignCatalog.Denethor] };
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.ChooseEffect, payload));
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShadowChoosesOneArmyAndCannotChargeTheOtherArmyForLosses(bool garrison)
    {
        var state = ArmyChoice();
        Assert.Equal(new[] { "minas-tirith:field", "minas-tirith:garrison" }, state.Pending!.PrivateChoices.Order());
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["pelargir:field"] });
        state = Choose(state, "minas-tirith:" + (garrison ? "garrison" : "field"), 4, 0, 0);
        var loss = EventLosses.Current(state)!;
        Assert.Equal(garrison, loss.Target.Garrison);
        var other = state.Units.First(u => u.Location == "minas-tirith" && u.Garrison != garrison && BoardRules.IsArmyUnit(u));
        LordsFixture.Reject(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(other.Id)] });
        var victim = loss.Target.Units(state).First(BoardRules.IsArmyUnit);
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(victim.Id)] });
        Assert.Equal("minas-tirith", state.Units.Single(u => u.Id == other.Id).Location);
        Assert.Equal("casualties", state.Units.Single(u => u.Id == victim.Id).Location);
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
    }

    internal static MatchState ArmyChoice()
    {
        var state = Ready(SovereignStatus.Corrupted);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = true;
        state = Play(state, Enemy);
        return Choose(state, SovereignCatalog.Denethor);
    }

    [Theory]
    [InlineData(SovereignStatus.Initial)]
    [InlineData(SovereignStatus.Awakened)]
    [InlineData(SovereignStatus.Corrupted)]
    public void EventEliminationRemovesLeadersButPreservesCompanionsAndCorrupted(SovereignStatus status)
    {
        var state = Ready(status);
        var regular = state.Units.Single(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).Id;
        var elite = state.Units.Single(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Elite).Id;
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, "minas-tirith").Id;
        Companion(state, Boromir, "minas-tirith");
        var position = state.Nations.Single(n => n.Id == "gondor").StepsToWar;
        state = Choose(Play(state, Enemy), SovereignCatalog.Denethor, 5, 5, 5);
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(regular), BattleFixture.Loss(elite)] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Equal("minas-tirith", state.Characters.Single(c => c.Id == Boromir).Location);
        Assert.Equal(status == SovereignStatus.Initial ? SovereignStatus.Removed : status, Sovereign(state, SovereignCatalog.Denethor).Status);
        Assert.Equal(position, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
        Assert.Empty(state.Resolution.PaidDieIds);
    }

    [Fact]
    public void OneHitCannotRemoveWholeEliteButAllowsDowngradeAndDoesNotRevealTiles()
    {
        var state = Ready();
        Tile(state, SovereignCatalog.Dain);
        state = Choose(Play(state, Enemy), SovereignCatalog.Denethor, 4, 0, 0);
        var elite = EventLosses.Current(state)!.Target.Units(state).Single(u => u.Kind == UnitKind.Elite).Id;
        LordsFixture.Reject(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(elite)] });
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(elite, BattleLossKind.Downgrade)] });
        Assert.Equal(2, state.Units.Count(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular));
        Assert.DoesNotContain(One, Json(state, Side.FreePeoples));
        Assert.Equal(3, state.RandomSamples.Count);
    }

    [Fact]
    public void MissesFinishEventAndExcessHitsAfterLastRegularDoNotRequireOtherVictims()
    {
        var state = Choose(Play(Ready(), Enemy), SovereignCatalog.Denethor, 0, 1, 2);
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
        Assert.Null(EventLosses.Current(state));
        state = Ready();
        BoardRules.ReturnToReserve(state.Units.Single(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Elite));
        state = Choose(Play(state, Enemy), SovereignCatalog.Denethor, 5, 5, 5);
        var victim = EventLosses.Current(state)!.Target.Units(state).Single(BoardRules.IsArmyUnit).Id;
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(victim)] });
        Assert.Contains(Enemy, state.Player(Side.Shadow).Discard);
        Assert.Null(EventLosses.Current(state));
    }
}
