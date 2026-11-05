using Wotr.Contracts;

namespace Wotr.Domain;

internal static class ActionBoundaryProjection
{
    public static ActionBoundaryView? For(MatchState state, Side viewer)
    {
        var declaration = FellowshipDeclarationProjection.For(state);
        var fellowshipMove = FellowshipMoveProjection.For(state);
        var cardHunt = CardHuntProjection.For(state);
        var battle = DirectBattle(state);
        var reviewedContinuation = declaration is not null || fellowshipMove is not null ||
            cardHunt is not null || battle is not null;
        // Compose only explicitly reviewed continuations with module facts;
        // arbitrary expansion procedures are not inferred from their decision kind.
        if (!KingsBoundaryProjection.Supports(state, reviewedContinuation) ||
            !LordsBoundaryProjection.Supports(state, reviewedContinuation))
            return null;
        if (state.Resolution.Frames.Count > 0 && declaration is null &&
            (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None))
            return null;
        if (state.Resolution.Frames.Count == 0 ? !SupportedWindow(state) :
            fellowshipMove is null && cardHunt is null && declaration is null && battle is null &&
            !CorruptionBoundaryProjection.Supports(state))
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
            state.Resolution.ActionSequence is not null || pending is null || frames.Count is < 1 or > 2)
            return null;

        var frame = frames[0];
        var battle = frame.Battle;
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
        if (!BattleDecisionMatches(state, pending, battle, loss) ||
            state.Resolution.PaidDieIds.Count != 1 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] &&
                die.Owner == battle.Attacker.Side && die.Location == DieLocation.Resolving))
            return null;
        return new() { CompletesPaidAction = true };
    }

    private static bool BattleDecisionMatches(MatchState state, PendingDecision pending, BattleState battle,
        BattleLossProcedure? loss) => pending.Kind switch
    {
        DecisionKind.Losses => loss is { Voluntary: false, AdditionalAttack: false, SourceId: "combat", Hits: > 0 } &&
            battle.Round == 1 && pending.RequiredCount == loss.Hits && pending.Owner == loss.Owner &&
            BattleRules.Alive(state, battle.Force(loss.Owner)) &&
            (battle.Stage == BattleStage.DefenderLosses && loss.Owner == battle.Attacker.Side ||
             battle.Stage == BattleStage.AfterLosses && loss.Owner == battle.Defender.Side),
        DecisionKind.CombatCard => battle.Round == 1 && !battle.CardsRevealed &&
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
