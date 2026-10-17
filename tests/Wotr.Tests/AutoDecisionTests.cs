using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

public class AutoDecisionTests
{
    internal static MatchState Battle(Side attacker = Side.FreePeoples, bool leader = false)
    {
        var state = State(attacker);
        state.Id = Guid.NewGuid().ToString("N");
        var nation = attacker == Side.FreePeoples ? "gondor" : "sauron";
        var opponent = attacker == Side.FreePeoples ? "sauron" : "gondor";
        var army = Put(state, nation, UnitKind.Regular, Origin);
        Put(state, opponent, UnitKind.Regular, Target);
        var ids = new List<string> { army.Id };
        if (leader)
            ids.Add(Put(state, nation, UnitKind.Leader, Origin).Id);
        state.Control["pelargir"] = attacker == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
        return Begin(state, ids);
    }

    internal static GameCommand Change(MatchState state, Side actor, bool enabled) => new()
    {
        MatchId = state.Id,
        CommandId = Guid.NewGuid().ToString("N"),
        Kind = CommandKind.SetCombatCardPolicy,
        ExpectedRevision = state.Revision,
        Payload = new()
        {
            AutoDecision = new()
            {
                BattleId = BattleRuntime.Active(state)!.Id,
                ExpectedVersion = state.Player(actor).AutoDecision.Version,
                HandVersion = state.Player(actor).HandVersion,
                Enabled = enabled
            }
        }
    };

    internal static MatchState Set(MatchState state, Side actor, bool enabled)
    {
        var result = Engine().Execute(state, actor, Change(state, actor, enabled), new SequenceRandom(0, 0));
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void PreferenceWhileWaitingIsPrivateAndBothRolesAnswerInOrder(Side attacker)
    {
        var state = Battle(attacker);
        var defender = BattleRuntime.Current(state)!.Defender.Side;
        var before = new Projection().For(state, attacker);
        state = Set(state, defender, true);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(new Projection().For(state, attacker)));
        var restored = StateCodec.Copy(state);
        var result = Engine().Execute(restored, attacker, Change(restored, attacker, true), new SequenceRandom(0, 0));
        state = result.State;
        Assert.True(result.Receipt.PublicStateChanged);
        Assert.Equal(before.Revision + 1, state.Revision);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.Equal(new[] { $"{attacker} declined a combat card.", $"{defender} declined a combat card." },
            state.Events.Where(e => e.Kind == "CombatCardCommitted").Select(e => e.Text));
        Assert.Equal(2, state.RandomSamples.Count);
        foreach (var player in state.Players)
        {
            Assert.Equal(1, player.AutoDecision.LastSkippedRound);
            Assert.Equal(1, player.AutoDecision.Version);
            Assert.Equal(0, player.HandVersion);
        }
        state = Execute(state, CommandKind.ContinueBattle);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        state = Execute(state, CommandKind.HoldBattle, random: new SequenceRandom(0, 0));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.Equal(2, state.Player(attacker).AutoDecision.LastSkippedRound);
        Assert.Equal(4, state.RandomSamples.Count);
    }

    [Fact]
    public void CancellationAfterCommittedAnswerStopsOnlyFutureCardsDespiteOldPublicRevision()
    {
        var state = Set(Battle(), Side.FreePeoples, true);
        var cancel = Change(state, Side.FreePeoples, false);
        state = Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0));
        var result = Engine().Execute(state, Side.FreePeoples, cancel, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        Assert.False(result.Receipt.PublicStateChanged);
        Assert.Equal(state.Revision, result.State.Revision);
        Assert.Equal(1, result.Receipt.AutoDecision!.LastSkippedRound);
        state = Execute(result.State, CommandKind.ContinueBattle);
        state = Execute(state, CommandKind.HoldBattle);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Null(state.Player(Side.FreePeoples).AutoDecision.Policy);
        Assert.Equal(2, state.Events.Count(e => e.Kind == "CombatCardCommitted"));
    }

    [Fact]
    public void OptInIsRequiredEvenWithEmptyHandAndNeitherRerollsNorLossesAreAutomated()
    {
        var state = Battle(leader: true);
        Assert.Empty(state.Player(Side.FreePeoples).Hand);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        state = Set(state, Side.Shadow, true);
        state = Set(state, Side.FreePeoples, true);
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
        Assert.Equal(2, state.RandomSamples.Count);
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = [BattleRuntime.Current(state)!.Attacker.Round.Dice[0].Id] }, new SequenceRandom(5));
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.NotNull(state.Player(Side.Shadow).AutoDecision.Policy);
    }

    [Fact]
    public void HandMutationExpiresEvenWhenTheFinalHandIsTheSameAndNoOpRemovalDoesNot()
    {
        var state = Set(Battle(), Side.Shadow, true);
        var player = state.Player(Side.Shadow);
        var version = player.HandVersion;
        Assert.False(HandRules.Remove(player, "missing"));
        Assert.NotNull(player.AutoDecision.Policy);
        player.Decks["character"] = [ShCard];
        CardFlow.Draw(state, Side.Shadow, "character");
        HandRules.Remove(player, ShCard);
        Assert.Empty(player.Hand);
        Assert.Equal(version + 2, player.HandVersion);
        Assert.Null(player.AutoDecision.Policy);
        state = Execute(state, CommandKind.CommitCombatCard);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
    }

    [Fact]
    public void CardEffectChangingTheHandExpiresThePreferenceBeforeTheNextRound()
    {
        var state = Set(Battle(), Side.FreePeoples, true);
        var catalog = Catalog([new(3, "test.unused")], [new(3, "test.draw-opponent")],
            new("test.unused", []), new("test.draw-opponent", [new DrawOpponentStep()]));
        state.Player(Side.Shadow).Hand.Add(ShCard);
        state.Player(Side.FreePeoples).Decks["character"] = [FpCard];
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = ShCard }, new SequenceRandom(0, 0), catalog);
        Assert.Equal(1, state.Player(Side.Shadow).HandVersion);
        Assert.Equal(1, state.Player(Side.FreePeoples).HandVersion);
        Assert.Null(state.Player(Side.FreePeoples).AutoDecision.Policy);
        state = Execute(state, CommandKind.ContinueBattle, catalog: catalog);
        state = Execute(state, CommandKind.HoldBattle, catalog: catalog);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Single(state.Player(Side.FreePeoples).Hand);
    }

    private sealed record DrawOpponentStep : EffectStep
    {
        public override void Apply(EffectContext context) => CardFlow.Draw(context.State,
            BattleRuntime.Active(context.State)!.Opponent(context.Owner).Side, "character");
    }

    [Fact]
    public void EndAndNewBattleNeverInheritThePreference()
    {
        var state = Set(Battle(), Side.Shadow, true);
        state = Set(state, Side.FreePeoples, true);
        var oldBattle = BattleRuntime.Current(state)!.Id;
        state = Execute(state, CommandKind.StopBattle);
        Assert.All(state.Players, p => Assert.Null(p.AutoDecision.Policy));
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Begin(state, state.Units.Where(u => u.Location == Origin).Select(u => u.Id));
        Assert.NotEqual(oldBattle, BattleRuntime.Current(state)!.Id);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(1, state.Player(Side.FreePeoples).AutoDecision.LastSkippedRound);
    }

    [Fact]
    public void StalePreferenceHandBattleAndMissingPayloadAreRejectedWithoutMutation()
    {
        var state = Battle();
        var stale = Change(state, Side.Shadow, true);
        state = Set(state, Side.Shadow, true);
        Rejected(state, stale, Side.Shadow);
        var wrongHand = Change(state, Side.FreePeoples, true);
        wrongHand.Payload.AutoDecision!.HandVersion++;
        Rejected(state, wrongHand);
        var wrongBattle = Change(state, Side.FreePeoples, true);
        wrongBattle.Payload.AutoDecision!.BattleId = "previous";
        Rejected(state, wrongBattle);
        Rejected(state, Command(state, CommandKind.SetCombatCardPolicy));
        var view = new Projection().For(state, Side.Shadow);
        view.AutoDecision.Policy!.BattleId = "changed-client-copy";
        Assert.NotEqual(view.AutoDecision.Policy.BattleId, state.Player(Side.Shadow).AutoDecision.Policy!.BattleId);
    }
}
