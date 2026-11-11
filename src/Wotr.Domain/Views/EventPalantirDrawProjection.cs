using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// Shadow's Palantir of Orthanc after an event paid with an Event die. The event has finished its own
// steps; the owner's trail holds its choices, which no later step reads. The West Has Failed, whose
// draw follows a nested corruption procedure, has its own context.
internal static class EventPalantirDrawProjection
{
    internal static EventPalantirDrawView? For(MatchState state)
    {
        const string palantir = ShadowCharacterCards04.Palantir;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || state.Resolution.Window.Length != 0 ||
            state.PoliticalReactions.Count != 0 || state.Resolution.PaidDieIds is not [var paid] ||
            state.Dice.Count(die => die.Location == DieLocation.Resolving) != 1 ||
            !state.Dice.Any(die => die.Id == paid && die.Owner == Side.Shadow &&
                die.Location == DieLocation.Resolving && die.Face == DieFace.Event) ||
            state.Resolution.Frames is not [var eventFrame, var drawFrame] ||
            eventFrame.Effect is not { } execution || drawFrame.Effect is not { } draw ||
            execution.SourceId == KingsCards02.West ||
            !CardCatalog.All.TryGetValue(execution.SourceId, out var card) || card.Side != Side.Shadow ||
            !EffectCatalog.All.Events.TryGetValue(execution.SourceId, out var definition) ||
            !EffectCatalog.All.Programs.TryGetValue(definition.ProgramId, out var eventProgram) ||
            !EffectCatalog.All.Programs.TryGetValue(ShadowCharacterCards04.PalantirDraw, out var drawProgram) ||
            drawProgram.Steps is not [PalantirDrawStep])
            return null;
        var player = state.Player(Side.Shadow);
        var played = execution.SourceId;
        return eventFrame is { Window: "", ReturnDecision: null, Fellowship: null, Hunt: null, Battle: null,
                BattleLoss: null, EventLoss: null, Corruption: null } &&
            eventFrame.EffectId == definition.ProgramId && eventFrame.NextStep == eventProgram.Steps.Count &&
            execution is { Owner: Side.Shadow, Return: EffectReturn.FinishAction, Wait: EffectWait.None,
                IsEvent: true, PaidFace: DieFace.Event, PalantirDrawPending: false, AnarionDrawPending: false,
                CompletionWindowOffered: false, NationId: "", ArmyOrder: null, AppliedHits: 0,
                ReactionWindow: "", InspectedHand: null, InspectedSovereign: null, RingRecovery: null,
                MadnessTarget: null, FellowshipMove: false } &&
            (execution.RegionId.Length == 0 || BoardRules.Regions.ContainsKey(execution.RegionId)) &&
            execution.TargetIds.Count == 0 && execution.OfferedReactions.Count == 0 &&
            execution.Committed.Count >= 1 && execution.Committed[0] == new EffectSelection(Side.Shadow, played) &&
            execution.Committed.All(selection => selection.Owner == Side.Shadow &&
                DirectEventChoices.BelongsToEvent(selection, played)) &&
            CardCatalog.Enabled(state.Configuration, played) && !player.Hand.Contains(played) &&
            (definition.StaysOnTable ? player.Table : player.Discard).Contains(played) &&
            player.Table.Contains(palantir) &&
            drawFrame is { NextStep: 1, Window: "", ReturnDecision: null, Fellowship: null, Hunt: null, Battle: null,
                BattleLoss: null, EventLoss: null, Corruption: null } &&
            drawFrame.EffectId == ShadowCharacterCards04.PalantirDraw &&
            draw is { Owner: Side.Shadow, Return: EffectReturn.Parent, Wait: EffectWait.Choice, IsEvent: false,
                PalantirDrawPending: false, AnarionDrawPending: false, CompletionWindowOffered: false,
                NationId: "", RegionId: "", AppliedHits: 0 } &&
            draw.SourceId == palantir && draw.Committed.Count == 0 && draw.TargetIds.Count == 0 &&
            state.Pending is { Kind: DecisionKind.EffectChoice, Owner: Side.Shadow, Presentation: "Deck",
                SourceId: palantir, RequiredCount: 1, Window: "", Commands: [CommandKind.ChooseEffect] }
            ? new() { CardId = played, CardCost = execution.PaidFace, RegionId = execution.RegionId } : null;
    }
}
