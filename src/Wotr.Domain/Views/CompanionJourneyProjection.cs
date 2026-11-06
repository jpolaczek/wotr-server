using Wotr.Contracts;

namespace Wotr.Domain;

internal static class CompanionJourneyProjection
{
    internal static CompanionJourneyView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        var pending = state.Pending;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || frames.Count != 2 ||
            state.Resolution.PaidDieIds.Count != 1 || pending is null ||
            pending.Owner != Side.FreePeoples)
            return null;

        var effectFrame = frames[0];
        var effect = effectFrame.Effect;
        var card = effect?.SourceId ?? "";
        if (card is not (FreePeoplesCharacterCards03.Gwaihir or FreePeoplesCharacterCards03.Swifter) ||
            effectFrame.EffectId != card || effectFrame.NextStep != 1 || effectFrame.Window != "" ||
            effectFrame.ReturnDecision is not null || effectFrame.Battle is not null ||
            effectFrame.BattleLoss is not null || effectFrame.EventLoss is not null ||
            effectFrame.Corruption is not null || effectFrame.Hunt is not null ||
            effectFrame.Fellowship is not null || effect is not { Owner: Side.FreePeoples,
                Return: EffectReturn.FinishAction, Wait: EffectWait.None, IsEvent: true,
                CompletionWindowOffered: false, PalantirDrawPending: false, AnarionDrawPending: false,
                FellowshipMove: false, NationId: "", RegionId: "", TargetIds.Count: 0,
                AppliedHits: 0, ReactionWindow: "", OfferedReactions.Count: 0,
                Committed.Count: 1 } ||
            effect.SourceId != card || effect.PaidFace is not (DieFace.Character or DieFace.Event) ||
            effect.ArmyOrder is not null || effect.InspectedHand is not null ||
            effect.InspectedSovereign is not null || effect.RingRecovery is not null ||
            effect.MadnessTarget is not null || effect.Committed[0] is not
                { Owner: Side.FreePeoples, SourceId: var committedCard, ChoiceId: "", TargetId: "" } ||
            committedCard != card || !PlayedCardIsPubliclyDiscarded(state, card) ||
            !SinglePaidEventDieMatches(state, effect.PaidFace))
            return null;

        if (!EffectCatalog.All.Programs.TryGetValue(card, out var program) ||
            program.Steps is not [CompanionJourneyStep step])
            return null;

        var fellowshipFrame = frames[1];
        if (!HuntChoiceProjection.PlainFrame(fellowshipFrame) ||
            fellowshipFrame.EffectId != "fellowship.Separation" ||
            fellowshipFrame.Fellowship is not { Return: FellowshipReturn.Parent, SourceId: var source,
                Journey: { } journey } fellowship || source != card ||
            fellowship.FixedCompanions.Count != 0 || fellowship.SeparationOrigin != "" ||
            fellowship.SeparationRange != 0 || fellowship.SeparationBonus != 0 ||
            fellowship.Route.Count != 0 || fellowship.AdditionalHuntRegions.Count != 0 ||
            fellowship.AwaitingHunts || fellowship.RevealEffectsPending || fellowship.FireAndShadowPending ||
            fellowship.ChooseGuideAfterPlacement || !JourneyMatchesCard(journey, step))
            return null;

        var stage = fellowship.Kind switch
        {
            FellowshipProcedureKind.Separation when pending.Kind == DecisionKind.CompanionSeparation &&
                pending.SourceId == card && pending.Window == "" && pending.RequiredCount == 0 &&
                pending.Commands.SequenceEqual([CommandKind.SeparateCompanions, CommandKind.MoveCompanions]) =>
                FellowshipProcedureKind.Separation.ToString(),
            FellowshipProcedureKind.Guide when pending.Kind == DecisionKind.Guide && pending.SourceId == "" &&
                pending.Window == "" && pending.RequiredCount == 0 &&
                pending.Commands.SequenceEqual([CommandKind.SelectGuide]) => FellowshipProcedureKind.Guide.ToString(),
            _ => ""
        };
        if (stage.Length == 0 || state.PoliticalReactions.Count != 0 &&
            stage != nameof(FellowshipProcedureKind.Guide))
            return null;

        return new()
        {
            CardId = card,
            Stage = stage,
            AllowSeparation = journey.AllowSeparation,
            AllowNoMovement = journey.AllowNoMovement,
            MaximumGroups = journey.MaximumGroups,
            FixedLevel = journey.FixedLevel,
            Bonus = journey.Bonus,
            AllowBesiegedDestination = journey.AllowBesiegedDestination
        };
    }

    private static bool PlayedCardIsPubliclyDiscarded(MatchState state, string card)
    {
        var player = state.Player(Side.FreePeoples);
        return player.Discard.Contains(card) && !player.Hand.Contains(card) && !player.Table.Contains(card);
    }

    private static bool SinglePaidEventDieMatches(MatchState state, DieFace face)
    {
        var resolving = state.Dice.Where(die => die.Location == DieLocation.Resolving).ToArray();
        if (resolving.Length != 1)
            return false;
        var die = resolving[0];
        return die.Owner == Side.FreePeoples && die.Face == face &&
            state.Resolution.PaidDieIds.SequenceEqual([die.Id]);
    }

    private static bool JourneyMatchesCard(CompanionJourney journey, CompanionJourneyStep step) =>
        journey.AllowSeparation && !journey.AllowNoMovement && journey.MaximumGroups == 1 &&
        journey.FixedLevel == step.FixedLevel && journey.Bonus == step.Bonus &&
        journey.AllowBesiegedDestination;
}
