using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class Shadow03CombatTests
{
    [Theory]
    [InlineData(Lure)]
    [InlineData(Breaking)]
    [InlineData(Worn)]
    [InlineData(Crebain)]
    [InlineData(Balrog)]
    [InlineData(Lidless)]
    public void EveryCombatRequiresOwnershipActorAndCorrectWindow(string card)
    {
        var state = Do(card == Balrog ? MoriaBattle() : BeforeCombat(card), CommandKind.CommitCombatCard);
        var payload = new CommandPayload { CardId = card };
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, payload);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.CommitCombatCard, payload);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Pending!.Kind = DecisionKind.Action;
        Reject(state, CommandKind.CommitCombatCard, payload);
    }

    [Theory]
    [InlineData(Worn, false)]
    [InlineData(Worn, true)]
    [InlineData(Lidless, false)]
    [InlineData(Lidless, true)]
    public void WordsCancelsLeadershipAndCaptainOnlyForTheChosenParticipantAndRound(string card, bool shadowAttacks)
    {
        var state = Reveal(BeforeCombat(card, shadowAttacks), card);
        Assert.Equal("Companion", state.Pending!.Presentation);
        var battle = BattleRuntime.Active(state)!;
        var king = BattleRules.Characters(state, battle.Force(Side.FreePeoples)).Single(c => c.DefinitionId == CharacterRules.Aragorn).Id;
        Assert.DoesNotContain(CharacterRules.WitchKing, Choices(state).EligibleIds);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [king] }, new SequenceRandom(new int[8]));
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(4, battle.Force(Side.FreePeoples).Round.Dice.Count);
        Assert.Equal(2, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Equal(1, BattleRules.CaptainBonus(state, battle.Force(Side.FreePeoples)));
        BattleRuntime.NextRound(battle);
        Assert.Equal(4, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Equal(2, BattleRules.CaptainBonus(state, battle.Force(Side.FreePeoples)));
    }

    [Fact]
    public void WordsSuppressesWhiteRiderForOneRoundAndDaringCannotPayItAgain()
    {
        var state = BeforeCombat(Worn, companion: CharacterRules.White, fpCard: "base.fp.character.15");
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        state = Reveal(state, Worn, "base.fp.character.15");
        Assert.Equal("Companion", state.Pending!.Presentation);
        var white = state.Characters.Single(c => c.DefinitionId == CharacterRules.White).Id;
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [white] }, new SequenceRandom(new int[8]));
        var battle = BattleRuntime.Active(state)!;
        Assert.False(BattleRules.WhiteRiderActive(state));
        Assert.Equal(3, BattleLeadership.Nazgul(state, battle.Force(Side.Shadow)).Values.Sum());
        Assert.Equal(0, BattleLeadership.Available(state, battle.Force(Side.FreePeoples))[white]);
        BattleRuntime.NextRound(battle);
        Assert.True(BattleRules.WhiteRiderActive(state));
        Assert.Empty(BattleLeadership.Nazgul(state, battle.Force(Side.Shadow)));
    }

    [Fact]
    public void DaringCancelsWordsBeforeItsTargetChoiceWhenCostIsAvailable()
    {
        var state = Reveal(BeforeCombat(Worn, fpCard: "base.fp.character.15"), Worn, "base.fp.character.15");
        Assert.True(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardCancelled);
        Assert.NotEqual(DecisionKind.EffectChoice, state.Pending!.Kind);
    }

    [Theory]
    [InlineData("moria", true)]
    [InlineData("lorien", true)]
    [InlineData("old-forest-road", false)]
    [InlineData("rivendell", false)]
    [InlineData("minas-tirith", false)]
    public void DurinsBaneUsesDefendingArmyAndTraversableDistance(string region, bool legal)
    {
        var state = MoriaBattle();
        var battle = BattleRuntime.Active(state)!;
        battle.Defender.Region = region;
        Assert.Equal(legal, new NearMoriaBattle().CanPlay(state, battle, Side.Shadow));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DurinsBaneRollsThreeUnmodifiedDiceAtFourAndAppliesLossesImmediately(bool shadowAttacks)
    {
        var state = MoriaBattle(shadowAttacks);
        state = Reveal(state, Balrog, dice: [2, 3, 5]);
        var attack = Assert.Single(BattleRuntime.Active(state)!.SpecialAttacks);
        Assert.Equal(2, attack.Dice.Count(d => d.Hit));
        Assert.All(attack.Dice, d => { Assert.Equal(4, d.HitTarget); Assert.Equal(0, d.Modifier); });
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Empty(BattleRuntime.Active(state)!.Attacker.Round.Dice);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BalrogTableCommitHasSamePublicCountsAsHandAndDiscardsOnlyAtReveal(bool useBalrog)
    {
        var state = MoriaBattle(shadowAttacks: true, table: true);
        EventTests.GiveCard(state, Side.Shadow, Worn);
        var before = new Projection().For(state, Side.FreePeoples).Players.Single(p => p.Side == Side.Shadow).HandCount;
        var card = useBalrog ? Balrog : Worn;
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = card });
        var other = new Projection().For(state, Side.FreePeoples);
        Assert.Null(other.Players.Single(p => p.Side == Side.Shadow).VisibleCombatCard);
        Assert.Equal(before - 1, other.Players.Single(p => p.Side == Side.Shadow).HandCount);
        Assert.Contains(other.Effects, e => e.SourceId == Balrog);
        state = Do(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(useBalrog ? [3, 0, 0] : []));
        Assert.Equal(!useBalrog, state.Player(Side.Shadow).Table.Contains(Balrog));
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Discard);
    }

    [Theory]
    [InlineData(Lure)]
    [InlineData(Breaking)]
    [InlineData(Crebain)]
    public void ReusedNazgulCardsUseTheirOwnCostAndPrintedInitiative(string card)
    {
        var state = Reveal(BeforeCombat(card), card);
        Assert.Equal("NazgulLeadership", state.Pending!.Presentation);
        var point = Choices(state).EligibleIds[0];
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [point] }, new SequenceRandom(new int[8]));
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.Shadow).Round.ForfeitedLeadership.Values.Sum());
        Assert.Equal(card == Breaking ? 1 : 0, battle.Force(Side.FreePeoples).Round.CombatDicePenalty);
        Assert.Equal(card == Breaking ? 0 : 1, battle.Force(Side.Shadow).Round.RerollModifier);
    }
}
