using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;
using static Wotr.Tests.CardPackBattleFixture;
using static Wotr.Domain.FreePeoplesCharacterCards02;

namespace Wotr.Tests;

public class CasualtyCombatCardTests
{
    internal static MatchState HeroicChoice() => NoRerolls(Reveal(BeforeCards(AnotherWay), AnotherWay,
        dice: [0, 0, 0, 0, 0, 5, 5, 5]));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void HeroicDeathCommitsOneCompanionAndChosenHitCountWithNoLeadershipRequirement(int hits)
    {
        var state = HeroicChoice();
        var battle = BattleRuntime.Active(state)!;
        battle.Attacker.Round.CancelledLeadership.Add(CharacterRules.Strider);
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider] }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [HeroicDeathStep.HitPrefix + 1, HeroicDeathStep.HitPrefix + 2] }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli", HeroicDeathStep.HitPrefix + 3] }));
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            Command(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider, HeroicDeathStep.HitPrefix + hits] }));
        Assert.Equal([CharacterRules.Strider], preview.RemovedIds);
        Assert.Equal(before, StateCodec.Write(state));
        state = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect,
            new() { Ids = [CharacterRules.Strider, HeroicDeathStep.HitPrefix + hits] });
        battle = BattleRuntime.Active(state)!;
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.Strider).Location);
        Assert.Equal(hits, battle.Attacker.Round.CancelHits);
        Assert.Equal(3, BattleRoll.HitCount(battle.Defender));
        Assert.Equal(hits == 3 ? DecisionKind.BattleContinue : DecisionKind.Losses, state.Pending!.Kind);
        if (hits < 3)
            Assert.Equal(3 - hits, BattleLosses.Current(state)!.Hits);
    }

    [Fact]
    public void HeroicDeathLeaderCancelsExactlyOneAndDeclineCostsNothing()
    {
        var state = HeroicChoice();
        var leader = BattleRules.Units(state, BattleRuntime.Active(state)!.Attacker).Single(u => u.Kind == UnitKind.Leader).Id;
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [leader, HeroicDeathStep.HitPrefix + 0] }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [leader, HeroicDeathStep.HitPrefix + 2] }));
        var declined = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [HeroicDeathStep.Decline] });
        Assert.Equal(3, BattleLosses.Current(declined)!.Hits);
        Assert.Equal(Origin, declined.Units.Single(u => u.Id == leader).Location);
        state = Execute(state, CommandKind.ChooseEffect, new() { Ids = [leader, HeroicDeathStep.HitPrefix + 1] });
        Assert.Equal("casualties", state.Units.Single(u => u.Id == leader).Location);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
    }

    [Fact]
    public void HeroicDeathIgnoresSpecialHitsAndCannotSacrificeAlreadyEliminatedFigures()
    {
        var state = HeroicChoice();
        var battle = BattleRuntime.Active(state)!;
        var execution = EffectRuntime.Current(state)!.Effect!;
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == CharacterRules.Strider));
        Assert.DoesNotContain(CharacterRules.Strider, new HeroicDeathStep().Candidates(state, execution));
        battle.Defender.Round.Dice.ForEach(d => d.Hit = false);
        battle.Defender.Round.SpecialHits = 4;
        Assert.Empty(new HeroicDeathStep().Candidates(state, execution));
    }

    internal static MatchState FatefulChoice(int hits = 2, EffectCatalog? catalog = null)
    {
        var state = Reveal(BeforeCards(Bilbo, catalog: catalog), Bilbo, catalog);
        var dice = BattleRuntime.Active(state)!.Attacker.Round.Dice;
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = dice.Take(2).Select(d => d.Id).ToList() }, catalog: catalog);
        return Execute(state, CommandKind.RerollBattle, random: new SequenceRandom(hits > 0 ? 5 : 0, hits > 1 ? 5 : 0), catalog: catalog);
    }

    [Fact]
    public void FatefulAddsChosenNazgulOrMinionEliminationWithoutSpendingItsRerollHits()
    {
        var state = FatefulChoice();
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Contains(CharacterRules.WitchKing, state.Pending.PrivateChoices);
        Assert.Contains(CharacterRules.Mouth, state.Pending.PrivateChoices);
        Assert.DoesNotContain(FatefulStrikeStep.Decline, state.Pending.PrivateChoices);
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider] }));
        state = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [CharacterRules.WitchKing] });
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
        var force = BattleRuntime.Active(state)!.Attacker;
        Assert.Equal(2, BattleRoll.HitCount(force));
        Assert.Equal(0, force.Round.SpentHits);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
    }

    [Fact]
    public void OneFatefulHitEliminatesOrdinaryNazgulButNotWitchKingAndZeroHitsDoNothing()
    {
        var state = FatefulChoice(1);
        Assert.DoesNotContain(state.Units, u => u.Kind == UnitKind.Nazgul && u.Location == Target);
        Assert.Equal(Target, state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        state = FatefulChoice(0);
        Assert.Contains(state.Units, u => u.Kind == UnitKind.Nazgul && u.Location == Target);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
    }

    [Fact]
    public void FatefulSurvivesLossOfAllOwnLeadersAndCanDeclineMinionOnlyAlternative()
    {
        var catalog = Opponent(new EliminateFpLeadersStep(), 6);
        var state = FatefulChoice(catalog: catalog);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.All(state.Characters.Where(c => c.Side == Side.FreePeoples), c => Assert.Equal("casualties", c.Location));
        foreach (var nazgul in BattleRules.Units(state, BattleRuntime.Active(state)!.Defender).Where(u => u.Kind == UnitKind.Nazgul))
            BoardRules.RemoveCasualty(nazgul);
        var options = new EffectOptions(catalog).GetOptions(state, Side.FreePeoples).Single();
        Assert.Contains(FatefulStrikeStep.Decline, options.EligibleIds);
        state = Execute(state, CommandKind.ChooseEffect, new() { Ids = [FatefulStrikeStep.Decline] }, catalog: catalog);
        Assert.Equal(Target, state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
        Assert.Equal(2, BattleLosses.Current(state)!.Hits);
    }

    [Fact]
    public void EarlierBlackBreathLeavesNoFigureForHeroicDeath()
    {
        var catalog = Opponent(new EliminateFpLeadersStep(), 6);
        var state = NoRerolls(Reveal(BeforeCards(AnotherWay, catalog: catalog), AnotherWay, catalog,
            0, 0, 0, 0, 0, 5, 5, 5), catalog);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(3, BattleLosses.Current(state)!.Hits);
        Assert.Equal(0, BattleRuntime.Active(state)!.Attacker.Round.CancelHits);
    }

    private sealed record EliminateFpLeadersStep : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var force = BattleRuntime.Active(context.State)!.Force(Side.FreePeoples);
            foreach (var companion in CombatCardFigures.Companions(context.State, force))
                CharacterLifecycle.Eliminate(context.State, companion);
            foreach (var leader in CombatCardFigures.Leaders(context.State, force))
                BoardRules.RemoveCasualty(leader);
        }
    }
}
