using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.BattleFixture;
using static Wotr.Domain.FreePeoplesCharacterCards01;

namespace Wotr.Tests;

public class CompanionCombatCardTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void EachGiftUsesDefendingRegionAndAddsToCombatAndLeaderRolls(int index, bool fpAttacks)
    {
        var card = SpecialTiles[index].Card;
        var state = State(fpAttacks ? Side.FreePeoples : Side.Shadow);
        var fpRegion = fpAttacks ? Origin : Target;
        for (var i = 0; i < 2; i++)
            Put(state, "gondor", UnitKind.Regular, fpRegion);
        Put(state, "gondor", UnitKind.Leader, fpRegion);
        for (var i = 0; i < 3; i++)
            Put(state, "sauron", UnitKind.Regular, fpAttacks ? Target : Origin);
        EventTests.GiveCard(state, Side.FreePeoples, card);
        state.Fellowship.RegionId = Target;
        state = Begin(state, state.Units.Where(u => u.Location == Origin).Select(u => u.Id));
        var battle = BattleRuntime.Active(state)!;
        var cards = new BattleCards(EffectCatalog.Base);
        Assert.True(cards.CanCommit(state, battle, Side.FreePeoples, card));
        state.Fellowship.RegionId = Origin;
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, card));
        state.Fellowship.RegionId = Target;
        state.Fellowship.MordorStep = 0;
        Assert.False(cards.CanCommit(state, battle, Side.FreePeoples, card));
        state.Fellowship.MordorStep = null;
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = fpAttacks ? card : "" });
        var rolls = fpAttacks ? new[] { 0, 3, 0, 0, 0 } : [0, 0, 0, 0, 3];
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = fpAttacks ? "" : card }, new SequenceRandom(rolls));
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        Assert.False(force.Round.Dice[0].Hit); // Natural 1 remains a miss.
        Assert.True(force.Round.Dice[1].Hit); // Four plus Gift hits.
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(1, force.Round.RerollModifier);
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = [force.Round.Dice[0].Id] }, new SequenceRandom(3));
        force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        Assert.True(force.Round.Dice[0].RerollHit);
        Assert.Equal(2, BattleRoll.HitCount(force));
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
    }

    internal static MatchState BladeChoice(EffectCatalog? catalog = null, string opposingCard = "")
    {
        var state = State();
        Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "gondor", UnitKind.Leader, Origin);
        CharacterFixture.Put(state, CharacterHuntTests.Merry, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Nazgul, Target);
        CharacterFixture.Put(state, CharacterRules.WitchKing, Target);
        CharacterFixture.Put(state, CharacterRules.Mouth, Target);
        CharacterFixture.Put(state, CharacterRules.Saruman, "orthanc");
        EventTests.GiveCard(state, Side.FreePeoples, Mithril);
        if (opposingCard != "")
            EventTests.GiveCard(state, Side.Shadow, opposingCard);
        var ids = state.Units.Where(u => u.Location == Origin).Select(u => u.Id).Append(CharacterHuntTests.Merry);
        state = Begin(state, ids, catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = Mithril }, catalog: catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = opposingCard }, new SequenceRandom(0, 0, 0, 0), catalog);
        var misses = BattleRoll.Failed(BattleRuntime.Active(state)!.Attacker);
        state = Execute(state, CommandKind.RerollBattle, new() { Ids = misses }, catalog: catalog);
        return Execute(state, CommandKind.RerollBattle, random: new SequenceRandom(5, 0), catalog: catalog);
    }

    [Fact]
    public void BladeConsumesOneLeaderHitAndChoosesOnlyParticipatingMinions()
    {
        var state = BladeChoice();
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal(new[] { CharacterRules.WitchKing, CharacterRules.Mouth }.Order(), state.Pending.PrivateChoices.Order());
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Saruman] }));
        state = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [CharacterRules.WitchKing] });
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(0, BattleRoll.HitCount(battle.Attacker));
        Assert.Equal(1, battle.Attacker.Round.SpentHits);
        Assert.True(battle.Attacker.Round.Dice[0].RerollHit);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
        Assert.Equal(2, BattleRules.Strength(state, battle.Defender));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        state = Execute(state, CommandKind.StopBattle);
        Assert.Empty(state.Resolution.Frames);
        Assert.Contains(Mithril, state.Player(Side.FreePeoples).Discard);
    }

    [Fact]
    public void BladeSurvivesHobbitEliminationAtEarlierInitiativeButNeedsALeaderHitAndTarget()
    {
        var catalog = OpponentEffect(new KillHobbitStep(), 6);
        var state = BladeChoice(catalog, ShCard);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == CharacterHuntTests.Merry).Location);
        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        var step = new BladeOfWesternesseStep();
        var execution = EffectRuntime.Current(state)!.Effect!;
        var force = BattleRuntime.Active(state)!.Attacker;
        force.Round.Dice[0].RerollHit = false;
        force.Round.Dice[0].Hit = true;
        Assert.Empty(step.Candidates(state, execution)); // A combat hit cannot pay Blade.
        force.Round.Dice[0].RerollHit = true;
        foreach (var c in state.Characters.Where(c => c.Side == Side.Shadow).ToArray())
            CharacterLifecycle.Eliminate(state, c);
        Assert.Empty(step.Candidates(state, execution));
        Assert.Equal(1, BattleRoll.HitCount(force));
    }

    internal static MatchState MightyChoice(EffectCatalog? catalog = null, string opposingCard = "", params int[] rolls)
    {
        var state = State();
        Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "gondor", UnitKind.Regular, Origin);
        var aragorn = CharacterFixture.Put(state, CharacterRules.Strider, Origin);
        CharacterLifecycle.ApplyVersion(aragorn, Wotr.Domain.Content.CharacterCatalog.Base[CharacterRules.Aragorn]);
        CharacterFixture.Put(state, "base.character.gimli", Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        Put(state, "sauron", UnitKind.Regular, Target);
        EventTests.GiveCard(state, Side.FreePeoples, Axe);
        if (opposingCard != "")
            EventTests.GiveCard(state, Side.Shadow, opposingCard);
        var ids = state.Units.Where(u => u.Location == Origin).Select(u => u.Id).Concat(state.Characters.Select(c => c.Id));
        state = Begin(state, ids, catalog);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = Axe }, catalog: catalog);
        return Execute(state, CommandKind.CommitCombatCard, new() { CardId = opposingCard },
            new SequenceRandom(rolls.Length > 0 ? rolls : [0, 0, 0, 0, 0, 0]), catalog);
    }

    [Fact]
    public void MightyCommitsCompanionAndMissTogetherForfeitingFullLeadership()
    {
        var state = MightyChoice();
        var force = BattleRuntime.Active(state)!.Attacker;
        var die = force.Round.Dice[0];
        die.CannotReroll = true; // Still a legal conversion; picking this die has strategic value.
        var option = Assert.Single(new EffectOptions().GetOptions(state, Side.FreePeoples));
        Assert.Equal(2, option.Minimum);
        Assert.Equal(2, option.EffectChoices.Count(c => c.SelectionGroup == "Companion"));
        Assert.Equal(4, option.EffectChoices.Count(c => c.SelectionGroup == "Miss"));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [die.Id] }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [die.Id, force.Round.Dice[1].Id] }));
        Rejected(state, Command(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider, "base.character.gimli"] }));
        state = Execute(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider, die.Id] });
        force = BattleRuntime.Active(state)!.Attacker;
        Assert.Equal(2, force.Round.ForfeitedLeadership[CharacterRules.Strider]);
        Assert.Equal(1, BattleLeadership.Total(state, force));
        Assert.True(force.Round.Dice[0].ConvertedHit);
        Assert.False(force.Round.Dice[0].Hit);
        Assert.DoesNotContain(die.Id, BattleRoll.Failed(force));
        Assert.Equal(1, BattleRoll.HitCount(force));
        Assert.Equal(DecisionKind.BattleReroll, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MightyCannotUseCancelledLeadershipOrAnAbsentLeaderReroll(bool cancelReroll)
    {
        var catalog = OpponentEffect(new CancelLeadershipStep(cancelReroll), 3);
        var state = MightyChoice(catalog, ShCard);
        var force = BattleRuntime.Active(state)!.Attacker;
        Assert.Empty(force.Round.ForfeitedLeadership);
        Assert.Equal(0, BattleRoll.HitCount(force));
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
    }

    [Fact]
    public void CombatPresenceConditionsExcludeNonparticipantsAndNoMissSkipsMightyCost()
    {
        var state = MightyChoice(rolls: [5, 5, 5, 5, 0, 0]);
        var battle = BattleRuntime.Active(state)!;
        Assert.Empty(battle.Attacker.Round.ForfeitedLeadership);
        Assert.Equal(4, BattleRoll.HitCount(battle.Attacker));
        var condition = new CompanionInBattle();
        Assert.True(condition.CanPlay(state, battle, Side.FreePeoples));
        battle.Attacker.Participants.RemoveAll(id => state.Characters.Any(c => c.Id == id));
        Assert.False(condition.CanPlay(state, battle, Side.FreePeoples));
        Assert.False(new CompanionInBattle(true).CanPlay(state, battle, Side.FreePeoples));
    }

    private static EffectCatalog OpponentEffect(EffectStep step, int initiative) => new(
        EffectCatalog.Base.Programs.Values.Where(p => p.Id != EffectCatalog.ReactionProgram).Append(new("test.opponent", [step])),
        EffectCatalog.Base.Events.Values, EffectCatalog.Base.Reactions.Values,
        EffectCatalog.Base.Combats.Values.Where(c => c.CardId != ShCard).Append(new(ShCard, new UnconditionalCombat(), [new(initiative, "test.opponent")])));

    private sealed record KillHobbitStep : EffectStep
    {
        public override void Apply(EffectContext context) => CharacterLifecycle.Eliminate(context.State,
            context.State.Characters.Single(c => c.Id == CharacterHuntTests.Merry));
    }

    private sealed record CancelLeadershipStep(bool Reroll) : EffectStep
    {
        public override void Apply(EffectContext context)
        {
            var force = BattleRuntime.Active(context.State)!.Opponent(context.Owner);
            if (Reroll)
                force.Round.RerollLimit = 0;
            else
                force.Round.CancelledLeadership.UnionWith(BattleLeadership.Available(context.State, force).Keys);
        }
    }
}
