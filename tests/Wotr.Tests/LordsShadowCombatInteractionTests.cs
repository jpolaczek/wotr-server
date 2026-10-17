using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsShadowCardsFixture;

namespace Wotr.Tests;

public class LordsShadowCombatInteractionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FearWorksWithGothmogAsTheOnlyMinionAndResetsForTheNextRound(bool fpAttacks)
    {
        var state = Battle(LordsShadowCards.WeWontGoBack, fpAttacks);
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(Side.Shadow);
        force.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id));
        var gothmog = LordsCharacterFixture.Put(state, LordsCharacters.Gothmog, force.Region);
        force.Participants.Add(gothmog.Id);
        state = Reveal(state, LordsShadowCards.WeWontGoBack, new int[8]);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(2, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(1, battle.Force(Side.Shadow).Round.ForfeitedLeadership[gothmog.Id]);
        Assert.True(battle.Force(Side.FreePeoples).Round.DoubleSixes);
        BattleRuntime.NextRound(battle);
        Assert.False(battle.Force(Side.FreePeoples).Round.DoubleSixes);
        Assert.Empty(battle.Force(Side.Shadow).Round.ForfeitedLeadership);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.CombatModifier);
    }

    [Fact]
    public void ServantCannotReduceBelowOneDieAndCancelledMinionsCannotPay()
    {
        var state = Battle(LordsShadowCards.Captain);
        var battle = BattleRuntime.Active(state)!;
        var fp = battle.Force(Side.FreePeoples);
        fp.Participants = [BattleRules.Units(state, fp).First(BoardRules.IsArmyUnit).Id];
        var sh = battle.Force(Side.Shadow);
        var balrog = LordsCharacterFixture.Put(state, LordsCharacters.Balrog, sh.Region);
        sh.Participants.Add(balrog.Id);
        sh.Round.CancelledLeadership.Add(CharacterRules.WitchKing);
        state = Reveal(state, LordsShadowCards.Captain);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.WitchKing] });
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [balrog.Id] }, new SequenceRandom(new int[6]));
        battle = BattleRuntime.Active(state)!;
        Assert.Single(battle.Force(Side.FreePeoples).Round.Dice);
        Assert.Equal(3, battle.Force(Side.Shadow).Round.ForfeitedLeadership[balrog.Id]);
        BattleRuntime.NextRound(battle);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatDicePenalty);
    }

    [Fact]
    public void WordsCancelsChosenCouncilCompanionLeadershipAndAbilitiesForOneRound()
    {
        var state = Battle(LordsShadowCards.Gates);
        state.Configuration.Council = true;
        var gimli = state.Characters.Single(c => c.Id == "base.character.gimli");
        CharacterLifecycle.ApplyVersion(gimli, CharacterCatalog.Lords["lords.character.gimli"]);
        state = Reveal(state, LordsShadowCards.Gates);
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [gimli.Id] }, new SequenceRandom(new int[8]));
        var battle = BattleRuntime.Active(state)!;
        var fp = battle.Force(Side.FreePeoples);
        Assert.Contains(gimli.Id, fp.Round.CancelledAbilities);
        Assert.Contains(gimli.Id, fp.Round.CancelledLeadership);
        Assert.DoesNotContain(CharacterRules.Strider, fp.Round.CancelledAbilities);
        BattleRuntime.NextRound(battle);
        Assert.Empty(fp.Round.CancelledAbilities);
        Assert.Empty(fp.Round.CancelledLeadership);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void MumakilIncludesSpecialHitsAndRequiresStrictSuperiorityAfterBothRerolls(int hits, int extra)
    {
        var state = Battle(LordsShadowCards.DolGuldur);
        var battle = BattleRuntime.Active(state)!;
        var sh = battle.Force(Side.Shadow);
        sh.Participants.Add(BattleFixture.Put(state, "southrons", UnitKind.Elite, sh.Region).Id);
        var samples = new int[9];
        for (var i = 0; i < hits; i++)
            samples[5 + i] = 3;
        state = Reveal(state, LordsShadowCards.DolGuldur, samples);
        battle = BattleRuntime.Active(state)!;
        Assert.Equal(1, battle.Force(Side.Shadow).Round.CombatModifier);
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollModifier);
        // Represents the already removed casualties of a pre-combat Charge.
        battle.Force(Side.FreePeoples).Round.SpecialHits = 1;
        while (state.Pending!.Kind == DecisionKind.BattleReroll)
            state = LordsFixture.Do(state, CommandKind.RerollBattle);
        Assert.Equal(extra, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ExtraHits);
    }

    [Theory]
    [MemberData(nameof(LordsCards02BoundaryTests.Cards), MemberType = typeof(LordsCards02BoundaryTests))]
    public void DaringCancelsEveryLordsCombatHalfBeforeCostsOrChoices(string card)
    {
        var state = LordsCards02BoundaryTests.CombatState(card);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Alone);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = FreePeoplesCharacterCards02.Alone });
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = card }, new SequenceRandom(new int[10]));
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.Shadow).Round.CardCancelled);
        Assert.Empty(battle.Force(Side.Shadow).Round.ForfeitedLeadership);
        Assert.False(battle.Force(Side.FreePeoples).Round.DoubleSixes);
        Assert.Equal(0, battle.Force(Side.FreePeoples).Round.CombatDicePenalty);
        Assert.Empty(battle.Force(Side.FreePeoples).Round.CancelledAbilities);
        Assert.Empty(battle.SpecialAttacks);
        Assert.NotEqual(DecisionKind.EffectChoice, state.Pending!.Kind);
    }
}
