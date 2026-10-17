using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;
using static Wotr.Tests.CardPackPersistenceTests;

namespace Wotr.Tests;

public class Shadow03PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestoresPaymentAndRetryWithoutRepeatingRandomSelection()
    {
        foreach (var card in new[] { Lure, Breaking, Worn, Crebain, Balrog, Lidless })
        {
            var state = RevealedEvent(card);
            Pool(state, HuntTests.Eye);
            state = await RestartDecision(state, CommandKind.PlayEvent, EventCost(state, card),
                new SequenceRandom(card is Lure or Breaking ? [0] : []));
            if (card == Lure)
                state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [LureChoiceStep.Corruption] }, new SequenceRandom());
            if (card == Lidless)
                state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = Choices(state).EligibleIds.Take(3).ToList() }, new SequenceRandom());
            Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
            Assert.Empty(state.Resolution.Frames);
        }
    }

    [PostgresFact]
    public async Task BreakingRestoresReplacementSelectionAndNewGuide()
    {
        var state = RevealedEvent(Breaking);
        Table(state, FreePeoplesCharacterCards01.Mithril);
        Pool(state, HuntTests.Eye, HuntTests.Three);
        state = Play(state, Breaking, 0);
        state = await RestartDecision(state, CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        state = await RestartDecision(state, CommandKind.ChooseEffect,
            new() { Ids = ["base.character.merry", "base.character.pippin", "base.character.boromir"] }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider }, new SequenceRandom());
        Assert.Equal(4, state.Fellowship.Companions.Count);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task WornRestoresCasualtyReactionPrivateDiscardAndHuntContinuation()
    {
        var state = Shadow03HuntTests.WornChoice();
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = WornReaction }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = [WornDiscardStep.RandomHand] }, new SequenceRandom(0));
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Single(state.Events, e => e.Kind == "CardDiscarded");
        Assert.Contains(Worn, state.Player(Side.Shadow).Table);
    }

    [PostgresFact]
    public async Task CrebainRestoresBeforeRollRerollAndTileDamageWithoutRepeatingDice()
    {
        var state = HuntTests.State(2, "moria");
        Table(state, Crebain);
        Pool(state, HuntTests.Three);
        state = HuntTests.Move(state, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = CrebainReaction }, new SequenceRandom(4, 0));
        state = await RestartDecision(state, CommandKind.RerollHunt, new() { Ids = ["hunt-die-1"] }, new SequenceRandom(4, 0));
        state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom());
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(1, state.Fellowship.Progress);
        Assert.Single(state.Events, e => e.Kind == "HuntRolled");
        Assert.DoesNotContain(Crebain, state.Player(Side.Shadow).Table);
    }

    [PostgresFact]
    public async Task BalrogRestoresDeclarationRouteReactionAndAdditionalStronghold()
    {
        var state = Declaration("hollin");
        Pool(state, RevealOne, HuntTests.Three);
        state = await RestartDecision(state, CommandKind.DeclareFellowship, new() { Route = ["moria", "hollin"] }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = BalrogReaction + ".declaration" }, new SequenceRandom(0));
        state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom(0));
        Assert.Equal("moria", HuntRuntime.Active(state)!.SourceId);
        state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom());
        Assert.Equal(7, state.Fellowship.Corruption);
        Assert.False(state.Fellowship.Hidden);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
    }

    [PostgresFact]
    public async Task WordsAndTableBalrogRestorePrivateCommitTargetAndImmediateLosses()
    {
        var state = MoriaBattle(shadowAttacks: true, table: true);
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new() { CardId = Balrog }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(3, 0, 0));
        var unit = BattleRules.Units(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)).First(BoardRules.IsArmyUnit);
        state = await RestartDecision(state, CommandKind.TakeBattleLosses, new() { Losses = [new() { UnitId = unit.Id, Kind = BattleLossKind.Remove }] },
            new SequenceRandom(new int[8]));
        Assert.DoesNotContain(Balrog, state.Player(Side.Shadow).Table);
        state = Reveal(BeforeCombat(Worn), Worn);
        state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = ["base.character.gimli"] }, new SequenceRandom(new int[8]));
        Assert.Contains("base.character.gimli", BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelledAbilities);
    }
}
