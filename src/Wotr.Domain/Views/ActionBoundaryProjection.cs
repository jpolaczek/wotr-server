using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

internal static class ActionBoundaryProjection
{
    public static ActionBoundaryView? For(MatchState state, Side viewer)
    {
        var declaration = FellowshipDeclarationProjection.For(state);
        var fellowshipMove = FellowshipMoveProjection.For(state);
        var cardHunt = CardHuntProjection.For(state);
        var lureChoice = LureChoiceProjection.For(state);
        var lurePlacement = LurePlacementProjection.For(state, viewer);
        var companionJourney = CompanionJourneyProjection.For(state);
        var fellowshipGuide = FellowshipGuideProjection.For(state);
        var battle = DirectBattle(state) ?? SpecialAttackLoss(state) ?? ReliefEventBattle(state) ??
            LandingEventBattle(state);
        if (battle is not null && BattleRuntime.Active(state) is { } active)
        {
            battle.AttackerCancelledHits = active.Attacker.Round.CancelHits;
            battle.DefenderCancelledHits = active.Defender.Round.CancelHits;
        }
        var corruptionEvent = CorruptionBoundaryProjection.Event(state);
        var westPalantirDraw = CorruptionBoundaryProjection.WestPalantirDraw(state);
        var westPalantirDiscard = CorruptionBoundaryProjection.WestPalantirDiscard(state, viewer);
        var fateMonsters = FateMonstersEvent(state);
        var anarionDraw = KingsActionBoundaryProjection.AnarionDraw(state);
        var corruptionRemoval = KingsActionBoundaryProjection.CorruptionRemoval(state);
        var directEventChoice = declaration is null && fellowshipMove is null && cardHunt is null && lureChoice is null &&
            lurePlacement is null && companionJourney is null && fellowshipGuide is null && battle is null &&
            corruptionEvent is null && westPalantirDraw is null && westPalantirDiscard is null && fateMonsters is null &&
            anarionDraw is null && corruptionRemoval is null
            ? DirectEventChoice(state) : null;
        var reviewedContinuation = declaration is not null || fellowshipMove is not null ||
            cardHunt is not null || lureChoice is not null || lurePlacement is not null || companionJourney is not null ||
            fellowshipGuide is not null || battle is not null || corruptionEvent is not null ||
            westPalantirDraw is not null || westPalantirDiscard is not null || fateMonsters is not null ||
            directEventChoice is not null || anarionDraw is not null || corruptionRemoval is not null;
        // Compose only explicitly reviewed continuations with module facts;
        // arbitrary expansion procedures are not inferred from their decision kind.
        if (!KingsBoundaryProjection.Supports(state, reviewedContinuation) ||
            !LordsBoundaryProjection.Supports(state, reviewedContinuation))
            return null;
        if (state.Resolution.Frames.Count > 0 && declaration is null && westPalantirDiscard is null &&
            anarionDraw is not { Discard: true } &&
            (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None))
            return null;
        if (state.Resolution.Frames.Count == 0 ? !SupportedWindow(state) :
            fellowshipMove is null && cardHunt is null && lureChoice is null && lurePlacement is null && companionJourney is null &&
            fellowshipGuide is null && declaration is null && battle is null &&
            !CorruptionBoundaryProjection.Supports(state) && fateMonsters is null && directEventChoice is null &&
            anarionDraw is null && corruptionRemoval is null)
            return null;

        // Base/Fate discards never return to a hand/deck. Only a visible discharge
        // establishes an opposing discard; a previously inspected/drawn card does not.
        var revealed = Discharged(state.Events.Where(e => Visibility.CanRead(viewer, e.OnlyFor)));
        var publicDiscards = Discharged(state.Events.Where(e => e.OnlyFor is null));
        var tiles = HuntTileZonesProjection.For(state, viewer);
        return new()
        {
            FellowshipDeclaredThisTurn = state.Fellowship.LastDeclarationTurn == state.Turn,
            MinionEverEntered = state.MinionEverEntered,
            MandatoryHuntAllocation = state.MandatoryHuntAllocation,
            FellowshipMove = fellowshipMove,
            CardHunt = cardHunt,
            FellowshipDeclaration = declaration,
            Battle = battle,
            LureChoice = lureChoice,
            LurePlacement = lurePlacement,
            CompanionJourney = companionJourney,
            FellowshipGuide = fellowshipGuide,
            CorruptionEvent = corruptionEvent,
            WestPalantirDraw = westPalantirDraw,
            WestPalantirDiscard = westPalantirDiscard,
            FateMonsters = fateMonsters,
            DirectEventChoice = directEventChoice,
            AnarionDraw = anarionDraw,
            CorruptionRemoval = corruptionRemoval,
            Lords = LordsBoundaryProjection.For(state),
            Kings = KingsBoundaryProjection.For(state, tiles.Unassigned),
            PaidDieIds = [.. state.Resolution.PaidDieIds],
            PendingPolitics = state.PoliticalReactions.Select(p => new PendingPoliticsView
                { Cause = p.Trigger.ToString(), NationId = p.NationId, RegionId = p.RegionId }).ToList(),
            Companions = [.. state.Fellowship.Companions],
            Characters = state.Characters.Where(c => LordsBoundaryProjection.Visible(state, viewer, c)).Select(c => new ActionCharacterView
            {
                Character = Projection.Character(c),
                ConvertedArmyThisTurn = c.LastArmyConversionTurn == state.Turn
            }).ToList(),
            Players = state.Players.Select(p => new ActionPlayerView
            {
                Side = p.Side,
                ExtraDiceNextRecovery = p.ExtraDiceNextRecovery,
                RingActionPending = p.RingActionPending,
                KnownDiscards = p.Discard.Where(id => p.Side == viewer || revealed.Contains(id) ||
                        id == SmeagolRules.WeShallGetIt)
                    .Order(StringComparer.Ordinal).ToList(),
                PublicDiscards = p.Discard.Where(id => publicDiscards.Contains(id) || id == SmeagolRules.WeShallGetIt)
                    .Order(StringComparer.Ordinal).ToList()
            }).ToList(),
            HuntTiles = tiles.Zones
        };
    }

    private static BattleContextView? DirectBattle(MatchState state)
    {
        var pending = state.Pending;
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || pending is null || frames.Count is < 1 or > 3)
            return null;

        if (pending.Kind == DecisionKind.EffectChoice)
            return IsReviewedScoutsChoice(state, pending, frames) || IsReviewedHeroicDeathChoice(state, pending, frames) ||
                IsReviewedMightyAttackChoice(state, pending, frames) || IsReviewedOrderlyWithdrawalChoice(state, pending, frames) ||
                IsReviewedChieftainChoice(state, pending, frames) || IsReviewedBrandChoice(state, pending, frames)
                ? new() { CompletesPaidAction = true } : null;
        if (pending.Kind == DecisionKind.Reaction)
            return IsReviewedSorcererWindow(state, pending, frames) ? new() { CompletesPaidAction = true } : null;

        var frame = frames[0];
        var battle = frame.Battle;
        var reviewedVoluntaryLoss = frames.Count == 3 && battle is not null &&
            IsReviewedVoluntaryBattleLoss(state, pending, battle, frames);
        if (frames.Count > 2 && !reviewedVoluntaryLoss)
            return null;
        if (frame.EffectId != "" || frame.NextStep != 0 || frame.Window != "" ||
            frame.ReturnDecision is not null || frame.Effect is not null || frame.Fellowship is not null ||
            frame.Hunt is not null || frame.BattleLoss is not null || frame.EventLoss is not null ||
            frame.Corruption is not null || battle is not { Return: EffectReturn.FinishAction } ||
            battle.Id.Length == 0 || battle.Mode == BattleMode.Relief ||
            battle.Attacker.Side == battle.Defender.Side)
            return null;

        BattleLossProcedure? loss = null;
        if (frames.Count == 2)
        {
            var lossFrame = frames[1];
            loss = lossFrame.BattleLoss;
            if (lossFrame.EffectId != "" || lossFrame.NextStep != 0 || lossFrame.Window != "" ||
                lossFrame.ReturnDecision is not null || lossFrame.Effect is not null ||
                lossFrame.Fellowship is not null || lossFrame.Hunt is not null || lossFrame.Battle is not null || loss is null ||
                lossFrame.EventLoss is not null || lossFrame.Corruption is not null)
                return null;
        }
        else if (reviewedVoluntaryLoss)
            loss = frames[2].BattleLoss;
        if (!BattleDecisionMatches(state, pending, battle, loss) ||
            state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving))
            return null;
        return new() { CompletesPaidAction = true };
    }

    // Scouts opens at initiative 1, before any roll, and rests on revealed cards and the
    // public board. Its options go to the defender alone.
    private static bool IsReviewedScoutsChoice(MatchState state, PendingDecision pending, IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var effectFrame] || battleFrame.Battle is not { } battle ||
            effectFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending.Commands is not [CommandKind.ChooseEffect] || pending.Presentation != "RegionOrShelter" ||
            !ReviewedCombatCards.IsScouts(pending.SourceId) || !CardCatalog.Enabled(state, pending.SourceId))
            return false;

        var scouts = new CombatWork(pending.Owner, pending.SourceId, ReviewedCombatCards.ScoutsProgram, 1);
        if (battle is not { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeRoll, Round: 1, CardsRevealed: true,
                Mode: not BattleMode.Relief, CompletedRounds: 0, EndedByEffect: false, NextWork: 1, WhiteRiderUsed: false } ||
            battle.Id.Length == 0 || battle.Defender.Side != pending.Owner || battle.Attacker.Side == battle.Defender.Side ||
            battle.Work.Count == 0 || battle.Work[0] != scouts || battle.SpecialAttacks.Count != 0 ||
            battle.Defender.Round.CardId != pending.SourceId || battle.Defender.Round.CardCancelled ||
            battle.Attacker.Round.Dice.Count != 0 || battle.Defender.Round.Dice.Count != 0 ||
            effectFrame.EffectId != scouts.ProgramId || effectFrame.NextStep != 1 || effectFrame.Window.Length != 0 ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.Choice,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false, AppliedHits: 0 } ||
            execution.Owner != pending.Owner || execution.SourceId != pending.SourceId ||
            execution.Committed.Count != 0 || execution.TargetIds.Count != 0 || execution.RegionId.Length != 0 ||
            !EffectCatalog.All.Programs.TryGetValue(scouts.ProgramId, out var program) || program.Steps is not [ScoutsStep step] ||
            !EffectCatalog.All.Combats[pending.SourceId].Condition.CanPlay(state, battle, pending.Owner) ||
            !pending.PrivateChoices.SequenceEqual(step.Candidates(state, execution)))
            return false;

        return state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    // Heroic Death opens at initiative 6, after both rolls and re-rolls and before any
    // casualty. Its options come from the owner's public figures and the incoming hits.
    private static bool IsReviewedHeroicDeathChoice(MatchState state, PendingDecision pending,
        IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var effectFrame] || battleFrame.Battle is not { } battle ||
            effectFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending.Commands is not [CommandKind.ChooseEffect] || pending.Presentation != "SacrificeAndHits" ||
            !ReviewedCombatCards.IsHeroicDeath(pending.SourceId) || !CardCatalog.Enabled(state, pending.SourceId))
            return false;

        var work = new CombatWork(pending.Owner, pending.SourceId, ReviewedCombatCards.HeroicDeathProgram, 6);
        if (battle is not { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeLosses, Round: >= 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, EndedByEffect: false } ||
            battle.CompletedRounds != battle.Round - 1 || battle.Id.Length == 0 ||
            battle.Attacker.Side == battle.Defender.Side || battle.NextWork < 1 ||
            battle.NextWork > battle.Work.Count || battle.Work[battle.NextWork - 1] != work ||
            battle.Force(pending.Owner).Round.CardId != pending.SourceId || battle.Force(pending.Owner).Round.CardCancelled ||
            effectFrame.EffectId != work.ProgramId || effectFrame.NextStep != 1 || effectFrame.Window.Length != 0 ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.Choice,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false, AppliedHits: 0 } ||
            execution.Owner != pending.Owner || execution.SourceId != pending.SourceId ||
            execution.Committed.Count != 0 || execution.TargetIds.Count != 0 || execution.RegionId.Length != 0 ||
            !EffectCatalog.All.Programs.TryGetValue(work.ProgramId, out var program) ||
            program.Steps is not [HeroicDeathStep step] ||
            !EffectCatalog.All.Combats[pending.SourceId].Condition.CanPlay(state, battle, pending.Owner) ||
            !pending.PrivateChoices.SequenceEqual(step.Candidates(state, execution)))
            return false;

        return state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    // Mighty Attack opens at initiative 4, after the combat roll and before any re-roll. Its
    // options are the owner's Companions with Leadership left and the public missed dice.
    private static bool IsReviewedMightyAttackChoice(MatchState state, PendingDecision pending,
        IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var effectFrame] || battleFrame.Battle is not { } battle ||
            effectFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending.Commands is not [CommandKind.ChooseEffect] || pending.Presentation != "LeadershipAndMiss" ||
            !ReviewedCombatCards.IsMightyAttack(pending.SourceId) || !CardCatalog.Enabled(state, pending.SourceId))
            return false;

        var work = new CombatWork(pending.Owner, pending.SourceId, ReviewedCombatCards.MightyAttackProgram, 4);
        if (battle is not { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeReroll, Round: >= 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, EndedByEffect: false } ||
            battle.CompletedRounds != battle.Round - 1 || battle.Id.Length == 0 ||
            battle.Attacker.Side == battle.Defender.Side || battle.NextWork < 1 ||
            battle.NextWork > battle.Work.Count || battle.Work[battle.NextWork - 1] != work ||
            battle.Force(pending.Owner).Round.CardId != pending.SourceId || battle.Force(pending.Owner).Round.CardCancelled ||
            effectFrame.EffectId != work.ProgramId || effectFrame.NextStep != 1 || effectFrame.Window.Length != 0 ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.Choice,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false, AppliedHits: 0 } ||
            execution.Owner != pending.Owner || execution.SourceId != pending.SourceId ||
            execution.Committed.Count != 0 || execution.TargetIds.Count != 0 || execution.RegionId.Length != 0 ||
            !EffectCatalog.All.Programs.TryGetValue(work.ProgramId, out var program) ||
            program.Steps is not [MightyAttackStep step] ||
            !EffectCatalog.All.Combats[pending.SourceId].Condition.CanPlay(state, battle, pending.Owner) ||
            !pending.PrivateChoices.SequenceEqual(step.Candidates(state, execution)))
            return false;

        return state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    // Orderly Withdrawal opens at initiative 6, after both rolls and re-rolls and before any
    // casualty, while the opposing hits exceed those already cancelled.
    private static bool IsReviewedOrderlyWithdrawalChoice(MatchState state, PendingDecision pending,
        IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var effectFrame] || battleFrame.Battle is not { } battle ||
            effectFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending.Commands is not [CommandKind.ChooseEffect] || pending.Presentation != "CombatEffect" ||
            !ReviewedCombatCards.IsOrderlyWithdrawal(pending.SourceId) || !CardCatalog.Enabled(state, pending.SourceId))
            return false;

        var work = new CombatWork(pending.Owner, pending.SourceId, ReviewedCombatCards.OrderlyWithdrawalProgram, 6);
        if (battle is not { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeLosses, Round: >= 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, EndedByEffect: false } ||
            battle.CompletedRounds != battle.Round - 1 || battle.Id.Length == 0 ||
            battle.Attacker.Side == battle.Defender.Side || battle.NextWork < 1 ||
            battle.NextWork > battle.Work.Count || battle.Work[battle.NextWork - 1] != work ||
            battle.Force(pending.Owner).Round.CardId != pending.SourceId || battle.Force(pending.Owner).Round.CardCancelled ||
            effectFrame.EffectId != work.ProgramId || effectFrame.NextStep != 1 || effectFrame.Window.Length != 0 ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.Choice,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false, AppliedHits: 0 } ||
            execution.Owner != pending.Owner || execution.SourceId != pending.SourceId ||
            execution.Committed.Count != 0 || execution.TargetIds.Count != 0 || execution.RegionId.Length != 0 ||
            !EffectCatalog.All.Programs.TryGetValue(work.ProgramId, out var program) ||
            program.Steps is not [OrderlyWithdrawalStep step] ||
            !EffectCatalog.All.Combats[pending.SourceId].Condition.CanPlay(state, battle, pending.Owner) ||
            !pending.PrivateChoices.SequenceEqual(step.Candidates(state, execution)))
            return false;

        return state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    // A Kings chieftain may replace Shadow's card effects once both cards are committed and
    // before they are revealed. The options are the public chieftains fighting for Shadow.
    private static bool IsReviewedChieftainChoice(MatchState state, PendingDecision pending,
        IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var choiceFrame] || battleFrame.Battle is not { } battle ||
            choiceFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending is not { Owner: Side.Shadow, Presentation: "CombatReplacement", Window: "",
                Commands: [CommandKind.ChooseEffect] } || pending.SourceId != KingsChieftainCombat.Choice ||
            !EffectCatalog.All.Programs.TryGetValue(KingsChieftainCombat.Choice, out var program) ||
            program.Steps is not [ChieftainCombatChoiceStep step])
            return false;

        return battle is { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeReveal, Round: >= 1,
                CardsRevealed: false, Mode: not BattleMode.Relief, EndedByEffect: false } &&
            battle.CompletedRounds == battle.Round - 1 && battle.Id.Length != 0 &&
            battle.Attacker.Side != battle.Defender.Side && battle.Work.Count == 0 && battle.NextWork == 0 &&
            battle.Force(Side.Shadow).Round.CardId.Length != 0 &&
            battle.Attacker.Round.ReplacementSource is null && battle.Defender.Round.ReplacementSource is null &&
            choiceFrame is { EffectId: KingsChieftainCombat.Choice, NextStep: 1, Window: "", ReturnDecision: null,
                Fellowship: null, Hunt: null, Battle: null, BattleLoss: null, EventLoss: null, Corruption: null } &&
            execution is { Owner: Side.Shadow, SourceId: KingsChieftainCombat.Choice, Return: EffectReturn.Parent,
                Wait: EffectWait.Choice, IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false,
                CompletionWindowOffered: false, AppliedHits: 0 } &&
            execution.Committed.Count == 0 && execution.TargetIds.Count == 0 &&
            pending.PrivateChoices.SequenceEqual(step.Candidates(state, execution)) &&
            state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    // Brand may replace his owner's card effects once per battle, after the reveal and before
    // the roll. His public status names the owner, and both cards are public.
    private static bool IsReviewedBrandChoice(MatchState state, PendingDecision pending,
        IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var choiceFrame] || battleFrame.Battle is not { } battle ||
            choiceFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending is not { Presentation: "CombatReplacement", Window: "", Commands: [CommandKind.ChooseEffect] } ||
            pending.SourceId != SovereignCatalog.Brand ||
            !EffectCatalog.All.Programs.TryGetValue(KingsBattle.BrandChoice, out var program) ||
            program.Steps is not [BrandCombatChoiceStep step])
            return false;

        return battle is { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeRoll, Round: >= 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, EndedByEffect: false, NextWork: 0,
                Kings: { BrandUsed: false } kings } &&
            kings.BrandOfferedRound == battle.Round && battle.CompletedRounds == battle.Round - 1 &&
            battle.Id.Length != 0 && battle.Attacker.Side != battle.Defender.Side &&
            battle.SpecialAttacks.Count == 0 && battle.Attacker.Round.Dice.Count == 0 &&
            battle.Defender.Round.Dice.Count == 0 && battle.Attacker.Round.ReplacementSource is null &&
            battle.Defender.Round.ReplacementSource is null && battle.Force(pending.Owner).Round.CardId.Length != 0 &&
            choiceFrame is { EffectId: KingsBattle.BrandChoice, NextStep: 1, Window: "", ReturnDecision: null,
                Fellowship: null, Hunt: null, Battle: null, BattleLoss: null, EventLoss: null, Corruption: null } &&
            execution is { Return: EffectReturn.Parent, Wait: EffectWait.Choice, IsEvent: false,
                PalantirDrawPending: false, AnarionDrawPending: false, CompletionWindowOffered: false, AppliedHits: 0 } &&
            execution.Owner == pending.Owner && execution.SourceId == SovereignCatalog.Brand &&
            execution.Committed.Count == 0 && execution.TargetIds.Count == 0 &&
            pending.PrivateChoices.SequenceEqual(step.Candidates(state, execution)) &&
            state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    // The Witch-king's Sorcerer is offered once round one has closed: its cards are discarded and
    // the battle goes on or ends. The offer rests on the public battle and the revealed card.
    private static bool IsReviewedSorcererWindow(MatchState state, PendingDecision pending,
        IReadOnlyList<ResolutionFrame> frames)
    {
        if (frames is not [var battleFrame, var reactionFrame] || battleFrame.Battle is not { } battle ||
            reactionFrame.Effect is not { } execution || !IsPlainBattleFrame(battleFrame) ||
            pending is not { Owner: Side.Shadow, Window: "battle.round.closed",
                Commands: [CommandKind.RespondEffect, CommandKind.DeclineReaction] } ||
            pending.SourceId != CharacterRules.WitchKing)
            return false;

        return battle is { Return: EffectReturn.FinishAction, Round: 1, CardsRevealed: true, CompletedRounds: 1,
                SorcererOffered: true, Stage: BattleStage.Finish or BattleStage.NextRound } &&
            battle.Mode != BattleMode.Relief && battle.Id.Length != 0 &&
            battle.Attacker.Round.Closed && battle.Defender.Round.Closed &&
            reactionFrame is { NextStep: 0, Window: "", ReturnDecision: null, Fellowship: null, Hunt: null,
                Battle: null, BattleLoss: null, EventLoss: null, Corruption: null } &&
            reactionFrame.EffectId == EffectCatalog.ReactionProgram &&
            execution is { Owner: Side.Shadow, Return: EffectReturn.Parent, Wait: EffectWait.Reaction,
                ReactionWindow: "battle.round.closed", IsEvent: false } &&
            execution.SourceId == CharacterRules.WitchKing && execution.Committed.Count == 0 &&
            state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1 &&
            state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving);
    }

    private static bool IsPlainBattleFrame(ResolutionFrame frame) =>
        frame is { EffectId: "", NextStep: 0, Window: "", ReturnDecision: null, Effect: null, Fellowship: null,
            Hunt: null, BattleLoss: null, EventLoss: null, Corruption: null };

    private static BattleContextView? ReliefEventBattle(MatchState state)
    {
        var pending = state.Pending;
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || pending is null || frames.Count is < 2 or > 3)
            return null;

        const string cardId = FreePeoplesStrategyCards02.HelpUnlookedFor;
        var eventFrame = frames[0];
        var battleFrame = frames[1];
        var execution = eventFrame.Effect;
        var battle = battleFrame.Battle;
        var defenderRetreated = battle is not null && battle.Defender.Region != battle.Region;
        var region = battle is not null && BoardRules.Regions.TryGetValue(battle.Region, out var knownRegion)
            ? knownRegion
            : null;
        if (eventFrame.EffectId != cardId || eventFrame.NextStep != 1 || eventFrame.Window.Length != 0 ||
            eventFrame.ReturnDecision is not null || eventFrame.Battle is not null || eventFrame.BattleLoss is not null ||
            eventFrame.EventLoss is not null || eventFrame.Fellowship is not null || eventFrame.Hunt is not null ||
            eventFrame.Corruption is not null || execution is not
                { Owner: Side.FreePeoples, SourceId: cardId, Return: EffectReturn.FinishAction, Wait: EffectWait.None,
                    IsEvent: true, PalantirDrawPending: false, AnarionDrawPending: false,
                    CompletionWindowOffered: false, ArmyOrder: null } ||
            execution.PaidFace is not (DieFace.Army or DieFace.Event) ||
            execution.ReactionWindow.Length != 0 || execution.OfferedReactions.Count != 0 ||
            execution.AppliedHits != 0 || execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 ||
            execution.NationId.Length != 0 || execution.InspectedSovereign is not null ||
            execution.InspectedHand is not null || execution.RingRecovery is not null ||
            execution.MadnessTarget is not null || execution.FellowshipMove ||
            execution.Committed is not [{ Owner: Side.FreePeoples, SourceId: cardId, ChoiceId: "", TargetId: "" }] ||
            battleFrame.EffectId.Length != 0 || battleFrame.NextStep != 0 || battleFrame.Window.Length != 0 ||
            battleFrame.ReturnDecision is not null || battleFrame.Effect is not null ||
            battleFrame.Fellowship is not null || battleFrame.Hunt is not null || battleFrame.BattleLoss is not null ||
            battleFrame.EventLoss is not null || battleFrame.Corruption is not null ||
            battle is not { Return: EffectReturn.Parent, Mode: BattleMode.Relief } ||
            battle.Attacker.Side != Side.FreePeoples || battle.Defender.Side != Side.Shadow ||
            battle.Attacker.Region == battle.Region || battle.Attacker.Garrison ||
            battle.Defender.Garrison ||
            defenderRetreated && (region is null || battle.Stage != BattleStage.Advance || !battle.CardsRevealed ||
                !region.Neighbors.Contains(battle.Defender.Region) || !BattleRules.Alive(state, battle.Defender) ||
                !BattleMovement.IsFree(state, Side.Shadow, battle.Defender.Region)) ||
            region is null || region.Settlement != "Stronghold" ||
            state.Control.GetValueOrDefault(battle.Region) != Side.FreePeoples ||
            !state.Player(Side.FreePeoples).Discard.Contains(cardId) ||
            state.Player(Side.FreePeoples).Hand.Contains(cardId) || state.Player(Side.FreePeoples).Table.Contains(cardId) ||
            !Discharged(state.Events.Where(e => e.OnlyFor is null)).Contains(cardId) ||
            EffectCatalog.All.Programs.TryGetValue(cardId, out var program) == false ||
            program.Steps is not [ArmyOrderStep { Order: ArmyEventOrder.ReliefAttack }])
            return null;

        var reliefReduction = state.Units.Count(unit => unit.Location == battle.Region &&
            unit.Garrison != (battle.Stage == BattleStage.Advance) &&
            BoardRules.Owner(unit) == Side.FreePeoples && BoardRules.IsArmyUnit(unit));
        if (reliefReduction == 0 || battle.Defender.CombatDiceReduction != reliefReduction ||
            battle.Attacker.CombatDiceReduction != 0)
            return null;

        BattleLossProcedure? loss = null;
        if (frames.Count == 3)
        {
            var lossFrame = frames[2];
            loss = lossFrame.BattleLoss;
            if (lossFrame.EffectId.Length != 0 || lossFrame.NextStep != 0 || lossFrame.Window.Length != 0 ||
                lossFrame.ReturnDecision is not null || lossFrame.Effect is not null ||
                lossFrame.Fellowship is not null || lossFrame.Hunt is not null || lossFrame.Battle is not null ||
                loss is null || lossFrame.EventLoss is not null || lossFrame.Corruption is not null)
                return null;
        }

        if (!BattleDecisionMatches(state, pending, battle, loss) || state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] && die.Owner == Side.FreePeoples &&
                die.Location == DieLocation.Resolving && (die.Face == DieFace.Event || die.Face == execution.PaidFace)))
            return null;

        return new()
        {
            ReliefEvent = new() { CardId = cardId, CardCost = execution.PaidFace }
        };
    }

    // Corsairs of Umbar land Shadow's Umbar army on the Gondor coast and attack the Free Peoples
    // army there at once. The paid event waits under the battle with the landing region recorded;
    // outside a siege the arrival keeps the attacker fighting until the defender withdraws.
    private static BattleContextView? LandingEventBattle(MatchState state)
    {
        var pending = state.Pending;
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || pending is null || frames.Count is < 2 or > 3)
            return null;

        const string cardId = ShadowStrategyCards02.Corsairs;
        var eventFrame = frames[0];
        var battleFrame = frames[1];
        var execution = eventFrame.Effect;
        var battle = battleFrame.Battle;
        if (eventFrame is not { EffectId: cardId, NextStep: 2, Window: "", ReturnDecision: null, Battle: null,
                BattleLoss: null, EventLoss: null, Fellowship: null, Hunt: null, Corruption: null } ||
            execution is not { Owner: Side.Shadow, SourceId: cardId, Return: EffectReturn.FinishAction,
                Wait: EffectWait.None, IsEvent: true, PalantirDrawPending: false, AnarionDrawPending: false,
                CompletionWindowOffered: false, ArmyOrder: null, FellowshipMove: false, AppliedHits: 0,
                InspectedSovereign: null, InspectedHand: null, RingRecovery: null, MadnessTarget: null } ||
            execution.PaidFace is not (DieFace.Army or DieFace.Event) || execution.ReactionWindow.Length != 0 ||
            execution.OfferedReactions.Count != 0 || execution.TargetIds.Count != 0 || execution.NationId.Length != 0 ||
            execution.Committed is not [{ Owner: Side.Shadow, SourceId: cardId, ChoiceId: "", TargetId: "" }] ||
            !IsPlainBattleFrame(battleFrame) ||
            battle is not { Return: EffectReturn.Parent, ArrivalBeforeBattle: true,
                Mode: BattleMode.Field or BattleMode.Siege } ||
            battle.Id.Length == 0 || battle.Region != execution.RegionId ||
            battle.Attacker.Side != Side.Shadow || battle.Defender.Side != Side.FreePeoples ||
            battle.Attacker.Region != battle.Region || battle.Attacker.Garrison ||
            battle.Defender.Region != battle.Region ||
            !BoardRules.CurrentRegions(state).Any(region => region.Id == battle.Region && region.Nation == "gondor" &&
                region.Coastal) ||
            !state.Player(Side.Shadow).Discard.Contains(cardId) ||
            state.Player(Side.Shadow).Hand.Contains(cardId) || state.Player(Side.Shadow).Table.Contains(cardId) ||
            !Discharged(state.Events.Where(e => e.OnlyFor is null)).Contains(cardId) ||
            !EffectCatalog.All.Programs.TryGetValue(cardId, out var program) ||
            program.Steps is not [ArmyOrderStep { Order: ArmyEventOrder.Corsairs }, CorsairBattleStep])
            return null;

        BattleLossProcedure? loss = null;
        if (frames.Count == 3)
        {
            var lossFrame = frames[2];
            loss = lossFrame.BattleLoss;
            if (lossFrame.EffectId.Length != 0 || lossFrame.NextStep != 0 || lossFrame.Window.Length != 0 ||
                lossFrame.ReturnDecision is not null || lossFrame.Effect is not null ||
                lossFrame.Fellowship is not null || lossFrame.Hunt is not null || lossFrame.Battle is not null ||
                loss is null || lossFrame.EventLoss is not null || lossFrame.Corruption is not null)
                return null;
        }

        if (!BattleDecisionMatches(state, pending, battle, loss) || state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] && die.Owner == Side.Shadow &&
                die.Location == DieLocation.Resolving && (die.Face == execution.PaidFace ||
                    execution.PaidFace == DieFace.Army && die.Face == DieFace.ArmyMuster)))
            return null;

        return new() { LandingEvent = new() { CardId = cardId, CardCost = execution.PaidFace } };
    }

    private static FateMonstersEventView? FateMonstersEvent(MatchState state)
    {
        const string cardId = FateCards.Monsters;
        var pending = state.Pending;
        var frames = state.Resolution.Frames;
        if (!state.Configuration.Fate || state.Phase != GamePhase.ActionResolution ||
            state.AfterDiscard != DiscardContinuation.None || state.Resolution.ActionSequence is not null ||
            pending is not { Kind: DecisionKind.EffectChoice, Owner: Side.Shadow } || pending.SourceId != cardId ||
            state.Resolution.Window.Length != 0 || state.PoliticalReactions.Count != 0 ||
            frames.Count is < 1 or > 2 || state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !CardCatalog.Enabled(state.Configuration, cardId) ||
            !EffectCatalog.All.Programs.TryGetValue(cardId, out var program) ||
            program.Steps is not [FateMonstersBranchStep] ||
            !state.Player(Side.Shadow).Discard.Contains(cardId) || state.Player(Side.Shadow).Hand.Contains(cardId) ||
            state.Player(Side.Shadow).Table.Contains(cardId) ||
            !Discharged(state.Events.Where(e => e.OnlyFor is null)).Contains(cardId))
            return null;

        var parent = frames[0];
        var execution = parent.Effect;
        if (parent.EffectId != cardId || parent.NextStep != 1 || parent.Window.Length != 0 ||
            parent.ReturnDecision is not null || parent.Fellowship is not null || parent.Hunt is not null ||
            parent.Battle is not null || parent.BattleLoss is not null || parent.EventLoss is not null ||
            parent.Corruption is not null || execution is not
                { Owner: Side.Shadow, SourceId: cardId, Return: EffectReturn.FinishAction, IsEvent: true,
                    PalantirDrawPending: false, AnarionDrawPending: false, CompletionWindowOffered: false,
                    ArmyOrder: null, Wait: EffectWait.Choice or EffectWait.None } ||
            execution.ReactionWindow.Length != 0 || execution.OfferedReactions.Count != 0 ||
            execution.AppliedHits != 0 || execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 ||
            execution.NationId.Length != 0 ||
            execution.PaidFace != DieFace.Event && execution.PaidFace != CardCatalog.All[cardId].EventType ||
            execution.InspectedSovereign is not null || execution.InspectedHand is not null ||
            execution.RingRecovery is not null || execution.MadnessTarget is not null || execution.FellowshipMove ||
            execution.Committed.Count is < 1 or > 2 ||
            execution.Committed[0] != new EffectSelection(Side.Shadow, cardId, "", ""))
            return null;

        var branchId = "";
        if (frames.Count == 1)
        {
            if (execution.Wait != EffectWait.Choice || execution.Committed.Count != 1 ||
                pending.PrivateChoices.Count != 2 ||
                !pending.PrivateChoices.SequenceEqual(new[] { FateCards.MonstersWest, FateCards.MonstersEast }) ||
                parent.ReturnDecision is not null)
                return null;
        }
        else
        {
            branchId = FateCards.MonstersEast;
            var child = frames[1];
            if (execution.Wait != EffectWait.None || execution.Committed.Count != 2 ||
                execution.Committed[1] != new EffectSelection(Side.Shadow, cardId, branchId, "") ||
                child.EffectId != branchId || child.NextStep != 1 || child.Window.Length != 0 ||
                child.ReturnDecision is not null || child.Fellowship is not null || child.Hunt is not null ||
                child.Battle is not null || child.BattleLoss is not null || child.EventLoss is not null ||
                child.Corruption is not null || child.Effect is not
                    { Owner: Side.Shadow, SourceId: cardId, Return: EffectReturn.Parent, Wait: EffectWait.Choice,
                        IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false,
                        CompletionWindowOffered: false, ArmyOrder: null } childExecution ||
                childExecution.PaidFace != default || childExecution.ReactionWindow.Length != 0 ||
                childExecution.OfferedReactions.Count != 0 || childExecution.AppliedHits != 0 ||
                childExecution.RegionId.Length != 0 || childExecution.TargetIds.Count != 0 ||
                childExecution.NationId.Length != 0 || childExecution.Committed.Count != 0 ||
                childExecution.InspectedSovereign is not null || childExecution.InspectedHand is not null ||
                childExecution.RingRecovery is not null || childExecution.MadnessTarget is not null ||
                childExecution.FellowshipMove || pending.PrivateChoices.Count == 0 ||
                !pending.PrivateChoices.SequenceEqual(new FateMonstersEliteStep().Candidates(state, childExecution)))
                return null;
        }

        var paidDieId = state.Resolution.PaidDieIds[0];
        var paidDie = state.Dice.SingleOrDefault(die => die.Id == paidDieId);
        if (paidDie is not { Owner: Side.Shadow, Location: DieLocation.Resolving } ||
            !CanPayObservedFace(paidDie.Face, execution.PaidFace))
            return null;

        return new() { CardId = cardId, CardCost = execution.PaidFace, BranchId = branchId };
    }

    private static DirectEventChoiceView? DirectEventChoice(MatchState state)
    {
        var pending = state.Pending;
        var frames = state.Resolution.Frames;
        // A separation may move a Companion into a nation's region; that political consequence
        // waits, public, until the action completes.
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Resolution.Window.Length != 0 ||
            state.PoliticalReactions.Count != 0 && pending?.Kind != DecisionKind.Guide || pending is null ||
            frames.Count == 0 || state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1)
            return null;

        // The stack-limit and Guide decisions name no source; their event is the first frame.
        var cardId = pending.Kind is DecisionKind.RemoveExcess or DecisionKind.Guide
            ? frames[0].Effect?.SourceId ?? "" : pending.SourceId;
        if (!CardCatalog.All.TryGetValue(cardId, out var card))
            return null;
        var player = state.Player(card.Side);
        if (!CardCatalog.Enabled(state.Configuration, cardId) ||
            !EffectCatalog.All.Events.TryGetValue(cardId, out var definition) ||
            !EffectCatalog.All.Programs.TryGetValue(definition.ProgramId, out var program) ||
            !(definition.StaysOnTable ? player.Table : player.Discard).Contains(cardId) || player.Hand.Contains(cardId) ||
            !Discharged(state.Events.Where(item => item.OnlyFor is null)).Contains(cardId))
            return null;

        var frame = frames[0];
        var execution = frame.Effect;
        if (execution is null || frame.EffectId != definition.ProgramId || frame.Window.Length != 0 ||
            frame.ReturnDecision is not null || frame.Fellowship is not null || frame.Hunt is not null ||
            frame.Battle is not null || frame.BattleLoss is not null || frame.EventLoss is not null ||
            frame.Corruption is not null || execution is not
                { Owner: var owner, SourceId: var source, Return: EffectReturn.FinishAction, IsEvent: true,
                    AnarionDrawPending: false } || owner != card.Side || source != cardId ||
            execution.ArmyOrder is not null && pending.Kind != DecisionKind.ArmyEvent ||
            execution.PaidFace != DieFace.Event && execution.PaidFace != card.EventType ||
            execution.ReactionWindow.Length != 0 || execution.OfferedReactions.Count != 0 ||
            // The first entry names the card, and the nation an event is played against.
            execution.Committed.Count == 0 || execution.Committed[0] != new EffectSelection(card.Side, cardId) &&
                execution.Committed[0] != new EffectSelection(card.Side, cardId, execution.NationId) ||
            execution.AppliedHits != 0 ||
            execution.InspectedSovereign is not null && !DirectEventChoices.IsInspection(program) ||
            execution.InspectedHand is not null || execution.RingRecovery is not null ||
            execution.MadnessTarget is not null && !HasMadnessTarget(execution, program) || execution.FellowshipMove)
            return null;

        // Losses fall to the target's owner, usually the event's opponent. Every other
        // reviewed decision belongs to the event's owner.
        var reviewed = pending.Kind switch
        {
            DecisionKind.Losses => IsReviewedEventLoss(state, pending, frame, execution, program),
            DecisionKind.EffectChoice when pending.Owner != card.Side =>
                IsReviewedOpponentFigureLoss(state, pending, frame, execution, program),
            _ when pending.Owner != card.Side => false,
            DecisionKind.EffectChoice => IsReviewedEventChoice(state, pending, frame, execution, program),
            DecisionKind.Reaction => IsReviewedEventReaction(state, pending, frame, execution, program),
            DecisionKind.CharacterEvent => IsReviewedEventTravel(state, pending, frame, execution, program),
            DecisionKind.CompanionSeparation => IsReviewedEventSeparation(state, pending, frame, execution, program),
            DecisionKind.Guide => IsReviewedEventGuide(state, pending, frame, execution, program),
            DecisionKind.ArmyEvent => IsReviewedArmyOrder(state, pending, frame, execution, program),
            DecisionKind.RemoveExcess => IsReviewedEventExcess(state, pending, frame, execution, program),
            _ => false
        };
        if (!reviewed)
            return null;

        var paidDie = state.Dice.SingleOrDefault(die => die.Id == state.Resolution.PaidDieIds[0]);
        if (paidDie is null || paidDie.Owner != card.Side || paidDie.Location != DieLocation.Resolving ||
            !CanPayObservedFace(paidDie.Face, execution.PaidFace))
            return null;

        return new()
        {
            CardId = cardId, CardCost = execution.PaidFace,
            RegionId = execution.MadnessTarget?.Region ?? (DirectEventChoices.LeavesARegion(program) ? execution.RegionId : ""),
            TargetIds = DirectEventChoices.RepeatsOnTargets(program) ? [.. execution.TargetIds] : []
        };
    }

    // The public loss view names the target, hits and rolls. The program's remaining
    // steps do not read the rolled target, so nothing else needs restoring.
    private static bool IsReviewedEventLoss(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        DirectEventChoices.RepeatsOnTargets(program)
            ? IsReviewedRepeatedLoss(state, pending, frame, execution)
            : IsReviewedSingleLoss(state, pending, frame, execution, program);

    // Return to Valinor's cursor steps back before each roll, so its losses wait at step zero
    // with the region they struck last.
    private static bool IsReviewedRepeatedLoss(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution) =>
        pending.Commands is [CommandKind.TakeEventLosses] && state.Resolution.Frames is [_, var lossFrame] &&
        lossFrame is { EffectId: "", NextStep: 0, Window: "", ReturnDecision: null, Effect: null, Fellowship: null,
            Hunt: null, Battle: null, BattleLoss: null, Corruption: null,
            EventLoss: { Cause: CasualtyCause.Event } loss } &&
        loss.SourceId == execution.SourceId && loss.Target.Owner == pending.Owner && loss.Hits > 0 &&
        pending.RequiredCount == loss.Hits && frame.NextStep == 0 &&
        execution is { Wait: EffectWait.None, CompletionWindowOffered: false, TargetIds.Count: > 0 } &&
        loss.Target.Region == execution.TargetIds[^1] && !loss.Target.Garrison &&
        DirectEventChoices.RecordsStruckRegions(execution) &&
        execution.RegionId.Length == 0 && execution.NationId.Length == 0 &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution);

    private static bool IsReviewedSingleLoss(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending.Commands is [CommandKind.TakeEventLosses] && state.Resolution.Frames is [_, var lossFrame] &&
        lossFrame is { EffectId: "", NextStep: 0, Window: "", ReturnDecision: null, Effect: null, Fellowship: null,
            Hunt: null, Battle: null, BattleLoss: null, Corruption: null,
            EventLoss: { Cause: CasualtyCause.Event } loss } &&
        loss.SourceId == execution.SourceId && loss.Target.Owner == pending.Owner && loss.Hits > 0 &&
        pending.RequiredCount == loss.Hits &&
        execution is { Wait: EffectWait.None, CompletionWindowOffered: false } &&
        DirectEventChoices.LossStep(program) is > 0 and var step && frame.NextStep == step &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution) &&
        (program.Steps is not [NorthRangersAttackStep, ..] || execution.RegionId == loss.Target.Region &&
            execution.Committed.Count == (DirectEventChoices.RangersTargets(state) == 1 ? 1 : 2)) &&
        (!DirectEventChoices.IsMadness(program) || execution.MadnessTarget == new MadnessTarget(loss.Target.Region,
            DirectEventChoices.MadnessBesieged(state, loss.Target.Region, loss.Target.Garrison)) &&
            execution.Committed.Count == (DirectEventChoices.MadnessTargets(state) == 1 ? 1 : 2));

    // Madness keeps its target from the owner's first choice, whose region it names; a lone
    // option was taken automatically and left no choice in the trail.
    private static bool HasMadnessTarget(EffectExecution execution, EffectDefinition program) =>
        DirectEventChoices.IsMadness(program) && execution.MadnessTarget is { } target &&
        BoardRules.Regions.ContainsKey(target.Region) && (execution.Committed is [_] ||
            execution.Committed is [_, var chosen] && chosen.Owner == execution.Owner &&
            chosen.SourceId == execution.SourceId && chosen.ChoiceId == target.Region && chosen.TargetId == target.Region);

    private static bool IsReviewedEventSeparation(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending is { Commands: [CommandKind.SeparateCompanions], Window: "", RequiredCount: 0 } &&
        state.Resolution.Frames is [_, var separation] && HuntChoiceProjection.PlainFrame(separation) &&
        separation.EffectId == "fellowship.Separation" &&
        separation.Fellowship is { Kind: FellowshipProcedureKind.Separation, Return: FellowshipReturn.Parent, Journey: null,
            FixedCompanions.Count: 0, SeparationOrigin: "", SeparationRange: 0, Route.Count: 0,
            AdditionalHuntRegions.Count: 0, AwaitingHunts: false, RevealEffectsPending: false,
            FireAndShadowPending: false, ChooseGuideAfterPlacement: false } fellowship &&
        fellowship.SourceId == execution.SourceId && program.Steps.Count > 0 &&
        program.Steps[0] is SeparateCompanionsStep step && fellowship.SeparationBonus == step.Bonus &&
        frame.NextStep == 1 && execution is { Wait: EffectWait.None, CompletionWindowOffered: false } &&
        HasNoEventSelection(execution);

    // A separation that takes the Guide away asks for a new one inside the same procedure,
    // among the public Companions left in the Fellowship.
    private static bool IsReviewedEventGuide(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending is { Commands: [CommandKind.SelectGuide], Window: "", RequiredCount: 0, SourceId: "" } &&
        pending.PrivateChoices.Count >= 2 && pending.PrivateChoices.SequenceEqual(CompanionRules.Guides(state)) &&
        state.Resolution.Frames is [_, var guide] && HuntChoiceProjection.PlainFrame(guide) &&
        guide.EffectId == "fellowship.Separation" &&
        guide.Fellowship is { Kind: FellowshipProcedureKind.Guide, Return: FellowshipReturn.Parent, Journey: null,
            FixedCompanions.Count: 0, SeparationOrigin: "", SeparationRange: 0, Route.Count: 0,
            AdditionalHuntRegions.Count: 0, AwaitingHunts: false, RevealEffectsPending: false,
            FireAndShadowPending: false, ChooseGuideAfterPlacement: false } fellowship &&
        fellowship.SourceId == execution.SourceId && program.Steps.Count > 0 &&
        program.Steps[0] is SeparateCompanionsStep step && fellowship.SeparationBonus == step.Bonus &&
        frame.NextStep == 1 && execution is { Wait: EffectWait.None, CompletionWindowOffered: false } &&
        HasNoEventSelection(execution);

    private static bool IsReviewedEventChoice(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program)
    {
        var cardId = execution.SourceId;
        if (DirectEventChoices.IsInspection(program))
            return IsReviewedInspectionChoice(state, pending, frame, execution, program);
        if (program.Steps is [WisdomBranchStep] && state.Resolution.Frames is [_, { EffectId: WisdomBranchStep.Politics } politics])
            return IsReviewedWisdomPolitics(state, pending, frame, execution, politics);
        if (program.Steps is [WisdomBranchStep] && state.Resolution.Frames.Count == 2)
            return IsReviewedWisdomInspection(state, pending, frame, execution);
        if (DirectEventChoices.RepeatsOnTargets(program))
            return IsReviewedRepeatedChoice(state, pending, frame, execution, program);
        if (pending.Commands is [CommandKind.ChooseEffect] &&
            DirectEventChoices.IsNationalCard(program, out _, out var otherwise, out var recruit))
            return IsReviewedNationalCardChoice(state, pending, frame, execution, otherwise, recruit);
        if (pending.Commands is not [CommandKind.ChooseEffect] || state.Resolution.Frames.Count != 1 ||
            execution is not { Wait: EffectWait.Choice, CompletionWindowOffered: false } ||
            !DirectEventChoices.IsReviewed(cardId, frame.NextStep, program) ||
            program.Steps[frame.NextStep - 1] is not EffectChoiceStep choice ||
            pending.Presentation != choice.Presentation)
            return false;

        var candidates = choice.Candidates(state, execution);
        if (candidates.Count == 0 || !pending.PrivateChoices.SequenceEqual(candidates) ||
            execution.PalantirDrawPending != DirectEventChoices.PalantirDrawExpected(state, execution))
            return false;

        if (DirectEventChoices.IsMadness(program) && frame.NextStep == 2)
            return execution.MadnessTarget is not null;
        // A region an earlier step chose is public through its roll or its recruitment limits.
        if (DirectEventChoices.LeavesARegion(program) && frame.NextStep >= 2)
            return execution.RegionId.Length == 0 || BoardRules.Regions.ContainsKey(execution.RegionId) &&
                execution.TargetIds.Count == 0 && execution.NationId.Length == 0 &&
                execution.Committed.All(selection => selection.Owner == execution.Owner &&
                    DirectEventChoices.BelongsToEvent(selection, execution.SourceId));
        return cardId == KingsCards02.Service && frame.NextStep == 2
            ? ServiceSovereignSelected(state, execution)
            : DirectEventChoices.ReadsAnEarlierRegion(choice)
                ? HasPublicRecruitRegion(execution)
                : HasNoEventSelection(execution);
    }

    // Each Return to Valinor choice names one more Elven Stronghold; the regions already struck
    // are public and leave their options.
    private static bool IsReviewedRepeatedChoice(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program)
    {
        if (pending.Commands is not [CommandKind.ChooseEffect] || state.Resolution.Frames.Count != 1 ||
            frame.NextStep != 1 || execution is not { Wait: EffectWait.Choice, CompletionWindowOffered: false } ||
            program.Steps[0] is not EffectChoiceStep choice || pending.Presentation != choice.Presentation ||
            execution.RegionId.Length != 0 || execution.NationId.Length != 0 ||
            !DirectEventChoices.RecordsStruckRegions(execution) ||
            execution.PalantirDrawPending != DirectEventChoices.PalantirDrawExpected(state, execution))
            return false;
        var candidates = choice.Candidates(state, execution);
        return candidates.Count != 0 && pending.PrivateChoices.SequenceEqual(candidates);
    }

    // Wisdom's politics branch runs in its own nested program: the Free Peoples nation it
    // activates and advances, chosen from the public board.
    private static bool IsReviewedWisdomPolitics(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, ResolutionFrame nested)
    {
        if (pending.Commands is not [CommandKind.ChooseEffect] || frame.NextStep != 1 ||
            execution is not { Wait: EffectWait.None, CompletionWindowOffered: false } ||
            execution.PalantirDrawPending != DirectEventChoices.PalantirDrawExpected(state, execution) ||
            execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 || execution.NationId.Length != 0 ||
            execution.InspectedSovereign is not null ||
            nested is not { NextStep: 1, Window: "", ReturnDecision: null, Fellowship: null, Hunt: null,
                Battle: null, BattleLoss: null, EventLoss: null, Corruption: null } ||
            nested.Effect is not { Return: EffectReturn.Parent, Wait: EffectWait.Choice, IsEvent: false,
                PalantirDrawPending: false, AnarionDrawPending: false, CompletionWindowOffered: false, AppliedHits: 0,
                ArmyOrder: null, InspectedHand: null, InspectedSovereign: null, RingRecovery: null,
                MadnessTarget: null, FellowshipMove: false, NationId: "", RegionId: "" } inner ||
            inner.Owner != execution.Owner || inner.SourceId != execution.SourceId || inner.Committed.Count != 0 ||
            inner.TargetIds.Count != 0 || inner.ReactionWindow.Length != 0 || inner.OfferedReactions.Count != 0 ||
            !EffectCatalog.All.Programs.TryGetValue(WisdomBranchStep.Politics, out var politics) ||
            politics.Steps is not [KingsNationStep { Activate: true } choice] || pending.Presentation != choice.Presentation ||
            !execution.Committed.SequenceEqual([new EffectSelection(execution.Owner, execution.SourceId),
                new EffectSelection(execution.Owner, execution.SourceId, WisdomBranchStep.Politics)]))
            return false;
        var candidates = choice.Candidates(state, inner);
        return candidates.Count != 0 && pending.PrivateChoices.SequenceEqual(candidates);
    }

    // Wisdom's inspection runs in its own nested program: the sovereign, then the tile to
    // return. The owner's trail holds the branch, then the sovereign once chosen.
    private static bool IsReviewedWisdomInspection(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution)
    {
        if (pending.Commands is not [CommandKind.ChooseEffect] || state.Resolution.Frames is not [_, var nested] ||
            frame.NextStep != 1 || execution is not { Wait: EffectWait.None, CompletionWindowOffered: false } ||
            execution.PalantirDrawPending != DirectEventChoices.PalantirDrawExpected(state, execution) ||
            execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 || execution.NationId.Length != 0 ||
            execution.InspectedSovereign is not null || nested.EffectId != WisdomBranchStep.Inspect ||
            nested is not { NextStep: 1 or 2, Window: "", ReturnDecision: null, Fellowship: null, Hunt: null,
                Battle: null, BattleLoss: null, EventLoss: null, Corruption: null } ||
            nested.Effect is not { Return: EffectReturn.Parent, Wait: EffectWait.Choice, IsEvent: false,
                PalantirDrawPending: false, AnarionDrawPending: false, CompletionWindowOffered: false, AppliedHits: 0,
                ArmyOrder: null, InspectedHand: null, RingRecovery: null, MadnessTarget: null, FellowshipMove: false } inner ||
            inner.Owner != execution.Owner || inner.SourceId != execution.SourceId || inner.Committed.Count != 0 ||
            inner.TargetIds.Count != 0 || inner.NationId.Length != 0 || inner.RegionId.Length != 0 ||
            inner.ReactionWindow.Length != 0 || inner.OfferedReactions.Count != 0 ||
            !EffectCatalog.All.Programs.TryGetValue(WisdomBranchStep.Inspect, out var inspect) ||
            inspect.Steps is not [SelectInspectedSovereignStep, InspectCorruptionStep] ||
            inspect.Steps[nested.NextStep - 1] is not EffectChoiceStep choice || pending.Presentation != choice.Presentation)
            return false;

        var trail = execution.Committed;
        var automatic = nested.NextStep == 2 && trail.Count == 2 && inner.InspectedSovereign is { } lone &&
            DirectEventChoices.LoneInspectedSovereign(state) == lone;
        if (trail.Count != nested.NextStep + 1 && !automatic ||
            trail[0] != new EffectSelection(execution.Owner, execution.SourceId) ||
            trail.Skip(1).Any(selection => selection.Owner != execution.Owner || selection.SourceId != execution.SourceId) ||
            trail[1].ChoiceId != WisdomBranchStep.Inspect || trail[1].TargetId.Length != 0 ||
            (nested.NextStep == 1 ? inner.InspectedSovereign is not null
                : !automatic && inner.InspectedSovereign != trail[2].ChoiceId))
            return false;
        var candidates = choice.Candidates(state, inner);
        return candidates.Count != 0 && pending.PrivateChoices.SequenceEqual(candidates);
    }

    // Each inspection step finds its earlier choices in the owner's trail: the nation, then the
    // sovereign whose tiles only the owner may now read.
    private static bool IsReviewedInspectionChoice(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program)
    {
        if (pending.Commands is not [CommandKind.ChooseEffect] || state.Resolution.Frames.Count != 1 ||
            execution is not { Wait: EffectWait.Choice, CompletionWindowOffered: false } ||
            frame.NextStep is < 1 or > 3 || program.Steps[frame.NextStep - 1] is not EffectChoiceStep choice ||
            pending.Presentation != choice.Presentation || execution.RegionId.Length != 0 ||
            execution.TargetIds.Count != 0 ||
            execution.PalantirDrawPending != DirectEventChoices.PalantirDrawExpected(state, execution))
            return false;

        var chosen = execution.Committed.Skip(1).ToArray();
        var automatic = frame.NextStep == 3 && chosen.Length == 1 && execution.InspectedSovereign is { } lone &&
            DirectEventChoices.LoneInspectedSovereign(state) == lone;
        if (chosen.Length != frame.NextStep - 1 && !automatic ||
            chosen.Any(selection => selection.Owner != execution.Owner || selection.SourceId != execution.SourceId) ||
            (chosen.Length >= 1 ? chosen[0].ChoiceId != execution.NationId || chosen[0].TargetId.Length != 0
                : execution.NationId.Length != 0) ||
            (chosen.Length == 2 ? execution.InspectedSovereign != chosen[1].ChoiceId
                : !automatic && execution.InspectedSovereign is not null))
            return false;

        var candidates = choice.Candidates(state, execution);
        return candidates.Count > 0 && pending.PrivateChoices.SequenceEqual(candidates);
    }

    // The route options come from the public Nazgul or Balrog positions. The card's
    // only earlier state is the owner's order to fly or to travel alone, which stays
    // in their own trail.
    private static bool IsReviewedEventTravel(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending is { Owner: Wotr.Contracts.Side.Shadow, Commands: [CommandKind.MoveShadowCharacters] } &&
        state.Resolution.Frames.Count == 1 && execution is { CompletionWindowOffered: false } && frame.NextStep == 1 &&
        execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 && execution.NationId.Length == 0 &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution) &&
        (execution.Wait == EffectWait.NazgulTravel && DirectEventChoices.TravelsFirst(program) &&
            execution.Committed.Skip(1).All(selection => DirectEventChoices.IsSpearOfTerror(selection) ||
                DirectEventChoices.IsCaptainFlight(selection)) && execution.Committed.Count <= 3 ||
         execution.Wait == EffectWait.BalrogTravel &&
            DirectEventChoices.ChoseBalrog(program, frame.NextStep, execution.Committed, BalrogEventStep.Alone) &&
            BalrogEventTravel.Travel(state) is { } travel && travel.Targets.Any(region => region != travel.Origin));

    private static bool IsReviewedOpponentFigureLoss(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending is { Commands: [CommandKind.ChooseEffect], Window: "" } && state.Resolution.Frames.Count == 1 &&
        DirectEventChoices.IsOpponentFigureLoss(program, frame.NextStep) &&
        program.Steps[^1] is EffectChoiceStep step && pending.Presentation == step.Presentation &&
        pending.Owner == step.Chooser(state, execution) && pending.Owner != execution.Owner &&
        execution is { Wait: EffectWait.Choice, CompletionWindowOffered: false } &&
        execution.NationId.Length > 0 && execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 &&
        execution.Committed.Skip(1).All(DirectEventChoices.IsSpearOfTerror) && execution.Committed.Count <= 2 &&
        step.Candidates(state, execution) is { Count: > 0 } candidates && pending.PrivateChoices.SequenceEqual(candidates) &&
        candidates.All(id => state.Units.SingleOrDefault(unit => unit.Id == id)?.Nation == execution.NationId) &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution);

    // An event that leaves an army over the stack limit asks its owner to return the excess
    // before it completes. Every step is done, so only the completion facts remain.
    private static bool IsReviewedEventExcess(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending is { Commands: [CommandKind.RemoveExcess], Window: "" } && pending.SourceId.Length == 0 &&
        state.Resolution.Frames.Count == 1 && frame.NextStep == program.Steps.Count &&
        execution is { Wait: EffectWait.None, CompletionWindowOffered: false } &&
        execution.Committed.All(selection => selection.Owner == execution.Owner &&
            DirectEventChoices.BelongsToEvent(selection, execution.SourceId)) &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution);

    private static bool IsReviewedArmyOrder(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        state.Resolution.Frames.Count == 1 &&
        DirectEventChoices.ArmyOrderAt(program, frame.NextStep, execution.Committed) is { } order &&
        execution is { Wait: EffectWait.Army, CompletionWindowOffered: false } && execution.ArmyOrder == order &&
        pending.Presentation == order.ToString() && pending.Commands.SequenceEqual(ArmyOrderStep.Commands(order)) &&
        execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 && execution.NationId.Length == 0 &&
        execution.Committed.All(selection => selection.Owner == execution.Owner &&
            DirectEventChoices.BelongsToEvent(selection, execution.SourceId)) &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution) &&
        ArmyEventOptions.Armies(state, execution.Owner, order).Any();

    // The branch choice and the recruitment it opens each live in their own nested
    // program. Their candidates come from the public board; the owner's trail holds
    // the branch once chosen.
    private static bool IsReviewedNationalCardChoice(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, string otherwise, string recruit)
    {
        var frames = state.Resolution.Frames;
        if (frames.Count is not (2 or 3) || frame.NextStep != 1 ||
            execution is not { Wait: EffectWait.None, CompletionWindowOffered: false } ||
            execution.PalantirDrawPending != DirectEventChoices.PalantirDrawExpected(state, execution) ||
            execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 || execution.NationId.Length != 0 ||
            !IsNestedEventFrame(frames[1], execution, otherwise, frames.Count == 2 ? EffectWait.Choice : EffectWait.None))
            return false;

        var played = new EffectSelection(execution.Owner, execution.SourceId);
        var branch = frames.Count == 2;
        var active = branch ? frames[1] : frames[2];
        if (!branch && !IsNestedEventFrame(active, execution, recruit, EffectWait.Choice))
            return false;
        var expectedTrail = branch ? new[] { played } :
            new[] { played, new EffectSelection(execution.Owner, execution.SourceId, "recruit") };
        if (!execution.Committed.SequenceEqual(expectedTrail) ||
            EffectCatalog.All.Programs[active.EffectId].Steps[0] is not EffectChoiceStep choice ||
            pending.Presentation != choice.Presentation)
            return false;
        var candidates = choice.Candidates(state, active.Effect!);
        return candidates.Count != 0 && pending.PrivateChoices.SequenceEqual(candidates);
    }

    private static bool IsNestedEventFrame(ResolutionFrame frame, EffectExecution parent, string programId, EffectWait wait) =>
        frame is { NextStep: 1, Window: "", ReturnDecision: null, Fellowship: null, Hunt: null, Battle: null,
            BattleLoss: null, EventLoss: null, Corruption: null } && frame.EffectId == programId &&
        frame.Effect is { Return: EffectReturn.Parent, IsEvent: false, PalantirDrawPending: false,
            AnarionDrawPending: false, CompletionWindowOffered: false, AppliedHits: 0, ArmyOrder: null } nested &&
        nested.Wait == wait && nested.Owner == parent.Owner && nested.SourceId == parent.SourceId &&
        nested.Committed.Count == 0 && nested.TargetIds.Count == 0 && nested.NationId.Length == 0 &&
        nested.RegionId.Length == 0 && nested.ReactionWindow.Length == 0 && nested.OfferedReactions.Count == 0 && nested.InspectedSovereign is null &&
        nested.InspectedHand is null && nested.RingRecovery is null && nested.MadnessTarget is null && !nested.FellowshipMove;

    // A window opened on an event in play holds no step state: event.played comes
    // before the first step, and event.completed after the last.
    private static bool IsReviewedEventReaction(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program)
    {
        if (pending.Commands is not [CommandKind.RespondEffect, CommandKind.DeclineReaction] ||
            state.Resolution.Frames is not [_, var window] || window.EffectId != EffectCatalog.ReactionProgram ||
            window.NextStep != 0 || window.Window.Length != 0 || window.ReturnDecision is not null ||
            window.Effect is not { Return: EffectReturn.Parent, Wait: EffectWait.Reaction, IsEvent: false } reaction ||
            reaction.Owner != pending.Owner || reaction.SourceId != pending.SourceId ||
            reaction.ReactionWindow != pending.Window || reaction.Committed.Count != 0 ||
            execution.Wait != EffectWait.None)
            return false;

        return pending.Window switch
        {
            "event.played" => frame.NextStep == 0 && !execution.CompletionWindowOffered &&
                execution.Committed.Count == 1 && HasNoEventSelection(execution) &&
                execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution),
            "event.completed" => frame.NextStep == program.Steps.Count && execution.CompletionWindowOffered &&
                !execution.PalantirDrawPending,
            _ => false
        };
    }

    private static bool HasPublicRecruitRegion(EffectExecution execution) =>
        BoardRules.Regions.ContainsKey(execution.RegionId) && execution.TargetIds.Count == 0 &&
        execution.NationId.Length == 0 && execution.Committed.All(selection =>
            selection.Owner == execution.Owner && DirectEventChoices.BelongsToEvent(selection, execution.SourceId));

    private static bool HasNoEventSelection(EffectExecution execution) =>
        execution.Committed.Skip(1).All(DirectEventChoices.IsSpearOfTerror) && execution.Committed.Count <= 2 &&
        execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 && execution.NationId.Length == 0;

    // The engine selects a lone eligible ruler without recording an owner choice,
    // so only that forced case may leave the selection out of the effect trail.
    private static bool ServiceSovereignSelected(MatchState state, EffectExecution execution)
    {
        var eligible = NonCorruptedSovereigns.OnBoard(state).ToArray();
        if (execution.TargetIds is not [var sovereignId] ||
            eligible.SingleOrDefault(item => item.Id == sovereignId) is not { } sovereign)
            return false;
        var region = SovereignLifecycle.Region(state, sovereign);
        if (execution.RegionId != region || execution.NationId != SovereignCatalog.All[sovereign.Id].Nation)
            return false;
        return execution.Committed.Count == 1 ? eligible.Length == 1 : execution.Committed is [_, var selection] &&
            selection == new EffectSelection(execution.Owner, execution.SourceId, sovereign.Id, region);
    }

    private static bool CanPayObservedFace(DieFace face, DieFace cost) =>
        face == cost || face == DieFace.WillOfTheWest ||
        face == DieFace.ArmyMuster && cost is DieFace.Army or DieFace.Muster;

    private static bool IsRelentlessAssaultLoss(MatchState state, PendingDecision pending,
        BattleState battle, IReadOnlyList<ResolutionFrame> frames)
    {
        var effectFrame = frames[1];
        var lossFrame = frames[2];
        var execution = effectFrame.Effect;
        var loss = lossFrame.BattleLoss;
        var source = execution?.SourceId ?? "";
        // Relentless Assault asks before the roll of any round; earlier rounds have closed.
        return pending.Kind == DecisionKind.Losses && ReviewedCombatCards.IsRelentlessAssault(source) &&
            battle is { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeRoll, Round: >= 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, EndedByEffect: false, NextWork: > 0 } &&
            battle.CompletedRounds == battle.Round - 1 &&
            effectFrame.EffectId == "combat.relentless-assault" && effectFrame.NextStep == 1 &&
            effectFrame.Window.Length == 0 && effectFrame.ReturnDecision is null &&
            effectFrame.Fellowship is null && effectFrame.Hunt is null && effectFrame.Battle is null &&
            effectFrame.BattleLoss is null && effectFrame.EventLoss is null && effectFrame.Corruption is null &&
            execution is { Owner: Side.Shadow,
                Return: EffectReturn.Parent, Wait: EffectWait.None, AppliedHits: 0,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false } &&
            execution.NationId.Length == 0 && execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 &&
            execution.PaidFace == default && execution.ArmyOrder is null && execution.ReactionWindow.Length == 0 &&
            !execution.CompletionWindowOffered && execution.OfferedReactions.Count == 0 &&
            execution.Committed.Count == 0 && execution.InspectedSovereign is null && execution.InspectedHand is null &&
            execution.RingRecovery is null && execution.MadnessTarget is null && !execution.FellowshipMove &&
            lossFrame.EffectId.Length == 0 && lossFrame.NextStep == 0 && lossFrame.Window.Length == 0 &&
            lossFrame.ReturnDecision is null && lossFrame.Effect is null && lossFrame.Fellowship is null &&
            lossFrame.Hunt is null && lossFrame.Battle is null && lossFrame.EventLoss is null &&
            lossFrame.Corruption is null && loss is { Owner: Side.Shadow, Hits: 2, Voluntary: true,
                AdditionalAttack: false } &&
            pending.Owner == loss.Owner && pending.RequiredCount == loss.Hits && pending.SourceId == loss.SourceId &&
            pending.Presentation == "VoluntaryLosses" && pending.Commands is [CommandKind.TakeBattleLosses] &&
            loss.SourceId == execution.SourceId && HasOnlyEarlierSuddenStrikes(battle) &&
            battle.Work[battle.NextWork - 1] == new CombatWork(Side.Shadow, source,
                "combat.relentless-assault", 3) &&
            battle.Force(Side.Shadow).Round is { CardCancelled: false, VoluntaryLossHits: null } shadowRound &&
            shadowRound.CardId == source && battle.Attacker.Round.Dice.Count == 0 && battle.Defender.Round.Dice.Count == 0 &&
            CardCatalog.Enabled(state, source) &&
            EffectCatalog.All.Combats.TryGetValue(source, out var combat) &&
            combat.Effects is [{ Initiative: 3, ProgramId: "combat.relentless-assault" }] &&
            combat.Condition.CanPlay(state, battle, Side.Shadow) &&
            EffectCatalog.All.Programs.TryGetValue("combat.relentless-assault", out var program) &&
            program.Steps is [VoluntaryLossesStep { Maximum: 2, AdditionalAttack: false },
                SacrificeResultStep { AdditionalAttack: false }] &&
            BattleRules.Alive(state, battle.Force(Side.Shadow));
    }

    // A Sudden Strike resolved at initiative 2 leaves its public special-attack dice; its hits
    // were taken before Relentless Assault opens.
    private static bool HasOnlyEarlierSuddenStrikes(BattleState battle)
    {
        var strikes = battle.Work.Take(battle.NextWork - 1).Where(work =>
            EffectCatalog.All.Programs.TryGetValue(work.ProgramId, out var program) && program.Steps is [SuddenStrikeStep]).ToArray();
        return battle.SpecialAttacks.Count <= strikes.Length &&
            battle.SpecialAttacks.All(attack => strikes.Any(work => work.Owner == attack.Owner && work.CardId == attack.SourceId)) &&
            battle.SpecialAttacks.Select(attack => attack.SourceId).Distinct().Count() == battle.SpecialAttacks.Count;
    }

    private static bool IsReviewedVoluntaryBattleLoss(MatchState state, PendingDecision pending,
        BattleState battle, IReadOnlyList<ResolutionFrame> frames) =>
        IsRelentlessAssaultLoss(state, pending, battle, frames) ||
        IsOnslaughtVoluntaryLoss(state, pending, battle, frames);

    private static bool IsOnslaughtVoluntaryLoss(MatchState state, PendingDecision pending,
        BattleState battle, IReadOnlyList<ResolutionFrame> frames)
    {
        var effectFrame = frames[1];
        var lossFrame = frames[2];
        var execution = effectFrame.Effect;
        var loss = lossFrame.BattleLoss;
        return pending.Kind == DecisionKind.Losses &&
            battle is { Return: EffectReturn.FinishAction, Stage: BattleStage.AfterLosses, Round: >= 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, EndedByEffect: false } &&
            battle.CompletedRounds == battle.Round - 1 && effectFrame.EffectId == ReviewedCombatCards.OnslaughtProgram &&
            effectFrame.NextStep == 1 && effectFrame.Window.Length == 0 && effectFrame.ReturnDecision is null &&
            effectFrame.Fellowship is null && effectFrame.Hunt is null && effectFrame.Battle is null &&
            effectFrame.BattleLoss is null && effectFrame.EventLoss is null && effectFrame.Corruption is null &&
            execution is { Owner: Side.Shadow, Return: EffectReturn.Parent, Wait: EffectWait.None, AppliedHits: 0,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false } &&
            execution.NationId.Length == 0 && execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 &&
            execution.PaidFace == default && execution.ArmyOrder is null && execution.ReactionWindow.Length == 0 &&
            !execution.CompletionWindowOffered && execution.OfferedReactions.Count == 0 &&
            execution.Committed.Count == 0 && execution.InspectedSovereign is null && execution.InspectedHand is null &&
            execution.RingRecovery is null && execution.MadnessTarget is null && !execution.FellowshipMove &&
            lossFrame.EffectId.Length == 0 && lossFrame.NextStep == 0 && lossFrame.Window.Length == 0 &&
            lossFrame.ReturnDecision is null && lossFrame.Effect is null && lossFrame.Fellowship is null &&
            lossFrame.Hunt is null && lossFrame.Battle is null && lossFrame.EventLoss is null &&
            lossFrame.Corruption is null && loss is { Owner: Side.Shadow, Hits: 4, Voluntary: true,
                AdditionalAttack: true } && ReviewedCombatCards.IsOnslaught(execution.SourceId) &&
            pending.Owner == loss.Owner && pending.RequiredCount == loss.Hits && pending.SourceId == loss.SourceId &&
            pending.Presentation == "VoluntaryLosses" && pending.Commands is [CommandKind.TakeBattleLosses] &&
            loss.SourceId == execution.SourceId && battle.NextWork > 0 && battle.NextWork <= battle.Work.Count &&
            battle.Work[battle.NextWork - 1] == new CombatWork(Side.Shadow, execution.SourceId,
                ReviewedCombatCards.OnslaughtProgram, 7) &&
            battle.Force(Side.Shadow).Round is { CardCancelled: false, VoluntaryLossHits: null, SpentHits: 0 } &&
            battle.Force(Side.Shadow).Round.CardId == execution.SourceId && battle.Attacker.Round.SpentHits == 0 &&
            battle.Defender.Round.SpentHits == 0 && CardCatalog.Enabled(state, execution.SourceId) &&
            EffectCatalog.All.Combats.TryGetValue(execution.SourceId, out var combat) &&
            combat.Condition.CanPlay(state, battle, Side.Shadow) &&
            EffectCatalog.All.Programs.TryGetValue(ReviewedCombatCards.OnslaughtProgram, out var program) &&
            program.Steps is [VoluntaryLossesStep { Maximum: 4, AdditionalAttack: true },
                SacrificeResultStep { AdditionalAttack: true }] &&
            BattleRules.Alive(state, battle.Attacker) && BattleRules.Alive(state, battle.Defender);
    }

    private static BattleContextView? SpecialAttackLoss(MatchState state)
    {
        return FirstStepSpecialAttackLoss(state) ?? PostCombatSpecialAttackLoss(state) ?? GreatHostLoss(state);
    }

    private static BattleContextView? FirstStepSpecialAttackLoss(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Pending is not { Kind: DecisionKind.Losses } pending ||
            frames.Count != 3)
            return null;

        var battleFrame = frames[0];
        var effectFrame = frames[1];
        var lossFrame = frames[2];
        var battle = battleFrame.Battle;
        var execution = effectFrame.Effect;
        var loss = lossFrame.BattleLoss;
        if (battle is null || execution is null || loss is null)
            return null;
        if (battleFrame.EffectId != "" || battleFrame.NextStep != 0 || battleFrame.Window != "" ||
            battleFrame.ReturnDecision is not null || battleFrame.Effect is not null || battleFrame.Fellowship is not null ||
            battleFrame.Hunt is not null || battleFrame.BattleLoss is not null || battleFrame.EventLoss is not null ||
            battleFrame.Corruption is not null || battle is not { Return: EffectReturn.FinishAction,
                Stage: BattleStage.BeforeRoll, Round: 1, CardsRevealed: true, Mode: not BattleMode.Relief,
                NextWork: > 0, CompletedRounds: 0, EndedByEffect: false, WhiteRiderUsed: false, Kings: null } ||
            effectFrame.NextStep != 1 || effectFrame.Window != "" ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.None,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false } ||
            execution.NationId.Length != 0 || execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 ||
            execution.PaidFace != default || execution.ArmyOrder is not null || execution.ReactionWindow.Length != 0 ||
            execution.CompletionWindowOffered || execution.OfferedReactions.Count != 0 ||
            execution.Committed.Count != 0 || execution.InspectedSovereign is not null || execution.InspectedHand is not null ||
            execution.RingRecovery is not null || execution.MadnessTarget is not null || execution.FellowshipMove ||
            lossFrame.EffectId != "" || lossFrame.NextStep != 0 || lossFrame.Window != "" ||
            lossFrame.ReturnDecision is not null || lossFrame.Effect is not null || lossFrame.Fellowship is not null ||
            lossFrame.Hunt is not null || lossFrame.Battle is not null || lossFrame.EventLoss is not null ||
            lossFrame.Corruption is not null || loss is not { Voluntary: false, AdditionalAttack: false, Hits: > 0 } ||
            pending.Commands.Count != 1 || pending.Commands[0] != CommandKind.TakeBattleLosses ||
            pending.Owner != loss.Owner || pending.RequiredCount != loss.Hits || pending.SourceId != loss.SourceId ||
            loss.SourceId != execution.SourceId || battle.SpecialAttacks.Count is < 1 or > 2 ||
            execution.Owner != battle.SpecialAttacks[^1].Owner || loss.Owner != TurnFlow.Other(execution.Owner) ||
            battle.SpecialAttacks[^1].SourceId != loss.SourceId ||
            battle.SpecialAttacks[^1].Dice.Count == 0 ||
            battle.SpecialAttacks[^1].Dice.Count(die => die.Hit) != loss.Hits ||
            battle.Attacker.Round.Dice.Count != 0 || battle.Defender.Round.Dice.Count != 0 ||
            !FreshSpecialAttackRound(battle.Attacker.Round) || !FreshSpecialAttackRound(battle.Defender.Round) ||
            battle.NextWork > battle.Work.Count ||
            battle.Work[battle.NextWork - 1] != new CombatWork(execution.Owner, execution.SourceId,
                effectFrame.EffectId, 2) || !EarlierFirstStepAttacksResolved(state, battle) ||
            battle.Force(execution.Owner).Round.CardCancelled ||
            !CardCatalog.All.TryGetValue(execution.SourceId, out var sourceCard) || sourceCard.Side != execution.Owner ||
            !CardCatalog.Enabled(state, execution.SourceId) ||
            !EffectCatalog.All.Combats.TryGetValue(execution.SourceId, out var combat) ||
            combat.Effects.Count != 1 || combat.Effects[0].Initiative != 2 ||
            combat.Effects[0].ProgramId != effectFrame.EffectId || !combat.Condition.CanPlay(state, battle, execution.Owner) ||
            !EffectCatalog.All.Programs.TryGetValue(effectFrame.EffectId, out var program) || program.Steps.Count != 1 ||
            !IsSupportedFirstStepSpecialAttack(effectFrame.EffectId, program.Steps[0]) ||
            !IsSupportedFirstStepSpecialAttackOwner(execution.Owner, program.Steps[0]) ||
            battle.Force(execution.Owner).Round.CardId != execution.SourceId ||
            battle.Force(execution.Owner).Round.CardCancelled ||
            // A Kings game qualifies too: a combat replacement fails the fresh-round check,
            // and a Brand offer, made before any card effect, gives the battle Kings state.
            !BattleRules.Alive(state, battle.Force(loss.Owner)) ||
            state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving))
            return null;

        if (!FirstStepAttackRolled(state, battle, battle.SpecialAttacks[^1], program.Steps[0]))
            return null;

        return new() { SpecialAttackLoss = true };
    }

    // Effects queued ahead of this attack belong to a cancelled card or are the opposing card's own
    // first-step special attack at initiative 2, whose public dice have rolled and whose hits are taken.
    private static bool EarlierFirstStepAttacksResolved(MatchState state, BattleState battle)
    {
        var earlier = battle.Work.Take(battle.NextWork - 1)
            .Where(work => !battle.Force(work.Owner).Round.CardCancelled).ToArray();
        if (earlier.Length != battle.SpecialAttacks.Count - 1)
            return false;
        for (var index = 0; index < earlier.Length; index++)
        {
            var work = earlier[index];
            var attack = battle.SpecialAttacks[index];
            if (work.Initiative != 2 || attack.Owner != work.Owner || attack.SourceId != work.CardId ||
                battle.Force(work.Owner).Round.CardId != work.CardId || !CardCatalog.Enabled(state, work.CardId) ||
                !EffectCatalog.All.Combats.TryGetValue(work.CardId, out var combat) || combat.Effects.Count != 1 ||
                combat.Effects[0].Initiative != 2 || combat.Effects[0].ProgramId != work.ProgramId ||
                !EffectCatalog.All.Programs.TryGetValue(work.ProgramId, out var program) || program.Steps.Count != 1 ||
                !IsSupportedFirstStepSpecialAttack(work.ProgramId, program.Steps[0]) ||
                !IsSupportedFirstStepSpecialAttackOwner(work.Owner, program.Steps[0]) ||
                !FirstStepAttackRolled(state, battle, attack, program.Steps[0]))
                return false;
        }
        return true;
    }

    private static bool FirstStepAttackRolled(MatchState state, BattleState battle, SpecialAttackResult attack,
        EffectStep step)
    {
        var sourceForce = battle.Force(attack.Owner);
        var diceCount = FirstStepSpecialAttackDice(state, sourceForce, step);
        var hitTarget = FirstStepSpecialAttackHitTarget(state, battle, sourceForce, step);
        return attack.Dice.Count == diceCount && !attack.Dice.Any(die => die.Result is < 1 or > 6 ||
            die.Modifier != 0 || die.HitTarget != hitTarget || die.Hit != BattleRoll.Hits(die.Result, 0, hitTarget) ||
            die.ConvertedHit || die.Reroll.HasValue || die.RerollModifier != 0 || die.RerollHit || die.CannotReroll);
    }

    // Great Host rolls nothing: its one hit follows from the public army sizes.
    private static BattleContextView? GreatHostLoss(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Pending is not { Kind: DecisionKind.Losses } pending ||
            frames is not [var battleFrame, var effectFrame, var lossFrame] ||
            battleFrame.Battle is not { } battle || effectFrame.Effect is not { } execution ||
            lossFrame.BattleLoss is not { } loss || !IsPlainBattleFrame(battleFrame) ||
            !ReviewedCombatCards.IsGreatHost(execution.SourceId) || !CardCatalog.Enabled(state, execution.SourceId))
            return null;

        var work = new CombatWork(execution.Owner, execution.SourceId, ReviewedCombatCards.GreatHostProgram, 7);
        if (battle is not { Return: EffectReturn.FinishAction, Stage: BattleStage.AfterLosses, Round: >= 1, CardsRevealed: true,
                Mode: not BattleMode.Relief, EndedByEffect: false, WhiteRiderUsed: false, Kings: null, NextWork: > 0 } ||
            battle.CompletedRounds != battle.Round - 1 || battle.NextWork > battle.Work.Count ||
            battle.Work[battle.NextWork - 1] != work || battle.SpecialAttacks.Count != 0 ||
            battle.Force(execution.Owner).Round.CardCancelled ||
            battle.Force(execution.Owner).Round.CardId != execution.SourceId ||
            effectFrame.EffectId != work.ProgramId || effectFrame.NextStep != 1 || effectFrame.Window.Length != 0 ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.None,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false, AppliedHits: 0 } ||
            execution.Committed.Count != 0 || execution.TargetIds.Count != 0 || execution.RegionId.Length != 0 ||
            lossFrame is not { EffectId: "", NextStep: 0, Window: "", ReturnDecision: null, Effect: null, Fellowship: null,
                Hunt: null, Battle: null, EventLoss: null, Corruption: null } ||
            loss is not { Voluntary: false, AdditionalAttack: false, Hits: 1 } || loss.SourceId != execution.SourceId ||
            loss.Owner != TurnFlow.Other(execution.Owner) || pending.Commands is not [CommandKind.TakeBattleLosses] ||
            pending.Owner != loss.Owner || pending.RequiredCount != loss.Hits || pending.SourceId != loss.SourceId ||
            !EffectCatalog.All.Programs.TryGetValue(work.ProgramId, out var program) || program.Steps is not [GreatHostStep] ||
            !ReviewedCombatCards.GreatHostHits(state, battle, execution.Owner) ||
            !BattleRules.Alive(state, battle.Force(loss.Owner)) || state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving))
            return null;

        return new() { SpecialAttackLoss = true };
    }

    private static BattleContextView? PostCombatSpecialAttackLoss(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Pending is not { Kind: DecisionKind.Losses } pending ||
            frames.Count != 3)
            return null;

        var battleFrame = frames[0];
        var effectFrame = frames[1];
        var lossFrame = frames[2];
        var battle = battleFrame.Battle;
        var execution = effectFrame.Effect;
        var loss = lossFrame.BattleLoss;
        if (battle is null || execution is null || loss is null)
            return null;
        if (battleFrame.EffectId != "" || battleFrame.NextStep != 0 || battleFrame.Window != "" ||
            battleFrame.ReturnDecision is not null || battleFrame.Effect is not null || battleFrame.Fellowship is not null ||
            battleFrame.Hunt is not null || battleFrame.BattleLoss is not null || battleFrame.EventLoss is not null ||
            battleFrame.Corruption is not null || battle is not { Return: EffectReturn.FinishAction,
                Stage: BattleStage.AfterLosses, Round: >= 1, CardsRevealed: true, Mode: not BattleMode.Relief,
                CompletedRounds: var completedRounds, EndedByEffect: false, Kings: null } ||
            completedRounds != battle.Round - 1 || battle.WhiteRiderUsed ||
            effectFrame.NextStep is not (1 or 2) || effectFrame.Window != "" ||
            effectFrame.ReturnDecision is not null || effectFrame.Fellowship is not null || effectFrame.Hunt is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || execution is not { Return: EffectReturn.Parent, Wait: EffectWait.None,
                IsEvent: false, PalantirDrawPending: false, AnarionDrawPending: false } ||
            execution.NationId.Length != 0 || execution.RegionId.Length != 0 || execution.TargetIds.Count != 0 ||
            execution.PaidFace != default || execution.ArmyOrder is not null || execution.ReactionWindow.Length != 0 ||
            execution.CompletionWindowOffered || execution.OfferedReactions.Count != 0 ||
            execution.Committed.Count != 0 || execution.InspectedSovereign is not null || execution.InspectedHand is not null ||
            execution.RingRecovery is not null || execution.MadnessTarget is not null || execution.FellowshipMove ||
            lossFrame.EffectId != "" || lossFrame.NextStep != 0 || lossFrame.Window != "" ||
            lossFrame.ReturnDecision is not null || lossFrame.Effect is not null || lossFrame.Fellowship is not null ||
            lossFrame.Hunt is not null || lossFrame.Battle is not null || lossFrame.EventLoss is not null ||
            lossFrame.Corruption is not null || loss is not { Voluntary: false, AdditionalAttack: false, Hits: > 0 } ||
            pending.Commands.Count != 1 || pending.Commands[0] != CommandKind.TakeBattleLosses ||
            pending.Owner != loss.Owner || pending.RequiredCount != loss.Hits || pending.SourceId != loss.SourceId ||
            loss.SourceId != execution.SourceId || loss.Owner != TurnFlow.Other(execution.Owner) ||
            battle.SpecialAttacks.Count == 0 || battle.SpecialAttacks[^1].Owner != execution.Owner ||
            battle.SpecialAttacks[^1].SourceId != loss.SourceId ||
            battle.SpecialAttacks[^1].Dice.Count == 0 ||
            battle.SpecialAttacks[^1].Dice.Count(die => die.Hit) != loss.Hits ||
            battle.NextWork == 0 || battle.NextWork > battle.Work.Count ||
            battle.Work[battle.NextWork - 1] != new CombatWork(execution.Owner, execution.SourceId,
                effectFrame.EffectId, 7) || battle.Work.Take(battle.NextWork - 1).Any(work =>
                work.Initiative > 6 && !battle.Force(work.Owner).Round.CardCancelled) ||
            !CardCatalog.All.TryGetValue(execution.SourceId, out var sourceCard) ||
            sourceCard.Side != Side.Shadow || sourceCard.Side != execution.Owner ||
            !CardCatalog.Enabled(state, execution.SourceId) ||
            !EffectCatalog.All.Combats.TryGetValue(execution.SourceId, out var combat) ||
            combat.Effects.Count != 1 || combat.Effects[0].Initiative != 7 ||
            combat.Effects[0].ProgramId != effectFrame.EffectId ||
            !combat.Condition.CanPlay(state, battle, execution.Owner) ||
            battle.Force(execution.Owner).Round.CardCancelled ||
            !EffectCatalog.All.Programs.TryGetValue(effectFrame.EffectId, out var program) ||
            !BattleRules.Alive(state, battle.Force(loss.Owner)) ||
            battle.Attacker.Round.ReplacementSource is not null || battle.Defender.Round.ReplacementSource is not null ||
            state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving))
            return null;

        var weComeToKill = program.Steps is [WeComeToKillStep] && effectFrame.NextStep == 1 &&
            execution.AppliedHits == 0;
        var onslaught = program.Steps is [VoluntaryLossesStep { Maximum: 4, AdditionalAttack: true },
                SacrificeResultStep { AdditionalAttack: true }] && effectFrame.NextStep == 2 &&
            execution.AppliedHits is > 0 and <= 4;
        if (!weComeToKill && !onslaught)
            return null;

        var attack = battle.SpecialAttacks[^1];
        var force = battle.Force(execution.Owner);
        var diceCount = weComeToKill ? Math.Min(5, ChargeStep.Elites(state, force)) : execution.AppliedHits;
        var hitTarget = weComeToKill ? 5 : 4;
        if (attack.Dice.Count != diceCount || attack.Dice.Any(die => die.Result is < 1 or > 6 ||
            die.Modifier != 0 || die.HitTarget != hitTarget || die.Hit != BattleRoll.Hits(die.Result, 0, hitTarget) ||
            die.ConvertedHit || die.Reroll.HasValue || die.RerollModifier != 0 || die.RerollHit || die.CannotReroll))
            return null;

        return new() { SpecialAttackLoss = true };
    }

    private static bool IsSupportedFirstStepSpecialAttack(string programId, EffectStep step) =>
        (programId, step) is ("combat.charge", ChargeStep) or ("combat.sudden-strike", SuddenStrikeStep) or
            ("combat.durins-bane", DurinsBaneStep);

    private static bool IsSupportedFirstStepSpecialAttackOwner(Side owner, EffectStep step) =>
        owner == Side.FreePeoples && (step is ChargeStep or SuddenStrikeStep) ||
        owner == Side.Shadow && step is DurinsBaneStep;

    private static int FirstStepSpecialAttackDice(MatchState state, BattleForce force, EffectStep step) => step switch
    {
        ChargeStep => Math.Min(5, ChargeStep.Elites(state, force)),
        SuddenStrikeStep => Math.Clamp(BattleLeadership.Total(state, force), 0, 5),
        DurinsBaneStep => 3,
        _ => 0
    };

    private static int FirstStepSpecialAttackHitTarget(MatchState state, BattleState battle, BattleForce force,
        EffectStep step) => step is DurinsBaneStep ? 4 : BattleRules.HitTarget(state, battle, force);

    private static bool FreshSpecialAttackRound(BattleRoundState round) =>
        round.LeadershipPenalty == 0 && round.ReplacementSource is null && !round.DoubleSixes &&
        !round.OrderlyWithdrawal && round.CombatModifier == 0 && round.RerollModifier == 0 &&
        round.StrengthBonus == 0 && round.CombatDicePenalty == 0 && round.CombatDiceLimit == 5 &&
        round.RerollLimit == 5 && round.ExtraHits == 0 && round.SpentHits == 0 && round.CancelHits == 0 &&
        round.CancelledLeadership.Count == 0 && round.CancelledAbilities.Count == 0 &&
        round.ForfeitedLeadership.Count == 0 && round.SelectedRerolls.Count == 0;

    private static bool BattleDecisionMatches(MatchState state, PendingDecision pending, BattleState battle,
        BattleLossProcedure? loss) => pending.Kind switch
    {
        DecisionKind.Losses => loss is { Voluntary: false, AdditionalAttack: false, SourceId: "combat", Hits: > 0 } &&
                battle.Round >= 1 && pending.RequiredCount == loss.Hits && pending.Owner == loss.Owner &&
                BattleRules.Alive(state, battle.Force(loss.Owner)) &&
                (battle.Stage == BattleStage.DefenderLosses && loss.Owner == battle.Attacker.Side ||
                 battle.Stage == BattleStage.AfterLosses && loss.Owner == battle.Defender.Side) ||
            loss is { Owner: Side.Shadow, Hits: 2, Voluntary: true, AdditionalAttack: false } &&
            ReviewedCombatCards.IsRelentlessAssault(loss.SourceId) && battle.Stage == BattleStage.BeforeRoll &&
            battle.Round >= 1 && battle.CompletedRounds == battle.Round - 1 && battle.CardsRevealed &&
            battle.Mode != BattleMode.Relief &&
            pending.RequiredCount == loss.Hits && pending.Owner == loss.Owner &&
            pending.Presentation == "VoluntaryLosses" && BattleRules.Alive(state, battle.Force(loss.Owner)) ||
            loss is { Owner: Side.Shadow, Hits: 4, Voluntary: true, AdditionalAttack: true } &&
            ReviewedCombatCards.IsOnslaught(loss.SourceId) && battle.Stage == BattleStage.AfterLosses &&
            battle.Round >= 1 && battle.CardsRevealed && battle.Mode != BattleMode.Relief &&
            battle.CompletedRounds == battle.Round - 1 && pending.RequiredCount == loss.Hits &&
            pending.Owner == loss.Owner && pending.Presentation == "VoluntaryLosses" &&
            BattleRules.Alive(state, battle.Force(loss.Owner)),
        DecisionKind.CombatCard => battle.Round >= 1 && !battle.CardsRevealed &&
            !battle.Force(pending.Owner).Round.CardCommitted &&
            (battle.Stage == BattleStage.AttackerCard && pending.Owner == battle.Attacker.Side ||
             battle.Stage == BattleStage.DefenderCard && pending.Owner == battle.Defender.Side),
        DecisionKind.BattleReroll =>
            battle.Stage == BattleStage.AttackerReroll && pending.Owner == battle.Attacker.Side ||
            battle.Stage == BattleStage.DefenderReroll && pending.Owner == battle.Defender.Side,
        DecisionKind.BattleContinue => battle.Stage == BattleStage.Continue && pending.Owner == battle.Attacker.Side,
        DecisionKind.BattleRetreat => battle.Stage == BattleStage.Retreat && pending.Owner == battle.Defender.Side,
        DecisionKind.BattleAdvance => battle.Stage == BattleStage.Advance && pending.Owner == battle.Attacker.Side,
        DecisionKind.StrongholdDefense => battle.Stage == BattleStage.StrongholdDefense && pending.Owner == battle.Defender.Side,
        DecisionKind.SiegeContinuation => battle.Stage == BattleStage.SiegeContinuation && pending.Owner == battle.Attacker.Side,
        _ => false
    };

    private static bool SupportedWindow(MatchState state)
    {
        if (PhaseBoundaryProjection.Supports(state) || LordsBoundaryProjection.Completion(state))
            return true;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None)
            return false;
        var sequence = state.Resolution.ActionSequence;
        if (state.Pending?.Kind == DecisionKind.Action)
            return sequence is null && state.Resolution.PaidDieIds.Count == 0 &&
                state.PoliticalReactions.Count == 0;
        if (state.Pending?.Kind is not (DecisionKind.ActionSequence or DecisionKind.RemoveExcess) ||
            state.Resolution.PaidDieIds.Count != 1 ||
            !state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == state.Pending.Owner && d.Location == DieLocation.Resolving))
            return false;
        if (state.Pending.Kind == DecisionKind.RemoveExcess)
            return sequence is null;
        return sequence is { CompletedSteps: > 0, Remaining: > 0 } &&
            ActionStepRules.Supports(sequence.Kind) && sequence.Owner == state.Pending.Owner &&
            sequence.DieId == state.Resolution.PaidDieIds[0];
    }

    private static HashSet<string> Discharged(IEnumerable<GameEvent> events) => events
        .SelectMany(e => e.Activity?.Facts.Cards ?? [])
        .Where(c => c.Use is ActionCardUse.Event or ActionCardUse.CombatReveal or
            ActionCardUse.Discard or ActionCardUse.TableRemoved)
        .Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
}
