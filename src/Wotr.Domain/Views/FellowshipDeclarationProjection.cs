using Wotr.Contracts;

namespace Wotr.Domain;

internal static class FellowshipDeclarationProjection
{
    internal static FellowshipDeclarationView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (state.Phase != GamePhase.Fellowship || state.Pending is null ||
            state.AfterDiscard != DiscardContinuation.None || state.Resolution.PaidDieIds.Count != 0 ||
            state.Resolution.ActionSequence is not null || state.PoliticalReactions.Count != 0 ||
            frames.Count is < 2 or > 6 || !DeclarationFrame(frames[0]) ||
            state.Fellowship.LastDeclarationTurn != state.Turn || state.Fellowship.Progress != 0 ||
            state.Fellowship.MordorStep.HasValue)
            return null;
        var declaration = frames[0].Fellowship!;
        var route = declaration.Route;
        if (route.Count == 0 || route.Any(id => !BoardRules.Regions.ContainsKey(id)) ||
            route[^1] != state.Fellowship.RegionId || !TravelRules.ValidPath(route, route[0], route.Count - 1))
            return null;
        var facts = new FellowshipDeclarationView
        {
            Route = [.. route], RemainingStrongholds = [.. declaration.AdditionalHuntRegions]
        };
        if (frames.Count == 2 && frames[1].Effect is not null)
            return state.Fellowship.Hidden && declaration.AdditionalHuntRegions.Count == 0 &&
                HuntChoiceProjection.DeclarationWindow(state, frames[1]) ? facts : null;

        var balrog = frames.Count >= 4 && frames[2].EffectId == ShadowCharacterCards03.BalrogReaction;
        var index = balrog ? 3 : 1;
        if (frames[index].Hunt is not { } hunt)
            return null;
        if (balrog)
        {
            if (!state.Fellowship.Hidden || declaration.AdditionalHuntRegions.Count != 0 ||
                !BalrogHuntProjection.Supports(state, 0, declaration))
                return null;
        }
        else
        {
            var all = TravelRules.AdditionalHunts(state, route);
            var remaining = declaration.AdditionalHuntRegions;
            if (state.Fellowship.Hidden || remaining.Count == 0 || remaining.Count > all.Count ||
                !all.TakeLast(remaining.Count).SequenceEqual(remaining) ||
                frames[index].EffectId != "hunt" || !HuntChoiceProjection.PlainFrame(frames[index]) ||
                !FellowshipStrongholdProjection.StrongholdHunt(hunt, remaining[0]))
                return null;
        }
        if (!HuntChoiceProjection.Supports(state, hunt, index))
            return null;
        HuntChoiceProjection.CopyProgress(hunt, facts);
        return facts;
    }

    private static bool DeclarationFrame(ResolutionFrame frame) =>
        frame.EffectId == "fellowship.Declaration" && frame.NextStep == 0 && frame.Window == "" &&
        frame.ReturnDecision is { Kind: DecisionKind.FellowshipPhase, Owner: Side.FreePeoples } pending &&
        pending.Commands.SequenceEqual(TurnDecisionCommands.Fellowship()) && pending.PrivateChoices.Count == 0 &&
        pending.RequiredCount == 0 && pending.SourceId == "" && pending.Window == "" && pending.Presentation == "" &&
        frame.Effect is null && frame.Hunt is null && frame.Battle is null && frame.BattleLoss is null &&
        frame.EventLoss is null && frame.Corruption is null &&
        frame.Fellowship is { Kind: FellowshipProcedureKind.Declaration, Return: FellowshipReturn.Decision,
            SourceId: "fellowship", AwaitingHunts: true, RevealEffectsPending: false, FireAndShadowPending: false } declaration &&
        declaration.Journey is null && declaration.FixedCompanions.Count == 0 &&
        declaration.SeparationOrigin == "" && declaration.SeparationRange == 0 && declaration.SeparationBonus == 0 &&
        !declaration.ChooseGuideAfterPlacement;
}
