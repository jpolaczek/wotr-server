using Wotr.Contracts;

namespace Wotr.Domain;

// Phase and ordinary hand-limit decisions have semantic returns determined by
// their phase/payment, without a suspended program or a saved decision token.
internal static class PhaseBoundaryProjection
{
    internal static bool Supports(MatchState state)
    {
        if (state.Pending is not { } pending || state.Resolution.Frames.Count != 0 ||
            state.Resolution.ActionSequence is not null || state.PoliticalReactions.Count != 0)
            return false;
        if (pending.Kind == DecisionKind.DiscardCards)
            return Discard(state, pending);
        if (state.AfterDiscard != DiscardContinuation.None || state.Resolution.PaidDieIds.Count != 0)
            return false;
        return LordsBoundaryProjection.Phase(state, pending) || KingsBoundaryProjection.Phase(state, pending) ||
            state.Phase == GamePhase.Fellowship && pending.Kind == DecisionKind.FellowshipPhase &&
                pending.Owner == Side.FreePeoples ||
            state.Phase == GamePhase.HuntAllocation && pending.Kind == DecisionKind.AllocateHunt &&
                pending.Owner == Side.Shadow;
    }

    private static bool Discard(MatchState state, PendingDecision pending)
    {
        if (pending.RequiredCount != state.Player(pending.Owner).Hand.Count - 6 || pending.RequiredCount < 1)
            return false;
        if (state.Phase == GamePhase.RecoverDraw)
            return state.AfterDiscard == DiscardContinuation.RecoverDraw && state.Resolution.PaidDieIds.Count == 0 &&
                (pending.Owner == Side.FreePeoples || state.Player(Side.FreePeoples).Hand.Count <= 6);
        return state.Phase == GamePhase.ActionResolution && state.AfterDiscard == DiscardContinuation.FinishAction &&
            state.Actor == pending.Owner && state.Resolution.PaidDieIds.Count == 1 &&
            state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == pending.Owner && d.Location == DieLocation.Resolving);
    }
}
