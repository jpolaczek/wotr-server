using Wotr.Contracts;

namespace Wotr.Domain;

// Explicit player-facing scope for the ordinary paid move. Card-origin Hunts,
// additional stronghold Hunts and arbitrary effect programs need other contexts.
internal static class FellowshipMoveProjection
{
    public static FellowshipMoveView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (frames.Count is < 1 or > 2 || frames[0].Hunt is not { } hunt ||
            frames[0].EffectId != "hunt" || !PlainFrame(frames[0]) || !MovementHunt(hunt) || state.Pending is null ||
            state.Resolution.ActionSequence is not null || state.Resolution.PaidDieIds.Count != 1 ||
            !state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving) ||
            !SupportedChoice(state, hunt))
            return null;
        return new()
        {
            GuideWindowsOffered = [.. hunt.OfferedGuides],
            CasualtyConsequencesPending = hunt.CasualtyEffectsPending,
            VoluntaryReveal = hunt.VoluntaryReveal,
            DepartureRegionId = hunt.DepartureOrigin,
            DepartureProgress = hunt.DepartureProgress,
            DepartingCompanions = [.. hunt.DepartingCompanions],
            PlacementStarted = hunt.DeparturePlaced
        };
    }

    private static bool MovementHunt(HuntProcedure hunt) =>
        hunt.SourceId == "fellowship-move" && hunt.Movement && hunt.Return == HuntReturn.FinishMove &&
        !hunt.Rolled && hunt.TileRule == HuntTileRule.All && hunt.DamageRule == HuntDamageRule.Normal &&
        hunt.Purpose == HuntPurpose.Hunt && !hunt.DeclarationHunt && !hunt.IgnoreRevealIcon &&
        !hunt.FireAndShadow && hunt.DrawnBy == Side.Shadow && hunt.DiceReduction == 0 &&
        hunt.RerollBonus == 0 && (hunt.Stage is not (HuntStage.BeforeRoll or HuntStage.Roll) || hunt.RollBonus == 0);

    private static bool SupportedChoice(MatchState state, HuntProcedure hunt)
    {
        var pending = state.Pending!;
        var frames = state.Resolution.Frames;
        if (frames.Count == 1)
            return pending.Kind == DecisionKind.HuntReroll && pending.Owner == Side.Shadow &&
                hunt.Stage == HuntStage.BeforeDraw ||
                pending.Kind == DecisionKind.HuntDamage && pending.Owner == Side.FreePeoples &&
                hunt.Stage == HuntStage.Damage;
        var top = frames[1];
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
        return (hunt.Stage, effect.Owner, effect.SourceId) == expected &&
            effect.OfferedReactions.SequenceEqual(EffectCatalog.All.Reactions.Values
                .Where(r => r.Window == effect.ReactionWindow).Select(r => r.Id));
    }

    private static bool PlainFrame(ResolutionFrame frame) =>
        frame.NextStep == 0 && frame.Window == "" && frame.ReturnDecision is null &&
        frame.Battle is null && frame.BattleLoss is null && frame.EventLoss is null && frame.Corruption is null &&
        (frame.Hunt is null ? 0 : 1) + (frame.Fellowship is null ? 0 : 1) + (frame.Effect is null ? 0 : 1) == 1;
}
