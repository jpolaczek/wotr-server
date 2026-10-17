using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class Shadow04PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresItsPaidCostAndContinuationWithoutRepeatedRolls()
    {
        foreach (var card in Cards)
        {
            var state = State(card, card == Grond);
            state = await RestartDecision(state, CommandKind.PlayEvent, EventCost(state, card),
                new SequenceRandom(card == Spells ? [0, 0] : []));
            Assert.Single(state.Dice, d => d.Location is DieLocation.Spent or DieLocation.Resolving);
            Assert.Equal(card is Palantir or Wormtongue, state.Player(Side.Shadow).Table.Contains(card) ||
                card == Wormtongue && state.Player(Side.Shadow).Discard.Contains(card));
        }
    }

    [PostgresFact]
    public async Task SpellsRestoreSourceTargetRollAndOpponentLossChoice()
    {
        var state = State(Spells);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "north-ithilien");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lossarnach");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "lossarnach");
        state = Play(state, Spells);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["osgiliath:field"] }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["north-ithilien:field"] }, new SequenceRandom(4, 0));
        var loss = state.Units.Single(u => u.Location == "north-ithilien");
        state = await RestartDecision(state, CommandKind.TakeEventLosses, new() { Losses = [new() { UnitId = loss.Id }] }, new SequenceRandom());
        Assert.Equal("casualties", state.Units.Single(u => u.Id == loss.Id).Location);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task GrondRestoresEachRoundPoliticalQueueSorcererAndFinalPalantirDraw()
    {
        var state = State(Grond, true);
        Table(state, Palantir);
        EventTests.GiveCard(state, Side.Shadow, Wormtongue);
        var cost = EventCost(state, Grond);
        cost.CardCost = DieFace.Event;
        state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, cost);
        state = await RestartDecision(state, CommandKind.AttackArmy, Army(state, "minas-tirith", "minas-tirith"), new SequenceRandom());
        Assert.Single(state.PoliticalReactions, r => r.Trigger == PoliticalTrigger.ArmyAttack);
        for (var round = 1; round <= 3; round++)
        {
            state = await RestartDecision(state, CommandKind.CommitCombatCard,
                new() { CardId = round == 1 ? Wormtongue : "" }, new SequenceRandom(new int[6]));
            if (state.Pending?.Kind == DecisionKind.CombatCard)
                state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[6]));
            while (state.Pending?.Kind == DecisionKind.BattleReroll)
                state = await RestartDecision(state, CommandKind.RerollBattle, new(), new SequenceRandom());
            if (round == 1)
                state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = "character.sorcerer" }, new SequenceRandom());
        }
        Assert.Null(BattleRuntime.Active(state));
        Assert.Equal("Deck", state.Pending!.Presentation);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] }, new SequenceRandom());
        Assert.Empty(state.PoliticalReactions);
        Assert.Equal(2, state.Player(Side.Shadow).Hand.Count);
        Assert.Equal(18, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task CaptainRestoresRecruitFlightAndArmyManeuverWithoutSecondDie()
    {
        foreach (var order in new[] { BlackCaptainStep.Recruit, BlackCaptainStep.Fly })
        {
            var state = Played(Captain);
            state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [order] }, new SequenceRandom());
            if (order == BlackCaptainStep.Fly)
                state = await RestartDecision(state, CommandKind.MoveShadowCharacters, new(), new SequenceRandom());
            state = await RestartDecision(state, CommandKind.MoveArmies, Army(state, "osgiliath", "north-ithilien"), new SequenceRandom());
            Assert.Equal("north-ithilien", CharacterRules.InPlay(state, CharacterRules.WitchKing)!.Location);
            Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        }
        var finished = await RestartDecision(Shadow04ManeuverTests.Orders(), CommandKind.FinishArmyEvent, new(), new SequenceRandom());
        Assert.Empty(finished.Resolution.Frames);
    }

    [PostgresFact]
    public async Task PalantirRemovalRestoresRingOwnershipAndCannotPayTwice()
    {
        var state = State();
        Table(state, Palantir);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = Cost(state, DieFace.Army);
        payload.RingId = state.Rings.First(r => r.Owner == Side.FreePeoples).Id;
        state = await RestartDecision(state, CommandKind.DiscardPalantir, payload, new SequenceRandom());
        Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == payload.RingId).Owner);
        Assert.True(state.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.Single(state.Player(Side.Shadow).Discard, c => c == Palantir);
    }

    [PostgresFact]
    public async Task CombatRestoresConditionalModifierAndPrivateSuppressionChoices()
    {
        var state = Shadow04CombatTests.Combat(Spells);
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new() { CardId = Spells }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[9]));
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CombatModifier);
        state = Reveal(Shadow04CombatTests.Combat(Palantir), Palantir);
        var points = Choices(state).EffectChoices.Where(c => c.SourceId == CharacterRules.WitchKing).Select(c => c.Id).ToList();
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = points }, new SequenceRandom(new int[9]));
        Assert.Equal(2, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.ForfeitedLeadership[CharacterRules.WitchKing]);
        state = Reveal(Shadow04CombatTests.Combat(Ringwraiths), Ringwraiths);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] }, new SequenceRandom(new int[9]));
        Assert.Contains("base.character.gimli", BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelledAbilities);
    }
}
