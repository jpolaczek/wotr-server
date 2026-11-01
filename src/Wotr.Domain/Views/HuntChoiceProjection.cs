using Wotr.Contracts;

namespace Wotr.Domain;

internal static class HuntChoiceProjection
{
    internal static void CopyProgress(HuntProcedure hunt, HuntProgressView facts)
    {
        facts.GuideWindowsOffered = [.. hunt.OfferedGuides];
        facts.CasualtyConsequencesPending = hunt.CasualtyEffectsPending;
        facts.VoluntaryReveal = hunt.VoluntaryReveal;
        facts.DepartureRegionId = hunt.DepartureOrigin;
        facts.DepartureProgress = hunt.DepartureProgress;
        facts.DepartingCompanions = [.. hunt.DepartingCompanions];
        facts.PlacementStarted = hunt.DeparturePlaced;
    }

    internal static bool Supports(MatchState state, HuntProcedure hunt, int activeIndex)
    {
        var pending = state.Pending!;
        var frames = state.Resolution.Frames;
        if (frames.Count == activeIndex + 1)
            return pending.Kind == DecisionKind.HuntReroll && pending.Owner == Side.Shadow &&
                hunt.Stage == HuntStage.BeforeDraw ||
                pending.Kind == DecisionKind.HuntDamage && pending.Owner == Side.FreePeoples &&
                hunt.Stage == HuntStage.Damage;
        if (frames.Count != activeIndex + 2)
            return false;
        var top = frames[activeIndex + 1];
        if (!PlainFrame(top))
            return false;
        if (top.Fellowship is { } fellowship)
            return top.EffectId == "fellowship." + fellowship.Kind && pending.Owner == Side.FreePeoples &&
                FellowshipChoice(state, pending.Kind, hunt, fellowship);
        return ReactionChoice(state, hunt, top);
    }

    private static bool FellowshipChoice(MatchState state, DecisionKind kind, HuntProcedure hunt, FellowshipProcedure fellowship)
    {
        if (fellowship.Return != FellowshipReturn.Parent || fellowship.AwaitingHunts ||
            fellowship.RevealEffectsPending || fellowship.FireAndShadowPending || fellowship.Route.Count != 0 ||
            fellowship.AdditionalHuntRegions.Count != 0 || fellowship.Journey is not null ||
            fellowship.ChooseGuideAfterPlacement || fellowship.SeparationBonus != 0)
            return false;
        if (kind == DecisionKind.CompanionSeparation)
            return fellowship.Kind == FellowshipProcedureKind.Separation && hunt.Stage == HuntStage.Corruption &&
                hunt.DeparturePlaced && hunt.DepartingCompanions.Count > 0 &&
                fellowship.SourceId == hunt.TileId && fellowship.FixedCompanions.SequenceEqual(hunt.DepartingCompanions) &&
                fellowship.SeparationOrigin == hunt.DepartureOrigin &&
                fellowship.SeparationRange == hunt.DepartureProgress + hunt.DepartingCompanions
                    .Max(id => CharacterRules.MovementLevel(Content.CharacterCatalog.Companion(state, id)));
        if (fellowship.FixedCompanions.Count != 0 || fellowship.SeparationOrigin != "" || fellowship.SeparationRange != 0)
            return false;
        return kind == DecisionKind.Guide && fellowship.Kind == FellowshipProcedureKind.Guide &&
                fellowship.SourceId == "fellowship" && hunt.Stage == HuntStage.Guide ||
            kind == DecisionKind.FellowshipRoute && fellowship.Kind == FellowshipProcedureKind.Reveal &&
                fellowship.SourceId == hunt.TileId && hunt.Stage == HuntStage.Done;
    }

    private static bool ReactionChoice(MatchState state, HuntProcedure hunt, ResolutionFrame frame)
    {
        if (!ReactionFrame(state, frame))
            return false;
        var effect = frame.Effect!;
        var expected = effect.ReactionWindow switch
        {
            "hunt.roll" => (HuntStage.Roll, Side.Shadow, hunt.SourceId),
            "hunt.draw" => (HuntStage.Draw, Side.FreePeoples, hunt.SourceId),
            "hunt.replace" => (HuntStage.DetermineDamage, Side.FreePeoples, hunt.TileId),
            "hunt.table" => (HuntStage.Guide, Side.FreePeoples, hunt.TileId),
            "hunt.guide" => (HuntStage.Guide, Side.FreePeoples, state.Fellowship.Guide),
            "hunt.casualty" => (HuntStage.Guide, Side.Shadow, hunt.SourceId),
            _ => ((HuntStage)(-1), Side.FreePeoples, "")
        };
        return (hunt.Stage, effect.Owner, effect.SourceId) == expected;
    }

    internal static bool RevelationOffer(MatchState state, HuntProcedure hunt, ResolutionFrame frame) =>
        PlainFrame(frame) && ReactionFrame(state, frame) && hunt.Stage == HuntStage.Done &&
        frame.Effect is { Owner: Side.Shadow, ReactionWindow: "fellowship.revealed" } effect &&
        effect.SourceId == hunt.TileId;

    private static bool ReactionFrame(MatchState state, ResolutionFrame frame)
    {
        if (state.Pending!.Kind != DecisionKind.Reaction || frame.EffectId != EffectCatalog.ReactionProgram ||
            frame.Effect is not { Wait: EffectWait.Reaction, Return: EffectReturn.Parent } effect ||
            effect.Owner != state.Pending.Owner || effect.SourceId != state.Pending.SourceId ||
            effect.ReactionWindow != state.Pending.Window || effect.Committed.Count != 0 ||
            effect.IsEvent || effect.CompletionWindowOffered || effect.PalantirDrawPending ||
            effect.NationId != "" || effect.RegionId != "" || effect.TargetIds.Count != 0 ||
            effect.ArmyOrder is not null || effect.AppliedHits != 0 || effect.PaidFace != default ||
            effect.InspectedHand is not null || effect.InspectedSovereign is not null ||
            effect.RingRecovery is not null || effect.MadnessTarget is not null ||
            effect.AnarionDrawPending || effect.FellowshipMove)
            return false;
        return effect.OfferedReactions.SequenceEqual(EffectCatalog.All.Reactions.Values
                .Where(r => r.Window == effect.ReactionWindow).Select(r => r.Id));
    }

    internal static bool PlainFrame(ResolutionFrame frame) =>
        frame.NextStep == 0 && frame.Window == "" && frame.ReturnDecision is null &&
        frame.Battle is null && frame.BattleLoss is null && frame.EventLoss is null && frame.Corruption is null &&
        (frame.Hunt is null ? 0 : 1) + (frame.Fellowship is null ? 0 : 1) + (frame.Effect is null ? 0 : 1) == 1;
}
