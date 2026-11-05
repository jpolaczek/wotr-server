using Wotr.Contracts;

namespace Wotr.Domain;

internal static class CorruptionBoundaryProjection
{
    internal static bool Supports(MatchState state) => state.Configuration.Kings && state.Kings is not null &&
        state.Phase == GamePhase.ActionResolution && state.AfterDiscard == DiscardContinuation.None &&
        state.Pending is { Kind: DecisionKind.CorruptionTile, Owner: Side.Shadow } &&
        state.Resolution.ActionSequence is null && state.Resolution.PaidDieIds.Count == 0 &&
        state.PoliticalReactions.Count == 0 &&
        state.Resolution.Frames is [{ EffectId: "", NextStep: 0, Window: "", ReturnDecision: null,
            Effect: null, Fellowship: null, Hunt: null, Battle: null, BattleLoss: null, EventLoss: null,
            Corruption: { Return: EffectReturn.FinishAction, DieId.Length: > 0, DrawLimit: >= 1 and <= 3 } attempt }] &&
        state.Pending.SourceId == attempt.SovereignId &&
        attempt.Stage is CorruptionStage.Drawing or CorruptionStage.Choosing or CorruptionStage.SequentialChoice &&
        state.Dice.Count(d => d.Location == DieLocation.Resolving) == 1 &&
        state.Dice.Any(d => d.Id == attempt.DieId && d.Owner == Side.Shadow &&
            d.Face == DieFace.Eye && d.Location == DieLocation.Resolving);
}
