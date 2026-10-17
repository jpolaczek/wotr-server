using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsShadowCardsFixture;

namespace Wotr.Tests;

public class LordsCaptainTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptainCannotBePlayedWithoutWitchKingOrWithGollumGuide(bool gollum)
    {
        var state = State();
        if (gollum)
        {
            LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "dol-guldur");
            state.Fellowship.Guide = CharacterRules.Gollum;
        }
        LordsFixture.Reject(state, CommandKind.PlayEvent, Cost(state, LordsShadowCards.Captain));
    }

    internal static MatchState NestedAttack()
    {
        var state = Ready();
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria").Level = 2;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "dimrill-dale");
        state = LordsFixture.Do(state, CommandKind.PlayEvent, Cost(state, LordsShadowCards.Balrog, DieFace.Character));
        state = LordsCardsFixture.Choose(state, BalrogEventStep.Army);
        return LordsFixture.Do(state, CommandKind.AttackArmy, new()
        {
            Moves = [new() { From = "moria", To = "dimrill-dale", CharacterIds = [LordsCharacters.Balrog],
                UnitIds = state.Units.Where(u => u.Location == "moria").Select(u => u.Id).ToList() }]
        });
    }

    [Fact]
    public void CaptainWaitsUntilNestedBalrogBattleEndsBeforeAddingTheUsedDieToHunt()
    {
        var state = NestedAttack();
        var id = Assert.Single(state.Resolution.PaidDieIds);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == id).Location);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[4]));
        state = LordsFixture.Do(state, CommandKind.RerollBattle);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == id).Location);
        state = LordsFixture.Do(state, CommandKind.StopBattle);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == id).Location);
        Assert.Equal(CaptainOfDespair.HuntOrigin, state.Dice.Single(d => d.Id == id).HuntOrigin);
    }

    internal static MatchState Ready()
    {
        var state = State();
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "dol-guldur");
        var cost = Cost(state, LordsShadowCards.Captain, DieFace.Character);
        state = LordsFixture.Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    [Fact]
    public void OnlyLaterUsedCharacterResultsBecomeHuntEyesAndPreserveTheirOrigin()
    {
        var state = Ready();
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Character;
        var id = die.Id;
        state = LordsFixture.Do(state, CommandKind.MoveShadowCharacters, new()
        {
            DieId = id, CompanionMoves = [new() { Ids = [LordsCharacters.WitchKing], Route = ["dol-guldur", "moria"] }]
        });
        die = state.Dice.Single(d => d.Id == id);
        Assert.Equal(DieLocation.Hunt, die.Location);
        Assert.Equal(DieFace.Eye, die.Face);
        Assert.Equal(CaptainOfDespair.HuntOrigin, die.HuntOrigin);
        Assert.True(DiceRules.IsShadowHuntDie(die));
        Assert.False(state.MandatoryHuntAllocation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventResultsAndDiscardedCharacterResultsDoNotBecomeHuntDice(bool discard)
    {
        var state = Ready();
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = discard ? DieFace.Character : DieFace.Event;
        var id = die.Id;
        state = LordsFixture.Do(state, discard ? CommandKind.DiscardDie : CommandKind.DrawCard, new() { DieId = id, Deck = "character" });
        Assert.NotEqual(DieLocation.Hunt, state.Dice.Single(d => d.Id == id).Location);
        Assert.Empty(state.Lords!.CaptainDice);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LosingWitchKingOrGettingGollumDiscardsTheTableCard(bool gollum)
    {
        var state = Ready();
        if (gollum)
            state.Fellowship.Guide = CharacterRules.Gollum;
        else
            CharacterLifecycle.Eliminate(state, CharacterRules.WitchKingInPlay(state)!);
        ActiveEffects.CheckConditions(state);
        Assert.DoesNotContain(LordsShadowCards.Captain, state.Player(Side.Shadow).Table);
        Assert.Contains(LordsShadowCards.Captain, state.Player(Side.Shadow).Discard);
    }

    [Theory]
    [InlineData("lorien", false, false)]
    [InlineData("lorien", true, true)]
    [InlineData("dale", false, true)]
    public void DeclarationExpiresOnlyInAnUnconqueredFreeStronghold(string region, bool captured, bool remains)
    {
        var state = Ready();
        state.Fellowship.RegionId = region;
        state.Fellowship.Progress = 0;
        if (captured)
            state.Control[region] = Side.Shadow;
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = [region] });
        Assert.Equal(remains, state.Player(Side.Shadow).Table.Contains(LordsShadowCards.Captain));
    }
}
