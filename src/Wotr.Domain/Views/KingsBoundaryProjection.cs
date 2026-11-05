using Wotr.Contracts;

namespace Wotr.Domain;

internal static class KingsBoundaryProjection
{
    internal static bool Supports(MatchState state) => !state.Configuration.Kings ? state.Kings is null :
        state.Kings is not null && (state.Resolution.Frames.Count == 0 || CorruptionBoundaryProjection.Supports(state));

    internal static KingsRuleFactsView? For(MatchState state, List<HuntTileFaceView> unassigned) =>
        state.Kings is { } kings ? new()
        {
            PalantirUsedThisTurn = kings.PalantirUsedTurn == state.Turn,
            DismayDeclinedThisTurn = kings.DismayDeclinedTurn == state.Turn,
            RulerTokenUsedThisTurn = kings.RulerTokenUsedThisTurn,
            ElvenRingUsedThisTurn = kings.ElvenRingUsedThisTurn,
            TokenActionSovereign = kings.TokenActionSovereign,
            RulerDiscardOrder = [.. kings.RulerDiscardOrder],
            PendingCorruptionRemoval = [.. kings.PendingCorruptionRemoval],
            UnassignedHuntTiles = unassigned
        } : null;

    internal static bool Phase(MatchState state, PendingDecision pending) =>
        state.Configuration.Kings && state.Phase == GamePhase.ActionRoll && state.Kings is { } kings &&
        (pending.Kind == DecisionKind.RulerDiscard && kings.RulerDiscardOrder.Count > 0 &&
            kings.RulerDiscardOrder[0] == pending.Owner ||
         pending.Kind == DecisionKind.Dismay && pending.Owner == Side.Shadow &&
            kings.RulerDiscardOrder.Count == 0 && ConfigurationRules.DismayEnabled(state.Configuration));
}
