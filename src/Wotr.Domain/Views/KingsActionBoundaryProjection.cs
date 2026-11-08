using Wotr.Contracts;

namespace Wotr.Domain;

// Kings sovereign programs that run on top of an action which is otherwise complete:
// Denethor's extra draw after a card drawn with his Ruler die, and the optional corruption
// removal a political change offers before the action that caused it finishes.
internal static class KingsActionBoundaryProjection
{
    internal static AnarionDrawView? AnarionDraw(MatchState state)
    {
        if (!Plain(state) || state.Kings!.PendingCorruptionRemoval.Count != 0 ||
            state.Resolution.PaidDieIds is not [var paid] ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == paid && die.Owner == Side.FreePeoples &&
                die.Location == DieLocation.Resolving && die.Source == SovereignCatalog.RulerSource &&
                die.Face == DieFace.Event) ||
            state.Resolution.Frames is not [{ ReturnDecision: null } frame] ||
            !Completing(frame, KingsAnarion.ProgramId, SovereignCatalog.Denethor) ||
            frame.Effect is not { Owner: Side.FreePeoples, FellowshipMove: false } execution ||
            !EffectCatalog.All.Programs.TryGetValue(KingsAnarion.ProgramId, out var program) ||
            program.Steps is not [AnarionDrawStep])
            return null;

        var pending = state.Pending;
        if (state.AfterDiscard == DiscardContinuation.None && execution.Wait == EffectWait.Choice &&
            execution.Committed.Count == 0 && pending is
            {
                Kind: DecisionKind.EffectChoice, Owner: Side.FreePeoples, Presentation: "Deck",
                SourceId: SovereignCatalog.Denethor, RequiredCount: 1, Window: "", Commands: [CommandKind.ChooseEffect]
            })
            return new();
        return state.AfterDiscard == DiscardContinuation.ResumeEffect && execution.Wait == EffectWait.None &&
            execution.Committed is [{ Owner: Side.FreePeoples, SourceId: SovereignCatalog.Denethor, TargetId: "" } chosen] &&
            (chosen.ChoiceId == "decline" || state.Player(Side.FreePeoples).Decks.ContainsKey(chosen.ChoiceId)) &&
            pending is { Kind: DecisionKind.DiscardCards, Owner: Side.FreePeoples, Window: "",
                Commands: [CommandKind.DiscardCards] }
            ? new() { Discard = true } : null;
    }

    internal static CorruptionRemovalView? CorruptionRemoval(MatchState state)
    {
        if (!Plain(state) || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.Frames is not [var frame] || frame.Effect is not { } execution ||
            !Completing(frame, KingsPolitics.ProgramId, execution.SourceId) ||
            execution.Wait != EffectWait.Choice || execution.Committed.Count != 0 ||
            execution.FellowshipMove && execution.Owner != Side.FreePeoples ||
            SovereignLifecycle.Find(state, execution.SourceId) is not { } sovereign ||
            !KingsPolitics.Eligible(state, sovereign) ||
            !EffectCatalog.All.Programs.TryGetValue(KingsPolitics.ProgramId, out var program) ||
            program.Steps is not [RemoveCorruptionStep] ||
            state.Pending is not { Kind: DecisionKind.EffectChoice, Owner: Side.FreePeoples,
                Presentation: "CorruptionRemoval", RequiredCount: 1, Window: "",
                Commands: [CommandKind.ChooseEffect] } pending ||
            pending.SourceId != execution.SourceId ||
            state.Resolution.PaidDieIds.Count == 0 ||
            state.Resolution.PaidDieIds.Distinct().Count() != state.Resolution.PaidDieIds.Count ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != state.Resolution.PaidDieIds.Count ||
            !state.Resolution.PaidDieIds.All(id => state.Dice.Any(die => die.Id == id &&
                die.Owner == execution.Owner && die.Location == DieLocation.Resolving)))
            return null;

        return new() { Actor = execution.Owner, FellowshipMove = execution.FellowshipMove };
    }

    // A further queued removal is public and opens only after this program returns.
    private static bool Plain(MatchState state) => state.Configuration.Kings && state.Kings is { } kings &&
        state.Phase == GamePhase.ActionResolution && state.Resolution.ActionSequence is null &&
        state.Resolution.Window.Length == 0 && state.PoliticalReactions.Count == 0 &&
        kings.RulerDiscardOrder.Count == 0 && state.Lords?.ChoiceOrder.Count is null or 0 && state.Winner is null;

    // The program closes the action itself: its completion window is already spent and it
    // returns to the end of the paid action, not into a parent effect or decision. A removal
    // queued before that completion keeps a stale return decision the engine never reads.
    private static bool Completing(ResolutionFrame frame, string program, string source) =>
        frame.EffectId == program && frame.NextStep == 1 && frame.Window.Length == 0 &&
        frame.Fellowship is null && frame.Hunt is null &&
        frame.Battle is null && frame.BattleLoss is null && frame.EventLoss is null && frame.Corruption is null &&
        frame.Effect is { Return: EffectReturn.FinishAction, CompletionWindowOffered: true, IsEvent: false,
            PalantirDrawPending: false, AnarionDrawPending: false, NationId: "", RegionId: "",
            ReactionWindow: "", ArmyOrder: null, AppliedHits: 0, InspectedHand: null, InspectedSovereign: null,
            RingRecovery: null, MadnessTarget: null } effect &&
        effect.SourceId == source && effect.PaidFace == default && effect.TargetIds.Count == 0 &&
        effect.OfferedReactions.Count == 0;
}
