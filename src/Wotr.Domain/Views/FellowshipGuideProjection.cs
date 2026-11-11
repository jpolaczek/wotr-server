using Wotr.Contracts;

namespace Wotr.Domain;

internal static class FellowshipGuideProjection
{
    internal static FellowshipGuideView? For(MatchState state)
    {
        var pending = state.Pending;
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.ActionResolution || state.AfterDiscard != DiscardContinuation.None ||
            state.Resolution.ActionSequence is not null || pending is not
                { Kind: DecisionKind.Guide, Owner: Side.FreePeoples, RequiredCount: 0, SourceId: "", Window: "" } ||
            pending.Commands.Count != 1 || pending.Commands[0] != CommandKind.SelectGuide ||
            pending.PrivateChoices.Count < 2 || !pending.PrivateChoices.SequenceEqual(CompanionRules.Guides(state)) ||
            state.Resolution.PaidDieIds.Count != 1 || frames.Count != 1 || !GuideFrame(frames[0]))
            return null;
        var dice = state.Dice.Where(d => d.Id == state.Resolution.PaidDieIds[0]).ToArray();
        return dice is [{ Owner: Side.FreePeoples, Face: DieFace.Character, Location: DieLocation.Resolving }]
            ? new FellowshipGuideView { CompletesPaidAction = true }
            : null;
    }

    private static bool GuideFrame(ResolutionFrame frame) =>
        frame.EffectId == "fellowship.Guide" && frame.NextStep == 0 && frame.Window == "" &&
        frame.ReturnDecision is null && frame.Effect is null && frame.Hunt is null && frame.Battle is null &&
        frame.BattleLoss is null && frame.EventLoss is null && frame.Corruption is null &&
        frame.Fellowship is { Kind: FellowshipProcedureKind.Guide, Return: FellowshipReturn.FinishAction,
            SourceId: "fellowship", AwaitingHunts: false, RevealEffectsPending: false, FireAndShadowPending: false } guide &&
        guide.Route.Count == 0 && guide.AdditionalHuntRegions.Count == 0 && guide.Journey is null &&
        guide.FixedCompanions.Count == 0 && guide.SeparationOrigin == "" && guide.SeparationRange == 0 &&
        guide.SeparationBonus == 0 && !guide.ChooseGuideAfterPlacement;
}
