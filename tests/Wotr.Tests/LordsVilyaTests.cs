using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsVilyaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VilyaKeepsTheJustUsedMovementDieOrDeclinesAfterRestart(bool accept)
    {
        var state = Ready();
        var cost = state.Dice[0].Id;
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = cost });
        Assert.Equal(DecisionKind.KeepActionDie, state.Pending!.Kind);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Resolving, Die(state, cost).Location);
        var samples = state.RandomSamples.Count;
        var command = BattleFixture.Command(state, accept ? CommandKind.UseVilya : CommandKind.DeclineVilya,
            new() { DieId = cost, RingId = LordsDiceSetup.VilyaRing });
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples, command);
        Assert.True(preview.Legal);
        Assert.Equal(accept ? new[] { LordsDiceSetup.VilyaRing } : [], preview.Costs);
        Assert.False(preview.HasUnknownResult);
        var direct = Do(state, command.Kind, command.Payload);
        state = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal(StateCodec.Write(direct), StateCodec.Write(state));
        Assert.Equal(accept ? DieLocation.Available : DieLocation.Hunt, Die(state, cost).Location);
        Assert.Equal(accept ? "" : DiceRules.FellowshipMove, Die(state, cost).HuntOrigin);
        Assert.Equal(accept ? Side.Shadow : Side.FreePeoples, state.Rings.Single(r => r.Id == LordsDiceSetup.VilyaRing).Owner);
        Assert.Equal(samples, state.RandomSamples.Count);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Null(state.Lords!.ActionCompletion);
    }

    [Fact]
    public void WillOfTheWestCannotBeRecoveredEvenAfterItsFaceWasNormalizedForTheAction()
    {
        var state = Ready();
        state.Dice[0].Face = DieFace.WillOfTheWest;
        var player = state.Player(Side.FreePeoples);
        player.Hand.AddRange(player.Decks["character"].Take(6));
        player.Decks["character"].RemoveRange(0, 6);
        state = Do(state, CommandKind.DrawCard, new() { DieId = state.Dice[0].Id, Deck = "strategy" });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(DieFace.Event, state.Dice[0].Face);
        Assert.Single(state.Lords!.PaidWillOfWestIds);
        state = Do(StateCodec.Copy(state), CommandKind.DiscardCards, new() { Ids = [state.Player(Side.FreePeoples).Hand[0]] });
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Empty(state.Lords!.PaidWillOfWestIds);
        Assert.Equal(Side.FreePeoples, state.Rings.Single(r => r.Id == LordsDiceSetup.VilyaRing).Owner);
    }

    [Fact]
    public void HandDiscardPrecedesVilyaAndVilyaDoesNotDrawTwiceOrConsumeTheNormalRingLimit()
    {
        var state = Ready();
        state.Dice[0].Face = DieFace.Event;
        var player = state.Player(Side.FreePeoples);
        player.Hand.AddRange(player.Decks["character"].Take(6));
        player.Decks["character"].RemoveRange(0, 6);
        state = Do(state, CommandKind.DrawCard, new() { DieId = state.Dice[0].Id, Deck = "strategy" });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Null(state.Lords!.ActionCompletion);
        state = Do(state, CommandKind.DiscardCards, new() { Ids = [state.Player(Side.FreePeoples).Hand[0]] });
        Assert.Equal(DecisionKind.KeepActionDie, state.Pending!.Kind);
        state = Do(state, CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Single(state.Events, e => e.Kind == "CardDrawn");
        Assert.False(state.Player(Side.FreePeoples).RingUsedThisTurn);
    }

    [Fact]
    public void SpecialMovementDieCanBeKeptWithoutLosingItsDeferredRemoveSymbol()
    {
        var state = Ready();
        var special = Die(state, LordsDiceCatalog.Nenya);
        special.Location = DieLocation.Available;
        special.Face = DieFace.Character;
        special.Special!.Chosen = true;
        special.Special.RemoveSymbol = true;
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = special.Id });
        state = Do(state, CommandKind.UseVilya, new() { DieId = special.Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.Equal(DieLocation.Available, Die(state, special.Id).Location);
        Assert.True(Die(state, special.Id).Special!.RemoveSymbol);
        Assert.True(Die(state, special.Id).Special!.Chosen);
    }

    [Fact]
    public void VilyaWaitsForTheWholeBattleAndDoesNotReplayRolls()
    {
        var state = Ready();
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        state.Dice[0].Face = DieFace.Army;
        var attacker = BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Target);
        state = Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, [attacker.Id]));
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Null(state.Lords!.ActionCompletion);
        state = Do(state, CommandKind.CommitCombatCard);
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.Null(state.Lords!.ActionCompletion);
        var samples = state.RandomSamples.Count;
        state = Do(StateCodec.Copy(state), CommandKind.StopBattle);
        Assert.Equal(DecisionKind.KeepActionDie, state.Pending!.Kind);
        state = Do(StateCodec.Copy(state), CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.Equal(samples, state.RandomSamples.Count);
        Assert.Equal(DieLocation.Available, state.Dice[0].Location);
        Assert.Null(BattleRuntime.Active(state));
    }

    [Fact]
    public void ForgedDieRingDeadKeeperAndUseOutsideCompletionAreRejected()
    {
        var state = Ready();
        var payload = new CommandPayload { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing };
        Reject(state, CommandKind.UseVilya, payload);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = payload.DieId });
        Reject(state, CommandKind.UseVilya, new() { DieId = state.Dice[1].Id, RingId = LordsDiceSetup.VilyaRing });
        Reject(state, CommandKind.UseVilya, new() { DieId = payload.DieId, RingId = LordsDiceSetup.NaryaRing });
        Put(state, KeeperRings.Elrond, "casualties");
        Reject(state, CommandKind.UseVilya, payload);
        state = Do(state, CommandKind.DeclineVilya);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HuntCompletesBeforeVilyaAndElrondMustStillBePresent(bool elrondDied)
    {
        var state = Ready();
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        HuntTests.Pool(state, HuntTests.Three);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id }, new SequenceRandom(5, 0));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Null(state.Lords!.ActionCompletion);
        if (elrondDied)
            Put(state, KeeperRings.Elrond, "casualties");
        var samples = state.RandomSamples.Count;
        state = Do(StateCodec.Copy(state), CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Null(HuntRuntime.Active(state));
        Assert.Equal(elrondDied ? DecisionKind.Action : DecisionKind.KeepActionDie, state.Pending!.Kind);
        if (!elrondDied)
            state = Do(state, CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.Equal(samples, state.RandomSamples.Count);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Equal(elrondDied ? DieLocation.Hunt : DieLocation.Available, state.Dice[0].Location);
    }

    [Fact]
    public void VilyaCanFollowNaryaWhileTheNormalRingLimitRemainsUsed()
    {
        var state = LordsNaryaTests.Ready();
        Put(state, KeeperRings.Elrond, "rivendell");
        state = Do(state, CommandKind.UseRing, new() { RingId = LordsDiceSetup.NenyaRing, DieId = state.Dice[0].Id, Face = DieFace.Muster });
        state = Do(state, CommandKind.UseNarya, LordsNaryaTests.Cost(state));
        Assert.Equal(DecisionKind.KeepActionDie, state.Pending!.Kind);
        state = Do(state, CommandKind.UseVilya, new() { DieId = state.Dice[0].Id, RingId = LordsDiceSetup.VilyaRing });
        Assert.All(state.Rings, r => Assert.Equal(Side.Shadow, r.Owner));
        Assert.True(state.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.Equal(DieLocation.Available, state.Dice[0].Location);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "gondor").StepsToWar);
    }

    internal static MatchState Ready()
    {
        var state = Actions();
        Put(state, KeeperRings.Elrond, "rivendell");
        return state;
    }
}
