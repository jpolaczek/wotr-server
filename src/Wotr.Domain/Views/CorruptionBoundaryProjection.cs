using Wotr.Contracts;

namespace Wotr.Domain;

internal static class CorruptionBoundaryProjection
{
    internal static bool Supports(MatchState state) => OrdinaryEyeAttempt(state) || WestEventAttempt(state, out _);

    internal static CorruptionEventView? Event(MatchState state) => WestEventAttempt(state, out var effect)
        ? new()
        {
            CardId = effect.SourceId,
            CardCost = effect.PaidFace,
            PalantirDrawPending = effect.PalantirDrawPending
        }
        : null;

    private static bool OrdinaryEyeAttempt(MatchState state) => state.Configuration.Kings && state.Kings is not null &&
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

    private static bool WestEventAttempt(MatchState state, out EffectExecution effect)
    {
        effect = null!;
        if (!WestEventWindow(state) || !TryWestFrames(state, out var parent, out var attempt) ||
            !WestTarget(state, attempt, out var sovereign) ||
            !WestParent(state, parent.Effect, attempt, sovereign, out var execution))
            return false;

        effect = execution;
        return true;
    }

    private static bool WestEventWindow(MatchState state) => state.Configuration.Kings && state.Kings is not null &&
        state.Phase == GamePhase.ActionResolution && state.AfterDiscard == DiscardContinuation.None &&
        state.Pending is { Kind: DecisionKind.CorruptionTile, Owner: Side.Shadow } &&
        state.Resolution.ActionSequence is null && state.Resolution.PaidDieIds.Count == 1 &&
        state.Resolution.Window.Length == 0 && state.PoliticalReactions.Count == 0 &&
        state.Dice.Count(die => die.Location == DieLocation.Resolving) == 1;

    private static bool TryWestFrames(MatchState state, out ResolutionFrame parent, out CorruptionAttempt attempt)
    {
        parent = null!;
        attempt = null!;
        var frames = state.Resolution.Frames;
        if (frames.Count != 2)
            return false;

        var candidateParent = frames[0];
        var child = frames[1];
        if (candidateParent.EffectId != KingsCards02.West || candidateParent.NextStep != 1 ||
            candidateParent.Window.Length != 0 || candidateParent.ReturnDecision is not null ||
            candidateParent.Fellowship is not null || candidateParent.Hunt is not null ||
            candidateParent.Battle is not null || candidateParent.BattleLoss is not null ||
            candidateParent.EventLoss is not null || candidateParent.Corruption is not null ||
            child.EffectId.Length != 0 || child.NextStep != 0 || child.Window.Length != 0 ||
            child.ReturnDecision is not null || child.Effect is not null || child.Fellowship is not null ||
            child.Hunt is not null || child.Battle is not null || child.BattleLoss is not null ||
            child.EventLoss is not null || child.Corruption is not { Return: EffectReturn.Parent, DieId: "",
                DrawLimit: >= 1 and <= 3 } current)
            return false;

        parent = candidateParent;
        attempt = current;
        return true;
    }

    private static bool WestTarget(MatchState state, CorruptionAttempt attempt, out SovereignState sovereign)
    {
        sovereign = SovereignLifecycle.Find(state, attempt.SovereignId)!;
        var region = SovereignLifecycle.Region(state, attempt.SovereignId);
        return state.Pending!.SourceId == attempt.SovereignId &&
            attempt.Stage is CorruptionStage.Drawing or CorruptionStage.Choosing or CorruptionStage.SequentialChoice &&
            attempt.DrawLimit == Math.Min(3, CorruptionRules.Eyes(state).Count()) &&
            sovereign is { Status: SovereignStatus.Initial } && sovereign.LastAttemptTurn == state.Turn &&
            BoardRules.Regions.ContainsKey(region) &&
            state.Player(Side.Shadow).Discard.Contains(KingsCards02.West) &&
            !state.Player(Side.Shadow).Hand.Contains(KingsCards02.West) &&
            !state.Player(Side.Shadow).Table.Contains(KingsCards02.West) &&
            EffectCatalog.All.Programs.TryGetValue(KingsCards02.West, out var program) &&
            program.Steps is [WestCorruptionStep];
    }

    private static bool WestParent(MatchState state, EffectExecution? candidate, CorruptionAttempt attempt,
        SovereignState sovereign, out EffectExecution execution)
    {
        execution = null!;
        if (candidate is not { Owner: Side.Shadow, SourceId: KingsCards02.West,
                Return: EffectReturn.FinishAction, Wait: EffectWait.None, IsEvent: true,
                NationId: "", RegionId: "", ArmyOrder: null, ReactionWindow: "",
                CompletionWindowOffered: false, AnarionDrawPending: false, FellowshipMove: false,
                AppliedHits: 0, InspectedSovereign: null, InspectedHand: null, RingRecovery: null,
                MadnessTarget: null } root ||
            root.PaidFace is not (DieFace.Character or DieFace.Event) ||
            root.TargetIds.Count != 0 || root.OfferedReactions.Count != 0 ||
            root.Committed.Count != 2 ||
            root.Committed[0] != new EffectSelection(Side.Shadow, KingsCards02.West) ||
            root.Committed[1] != new EffectSelection(Side.Shadow, KingsCards02.West,
                attempt.SovereignId, SovereignLifecycle.Region(state, sovereign.Id)) ||
            root.PalantirDrawPending && root.PaidFace != DieFace.Event)
            return false;

        var paidDieId = state.Resolution.PaidDieIds[0];
        if (!state.Dice.Any(die => die.Id == paidDieId && die.Owner == Side.Shadow &&
                die.Location == DieLocation.Resolving && die.Face == root.PaidFace))
            return false;

        execution = root;
        return true;
    }
}
