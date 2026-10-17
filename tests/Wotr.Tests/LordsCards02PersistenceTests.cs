using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Tests;

public class LordsCards02PersistenceTests
{
    [PostgresFact]
    public async Task NestedCaptainBattleRestartsCardsRollAndDelayedHuntCost()
    {
        var state = LordsCaptainTests.NestedAttack();
        await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new());
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard);
        await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new(), new int[4]);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new(), new SequenceRandom(new int[4]));
        await LordsPersistenceTests.Restart(state, CommandKind.RerollBattle, new());
        state = LordsFixture.Do(state, CommandKind.RerollBattle);
        await LordsPersistenceTests.Restart(state, CommandKind.StopBattle, new());
    }

    [PostgresFact]
    public async Task SpecialAttackLossesAndFearRerollsResumeTheCorrectCombatProgram()
    {
        var state = LordsShadowCardsFixture.Reveal(LordsCards02BoundaryTests.CombatState(LordsShadowCards.Balrog), LordsShadowCards.Balrog, 3, 0, 0);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        var unit = BattleRules.Units(state, force).First(BoardRules.IsArmyUnit);
        await LordsPersistenceTests.Restart(state, CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(unit.Id)] }, new int[9]);
        state = LordsShadowCardsFixture.Reveal(LordsShadowCardsFixture.Battle(LordsShadowCards.Lore), LordsShadowCards.Lore, new int[8]);
        var die = BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.Dice[0];
        await LordsPersistenceTests.Restart(state, CommandKind.RerollBattle, new() { Ids = [die.Id] }, 5);
    }

    [PostgresFact]
    public async Task FinalSpecialDieChoiceRestartsIntoLoreBeforeActions()
    {
        var state = LordsDiceTests.BothSidesRolling();
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Lore);
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(Enumerable.Repeat(5, 15).ToArray()));
        state = LordsFixture.Do(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Gothmog });
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseSpecialDie, new() { DieId = LordsDiceCatalog.Nenya });
    }

    [PostgresFact]
    public async Task EveryEventRestartsItsCostAndEffectWithoutRepeatingRecruitment()
    {
        foreach (var card in LordsCards02BoundaryTests.Cards)
        {
            var state = LordsCards02BoundaryTests.EventState();
            await LordsPersistenceTests.Restart(state, CommandKind.PlayEvent, LordsShadowCardsFixture.Cost(state, card));
        }
    }

    [PostgresFact]
    public async Task EveryCombatRestartsSecretCommitAndReveal()
    {
        foreach (var card in LordsCards02BoundaryTests.Cards)
        {
            var state = LordsCards02BoundaryTests.CombatState(card);
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new());
            state = LordsFixture.Do(state, CommandKind.CommitCombatCard);
            int[] samples = card switch
            {
                LordsShadowCards.Balrog => [3, 0, 0],
                LordsShadowCards.Gates or LordsShadowCards.Captain => [],
                _ => new int[10]
            };
            await LordsPersistenceTests.Restart(state, CommandKind.CommitCombatCard, new() { CardId = card }, samples);
        }
    }

    [PostgresFact]
    public async Task BalrogPersistsModeAndTravelOrArmyMoveWithoutAnotherDie()
    {
        foreach (var mode in new[] { BalrogEventStep.Alone, BalrogEventStep.Army, BalrogEventStep.Decline })
        {
            var state = LordsShadowManeuverTests.BalrogChoice();
            await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [mode] });
            state = LordsCardsFixture.Choose(state, mode);
            if (mode == BalrogEventStep.Alone)
                await LordsPersistenceTests.Restart(state, CommandKind.MoveShadowCharacters, new()
                {
                    CompanionMoves = [new() { Ids = [LordsCharacters.Balrog], Route = ["moria", "dimrill-dale"] }]
                });
            if (mode == BalrogEventStep.Army)
                await LordsPersistenceTests.Restart(state, CommandKind.MoveArmies, new()
                {
                    Moves = [new() { From = "moria", To = "dimrill-dale", CharacterIds = [LordsCharacters.Balrog],
                        UnitIds = state.Units.Where(u => u.Location == "moria").Select(u => u.Id).ToList() }]
                });
        }
    }

    [PostgresFact]
    public async Task DolGuldurPersistsOptionalMoveAndDeclineAfterRecruitment()
    {
        var state = LordsShadowCardsFixture.Play(LordsCards02BoundaryTests.EventState(), LordsShadowCards.DolGuldur);
        await LordsPersistenceTests.Restart(state, CommandKind.FinishArmyEvent, new());
        await LordsPersistenceTests.Restart(state, CommandKind.MoveArmies, new()
        {
            Moves = [new() { From = "dol-guldur", To = "southern-mirkwood",
                UnitIds = state.Units.Where(u => u.Location == "dol-guldur").Select(u => u.Id).ToList() }]
        });
    }

    [PostgresFact]
    public async Task SmeagolPersistsBothPrivateCostsAndDeclineBeforeProtection()
    {
        foreach (var cost in new[] { WeWontGoBackStep.Decline, LordsShadowCards.WeWontGoBack, ShadowCharacterCards03.Lure })
            await LordsPersistenceTests.Restart(LordsWeWontGoBackTests.Window(true), CommandKind.ChooseEffect, new() { Ids = [cost] });
    }

    [PostgresFact]
    public async Task LorePersistsDieAndResultInOneCommandOrDeclines()
    {
        var state = LordsLoreTests.Window();
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.HuntOrigin == "rolled-eye");
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [die.Id, "lore.Character.keep"] });
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [LoreOfTheRings.Decline] });
    }

    [PostgresFact]
    public async Task GatesPersistsDeclarationReactionDamageAndRemainingChiefReaction()
    {
        var state = LordsGatesTests.Declaration();
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "dol-guldur");
        HuntTests.Pool(state, HuntTests.Three);
        var route = new CommandPayload { Route = ["moria", "hollin"] };
        await LordsPersistenceTests.Restart(state, CommandKind.DeclareFellowship, route);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, route);
        var reaction = new CommandPayload { ReactionId = LordsShadowCards.GatesReaction + ".declaration" };
        await LordsPersistenceTests.Restart(state, CommandKind.RespondEffect, reaction, 0);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, reaction, new SequenceRandom(0));
        var damage = new CommandPayload { HuntResponse = HuntResponse.Ring };
        await LordsPersistenceTests.Restart(state, CommandKind.ResolveHuntDamage, damage);
        state = LordsFixture.Do(state, CommandKind.ResolveHuntDamage, damage);
        await LordsPersistenceTests.Restart(state, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Follow });
        await LordsPersistenceTests.Restart(state, CommandKind.DeclineReaction, new());
    }

    [PostgresFact]
    public async Task TableRemovalRestartsWillCharacterAndSelectedRingCosts()
    {
        foreach (var card in new[] { LordsShadowCards.Lore, LordsShadowCards.Gates })
        {
            var state = LordsShadowCardsFixture.Play(LordsShadowCardsFixture.State(), card);
            var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
            var kind = card == LordsShadowCards.Lore ? CommandKind.DiscardLore : CommandKind.DiscardGates;
            die.Face = DieFace.WillOfTheWest;
            await LordsPersistenceTests.Restart(state, kind, new() { DieId = die.Id });
            die.Face = DieFace.Character;
            state.Fellowship.Guide = CharacterRules.Grey;
            await LordsPersistenceTests.Restart(state, kind, new()
            {
                DieId = die.Id, RingId = card == LordsShadowCards.Lore ? LordsDiceSetup.NenyaRing : ""
            });
        }
    }

    [PostgresFact]
    public async Task ServantAndWordsPersistSelectedFigureAndSubsequentRoll()
    {
        var state = LordsShadowCardsFixture.Reveal(LordsShadowCardsFixture.Battle(LordsShadowCards.Captain), LordsShadowCards.Captain);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.WitchKing] }, new int[6]);
        state = LordsShadowCardsFixture.Reveal(LordsShadowCardsFixture.Battle(LordsShadowCards.Gates), LordsShadowCards.Gates);
        await LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = [CharacterRules.Strider] }, new int[7]);
    }
}
