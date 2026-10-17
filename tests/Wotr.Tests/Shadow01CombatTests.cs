using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards01;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow01CombatTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void DarkLordRequiresFellowshipFigureAtBattleAndModifiesBothRolls(int index, bool shadowAttacks)
    {
        var card = SpecialTiles[index].Card;
        var state = BeforeCombat(card, shadowAttacks);
        var battle = BattleRuntime.Active(state)!;
        var condition = EffectCatalog.Base.Combats[card].Condition;
        state.Fellowship.Progress = 5;
        state.Fellowship.Hidden = false;
        Assert.True(condition.CanPlay(state, battle, Side.Shadow));
        state.Fellowship.Hidden = true;
        Assert.True(condition.CanPlay(state, battle, Side.Shadow));
        state.Fellowship.RegionId = battle.Attacker.Region;
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
        state.Fellowship.RegionId = battle.Region;
        state.Fellowship.MordorStep = 1;
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
        state.Fellowship.MordorStep = null;
        var rolls = new int[8];
        var first = shadowAttacks ? 0 : 5;
        rolls[first + 1] = 3;
        rolls[first + 2] = 5;
        state = Reveal(state, card, dice: rolls);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.All(force.Round.Dice, die => Assert.Equal(1, die.Modifier));
        Assert.False(force.Round.Dice[0].Hit);
        Assert.True(force.Round.Dice[1].Hit);
        Assert.True(force.Round.Dice[2].Hit);
        var id = force.Round.Dice[0].Id;
        state = RerollShadow(state, id, 3);
        var rerolled = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice.Single(d => d.Id == id);
        Assert.Equal(1, rerolled.RerollModifier);
        Assert.True(rerolled.RerollHit);
    }

    [Theory]
    [InlineData(OrcPatrol, false, false)]
    [InlineData(IsildursBane, false, true)]
    [InlineData(OrcPatrol, true, true)]
    [InlineData(IsildursBane, true, false)]
    public void CruelCostsExactlyTwoChosenNazgulPointsAndOnlyModifiesCombat(string card, bool shadowAttacks, bool split)
    {
        var state = Reveal(BeforeCombat(card, shadowAttacks), card);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal("NazgulLeadership", state.Pending.Presentation);
        var options = Choices(state);
        Assert.Equal(3, options.EffectChoices.Count);
        Assert.DoesNotContain(options.EffectChoices, c => c.SourceId == CharacterRules.Mouth);
        var witchKing = options.EffectChoices.Where(c => c.SourceId == CharacterRules.WitchKing).Select(c => c.Id).ToArray();
        var nazgul = options.EffectChoices.Single(c => c.SourceId != CharacterRules.WitchKing).Id;
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [witchKing[0]] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [witchKing[0], witchKing[0]] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [witchKing[0], CharacterRules.Mouth] });
        var before = StateCodec.Write(state);
        var payload = new CommandPayload { Ids = split ? [witchKing[0], nazgul] : [.. witchKing] };
        Assert.True(new PreviewService(Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.ChooseEffect, payload)).Legal);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, payload, new SequenceRandom(new int[8]));
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(2, force.Round.ForfeitedLeadership.Values.Sum());
        Assert.Equal(split ? 1 : 2, force.Round.ForfeitedLeadership[CharacterRules.WitchKing]);
        foreach (var viewer in new[] { Side.FreePeoples, Side.Shadow })
        {
            var projected = BattleProjection.View(state, viewer)!;
            var costs = (projected.Attacker.Side == Side.Shadow ? projected.Attacker : projected.Defender).ForfeitedLeadership;
            Assert.Equal(split ? 1 : 2, costs[CharacterRules.WitchKing]);
            costs.Clear();
            Assert.Equal(2, force.Round.ForfeitedLeadership.Values.Sum());
        }
        Assert.Equal(1, BattleLeadership.Nazgul(state, force).Values.Sum());
        Assert.All(force.Round.Dice, d => { Assert.Equal(1, d.Modifier); Assert.False(d.Hit); });
        var id = force.Round.Dice[0].Id;
        state = RerollShadow(state, id, 3);
        var rerolled = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice.Single(d => d.Id == id);
        Assert.Equal(0, rerolled.RerollModifier);
        Assert.False(rerolled.RerollHit);
    }

    [Theory]
    [InlineData(OrcPatrol, false)]
    [InlineData(IsildursBane, true)]
    public void AdvantageousPositionCountersBonusButStillRequiresForfeit(string card, bool shadowAttacks)
    {
        const string position = "base.fp.strategy.03";
        var state = Reveal(BeforeCombat(card, shadowAttacks, fpCard: position), card, position);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = Choices(state).EligibleIds.Take(2).ToList() }, new SequenceRandom(new int[8]));
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(2, force.Round.ForfeitedLeadership.Values.Sum());
        Assert.All(force.Round.Dice, d => Assert.Equal(0, d.Modifier));
    }

    [Theory]
    [InlineData(OrcPatrol)]
    [InlineData(IsildursBane)]
    public void DaringDefianceCancelsWholeEffectWithoutTakingNazgulLeadership(string card)
    {
        const string daring = FreePeoplesCharacterCards02.Alone;
        var state = Reveal(BeforeCombat(card, fpCard: daring), card, daring);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.True(force.Round.CardCancelled);
        Assert.Empty(force.Round.ForfeitedLeadership);
        Assert.All(force.Round.Dice, d => Assert.Equal(0, d.Modifier));
    }

    [Fact]
    public void CruelRequiresAvailableNazgulLeadershipAndWhiteRiderCancelsItBeforeCards()
    {
        var state = BeforeCombat(OrcPatrol);
        var battle = BattleRuntime.Active(state)!;
        var force = battle.Force(Side.Shadow);
        var condition = EffectCatalog.Base.Combats[OrcPatrol].Condition;
        var nazgul = BattleRules.Units(state, force).Single(u => u.Kind == UnitKind.Nazgul);
        force.Round.CancelledLeadership.Add(CharacterRules.WitchKing);
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
        force.Round.CancelledLeadership.Clear();
        force.Round.ForfeitedLeadership[CharacterRules.WitchKing] = 2;
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));
        force.Round.ForfeitedLeadership.Clear();
        force.Participants.Remove(nazgul.Id);
        Assert.True(condition.CanPlay(state, battle, Side.Shadow)); // The Witch-king alone provides two.
        force.Participants.Remove(CharacterRules.WitchKing);
        Assert.True(BattleLeadership.Total(state, force) >= 2); // Mouth is not a Nazgûl.
        Assert.False(condition.CanPlay(state, battle, Side.Shadow));

        state = BeforeCombat(OrcPatrol, companion: CharacterRules.White);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        state = Do(state, CommandKind.CommitCombatCard);
        Reject(state, CommandKind.CommitCombatCard, new() { CardId = OrcPatrol });
        Assert.DoesNotContain(new BattleOptions().GetOptions(state, Side.Shadow).SelectMany(o => o.EligibleIds), id => id == OrcPatrol);
    }

    [Fact]
    public void WitchKingAlonePaysAutomaticallyAndCostExpiresNextRound()
    {
        var state = BeforeCombat(OrcPatrol);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        force.Participants.RemoveAll(id => state.Units.Any(u => u.Id == id && u.Kind == UnitKind.Nazgul));
        state = Reveal(state, OrcPatrol);
        Assert.Equal(2, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ForfeitedLeadership[CharacterRules.WitchKing]);
        state = CardPackBattleFixture.NoRerolls(state);
        if (state.Pending!.Kind == DecisionKind.Reaction)
            state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.ContinueBattle);
        state = Do(state, CommandKind.HoldBattle);
        if (state.Pending!.Kind == DecisionKind.Reaction)
            state = Do(state, CommandKind.DeclineReaction);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Empty(force.Round.ForfeitedLeadership);
        Assert.Equal(0, force.Round.CombatModifier);
        Assert.Equal(2, BattleLeadership.Nazgul(state, force).Values.Sum());
    }

    private static MatchState RerollShadow(MatchState state, string id, int raw)
    {
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle,
                new() { Ids = state.Pending.Owner == Side.Shadow ? [id] : [] }, new SequenceRandom(raw));
        return state;
    }
}
