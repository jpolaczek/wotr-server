using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

internal static class CorruptionBoundaryProjection
{
    internal static bool Supports(MatchState state) => OrdinaryEyeAttempt(state) ||
        WestEventAttempt(state, out _) || WestPalantirDrawContext(state, out _) ||
        WestPalantirDiscardContext(state, out _, out _);

    internal static CorruptionEventView? Event(MatchState state) => WestEventAttempt(state, out var effect)
        ? new()
        {
            CardId = effect.SourceId,
            CardCost = effect.PaidFace,
            PalantirDrawPending = effect.PalantirDrawPending
        }
        : null;

    internal static WestPalantirDrawView? WestPalantirDraw(MatchState state) =>
        WestPalantirDrawContext(state, out var effect)
            ? new() { CardId = effect.SourceId, CardCost = effect.PaidFace }
            : null;

    internal static WestPalantirDiscardView? WestPalantirDiscard(MatchState state, Side viewer) =>
        viewer == Side.Shadow && WestPalantirDiscardContext(state, out var effect, out var drawnCardId)
            ? new() { CardId = effect.SourceId, CardCost = effect.PaidFace, DrawnCardId = drawnCardId }
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
        state.Dice.Any(d => d.Id == attempt.DieId && DiceRules.HuntsForShadow(d) &&
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
        var region = SovereignLifecycle.Region(state, sovereign);
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
                attempt.SovereignId, SovereignLifecycle.Region(state, sovereign)) ||
            root.PalantirDrawPending && root.PaidFace != DieFace.Event)
            return false;

        var paidDieId = state.Resolution.PaidDieIds[0];
        if (!PaidDieMatches(state, paidDieId, root.PaidFace))
            return false;

        execution = root;
        return true;
    }

    private static bool WestPalantirDrawContext(MatchState state, out EffectExecution execution)
    {
        execution = null!;
        if (!state.Configuration.Kings || state.Kings is null ||
            state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Resolution.Window.Length != 0 ||
            state.Pending is not { Kind: DecisionKind.EffectChoice, Owner: Side.Shadow,
                RequiredCount: 1, Presentation: "Deck", SourceId: ShadowCharacterCards04.Palantir } pending ||
            pending.Commands is not [CommandKind.ChooseEffect] ||
            state.Resolution.PaidDieIds.Count != 1 || state.PoliticalReactions.Count != 0 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards04.Palantir) ||
            !state.Player(Side.Shadow).Discard.Contains(KingsCards02.West) ||
            state.Player(Side.Shadow).Hand.Contains(KingsCards02.West) ||
            state.Player(Side.Shadow).Table.Contains(KingsCards02.West) ||
            !EffectCatalog.All.Programs.TryGetValue(KingsCards02.West, out var westProgram) ||
            westProgram.Steps is not [WestCorruptionStep] ||
            !EffectCatalog.All.Programs.TryGetValue(ShadowCharacterCards04.PalantirDraw, out var palantirProgram) ||
            palantirProgram.Steps is not [PalantirDrawStep] ||
            state.Resolution.Frames is not [var parent, var draw] ||
            !HuntEffectProjection.PlainShadowProgram(draw, ShadowCharacterCards04.PalantirDraw,
                ShadowCharacterCards04.Palantir, 1, "", EffectWait.Choice) ||
            draw.Effect!.Committed.Count != 0 || draw.Effect.OfferedReactions.Count != 0 ||
            !WestPalantirParent(state, parent, state.Resolution.PaidDieIds[0], out var root) ||
            pending.PrivateChoices.Count == 0 ||
            pending.PrivateChoices.Count != state.Player(Side.Shadow).Decks.Count(deck => deck.Value.Count > 0) ||
            !pending.PrivateChoices.ToHashSet(StringComparer.Ordinal).SetEquals(
                state.Player(Side.Shadow).Decks.Where(deck => deck.Value.Count > 0).Select(deck => deck.Key)))
            return false;

        execution = root;
        return true;
    }

    private static bool WestPalantirDiscardContext(MatchState state, out EffectExecution execution,
        out string drawnCardId)
    {
        execution = null!;
        drawnCardId = "";
        var hand = state.Player(Side.Shadow).Hand;
        if (!state.Configuration.Kings || state.Kings is null ||
            state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.ResumeEffect ||
            state.Pending is not { Kind: DecisionKind.DiscardCards, Owner: Side.Shadow } pending ||
            pending.Commands is not [CommandKind.DiscardCards] || pending.RequiredCount != hand.Count - 6 ||
            hand.Count != 7 || pending.RequiredCount != 1 || pending.PrivateChoices.Count != hand.Count ||
            !pending.PrivateChoices.ToHashSet(StringComparer.Ordinal).SetEquals(hand) ||
            state.Resolution.ActionSequence is not null || state.Resolution.Window.Length != 0 ||
            state.Resolution.PaidDieIds.Count != 1 || state.PoliticalReactions.Count != 0 ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards04.Palantir) ||
            !state.Player(Side.Shadow).Discard.Contains(KingsCards02.West) ||
            state.Player(Side.Shadow).Hand.Contains(KingsCards02.West) ||
            state.Player(Side.Shadow).Table.Contains(KingsCards02.West) ||
            state.Kings.RulerDiscardOrder.Count != 0 || state.Lords?.ChoiceOrder.Count > 0 ||
            !EffectCatalog.All.Programs.TryGetValue(KingsCards02.West, out var westProgram) ||
            westProgram.Steps is not [WestCorruptionStep] ||
            !EffectCatalog.All.Programs.TryGetValue(ShadowCharacterCards04.PalantirDraw, out var palantirProgram) ||
            palantirProgram.Steps is not [PalantirDrawStep] ||
            state.Resolution.Frames is not [var parent, var draw] ||
            !HuntEffectProjection.PlainShadowProgram(draw, ShadowCharacterCards04.PalantirDraw,
                ShadowCharacterCards04.Palantir, 1, "") ||
            draw.Effect!.Committed.Count != 0 || draw.Effect.OfferedReactions.Count != 0 ||
            !WestPalantirDiscardParent(state, parent, state.Resolution.PaidDieIds[0], out var root,
                out var selectedDeck) ||
            !TryCurrentPalantirDraw(state, hand, selectedDeck, out drawnCardId))
        {
            drawnCardId = "";
            return false;
        }

        execution = root;
        return true;
    }

    private static bool WestPalantirDiscardParent(MatchState state, ResolutionFrame frame, string paidDieId,
        out EffectExecution execution, out string selectedDeck)
    {
        execution = null!;
        selectedDeck = "";
        if (frame.EffectId != KingsCards02.West || frame.NextStep != 1 || frame.Window.Length != 0 ||
            frame.ReturnDecision is not null || frame.Fellowship is not null || frame.Hunt is not null ||
            frame.Battle is not null || frame.BattleLoss is not null || frame.EventLoss is not null ||
            frame.Corruption is not null || frame.Effect is not { Owner: Side.Shadow,
                SourceId: KingsCards02.West, PaidFace: DieFace.Event, Return: EffectReturn.FinishAction,
                Wait: EffectWait.None, IsEvent: true, PalantirDrawPending: false, AnarionDrawPending: false,
                NationId: "", RegionId: "", ArmyOrder: null, ReactionWindow: "",
                CompletionWindowOffered: false, FellowshipMove: false, AppliedHits: 0,
                InspectedSovereign: null, InspectedHand: null, RingRecovery: null, MadnessTarget: null } root ||
            root.TargetIds.Count != 0 || root.OfferedReactions.Count != 0 || root.Committed.Count != 3 ||
            root.Committed[0] != new EffectSelection(Side.Shadow, KingsCards02.West) ||
            root.Committed[1].Owner != Side.Shadow || root.Committed[1].SourceId != KingsCards02.West ||
            !SovereignCatalog.All.ContainsKey(root.Committed[1].ChoiceId) ||
            !BoardRules.Regions.ContainsKey(root.Committed[1].TargetId) ||
            root.Committed[2].Owner != Side.Shadow ||
            root.Committed[2].SourceId != ShadowCharacterCards04.Palantir ||
            root.Committed[2].TargetId.Length != 0 ||
            !state.Player(Side.Shadow).Decks.ContainsKey(root.Committed[2].ChoiceId) ||
            !PaidDieMatches(state, paidDieId, DieFace.Event))
            return false;

        selectedDeck = root.Committed[2].ChoiceId;
        execution = root;
        return true;
    }

    private static bool TryCurrentPalantirDraw(MatchState state, IReadOnlyCollection<string> hand,
        string selectedDeck, out string drawnCardId)
    {
        drawnCardId = "";
        if (state.Events.Count < 3)
            return false;

        var choice = state.Events[^3];
        var privateDraw = state.Events[^2];
        var publicDraw = state.Events[^1];
        if (choice.Revision != state.Revision || privateDraw.Revision != state.Revision ||
            publicDraw.Revision != state.Revision || choice.Kind != "OwnEffectChoice" ||
            choice.OnlyFor != Side.Shadow || choice.Activity is not { Actor: Side.Shadow } choiceActivity ||
            choiceActivity.Facts.SourceId != ShadowCharacterCards04.Palantir ||
            choiceActivity.Facts.Targets is not [var chosenDeck] || chosenDeck != selectedDeck ||
            privateDraw.Kind != "CardDrawn" || privateDraw.OnlyFor != Side.Shadow ||
            publicDraw.Kind != "CardsDrawn" || publicDraw.OnlyFor is not null ||
            privateDraw.Activity?.Facts.Cards is not [{ Owner: Side.Shadow, Use: ActionCardUse.Draw,
                Id.Length: > 0 } card] ||
            publicDraw.Activity?.Facts.Cards is not [{ Owner: Side.Shadow, Use: ActionCardUse.Draw,
                Id: "", Deck: var deck, Count: 1 }] || deck != selectedDeck ||
            card.Deck != selectedDeck || !hand.Contains(card.Id) ||
            !CardCatalog.All.TryGetValue(card.Id, out var definition) || definition.Deck != selectedDeck)
            return false;

        drawnCardId = card.Id;
        return true;
    }

    private static bool WestPalantirParent(MatchState state, ResolutionFrame frame, string paidDieId,
        out EffectExecution execution)
    {
        execution = null!;
        if (frame.EffectId != KingsCards02.West || frame.NextStep != 1 || frame.Window.Length != 0 ||
            frame.ReturnDecision is not null || frame.Fellowship is not null || frame.Hunt is not null ||
            frame.Battle is not null || frame.BattleLoss is not null || frame.EventLoss is not null ||
            frame.Corruption is not null || frame.Effect is not { Owner: Side.Shadow,
                SourceId: KingsCards02.West, PaidFace: DieFace.Event, Return: EffectReturn.FinishAction,
                Wait: EffectWait.None, IsEvent: true, PalantirDrawPending: false, AnarionDrawPending: false,
                NationId: "", RegionId: "", ArmyOrder: null, ReactionWindow: "",
                CompletionWindowOffered: false, FellowshipMove: false, AppliedHits: 0,
                InspectedSovereign: null, InspectedHand: null, RingRecovery: null, MadnessTarget: null } root ||
            root.TargetIds.Count != 0 || root.OfferedReactions.Count != 0 || root.Committed.Count != 2 ||
            root.Committed[0] != new EffectSelection(Side.Shadow, KingsCards02.West) ||
            root.Committed[1].Owner != Side.Shadow || root.Committed[1].SourceId != KingsCards02.West ||
            !SovereignCatalog.All.ContainsKey(root.Committed[1].ChoiceId) ||
            !BoardRules.Regions.ContainsKey(root.Committed[1].TargetId) ||
            !PaidDieMatches(state, paidDieId, DieFace.Event))
            return false;

        execution = root;
        return true;
    }

    // Barad-dur's Palantir lets a Muster die pay an Event card without changing
    // the card's effective paid face.
    private static bool PaidDieMatches(MatchState state, string dieId, DieFace face) =>
        state.Dice.Any(die => die.Id == dieId && die.Owner == Side.Shadow &&
            die.Location == DieLocation.Resolving && die.Face == face) ||
        face == DieFace.Event && state.Kings?.PalantirUsedTurn == state.Turn &&
        state.Player(Side.Shadow).Table.Contains(KingsCards03.Palantir) &&
        state.Dice.Any(die => die.Id == dieId && die.Owner == Side.Shadow &&
            die.Location == DieLocation.Resolving && die.Face == DieFace.Muster);
}
