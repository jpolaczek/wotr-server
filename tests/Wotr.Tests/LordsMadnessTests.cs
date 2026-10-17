using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsMadnessTests
{
    internal const string Target = "eagles-eyrie";

    internal static MatchState State()
    {
        var state = LordsShadowCardsFixture.State();
        state.Control["carrock"] = Side.Shadow;
        BattleFixture.Put(state, "gondor", UnitKind.Regular, Target);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, Target);
        BattleFixture.Put(state, "gondor", UnitKind.Elite, Target);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "moria");
        return state;
    }

    internal static MatchState Window() => LordsShadowCardsFixture.Play(State(), LordsShadowCards.Madness);

    internal static MatchState Attack(MatchState state, string region, params int[] samples) =>
        Do(state, CommandKind.ChooseEffect, new() { Ids = [region] }, new SequenceRandom(samples));

    [Theory]
    [InlineData(DieFace.Army)]
    [InlineData(DieFace.Event)]
    [InlineData(DieFace.ArmyMuster)]
    public void EventPaysOneLegalDieAndWaitsForAnArmyBeforeRolling(DieFace face)
    {
        var state = State();
        var payload = LordsShadowCardsFixture.Cost(state, LordsShadowCards.Madness, DieFace.Army);
        state.Dice.Single(d => d.Id == payload.DieId).Face = face;
        if (face == DieFace.Event)
            payload.CardCost = DieFace.Event;
        var rng = new SequenceRandom();
        state = Do(state, CommandKind.PlayEvent, payload, rng);
        Assert.Equal(0, rng.Calls);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(new[] { Target, "moria" }, state.Pending.PrivateChoices);
        Assert.Single(state.Resolution.PaidDieIds);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Contains(LordsShadowCards.Madness, state.Player(Side.Shadow).Discard);
    }

    [Theory]
    [InlineData("peace")]
    [InlineData("owner")]
    [InlineData("phase")]
    [InlineData("module")]
    [InlineData("spent")]
    [InlineData("muster")]
    [InlineData("hand")]
    public void IllegalEventCannotSpendDiscardOrRoll(string reason)
    {
        var state = State();
        var payload = LordsShadowCardsFixture.Cost(state, LordsShadowCards.Madness);
        if (reason == "peace") state.Nations.Single(n => n.Id == "sauron").StepsToWar = 1;
        if (reason == "owner") TurnFlow.SelectNext(state, Side.FreePeoples);
        if (reason == "phase") state.Phase = GamePhase.Fellowship;
        if (reason == "module") state.Configuration.Lords = false;
        if (reason == "spent") state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        if (reason == "muster") payload.CardCost = DieFace.Muster;
        if (reason == "hand") state.Player(Side.Shadow).Hand.Remove(LordsShadowCards.Madness);
        Reject(state, CommandKind.PlayEvent, payload);
    }

    [Fact]
    public void TargetsUseSauronStrongholdsRegardlessOfControlButNotOtherNations()
    {
        var state = State();
        state.Control["moria"] = Side.FreePeoples;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "orthanc");
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "minas-morgul");
        BattleFixture.Put(state, "rohan", UnitKind.Leader, "dol-guldur");
        state = LordsShadowCardsFixture.Play(state, LordsShadowCards.Madness);
        Assert.Contains("moria", state.Pending!.PrivateChoices);
        Assert.Contains("minas-morgul", state.Pending.PrivateChoices);
        Assert.DoesNotContain("orthanc", state.Pending.PrivateChoices);
        Assert.DoesNotContain("dol-guldur", state.Pending.PrivateChoices);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["orthanc"] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [Target, "moria"] });
    }

    [Fact]
    public void ThreeDiceHitOnFiveAndSixAndOpponentAllocatesEventLosses()
    {
        var state = Window();
        var command = BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [Target] });
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(before, StateCodec.Write(state));
        state = Attack(StateCodec.Copy(state), Target, 3, 4, 5);
        var loss = EventLosses.Current(state)!;
        Assert.Equal(new[] { 4, 5, 6 }, loss.Rolls);
        Assert.Equal(2, loss.Hits);
        Assert.Equal(CasualtyCause.Event, loss.Cause);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Single(state.Resolution.PaidDieIds);
        var elite = loss.Target.Units(state).Single(u => u.Kind == UnitKind.Elite);
        state = Do(StateCodec.Copy(state), CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(elite.Id)] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending.Kind);
    }

    [Fact]
    public void DestroyingArmyRemovesLeadersButCompanionSurvivesAndRecruitmentContinues()
    {
        var state = State();
        foreach (var unit in state.Units.Where(u => u.Location == Target && u.Kind == UnitKind.Elite))
            unit.Location = "reserve";
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, Target);
        var companion = LordsCharacterFixture.Put(state, "base.character.gimli", Target);
        state = Attack(LordsShadowCardsFixture.Play(state, LordsShadowCards.Madness), Target, 5, 4, 0);
        var losses = EventLosses.Current(state)!.Target.Units(state).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = losses });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal(Target, state.Characters.Single(c => c.Id == companion.Id).Location);
        Assert.Equal(Target, EffectRuntime.Current(state)!.Effect!.MadnessTarget!.Region);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
    }

    [Fact]
    public void DowngradingEliteWithoutRegularReserveRemovesItForOneHit()
    {
        var state = Window();
        var remaining = state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").ToArray();
        for (var i = 0; i < remaining.Length; i++)
            remaining[i].Location = i < 10 ? "lossarnach" : "pelargir";
        state = Attack(state, Target, 4, 0, 0);
        var elite = EventLosses.Current(state)!.Target.Units(state).Single(u => u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(elite.Id, BattleLossKind.Downgrade)] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == elite.Id).Location);
        Assert.Equal(2, state.Units.Count(u => u.Location == Target && BoardRules.IsArmyUnit(u)));
    }

    [Fact]
    public void NoArmyLeavesEventWithoutRollingOrRecruiting()
    {
        var state = LordsShadowCardsFixture.State();
        state = LordsShadowCardsFixture.Play(state, LordsShadowCards.Madness);
        Assert.Empty(state.Resolution.Frames);
        Assert.Empty(state.RandomSamples);
        Assert.DoesNotContain(state.Units, u => BoardRules.Regions.ContainsKey(u.Location));
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
