using Wotr.Contracts;

namespace Wotr.Domain;

internal static class ActionBoundaryProjection
{
    public static ActionBoundaryView? For(MatchState state, Side viewer)
    {
        // Expansion setup, hidden tile assignments and suspended procedures need
        // their own reviewed player contracts; never serialize ResolutionState.
        if (state.Configuration.Lords || state.Configuration.Kings ||
            state.Phase != GamePhase.ActionResolution || state.Pending?.Kind != DecisionKind.Action ||
            state.Resolution.Frames.Count != 0 || state.Resolution.ActionSequence is not null ||
            state.Resolution.PaidDieIds.Count != 0 || state.PoliticalReactions.Count != 0 ||
            state.AfterDiscard != DiscardContinuation.None)
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
            Companions = [.. state.Fellowship.Companions],
            Characters = state.Characters.Select(c => new ActionCharacterView
            {
                Character = Projection.Character(c),
                ConvertedArmyThisTurn = c.LastArmyConversionTurn == state.Turn
            }).ToList(),
            Players = state.Players.Select(p => new ActionPlayerView
            {
                Side = p.Side,
                ExtraDiceNextRecovery = p.ExtraDiceNextRecovery,
                RingActionPending = p.RingActionPending,
                KnownDiscards = p.Discard.Where(id => p.Side == viewer || revealed.Contains(id))
                    .Order(StringComparer.Ordinal).ToList(),
                PublicDiscards = p.Discard.Where(publicDiscards.Contains)
                    .Order(StringComparer.Ordinal).ToList()
            }).ToList(),
            HuntTiles = state.HuntTiles.GroupBy(t => t.Location).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new HuntTileZoneView { Location = g.Key, Tiles = HuntPoolProjection.Faces(g.Select(t => t.Id)) })
                .ToList()
        };
    }

    private static HashSet<string> Discharged(IEnumerable<GameEvent> events) => events
        .SelectMany(e => e.Activity?.Facts.Cards ?? [])
        .Where(c => c.Use is ActionCardUse.Event or ActionCardUse.CombatReveal or
            ActionCardUse.Discard or ActionCardUse.TableRemoved)
        .Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
}
