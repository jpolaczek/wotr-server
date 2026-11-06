using Wotr.Contracts;

namespace Wotr.Domain;

internal static class LurePlacementProjection
{
    internal static LurePlacementView? For(MatchState state, Side viewer)
    {
        const string card = ShadowCharacterCards03.Lure;
        var frames = state.Resolution.Frames;
        var pending = state.Pending;
        if (viewer != Side.FreePeoples || state.Phase != GamePhase.ActionResolution ||
            state.AfterDiscard != DiscardContinuation.None || state.Resolution.ActionSequence is not null ||
            frames.Count != 2 || state.Resolution.PaidDieIds.Count != 1 || pending?.Owner != Side.FreePeoples ||
            state.Fellowship.Guide == CharacterRules.Gollum || state.Fellowship.MordorStep.HasValue ||
            !state.Player(Side.Shadow).Discard.Contains(card) || state.Player(Side.Shadow).Hand.Contains(card) ||
            state.Player(Side.Shadow).Table.Contains(card) ||
            !EffectCatalog.All.Programs.TryGetValue(card, out var program) ||
            program.Steps is not [LureCompanionStep, LureChoiceStep])
            return null;

        var effectFrame = frames[0];
        if (effectFrame.EffectId != card || effectFrame.NextStep != program.Steps.Count ||
            effectFrame.Window != "" || effectFrame.ReturnDecision is not null ||
            effectFrame.Battle is not null || effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || effectFrame.Hunt is not null || effectFrame.Fellowship is not null ||
            effectFrame.Effect is not { Owner: Side.Shadow, SourceId: card, Return: EffectReturn.FinishAction,
                Wait: EffectWait.None, IsEvent: true, PalantirDrawPending: false, AnarionDrawPending: false,
                CompletionWindowOffered: false, FellowshipMove: false, NationId: "", RegionId: "",
                TargetIds.Count: 1, AppliedHits: 0, ReactionWindow: "", OfferedReactions.Count: 0,
                Committed.Count: 2 } effect || effect.PaidFace is not (DieFace.Character or DieFace.Event) ||
            effect.ArmyOrder is not null || effect.InspectedHand is not null || effect.InspectedSovereign is not null ||
            effect.RingRecovery is not null || effect.MadnessTarget is not null ||
            effect.Committed[0] is not { Owner: Side.Shadow, SourceId: card, ChoiceId: "", TargetId: "" } ||
            effect.Committed[1] is not { Owner: Side.FreePeoples, ChoiceId: LureChoiceStep.Eliminate,
                TargetId: "" } selected || selected.SourceId != effect.TargetIds[0])
            return null;

        var companion = state.Characters.SingleOrDefault(character => character.Id == selected.SourceId);
        if (companion is null || !CharacterRules.IsCompanion(companion) ||
            !CharacterRules.Has(companion, CharacterRules.TakenAlive) || companion.Garrison ||
            state.Fellowship.Companions.Contains(companion.Id, StringComparer.Ordinal) ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == state.Resolution.PaidDieIds[0] && die.Owner == Side.Shadow &&
                die.Location == DieLocation.Resolving && die.Face == effect.PaidFace))
            return null;

        var fellowshipFrame = frames[1];
        if (!HuntChoiceProjection.PlainFrame(fellowshipFrame) ||
            fellowshipFrame.EffectId != "fellowship.Separation" ||
            fellowshipFrame.Fellowship is not { Return: FellowshipReturn.Parent, SourceId: var source,
                FixedCompanions.Count: 1, ChooseGuideAfterPlacement: true, Journey: null } placement ||
            source != companion.Id || placement.FixedCompanions[0] != companion.Id ||
            placement.Route.Count != 0 || placement.AdditionalHuntRegions.Count != 0 ||
            placement.AwaitingHunts || placement.RevealEffectsPending || placement.FireAndShadowPending ||
            placement.SeparationBonus != 0 || placement.SeparationOrigin != state.Fellowship.RegionId ||
            placement.SeparationRange != state.Fellowship.Progress + CharacterRules.MovementLevel(companion))
            return null;

        var stage = pending.Kind switch
        {
            DecisionKind.CompanionSeparation when companion.Location == "separating" &&
                placement.Kind == FellowshipProcedureKind.Separation &&
                pending.SourceId == companion.Id &&
                pending.RequiredCount == 0 && pending.Window == "" &&
                pending.Commands.SequenceEqual([CommandKind.SeparateCompanions]) &&
                state.PoliticalReactions.Count == 0 => FellowshipProcedureKind.Separation.ToString(),
            DecisionKind.Guide when placement.Kind == FellowshipProcedureKind.Guide &&
                BoardRules.Regions.ContainsKey(companion.Location) &&
                pending.SourceId == "" && pending.RequiredCount == 0 && pending.Window == "" &&
                pending.Commands.SequenceEqual([CommandKind.SelectGuide]) &&
                CompanionRules.Guides(state).Count > 1 => FellowshipProcedureKind.Guide.ToString(),
            _ => ""
        };
        return stage.Length == 0 ? null : new()
        {
            CompanionId = companion.Id,
            Stage = stage,
            CardCost = effect.PaidFace
        };
    }
}
