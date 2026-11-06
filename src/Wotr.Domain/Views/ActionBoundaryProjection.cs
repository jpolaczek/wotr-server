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
        var battle = DirectBattle(state) ?? SpecialAttackLoss(state) ?? ReliefEventBattle(state);
        var corruptionEvent = CorruptionBoundaryProjection.Event(state);
        var westPalantirDraw = CorruptionBoundaryProjection.WestPalantirDraw(state);
        var westPalantirDiscard = CorruptionBoundaryProjection.WestPalantirDiscard(state, viewer);
        var fateMonsters = FateMonstersEvent(state);
        var directEventChoice = declaration is null && fellowshipMove is null && cardHunt is null && lureChoice is null &&
            lurePlacement is null && companionJourney is null && fellowshipGuide is null && battle is null &&
            corruptionEvent is null && westPalantirDraw is null && westPalantirDiscard is null && fateMonsters is null
            ? DirectEventChoice(state) : null;
        var reviewedContinuation = declaration is not null || fellowshipMove is not null ||
            cardHunt is not null || lureChoice is not null || lurePlacement is not null || companionJourney is not null ||
            fellowshipGuide is not null || battle is not null || corruptionEvent is not null ||
            westPalantirDraw is not null || westPalantirDiscard is not null || fateMonsters is not null ||
            directEventChoice is not null;
        // Compose only explicitly reviewed continuations with module facts;
        // arbitrary expansion procedures are not inferred from their decision kind.
        if (!KingsBoundaryProjection.Supports(state, reviewedContinuation) ||
            !LordsBoundaryProjection.Supports(state, reviewedContinuation))
            return null;
        if (state.Resolution.Frames.Count > 0 && declaration is null && westPalantirDiscard is null &&
            (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None))
            return null;
        if (state.Resolution.Frames.Count == 0 ? !SupportedWindow(state) :
            fellowshipMove is null && cardHunt is null && lureChoice is null && lurePlacement is null && companionJourney is null &&
            fellowshipGuide is null && declaration is null && battle is null &&
            !CorruptionBoundaryProjection.Supports(state) && fateMonsters is null && directEventChoice is null)
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
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Resolution.Window.Length != 0 ||
            state.PoliticalReactions.Count != 0 || pending is null || frames.Count == 0 ||
            state.Resolution.PaidDieIds.Count != 1 || state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1)
            return null;

        var cardId = pending.SourceId;
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
                    AnarionDrawPending: false, ArmyOrder: null } || owner != card.Side || source != cardId ||
            execution.PaidFace != DieFace.Event && execution.PaidFace != card.EventType ||
            execution.ReactionWindow.Length != 0 || execution.OfferedReactions.Count != 0 ||
            execution.Committed.Count == 0 || execution.Committed[0] != new EffectSelection(card.Side, cardId) ||
            execution.AppliedHits != 0 || execution.InspectedSovereign is not null ||
            execution.InspectedHand is not null || execution.RingRecovery is not null ||
            execution.MadnessTarget is not null || execution.FellowshipMove)
            return null;

        // Losses fall to the target's owner, usually the event's opponent. Every other
        // reviewed decision belongs to the event's owner.
        var reviewed = pending.Kind switch
        {
            DecisionKind.Losses => IsReviewedEventLoss(state, pending, frame, execution, program),
            _ when pending.Owner != card.Side => false,
            DecisionKind.EffectChoice => IsReviewedEventChoice(state, pending, frame, execution, program),
            DecisionKind.Reaction => IsReviewedEventReaction(state, pending, frame, execution, program),
            DecisionKind.CharacterEvent => IsReviewedNazgulTravel(state, pending, frame, execution, program),
            DecisionKind.CompanionSeparation => IsReviewedEventSeparation(state, pending, frame, execution, program),
            _ => false
        };
        if (!reviewed)
            return null;

        var paidDie = state.Dice.SingleOrDefault(die => die.Id == state.Resolution.PaidDieIds[0]);
        if (paidDie is null || paidDie.Owner != card.Side || paidDie.Location != DieLocation.Resolving ||
            !CanPayObservedFace(paidDie.Face, execution.PaidFace))
            return null;

        return new() { CardId = cardId, CardCost = execution.PaidFace };
    }

    // The public loss view names the target, hits and rolls. The program's remaining
    // steps do not read the rolled target, so nothing else needs restoring.
    private static bool IsReviewedEventLoss(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending.Commands is [CommandKind.TakeEventLosses] && state.Resolution.Frames is [_, var lossFrame] &&
        lossFrame is { EffectId: "", NextStep: 0, Window: "", ReturnDecision: null, Effect: null, Fellowship: null,
            Hunt: null, Battle: null, BattleLoss: null, Corruption: null,
            EventLoss: { Cause: CasualtyCause.Event } loss } &&
        loss.SourceId == execution.SourceId && loss.Target.Owner == pending.Owner && loss.Hits > 0 &&
        pending.RequiredCount == loss.Hits &&
        execution is { Wait: EffectWait.None, CompletionWindowOffered: false } &&
        DirectEventChoices.LossStep(program) is > 0 and var step && frame.NextStep == step &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution);

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

    private static bool IsReviewedEventChoice(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program)
    {
        var cardId = execution.SourceId;
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

        return cardId == KingsCards02.Service && frame.NextStep == 2
            ? ServiceSovereignSelected(state, execution)
            : choice is RecruitEffectStep { Region: "" }
                ? HasPublicRecruitRegion(execution)
                : HasNoEventSelection(execution);
    }

    // The route options come from the public Nazgul positions. The card's only
    // earlier state is the owner's order to fly, which stays in their own trail.
    private static bool IsReviewedNazgulTravel(MatchState state, PendingDecision pending, ResolutionFrame frame,
        EffectExecution execution, EffectDefinition program) =>
        pending is { Owner: Wotr.Contracts.Side.Shadow, Commands: [CommandKind.MoveShadowCharacters] } &&
        state.Resolution.Frames.Count == 1 && execution is { Wait: EffectWait.NazgulTravel, CompletionWindowOffered: false } &&
        frame.NextStep == 1 && DirectEventChoices.TravelsFirst(program) &&
        execution.RegionId.Length == 0 && execution.TargetIds.Count == 0 && execution.NationId.Length == 0 &&
        execution.Committed.Skip(1).All(selection => DirectEventChoices.IsSpearOfTerror(selection) ||
            DirectEventChoices.IsCaptainFlight(selection)) && execution.Committed.Count <= 3 &&
        execution.PalantirDrawPending == DirectEventChoices.PalantirDrawExpected(state, execution);

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
        return pending.Kind == DecisionKind.Losses &&
            battle is { Return: EffectReturn.FinishAction, Stage: BattleStage.BeforeRoll, Round: 1,
                CardsRevealed: true, Mode: not BattleMode.Relief, CompletedRounds: 0,
                EndedByEffect: false, NextWork: > 0 } &&
            effectFrame.EffectId == "combat.relentless-assault" && effectFrame.NextStep == 1 &&
            effectFrame.Window.Length == 0 && effectFrame.ReturnDecision is null &&
            effectFrame.Fellowship is null && effectFrame.Hunt is null && effectFrame.Battle is null &&
            effectFrame.BattleLoss is null && effectFrame.EventLoss is null && effectFrame.Corruption is null &&
            execution is { Owner: Side.Shadow, SourceId: ShadowStrategyCards01.Dawn,
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
                AdditionalAttack: false, SourceId: ShadowStrategyCards01.Dawn } &&
            pending.Owner == loss.Owner && pending.RequiredCount == loss.Hits && pending.SourceId == loss.SourceId &&
            pending.Presentation == "VoluntaryLosses" && pending.Commands is [CommandKind.TakeBattleLosses] &&
            loss.SourceId == execution.SourceId && battle.SpecialAttacks.Count == 0 &&
            battle.Work[battle.NextWork - 1] == new CombatWork(Side.Shadow, ShadowStrategyCards01.Dawn,
                "combat.relentless-assault", 3) &&
            battle.Force(Side.Shadow).Round is { CardId: ShadowStrategyCards01.Dawn, CardCancelled: false,
                VoluntaryLossHits: null } && battle.Attacker.Round.Dice.Count == 0 && battle.Defender.Round.Dice.Count == 0 &&
            CardCatalog.Enabled(state, ShadowStrategyCards01.Dawn) &&
            EffectCatalog.All.Combats.TryGetValue(ShadowStrategyCards01.Dawn, out var combat) &&
            combat.Effects is [{ Initiative: 3, ProgramId: "combat.relentless-assault" }] &&
            combat.Condition.CanPlay(state, battle, Side.Shadow) &&
            EffectCatalog.All.Programs.TryGetValue("combat.relentless-assault", out var program) &&
            program.Steps is [VoluntaryLossesStep { Maximum: 2, AdditionalAttack: false },
                SacrificeResultStep { AdditionalAttack: false }] &&
            BattleRules.Alive(state, battle.Force(Side.Shadow));
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
            battle.CompletedRounds == battle.Round - 1 && effectFrame.EffectId == "combat.onslaught" &&
            effectFrame.NextStep == 1 && effectFrame.Window.Length == 0 && effectFrame.ReturnDecision is null &&
            effectFrame.Fellowship is null && effectFrame.Hunt is null && effectFrame.Battle is null &&
            effectFrame.BattleLoss is null && effectFrame.EventLoss is null && effectFrame.Corruption is null &&
            execution is { Owner: Side.Shadow, SourceId: ShadowStrategyCards01.UrukHai,
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
            lossFrame.Corruption is null && loss is { Owner: Side.Shadow, Hits: 4, Voluntary: true,
                AdditionalAttack: true, SourceId: ShadowStrategyCards01.UrukHai } &&
            pending.Owner == loss.Owner && pending.RequiredCount == loss.Hits && pending.SourceId == loss.SourceId &&
            pending.Presentation == "VoluntaryLosses" && pending.Commands is [CommandKind.TakeBattleLosses] &&
            loss.SourceId == execution.SourceId && battle.NextWork > 0 && battle.NextWork <= battle.Work.Count &&
            battle.Work[battle.NextWork - 1] == new CombatWork(Side.Shadow, ShadowStrategyCards01.UrukHai,
                "combat.onslaught", 7) &&
            battle.Force(Side.Shadow).Round is { CardId: ShadowStrategyCards01.UrukHai, CardCancelled: false,
                VoluntaryLossHits: null, SpentHits: 0 } && battle.Attacker.Round.SpentHits == 0 &&
            battle.Defender.Round.SpentHits == 0 && CardCatalog.Enabled(state, ShadowStrategyCards01.UrukHai) &&
            EffectCatalog.All.Combats.TryGetValue(ShadowStrategyCards01.UrukHai, out var combat) &&
            combat.Effects is [{ Initiative: 7, ProgramId: "combat.onslaught" }] &&
            combat.Condition.CanPlay(state, battle, Side.Shadow) &&
            EffectCatalog.All.Programs.TryGetValue("combat.onslaught", out var program) &&
            program.Steps is [VoluntaryLossesStep { Maximum: 4, AdditionalAttack: true },
                SacrificeResultStep { AdditionalAttack: true }] &&
            BattleRules.Alive(state, battle.Attacker) && BattleRules.Alive(state, battle.Defender);
    }

    private static BattleContextView? SpecialAttackLoss(MatchState state)
    {
        return FirstStepSpecialAttackLoss(state) ?? PostCombatSpecialAttackLoss(state);
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
            loss.SourceId != execution.SourceId || battle.SpecialAttacks.Count != 1 ||
            execution.Owner != battle.SpecialAttacks[0].Owner || loss.Owner != TurnFlow.Other(execution.Owner) ||
            battle.SpecialAttacks[0].SourceId != loss.SourceId ||
            battle.SpecialAttacks[0].Dice.Count == 0 ||
            battle.SpecialAttacks[0].Dice.Count(die => die.Hit) != loss.Hits ||
            battle.Attacker.Round.Dice.Count != 0 || battle.Defender.Round.Dice.Count != 0 ||
            !FreshSpecialAttackRound(battle.Attacker.Round) || !FreshSpecialAttackRound(battle.Defender.Round) ||
            battle.NextWork > battle.Work.Count ||
            battle.Work[battle.NextWork - 1] != new CombatWork(execution.Owner, execution.SourceId,
                effectFrame.EffectId, 2) || battle.Work.Take(battle.NextWork - 1).Any(work =>
                !battle.Force(work.Owner).Round.CardCancelled) || battle.Force(execution.Owner).Round.CardCancelled ||
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
            state.Configuration.Kings ||
            !BattleRules.Alive(state, battle.Force(loss.Owner)) ||
            state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving))
            return null;

        var attack = battle.SpecialAttacks[0];
        var sourceForce = battle.Force(attack.Owner);
        var diceCount = FirstStepSpecialAttackDice(state, sourceForce, program.Steps[0]);
        var hitTarget = FirstStepSpecialAttackHitTarget(state, battle, sourceForce, program.Steps[0]);
        if (attack.Dice.Count != diceCount || attack.Dice.Any(die => die.Result is < 1 or > 6 ||
            die.Modifier != 0 || die.HitTarget != hitTarget || die.Hit != BattleRoll.Hits(die.Result, 0, hitTarget) ||
            die.ConvertedHit || die.Reroll.HasValue || die.RerollModifier != 0 || die.RerollHit || die.CannotReroll))
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
            !BattleRules.Alive(state, battle.Force(loss.Owner)) || state.Configuration.Kings ||
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
            loss is { Owner: Side.Shadow, Hits: 2, Voluntary: true, AdditionalAttack: false,
                SourceId: ShadowStrategyCards01.Dawn } && battle.Stage == BattleStage.BeforeRoll &&
            battle.Round == 1 && battle.CardsRevealed && battle.Mode != BattleMode.Relief &&
            pending.RequiredCount == loss.Hits && pending.Owner == loss.Owner &&
            pending.Presentation == "VoluntaryLosses" && BattleRules.Alive(state, battle.Force(loss.Owner)) ||
            loss is { Owner: Side.Shadow, Hits: 4, Voluntary: true, AdditionalAttack: true,
                SourceId: ShadowStrategyCards01.UrukHai } && battle.Stage == BattleStage.AfterLosses &&
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
