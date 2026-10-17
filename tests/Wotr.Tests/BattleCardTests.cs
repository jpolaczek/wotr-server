using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class BattleCardTests
{
    [Fact]
    public void AttackerCommitsFirstOnlyBackIsPublicThenInitiativeTiesFavorDefender()
    {
        var catalog = Catalog([new(3, "test.attacker"), new(5, "test.later")], [new(3, "test.defender")],
            new("test.attacker", [new TraceStep("attacker")]), new("test.defender", [new TraceStep("defender")]), new("test.later", [new TraceStep("later")]));
        var state = WithCards(catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard }, catalog: catalog);
        var view = new Projection(new BattleOptions(catalog)).For(state, Side.Shadow);
        Assert.Equal(DecisionKind.CombatCard, view.Decision!.Kind);
        Assert.Null(view.Battle!.Attacker.CardId);
        Assert.Equal("strategy", view.Battle.Attacker.CardBack);
        Assert.Equal("strategy", view.Players.Single(p => p.Side == Side.FreePeoples).CombatCardBack);
        Assert.DoesNotContain(FpCard, JsonSerializer.Serialize(view, BoardCatalog.Json));
        var own = new Projection().For(state, Side.FreePeoples);
        Assert.Equal(FpCard, own.Battle!.Attacker.CardId);
        Assert.Null(own.Decision);
        state = Execute(StateCodec.Copy(state), CommandKind.CommitCombatCard, new() { CardId = ShCard }, new SequenceRandom(5, 5), catalog);
        Assert.Equal(new[] { "defender", "attacker", "later" }, state.Events.Where(e => e.Kind == "TestCombatEffect").Select(e => e.Text));
        foreach (var side in Enum.GetValues<Side>())
        {
            var revealed = new Projection().For(state, side);
            Assert.Equal(FpCard, revealed.Battle!.Attacker.CardId);
            Assert.Equal(ShCard, revealed.Battle.Defender.CardId);
            Assert.True(revealed.Battle.CardsRevealed);
        }
    }

    [Fact]
    public void DefenderCancellationAtEqualInitiativePreventsAttackerProgramsAndBothCardsDiscard()
    {
        var catalog = Catalog([new(0, "test.attacker"), new(3, "test.later")], [new(0, "test.cancel")],
            new("test.attacker", [new TraceStep("attacker")]), new("test.later", [new TraceStep("later")]), new("test.cancel", [new CancelOpponentStep()]));
        var state = WithCards(catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard }, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = ShCard }, new SequenceRandom(0, 0), catalog);
        Assert.DoesNotContain(state.Events, e => e.Kind == "TestCombatEffect");
        Assert.True(BattleRuntime.Current(state)!.Attacker.Round.CardCancelled);
        Assert.DoesNotContain(FpCard, state.Player(Side.FreePeoples).Discard);
        Assert.DoesNotContain(ShCard, state.Player(Side.Shadow).Discard);
        state = Execute(state, CommandKind.StopBattle, catalog: catalog);
        Assert.Equal(1, state.Player(Side.FreePeoples).Discard.Count(c => c == FpCard));
        Assert.Equal(1, state.Player(Side.Shadow).Discard.Count(c => c == ShCard));
    }

    [Fact]
    public void DefenderCanPlayWhenAttackerDeclinesAndCardRetreatEndsBeforeAnyRoll()
    {
        var catalog = Catalog([new(3, "test.unused")], [new(1, "test.retreat")],
            new("test.unused", []), new("test.retreat", [new RetreatStep()]));
        var state = WithCards(catalog);
        state.Control["pelargir"] = Side.Shadow;
        state = Execute(state, CommandKind.CommitCombatCard, catalog: catalog);
        Assert.Contains(ShCard, Assert.Single(new BattleOptions(catalog).GetOptions(state, Side.Shadow)).EligibleIds);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = ShCard }, catalog: catalog);
        Assert.Empty(state.RandomSamples);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        var retreat = state.Pending.PrivateChoices[0];
        state = Execute(state, CommandKind.ChooseEffect, new() { Ids = [retreat] }, catalog: catalog);
        Assert.Empty(state.RandomSamples);
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Contains(ShCard, state.Player(Side.Shadow).Discard);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CombatRoll");
    }

    [Fact]
    public void SpecialAttackHasImmediateLossesNoLeaderRerollAndNoOpposingCombatModifier()
    {
        var catalog = Catalog([new(2, "test.special")], [new(1, "test.modifier")],
            new("test.special", [new SpecialStep(1), new TraceStep("resumed")]), new("test.modifier", [new OpponentPenaltyStep()]));
        var state = WithCards(catalog, 2);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard }, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = ShCard }, new SequenceRandom(4), catalog);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(FpCard, state.Pending.SourceId);
        var battle = BattleRuntime.Active(state)!;
        Assert.Empty(battle.Attacker.Round.Dice);
        Assert.Equal(0, battle.SpecialAttacks[0].Dice[0].Modifier);
        Assert.Null(battle.SpecialAttacks[0].Dice[0].Reroll);
        Assert.True(battle.SpecialAttacks[0].Dice[0].Hit);
        Assert.DoesNotContain(state.Events, e => e.Kind == "TestCombatEffect");
        var casualty = BattleRules.Units(state, battle.Defender).First(BoardRules.IsArmyUnit).Id;
        state = Execute(StateCodec.Copy(state), CommandKind.TakeBattleLosses, new() { Losses = [Loss(casualty)] }, new SequenceRandom(4, 0), catalog);
        Assert.Contains(state.Events, e => e.Kind == "TestCombatEffect" && e.Text == "resumed");
        Assert.False(BattleRuntime.Current(state)!.Attacker.Round.Dice[0].Hit);
        Assert.Equal(-10, BattleRuntime.Current(state)!.Attacker.Round.Dice[0].Modifier);
        Assert.Equal(3, state.RandomSamples.Count);
    }

    [Fact]
    public void LethalSpecialAttackStopsTheBattleWithoutOrdinaryRolls()
    {
        var catalog = Catalog([new(2, "test.special")], [new(3, "test.unused")], new("test.special", [new SpecialStep(1)]), new("test.unused", []));
        var state = WithCards(catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FpCard }, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(5), catalog: catalog);
        var unit = BattleRules.Units(state, BattleRuntime.Active(state)!.Defender).Single().Id;
        state = Execute(state, CommandKind.TakeBattleLosses, new() { Losses = [Loss(unit)] }, catalog: catalog);
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Single(state.RandomSamples);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CombatRoll");
    }

    private static MatchState WithCards(EffectCatalog catalog, int defenders = 1)
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        for (var i = 0; i < defenders; i++)
            Put(state, "sauron", UnitKind.Regular, Target);
        state.Player(Side.FreePeoples).Hand.Add(FpCard);
        state.Player(Side.Shadow).Hand.Add(ShCard);
        return Begin(state, [attacker.Id], catalog);
    }

    [Fact]
    public void BattleStartedByEventResumesItsProgramAndSettlesOnlyTheEventCost()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state.Player(Side.FreePeoples).Hand.Add(FpCard);
        state.Dice[0].Face = DieFace.Event;
        var catalog = new EffectCatalog([new("test.nested", [new BeginBattleStep(attacker.Id), new TraceStep("after-battle")])],
            [new(FpCard, "test.nested", new UnconditionalEvent())]);
        state = Execute(state, CommandKind.PlayEvent, new() { CardId = FpCard, DieId = state.Dice[0].Id, CardCost = DieFace.Event }, catalog: catalog);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(2, state.Resolution.Frames.Count);
        state = Execute(state, CommandKind.CommitCombatCard, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0), catalog: catalog);
        state = Execute(StateCodec.Copy(state), CommandKind.StopBattle, catalog: catalog);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Contains(state.Events, e => e.Kind == "TestCombatEffect" && e.Text == "after-battle");
    }

    private sealed record BeginBattleStep(string UnitId) : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var battle = BattleRules.Attack(context.State, context.Owner,
                new() { From = Origin, To = Target, UnitIds = [UnitId] }, DieFace.Army)!;
            BattleRuntime.Begin(context.State, battle, EffectReturn.Parent);
        }
    }

    [Fact]
    public void BattleReportWaitsForItsParentActionAndDoesNotMergeTwoBattlesInOneAction()
    {
        var state = State();
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state.Player(Side.FreePeoples).Hand.Add(FpCard);
        state.Dice[0].Face = DieFace.Event;
        var catalog = new EffectCatalog([new("test.two-battles", [new BeginBattleStep(attacker.Id),
            new ReportPauseStep(), new BeginBattleStep(attacker.Id)])],
            [new(FpCard, "test.two-battles", new UnconditionalEvent())]);
        state = Execute(state, CommandKind.PlayEvent,
            new() { CardId = FpCard, DieId = state.Dice[0].Id, CardCost = DieFace.Event }, catalog: catalog);
        var firstId = BattleRuntime.Active(state)!.Id;
        state = Execute(state, CommandKind.CommitCombatCard, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0), catalog: catalog);
        state = Execute(state, CommandKind.StopBattle, catalog: catalog);
        Assert.Equal(BattleReportStatus.AwaitingSettlement, BattleArchiveTests.Report(state, firstId).Status);
        var version = BattleArchiveTests.Report(state, firstId).Version;
        state = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["next"] }, catalog: catalog);
        var secondId = BattleRuntime.Active(state)!.Id;
        state = Execute(state, CommandKind.CommitCombatCard, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(1, 2), catalog: catalog);
        state = Execute(state, CommandKind.StopBattle, catalog: catalog);
        var first = BattleArchiveTests.Report(state, firstId);
        var second = BattleArchiveTests.Report(state, secondId);
        Assert.Equal(first.ActionId, second.ActionId);
        Assert.Equal(BattleReportStatus.Completed, first.Status);
        Assert.Equal(BattleReportStatus.Completed, second.Status);
        Assert.NotEqual(version, first.Version);
        Assert.Equal(1, first.Rounds.Single().Result!.Attacker.Dice[0].Result);
        Assert.Equal(2, second.Rounds.Single().Result!.Attacker.Dice[0].Result);
        Assert.DoesNotContain(first.Steps, s => s.Context.BattleId == secondId);
    }

    private sealed record ReportPauseStep : EffectChoiceStep
    {
        public override string Presentation => "Test report settlement";
        public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => ["next", "continue"];
        public override void Choose(EffectContext context, string id) => context.Record("ReportContinuation", "Continued the parent event.");
    }

    internal sealed record TraceStep(string Label) : EffectStep
    {
        public override void Apply(EffectContext context) => context.Record("TestCombatEffect", Label);
    }

    private sealed record CancelOpponentStep : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round.CardCancelled = true;
    }

    private sealed record OpponentPenaltyStep : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var battle = BattleRuntime.Active(context.State)!;
            battle.Opponent(context.Owner).Round.CombatModifier = -10;
            battle.Force(context.Owner).Round.CancelHits = 10;
        }
    }

    internal sealed record SpecialStep(int Count) : EffectStep
    {
        public override void Apply(EffectContext context) => BattleRoll.Special(context.State, BattleRuntime.Active(context.State)!, context.Owner,
            context.Execution.SourceId, Count, context.Random);
    }

    private sealed record RetreatStep : EffectChoiceStep
    {
        public override string Presentation => "RetreatMap";
        public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => BattleMovement.Retreats(state, BattleRuntime.Active(state)!).ToList();
        public override void Choose(EffectContext context, string id) => BattleMovement.Retreat(context.State, BattleRuntime.Active(context.State)!, id);
    }
}
