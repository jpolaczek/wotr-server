using Wotr.Contracts;

namespace Wotr.Domain;

internal static class ActionBoundaryProjection
{
    public static ActionBoundaryView? For(MatchState state, Side viewer)
    {
        // Hidden Kings assignments and suspended Lords procedures require their
        // own reviewed player facts; never serialize ResolutionState.
        if (state.Configuration.Kings || !LordsBoundaryProjection.Supports(state))
            return null;
        var declaration = FellowshipDeclarationProjection.For(state);
        if (state.Resolution.Frames.Count > 0 && declaration is null &&
            (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None))
            return null;
        var fellowshipMove = FellowshipMoveProjection.For(state);
        var cardHunt = CardHuntProjection.For(state);
        if (state.Resolution.Frames.Count == 0 ? !SupportedWindow(state) :
            fellowshipMove is null && cardHunt is null && declaration is null)
            return null;

        // Base/Fate discards never return to a hand/deck. Only a visible discharge
        // establishes an opposing discard; a previously inspected/drawn card does not.
        var revealed = Discharged(state.Events.Where(e => Visibility.CanRead(viewer, e.OnlyFor)));
        var publicDiscards = Discharged(state.Events.Where(e => e.OnlyFor is null));
        return new()
        {
            FellowshipDeclaredThisTurn = state.Fellowship.LastDeclarationTurn == state.Turn,
            MinionEverEntered = state.MinionEverEntered,
            MandatoryHuntAllocation = state.MandatoryHuntAllocation,
            FellowshipMove = fellowshipMove,
            CardHunt = cardHunt,
            FellowshipDeclaration = declaration,
            Lords = LordsBoundaryProjection.For(state),
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
            HuntTiles = state.HuntTiles.GroupBy(t => t.Location).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new HuntTileZoneView
                {
                    Location = g.Key,
                    Tiles = HuntPoolProjection.Faces(g.Select(t => t.Id)),
                    SpecialTileIds = g.Where(t => HuntPool.Definitions[t.Id].Pool != "standard")
                        .Select(t => t.Id).Order(StringComparer.Ordinal).ToList()
                })
                .ToList()
        };
    }

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
