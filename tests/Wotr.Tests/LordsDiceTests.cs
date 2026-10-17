using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsDiceTests
{
    // Independent transcription of the diagrams on LORDS pp9/11; duplicates are deliberate.
    public static IEnumerable<object[]> PrintedFaces()
    {
        var faces = new (string Id, DieFace[] Faces, int[] Stars)[]
        {
            (LordsDiceCatalog.Vilya, [DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.CardDraw, DieFace.CardDraw, DieFace.Eye], [0, 5]),
            (LordsDiceCatalog.Nenya, [DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.CardDraw, DieFace.CardDraw, DieFace.Eye], [0, 5]),
            (LordsDiceCatalog.Narya, [DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.CardDraw, DieFace.Army, DieFace.Eye], []),
            (LordsDiceCatalog.Balrog, [DieFace.Character, DieFace.UseBalrog, DieFace.UseBalrog, DieFace.Muster, DieFace.CardDraw, DieFace.Eye], [0, 5]),
            (LordsDiceCatalog.Gothmog, [DieFace.Army, DieFace.Army, DieFace.Muster, DieFace.Muster, DieFace.CardDraw, DieFace.Eye], [0, 5])
        };
        foreach (var entry in faces)
            for (var i = 0; i < 6; i++)
                yield return [entry.Id, i, entry.Faces[i], entry.Stars.Contains(i)];
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void V12PrintedFacesRollOnceAndExposeRemoveWithoutApplyingIt(string id, int roll, DieFace face, bool star)
    {
        var state = Setup();
        var die = Die(state, id);
        var random = new SequenceRandom(roll);
        LordsDiceRoll.Roll(die, random);
        Assert.Equal(1, random.Calls);
        Assert.Equal(face, die.Face);
        Assert.Equal(star, die.Special!.RemoveSymbol);
        Assert.Equal(DieLocation.AwaitingChoice, die.Location);
        Assert.False(die.Special.PermanentlyRemoved);
        var projected = Views().For(state, Side.FreePeoples).Dice.Single(d => d.Id == id);
        Assert.Equal(star, projected.RemoveSymbol);
        Assert.Equal(face, projected.Face);
    }

    [Theory]
    [InlineData(0, Side.Shadow)]
    [InlineData(3, Side.FreePeoples)]
    [InlineData(4, Side.FreePeoples)]
    public void V12ChoiceOrderCountsRolledDiceExcludesHuntAllocationAndTiesFavorFreePeoples(int allocation, Side first)
    {
        var state = BothSidesRolling();
        foreach (var die in DiceRules.HuntCandidates(state).Take(allocation).Select(id => Die(state, id)))
            die.Location = DieLocation.Hunt;
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(new int[15 - allocation]));
        Assert.Equal(6, state.Lords!.RolledCounts[Side.FreePeoples]);
        Assert.Equal(9 - allocation, state.Lords.RolledCounts[Side.Shadow]);
        Assert.Equal(first, state.Pending!.Owner);
        Assert.Equal(GamePhase.ActionRoll, state.Phase);
        var initial = StateCodec.Write(state);
        var chosen = LordsDiceRoll.LegalChoices(state, first)[0];
        var command = BattleFixture.Command(state, CommandKind.ChooseSpecialDie, new() { DieId = chosen });
        var preview = new PreviewService(Engine()).Preview(state, first, command);
        Assert.True(preview.Legal);
        Assert.Empty(preview.Costs);
        Assert.False(preview.HasUnknownResult);
        Assert.Equal(initial, StateCodec.Write(state));
        var direct = Do(state, command.Kind, command.Payload);
        var resumed = Do(StateCodec.Copy(state), command.Kind, command.Payload);
        Assert.Equal(StateCodec.Write(direct), StateCodec.Write(resumed));
        Assert.Equal(TurnFlow.Other(first), resumed.Pending!.Owner);
        Assert.Null(Views().For(resumed, first).Decision);
        Assert.Empty(Views().For(resumed, first).Options);
        resumed = Do(resumed, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceRoll.LegalChoices(resumed, resumed.Pending.Owner)[0] });
        Assert.Equal(DecisionKind.Action, resumed.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, resumed.Pending.Owner);
        Assert.Empty(resumed.Lords!.ChoiceOrder);
        Assert.Equal(state.RandomSamples.Count, resumed.RandomSamples.Count);
        Assert.Equal(2, resumed.Dice.Count(d => d.Special?.Chosen == true));
    }

    [Fact]
    public void V12MultipleEyesRequireAnExplicitChoiceAndRejectOtherFacesOwnersAndRepeatedCommands()
    {
        var state = BothSidesRolling();
        DiceRules.Roll(state, new SequenceRandom(Enumerable.Repeat(5, 15).ToArray()));
        Die(state, LordsDiceCatalog.Balrog).Face = DieFace.Muster;
        Assert.True(LordsDiceRoll.BeginChoices(state));
        state.Phase = GamePhase.ActionRoll;
        Reject(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Balrog });
        Reject(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Vilya });
        state = Do(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Gothmog });
        Assert.Equal(new[] { LordsDiceCatalog.Vilya, LordsDiceCatalog.Nenya }, LordsDiceRoll.LegalChoices(state, Side.FreePeoples));
        var command = BattleFixture.Command(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Nenya });
        var result = Engine().Execute(StateCodec.Copy(state), Side.FreePeoples, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted);
        var chosen = Die(result.State, LordsDiceCatalog.Nenya);
        Assert.Equal(Side.FreePeoples, chosen.Owner);
        Assert.Equal(LordsDiceCatalog.KeeperEye, chosen.HuntOrigin);
        Assert.Equal(DieLocation.Hunt, chosen.Location);
        Assert.Equal(DieLocation.Discarded, Die(result.State, LordsDiceCatalog.Vilya).Location);
        var view = Views().For(result.State, Side.Shadow);
        Assert.True(view.Dice.Single(d => d.Id == LordsDiceCatalog.Nenya).SpecialDieChosen);
        Assert.False(view.Dice.Single(d => d.Id == LordsDiceCatalog.Vilya).SpecialDieChosen);
        Assert.False(Engine().Execute(result.State, Side.FreePeoples, command, new SequenceRandom()).Receipt.Accepted);
    }

    [Fact]
    public void SingleSpecialResultNeedsNoChoiceAndKeeperEyeCountsOnlyTowardShadowHuntStrength()
    {
        var state = Setup();
        Put(state, "lords.character.elrond", "rivendell");
        state.Turn = 1;
        DiceRules.Recover(state);
        Assert.DoesNotContain(LordsDiceCatalog.Vilya, DiceRules.HuntCandidates(state));
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(Enumerable.Repeat(0, 11).Append(5).ToArray()));
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        var eye = Die(state, LordsDiceCatalog.Vilya);
        Assert.True(eye.Special!.Chosen);
        var hunt = new HuntProcedure { Stage = HuntStage.Roll };
        HuntRoll.Roll(state, hunt, new SequenceRandom(5));
        Assert.Single(hunt.Dice);
        Assert.Equal(0, hunt.Bonus);
        Assert.Equal(1, hunt.EyeDamage);
        state.Resolution.Frames.Add(new() { Hunt = hunt });
        Assert.True(new HuntBeforeRollCondition().CanPlay(state, Side.Shadow));
        DiceRules.Recover(state);
        Assert.False(state.MandatoryHuntAllocation);
    }

    [Fact]
    public void CardDrawIsNotEventAndSpecialFacesPayAnyDieButCannotBeChangedByRings()
    {
        var state = Actions();
        var die = Die(state, LordsDiceCatalog.Nenya);
        die.Face = DieFace.CardDraw;
        die.Location = DieLocation.Available;
        Assert.False(DiceRules.CanPay(die, DieFace.Event));
        Assert.Contains(die.Id, Views().For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.DrawCard).EligibleDice);
        Assert.DoesNotContain(die.Id, Views().For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.UseRing).EligibleDice);
        Reject(state, CommandKind.UseRing, new() { DieId = die.Id, RingId = LordsDiceSetup.NenyaRing, Face = DieFace.Character });
        var count = state.Player(Side.FreePeoples).Hand.Count;
        state = Do(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "character" });
        Assert.Equal(count + 1, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DieLocation.Spent, Die(state, die.Id).Location);
        var balrog = Die(state, LordsDiceCatalog.Balrog);
        balrog.Location = DieLocation.Available;
        balrog.Face = DieFace.UseBalrog;
        CharacterRules.PayAny(state, Side.Shadow, balrog.Id);
        Assert.Equal(DieLocation.Resolving, balrog.Location);
    }

    internal static MatchState BothSidesRolling()
    {
        var state = Setup();
        Put(state, "lords.character.elrond", "rivendell");
        Put(state, "lords.character.galadriel", "lorien");
        Put(state, "lords.character.balrog", "moria");
        Put(state, "lords.character.gothmog", "minas-morgul");
        state.Turn = 1;
        DiceRules.Recover(state);
        return state;
    }
}
