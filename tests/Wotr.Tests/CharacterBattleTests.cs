using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.CharacterFixture;

namespace Wotr.Tests;

public class CharacterBattleTests
{
    [Theory]
    [InlineData("gimli", 1)]
    [InlineData("boromir", 1)]
    [InlineData("legolas", 1)]
    [InlineData("gandalf-grey", 1)]
    [InlineData("strider", 1)]
    [InlineData("aragorn", 1)]
    [InlineData("gandalf-white", 0)]
    [InlineData("merry", 0)]
    [InlineData("pippin", 0)]
    public void CaptainsAddStrengthOnlyWhenParticipatingAndNeverExceedFiveDice(string id, int bonus)
    {
        var state = BattleFixture.State();
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        var companion = Put(state, "base.character." + id, BattleFixture.Origin);
        var force = new BattleForce { Side = Side.FreePeoples, Region = BattleFixture.Origin, Participants = [unit.Id, companion.Id] };
        Assert.Equal(1 + bonus, BattleRoll.DiceCount(state, force));
        force.Participants.Remove(companion.Id);
        Assert.Equal(1, BattleRoll.DiceCount(state, force));
        force.Participants.Add(companion.Id);
        for (var i = 0; i < 5; i++)
            force.Participants.Add(BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin).Id);
        Assert.Equal(5, BattleRoll.DiceCount(state, force));
    }

    [Fact]
    public void WhiteRiderIsOptionalBeforeCardsCancelsNazgulIncludingWitchKingForWholeBattle()
    {
        var state = BattleFixture.State();
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        var white = Put(state, CharacterRules.White, BattleFixture.Origin);
        var wk = Put(state, CharacterRules.WitchKing, BattleFixture.Target);
        var nazgul = BattleFixture.Put(state, "sauron", UnitKind.Nazgul, BattleFixture.Target);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Target);
        state = Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, [unit.Id, white.Id]));
        Assert.Equal("battle.start", state.Pending!.Window);
        var declined = Do(StateCodec.Copy(state), CommandKind.DeclineReaction);
        Assert.Equal(3, BattleLeadership.Total(declined, BattleRuntime.Active(declined)!.Defender));
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "character.white-rider" });
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
        Assert.Equal(0, BattleLeadership.Total(state, battle.Attacker));
        Assert.Equal(0, BattleLeadership.Total(state, battle.Defender));
        Assert.Contains(wk.Id, battle.Defender.Participants);
        Assert.Contains(nazgul.Id, battle.Defender.Participants);
        BattleRuntime.NextRound(battle);
        Assert.Equal(0, BattleLeadership.Total(state, battle.Defender));
        Assert.False(BattleLeadership.CanForfeit(state, battle.Attacker, new Dictionary<string, int> { [white.Id] = 1 }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SorcererWaitsForRetreatOrStopThenDrawsEvenIfCardWasCancelled(bool cancelled)
    {
        var catalog = CombatCatalog(cancelled ? new CancelShadowCardStep() : new NoEffectStep());
        var state = Field(catalog);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = BattleFixture.ShCard }, catalog: catalog);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = BattleFixture.FpCard }, new SequenceRandom(0, 0), catalog);
        state = Do(state, CommandKind.RerollBattle, catalog: catalog);
        Assert.Equal(DecisionKind.BattleContinue, state.Pending!.Kind);
        Assert.DoesNotContain(state.Events, e => e.Kind == "CardDrawn");
        state = Do(state, CommandKind.ContinueBattle, catalog: catalog);
        Assert.Equal(DecisionKind.BattleRetreat, state.Pending!.Kind);
        var target = BattleMovement.Retreats(state, BattleRuntime.Active(state)!).First();
        state = Do(state, CommandKind.RetreatBattle, new() { RegionId = target }, catalog: catalog);
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        Assert.Contains(BattleFixture.ShCard, state.Player(Side.Shadow).Discard);
        var count = state.Player(Side.Shadow).Hand.Count;
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect, new() { ReactionId = "character.sorcerer" }, catalog: catalog);
        Assert.Equal(count + 1, state.Player(Side.Shadow).Hand.Count);
        Assert.Equal(DecisionKind.BattleAdvance, state.Pending!.Kind);
        Assert.Equal("strategy", Wotr.Domain.Content.CardCatalog.Base[state.Player(Side.Shadow).Hand.Last()].Deck);
    }

    [Fact]
    public void SorcererDoesNotTriggerForKilledWitchKingOrSecondRoundAndScoutsCanEndFirstRoundEarly()
    {
        var catalog = CombatCatalog(new EndEarlyStep());
        var state = Field(catalog);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = BattleFixture.ShCard }, catalog: catalog);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = BattleFixture.FpCard }, catalog: catalog);
        Assert.Empty(state.RandomSamples);
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        state = Do(state, CommandKind.DeclineReaction, catalog: catalog);
        Assert.Empty(state.Resolution.Frames);

        state = Field(CombatCatalog(new NoEffectStep()));
        var battle = BattleRuntime.Active(state)!;
        battle.Force(Side.Shadow).Round.CardId = BattleFixture.ShCard;
        var condition = new CharacterReactionCondition(CharacterReaction.Sorcerer);
        Assert.True(condition.CanPlay(state, Side.Shadow));
        battle.Round = 2;
        Assert.False(condition.CanPlay(state, Side.Shadow));
        battle.Round = 1;
        CharacterLifecycle.Eliminate(state, state.Characters.Single());
        Assert.False(condition.CanPlay(state, Side.Shadow));
    }

    [Fact]
    public void SorcererWaitsForSiegeElitePaymentAndDisappearsIfThatPaymentKillsHisArmy()
    {
        var catalog = CombatCatalog(new NoEffectStep());
        var state = BattleFixture.State(Side.Shadow);
        var wk = Put(state, CharacterRules.WitchKing, "minas-tirith");
        var elite = BattleFixture.Put(state, "sauron", UnitKind.Elite, "minas-tirith");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = true;
        HandRules.Add(state.Player(Side.Shadow), BattleFixture.ShCard);
        state = Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, [elite.Id, wk.Id], "minas-tirith", "minas-tirith"), catalog: catalog);
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = BattleFixture.ShCard }, catalog: catalog);
        state = Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(0, 0), catalog: catalog);
        state = Do(state, CommandKind.RerollBattle, catalog: catalog);
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        var exhausted = StateCodec.Copy(state);
        foreach (var regular in exhausted.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular))
            regular.Location = "barad-dur";
        exhausted = Do(exhausted, CommandKind.ExtendSiege, new() { Ids = [elite.Id] }, catalog: catalog);
        Assert.DoesNotContain(exhausted.Resolution.Frames, f => f.Battle is not null);
        Assert.Equal("casualties", exhausted.Characters.Single().Location);
        state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] }, catalog: catalog);
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        Assert.Equal(1, BattleRuntime.Active(state)!.Round);
        state = Do(state, CommandKind.DeclineReaction, catalog: catalog);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.Equal(DecisionKind.CombatCard, state.Pending!.Kind);
    }

    private static MatchState Field(EffectCatalog catalog)
    {
        var state = BattleFixture.State(Side.Shadow);
        var unit = BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Origin);
        var wk = Put(state, CharacterRules.WitchKing, BattleFixture.Origin);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Target);
        HandRules.Add(state.Player(Side.Shadow), BattleFixture.ShCard);
        HandRules.Add(state.Player(Side.FreePeoples), BattleFixture.FpCard);
        return Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, [unit.Id, wk.Id]), catalog: catalog);
    }

    private static EffectCatalog CombatCatalog(EffectStep fp) => new(
        [new("test.fp", [fp]), new("test.sh", []), .. CharacterReactions.Programs], [], CharacterReactions.Reactions,
        [new(BattleFixture.FpCard, new UnconditionalCombat(), [new(0, "test.fp")]), new(BattleFixture.ShCard, new UnconditionalCombat(), [new(3, "test.sh")])]);

    private sealed record NoEffectStep : EffectStep { public override void Apply(EffectContext context) { } }
    private sealed record EndEarlyStep : EffectStep { public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.EndedByEffect = true; }
    private sealed record CancelShadowCardStep : EffectStep { public override void Apply(EffectContext context) => BattleRuntime.Active(context.State)!.Force(Side.Shadow).Round.CardCancelled = true; }
}
