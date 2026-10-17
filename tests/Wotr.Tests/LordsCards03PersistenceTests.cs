using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Tests;

public class LordsCards03PersistenceTests
{
    [PostgresFact]
    public async Task MordorReplacementRestartsAdvanceOrVictoryAndReturnsTheHeldTile()
    {
        foreach (var corrupted in new[] { false, true })
            await LordsPersistenceTests.Restart(LordsWeShallGetItInteractionTests.MordorReplacement(corrupted),
                CommandKind.DeclineReaction, new());
    }

    [PostgresFact]
    public async Task MadnessRestartsPaymentTargetRollLossesAndRecruitment()
    {
        var state = LordsMadnessTests.State();
        await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, LordsShadowCardsFixture.Cost(state, LordsShadowCards.Madness));
        state = LordsMadnessTests.Window();
        var target = new CommandPayload { Ids = [LordsMadnessTests.Target] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, target, 4, 0, 0);
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, target, new SequenceRandom(4, 0, 0));
        var unit = EventLosses.Current(state)!.Target.Units(state).First(u => u.Kind == UnitKind.Regular);
        var loss = new CommandPayload { Losses = [BattleFixture.Loss(unit.Id)] };
        await LordsPersistenceTests.Restart(state, CommandKind.TakeEventLosses, loss);
        state = LordsFixture.Do(state, CommandKind.TakeEventLosses, loss);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["carrock"] });
    }

    [PostgresFact]
    public async Task AnnihilatedGarrisonRetainsItsRecruitmentBranchAfterRestart()
    {
        var state = LordsMadnessTests.Attack(LordsMadnessRecruitTests.Siege(), "moria", 4, 0, 0);
        var unit = EventLosses.Current(state)!.Target.Units(state).Single();
        await LordsPersistenceTests.Restart(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(unit.Id)] });
    }

    [PostgresFact]
    public async Task RecruitmentAndExcessRemovalRetainOnePaidAction()
    {
        var state = LordsMadnessTests.Window();
        for (var i = 0; i < 9; i++)
            BattleFixture.Put(state, "southrons", UnitKind.Regular, "carrock");
        state = LordsMadnessTests.Attack(state, LordsMadnessTests.Target, 0, 0, 0);
        var payload = new CommandPayload { Ids = ["carrock"] };
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, payload);
        state = LordsFixture.Do(state, CommandKind.ChooseEffect, payload);
        await LordsPersistenceTests.Restart(state, CommandKind.RemoveExcess, new() { Ids = [state.Pending!.PrivateChoices[0]] });
    }

    [PostgresFact]
    public async Task SwarmRestartsBothSecretCommitsAndTheActualReroll()
    {
        foreach (var fpAttacks in new[] { true, false })
        {
            var state = LordsSwarmTests.Before(fpAttacks);
            var first = new CommandPayload { CardId = fpAttacks ? "" : LordsShadowCards.Madness };
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, first);
            state = LordsFixture.Do(state, CommandKind.CommitCombatCard, first);
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard,
                new() { CardId = fpAttacks ? LordsShadowCards.Madness : "" }, new int[8]);
        }
        var reroll = LordsSwarmTests.Reveal(LordsSwarmTests.Before());
        reroll = LordsFixture.Do(reroll, CommandKind.RerollBattle);
        var die = BattleRuntime.Active(reroll)!.Force(Side.Shadow).Round.Dice[0].Id;
        await LordsPersistenceTests.Restart(reroll, CommandKind.RerollBattle, new() { Ids = [die] }, 4);
    }

    [PostgresFact]
    public async Task SpecialCardRestartsDeclineAndReplacementBeforeFpProtection()
    {
        var state = LordsWeShallGetItTests.Window();
        LordsWeShallGetItTests.AddTile(state, HuntTests.Eye);
        await LordsPersistenceTests.Restart(state, CommandKind.DeclineReaction, new());
        await LordsPersistenceTests.Restart(state, CommandKind.RespondEffect, new() { ReactionId = WeShallGetIt.Reaction }, 0);
        state = LordsWeShallGetItTests.Use(state, 0);
        await LordsPersistenceTests.Restart(state, CommandKind.DeclineReaction, new());
    }

    [PostgresFact]
    public async Task ReplacementChainRestartsNenyaAndMithrilWithoutRestoringSpentCard()
    {
        var state = LordsWeShallGetItTests.Window();
        LordsCharacterFixture.Put(state, LordsCharacters.Galadriel, "lorien");
        LordsWeShallGetItTests.AddTile(state, HuntTests.Eye);
        state = LordsWeShallGetItTests.Use(state, 0);
        LordsWeShallGetItTests.AddTile(state, LordsWeShallGetItInteractionTests.SecondEye);
        var nenya = new CommandPayload { ReactionId = NenyaReaction.Id };
        await LordsPersistenceTests.Restart(state, CommandKind.RespondEffect, nenya, 0);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, nenya, new SequenceRandom(0));
        LordsWeShallGetItTests.AddTile(state, "base.hunt.special-fp.0.0.1");
        await LordsPersistenceTests.Restart(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, 0);
    }

    [PostgresFact]
    public async Task ReplacementGuideReactionAndRevelationResumeWithoutAnotherDraw()
    {
        var state = LordsWeShallGetItTests.Window(HuntTests.Eye, false);
        state.Fellowship.Hidden = true;
        LordsWeShallGetItTests.AddTile(state, HuntTests.Three);
        state = LordsWeShallGetItTests.Use(state, 0);
        var reaction = new CommandPayload { ReactionId = "character.gollum-reveal" };
        await LordsPersistenceTests.Restart(state, CommandKind.RespondEffect, reaction);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, reaction);
        await LordsPersistenceTests.Restart(state, CommandKind.RevealFellowship, new() { Route = ["hollin"] });
    }
}
