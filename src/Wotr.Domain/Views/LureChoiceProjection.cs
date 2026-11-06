using Wotr.Contracts;

namespace Wotr.Domain;

internal static class LureChoiceProjection
{
    internal static LureChoiceView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        var pending = state.Pending;
        var card = ShadowCharacterCards03.Lure;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || frames.Count != 1 ||
            pending is not { Kind: DecisionKind.EffectChoice, Owner: Side.FreePeoples,
                RequiredCount: 1, Presentation: "LureCompanion", SourceId: var source } ||
            source != card || pending.Commands is not [CommandKind.ChooseEffect] ||
            pending.PrivateChoices.Count != 2 ||
            !pending.PrivateChoices.Contains(LureChoiceStep.Corruption, StringComparer.Ordinal) ||
            !pending.PrivateChoices.Contains(LureChoiceStep.Eliminate, StringComparer.Ordinal) ||
            state.Resolution.PaidDieIds.Count != 1 ||
            !state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == Side.Shadow && d.Location == DieLocation.Resolving) ||
            state.Fellowship.Guide == CharacterRules.Gollum ||
            !state.Player(Side.Shadow).Discard.Contains(card) ||
            EffectCatalog.All.Programs[card].Steps is not [LureCompanionStep, LureChoiceStep])
            return null;

        var frame = frames[0];
        if (frame.EffectId != card || frame.NextStep != 2 || frame.Window != "" ||
            frame.ReturnDecision is not null || frame.Battle is not null || frame.BattleLoss is not null ||
            frame.EventLoss is not null || frame.Corruption is not null || frame.Hunt is not null ||
            frame.Fellowship is not null || frame.Effect is not { IsEvent: true, Owner: Side.Shadow,
                Return: EffectReturn.FinishAction, Wait: EffectWait.Choice } effect ||
            effect.SourceId != card || effect.PaidFace is not (DieFace.Character or DieFace.Event) ||
            effect.PalantirDrawPending || effect.AnarionDrawPending || effect.CompletionWindowOffered ||
            effect.FellowshipMove || effect.NationId != "" || effect.RegionId != "" ||
            effect.TargetIds.Count != 1 ||
            !state.Fellowship.Companions.Contains(effect.TargetIds[0], StringComparer.Ordinal) ||
            effect.ArmyOrder is not null || effect.AppliedHits != 0 || effect.ReactionWindow != "" ||
            effect.OfferedReactions.Count != 0 || effect.InspectedHand is not null ||
            effect.InspectedSovereign is not null || effect.RingRecovery is not null ||
            effect.MadnessTarget is not null ||
            effect.Committed is not [{ Owner: Side.Shadow, SourceId: var committedSource,
                ChoiceId: "", TargetId: "" }] || committedSource != card)
            return null;

        return new() { CardId = card, CardCost = effect.PaidFace };
    }
}
