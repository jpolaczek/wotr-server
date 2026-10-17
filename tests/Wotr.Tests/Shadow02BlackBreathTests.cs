using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards02;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow02CombatTests;

namespace Wotr.Tests;

public class Shadow02BlackBreathTests
{
    internal static MatchState Choice(string card = Strike, int hits = 2, bool shadowAttacks = false) =>
        RerollShadow(Reveal(BeforeCombat(card, shadowAttacks), card), hits, Enumerable.Repeat(5, hits).ToArray());

    [Theory]
    [InlineData(Strike, false)]
    [InlineData(Wound, true)]
    public void BlackBreathAddsEliminationWithoutSpendingRerollHits(string card, bool shadowAttacks)
    {
        var state = Choice(card, shadowAttacks: shadowAttacks);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var choices = Choices(state).EligibleIds;
        Assert.Contains("base.character.gimli", choices);
        Assert.DoesNotContain(CharacterRules.Strider, choices); // Aragorn's Level is three.
        Assert.Contains(BlackBreathStep.Decline, choices);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Mouth] });
        var payload = new CommandPayload { Ids = ["base.character.gimli"] };
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.ChooseEffect, payload));
        Assert.Equal(payload.Ids, preview.RemovedIds);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, CommandKind.ChooseEffect, payload);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == "base.character.gimli").Location);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(2, BattleRoll.HitCount(force));
        Assert.Equal(0, force.Round.SpentHits);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void BlackBreathRespectsRerollHitThresholdsAndCanAlwaysDecline(int hits)
    {
        var state = Choice(hits: hits);
        if (hits == 0)
        {
            Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
            return;
        }
        var choices = Choices(state).EligibleIds;
        Assert.Equal(hits >= 3, choices.Contains(CharacterRules.Strider));
        Assert.Equal(hits >= 2, choices.Contains("base.character.gimli"));
        var leader = CombatCardFigures.Leaders(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Single();
        Assert.Contains(leader.Id, choices);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [BlackBreathStep.Decline] });
        Assert.Equal(leader.Location, state.Units.Single(u => u.Id == leader.Id).Location);
        Assert.Equal(hits, BattleLosses.Current(state)!.Hits);
    }

    [Fact]
    public void BlackBreathOnlyNeedsNazgulPresenceEvenWithWhiteRiderAndOtherLeadershipProvidingHits()
    {
        var state = BeforeCombat(Strike, companion: CharacterRules.White);
        Put(state, CharacterRules.Saruman, "orthanc");
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        shadow.Participants.Add(BattleFixture.Put(state, "isengard", UnitKind.Elite, shadow.Region).Id);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, BattleLeadership.Nazgul(state, battle.Force(Side.Shadow)).Values.Sum());
        Assert.True(EffectCatalog.Base.Combats[Strike].Condition.CanPlay(state, battle, Side.Shadow));
        state = RerollShadow(Reveal(state, Strike), 3, 5, 5, 5);
        Assert.Contains(CharacterRules.Grey, Choices(state).EligibleIds); // White version, Level three, zero current Leadership.
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Grey] });
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Grey).Location);
        Assert.Equal(3, BattleLosses.Current(state)!.Hits);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TiedHeroicDeathAndBlackBreathResolveDefenderFirstAndCannotUseSameLeader(bool shadowAttacks)
    {
        const string heroic = FreePeoplesCharacterCards02.AnotherWay;
        var state = BeforeCombat(Strike, shadowAttacks, fpCard: heroic);
        var fp = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        fp.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id));
        state = RerollShadow(Reveal(state, Strike, heroic), 1, 5);
        var leader = CombatCardFigures.Leaders(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).Single().Id;
        Assert.Equal(shadowAttacks ? Side.FreePeoples : Side.Shadow, state.Pending!.Owner);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = shadowAttacks ? [leader, HeroicDeathStep.HitPrefix + 1] : [leader] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Equal(shadowAttacks ? DecisionKind.BattleContinue : DecisionKind.Losses, state.Pending!.Kind);
        Assert.Single(state.Events, e => e.Kind is "BlackBreath" or "HeroicDeath");
    }

    [Fact]
    public void OrdinaryLeaderIsAnAdditionalCasualtyAndCompanionsOutsideTheForceAreNotTargets()
    {
        var state = Choice(hits: 3);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Participants.Remove(CharacterRules.Strider);
        Assert.DoesNotContain(CharacterRules.Strider, Choices(state).EligibleIds);
        var leader = CombatCardFigures.Leaders(state, force).Single().Id;
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [leader] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Equal(3, BattleLosses.Current(state)!.Hits);
    }

    [Theory]
    [InlineData(Strike)]
    [InlineData(Wound)]
    public void BlackBreathRequiresNazgulOrWitchKingAtPlayTime(string card)
    {
        var state = BeforeCombat(card);
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(Side.Shadow);
        force.Participants.RemoveAll(id => state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Nazgul));
        Assert.True(EffectCatalog.Base.Combats[card].Condition.CanPlay(state, battle, Side.Shadow));
        force.Participants.Remove(CharacterRules.WitchKing);
        Assert.False(EffectCatalog.Base.Combats[card].Condition.CanPlay(state, battle, Side.Shadow));
    }

    [Fact]
    public void TargetsMustStillBeLivingParticipantsButOriginalNazgulPresenceIsNotRechecked()
    {
        var state = Choice(hits: 3);
        var battle = BattleRuntime.Active(state)!;
        foreach (var unit in BattleRules.Units(state, battle.Force(Side.Shadow)).Where(u => u.Kind == UnitKind.Nazgul))
            BoardRules.RemoveCasualty(unit);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == CharacterRules.WitchKing));
        Assert.Contains(CharacterRules.Strider, Choices(state).EligibleIds);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == "base.character.gimli"));
        Assert.DoesNotContain("base.character.gimli", Choices(state).EligibleIds);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider] });
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Strider).Location);
        Assert.Equal(3, BattleLosses.Current(state)!.Hits);
    }
}
