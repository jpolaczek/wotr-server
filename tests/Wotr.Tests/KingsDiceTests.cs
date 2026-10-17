using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsDiceTests
{
    internal static CharacterState Chieftain(MatchState state, string id = KingsChieftains.Serpent, string region = "near-harad")
    {
        var character = new CharacterState { Id = id, Location = region };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Kings[id]);
        state.Characters.Add(character);
        return character;
    }

    [Fact]
    public void RulersEnterOnlyAtNextRecoveryRemainThroughDeathAndCanReturnWithNewSources()
    {
        var state = Actions();
        Assert.Equal(2, state.Dice.Count(d => d.Source == SovereignCatalog.RulerSource));
        Assert.Equal(DieLocation.Removed, KingsDice.Ruler(state, Side.FreePeoples)!.Location);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Brand), "dale");
        Chieftain(state);
        CharacterLifecycle.ReconcileDice(state);
        Assert.All(state.Players, p => Assert.Equal(0, p.ExtraDiceNextRecovery));
        Assert.All(state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource), d => Assert.Equal(DieLocation.Removed, d.Location));
        TurnFlow.BeginTurn(state);
        Assert.All(state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource), d => Assert.Equal(DieLocation.Unrolled, d.Location));
        foreach (var die in state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource))
            die.Location = DieLocation.Available;
        foreach (var character in state.Characters.ToArray())
            CharacterLifecycle.Eliminate(state, character);
        Assert.All(state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource), d => Assert.Equal(DieLocation.Available, d.Location));
        DiceRules.Recover(state);
        Assert.All(state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource), d => Assert.Equal(DieLocation.Removed, d.Location));
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Dain), "erebor");
        Chieftain(state, KingsChieftains.Ugluk, "orthanc");
        DiceRules.Recover(state);
        Assert.All(state.Dice.Where(d => d.Source == SovereignCatalog.RulerSource), d => Assert.Equal(DieLocation.Unrolled, d.Location));
        Assert.Equal(13, state.Dice.Count(d => d.Location == DieLocation.Unrolled));
    }

    public static IEnumerable<object[]> Faces()
    {
        DieFace[] fp = [DieFace.Character, DieFace.Character, DieFace.ArmyMuster, DieFace.Muster, DieFace.Event, DieFace.WillOfTheWest];
        DieFace[] sh = [DieFace.Character, DieFace.Army, DieFace.ArmyMuster, DieFace.Muster, DieFace.Event, DieFace.Eye];
        foreach (var side in Enum.GetValues<Side>())
            for (var i = 0; i < 6; i++)
                yield return [side, i, (side == Side.FreePeoples ? fp : sh)[i]];
    }

    [Theory]
    [MemberData(nameof(Faces))]
    public void RulerUsesTheSixPrintedFacesAndOnlyShadowEyeAwaitsDiscard(Side side, int roll, DieFace face)
    {
        var state = Setup();
        foreach (var d in state.Dice)
            d.Location = DieLocation.Removed;
        var die = KingsDice.Ruler(state, side)!;
        die.Location = DieLocation.Unrolled;
        var random = new SequenceRandom(roll);
        DiceRules.Roll(state, random);
        Assert.Equal(face, die.Face);
        Assert.Equal(1, random.Calls);
        Assert.Equal(face == DieFace.Eye ? DieLocation.AwaitingChoice : DieLocation.Available, die.Location);
    }

    internal static MatchState Discards(bool shadowEye = true)
    {
        var state = Actions();
        state.Phase = GamePhase.ActionRoll;
        KingsDice.Ruler(state, Side.FreePeoples)!.Location = DieLocation.Available;
        var sh = KingsDice.Ruler(state, Side.Shadow)!;
        sh.Location = shadowEye ? DieLocation.AwaitingChoice : DieLocation.Available;
        sh.Face = shadowEye ? DieFace.Eye : DieFace.Character;
        var ordinaryEye = state.Dice.First(d => d.Owner == Side.Shadow && d.Source == "standard");
        ordinaryEye.Face = DieFace.Eye;
        ordinaryEye.Location = DieLocation.AwaitingChoice;
        Assert.True(KingsDice.BeginDiscards(state));
        return state;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShadowChoosesBeforeMovingEyesAndBeforeFreePeoplesWithoutLosingTheQueue(bool discardRuler)
    {
        var state = Discards();
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var ordinaryEye = state.Dice.Single(d => d.Owner == Side.Shadow && d.Source == "standard" && d.Face == DieFace.Eye);
        var standard = state.Dice.First(d => d.Owner == Side.Shadow && d.Source == "standard" && d.Face != DieFace.Eye);
        var dieId = discardRuler ? KingsDice.Shadow : standard.Id;
        var options = Assert.Single(LordsFixture.Views().For(state, Side.Shadow).Options);
        Assert.Contains(KingsDice.Shadow, options.EligibleDice);
        Assert.DoesNotContain(ordinaryEye.Id, options.EligibleDice);
        LordsFixture.Reject(state, CommandKind.DiscardRulerDie, new() { DieId = ordinaryEye.Id });
        LordsFixture.Reject(state, CommandKind.DiscardRulerDie, new() { DieId = KingsDice.FreePeoples });
        state = Do(StateCodec.Copy(state), CommandKind.DiscardRulerDie, new() { DieId = dieId });
        Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == ordinaryEye.Id).Location);
        Assert.Equal(discardRuler ? DieLocation.Discarded : DieLocation.Hunt, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Equal(new[] { Side.FreePeoples }, state.Kings!.RulerDiscardOrder);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        state = Do(StateCodec.Copy(state), CommandKind.DiscardRulerDie, new() { DieId = KingsDice.FreePeoples });
        Assert.Equal(GamePhase.ActionResolution, state.Phase);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Empty(state.Kings!.RulerDiscardOrder);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
    }

    [Fact]
    public void AllocationNeverIncludesRulerAndSpecialDiceNeverPayItsDiscard()
    {
        var state = Setup();
        Chieftain(state);
        TurnFlow.BeginTurn(state);
        state = Do(state, CommandKind.ContinueFellowship);
        Assert.DoesNotContain(KingsDice.Shadow, DiceRules.HuntCandidates(state));
        LordsFixture.Reject(state, CommandKind.AllocateHunt, new() { Ids = [KingsDice.Shadow] });
        var randomCount = state.Dice.Count(d => d.Location == DieLocation.Unrolled);
        state = Do(state, CommandKind.AllocateHunt, new(), new int[randomCount]);
        Assert.Equal(DecisionKind.RulerDiscard, state.Pending!.Kind);
        state.Dice.Add(new() { Id = LordsDiceCatalog.Gothmog, Source = LordsDiceCatalog.Gothmog, Owner = Side.Shadow,
            Face = DieFace.Muster, Location = DieLocation.Available, Special = new() });
        LordsFixture.Reject(state, CommandKind.DiscardRulerDie, new() { DieId = LordsDiceCatalog.Gothmog });
        Assert.DoesNotContain(LordsDiceCatalog.Gothmog, KingsDice.DiscardCandidates(state, Side.Shadow));
    }

    [PostgresFact]
    public Task ShadowDiscardRestartsBeforeEyesMoveAndBeforeTheFreePeoplesChoice() =>
        LordsPersistenceTests.Restart(Discards(), CommandKind.DiscardRulerDie, new() { DieId = KingsDice.Shadow });

    [PostgresFact]
    public Task FreePeoplesDiscardRestartsBeforeActionsAndCannotBePaidTwice() =>
        LordsPersistenceTests.Restart(Do(Discards(), CommandKind.DiscardRulerDie, new() { DieId = KingsDice.Shadow }),
            CommandKind.DiscardRulerDie, new() { DieId = KingsDice.FreePeoples });
}
