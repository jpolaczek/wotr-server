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
            frames.Count is < 2 or > 6 || !DeclarationFrame(state, frames[0]) ||
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
            Route = [.. route], RemainingStrongholds = [.. declaration.AdditionalHuntRegions],
            FireAndShadowPending = declaration.FireAndShadowPending
        };
        if (frames.Count == 2 && frames[1].Effect is not null)
        {
            if (!HuntChoiceProjection.DeclarationWindow(state, frames[1]) ||
                state.Fellowship.Hidden && declaration.AdditionalHuntRegions.Count != 0 ||
                !state.Fellowship.Hidden && !ValidRemainingStrongholds(state, route, declaration.AdditionalHuntRegions))
                return null;
            CopyCommittedReactions(frames[1], facts);
            return facts;
        }

        if (frames.Count >= 3 && frames[1].Effect is not null && frames[2].Hunt is { } gatesHunt)
        {
            var gatesResponse = frames[1].Effect!.Committed.Any(selection =>
                selection.Owner == Side.Shadow && selection.SourceId == LordsShadowCards.Gates);
            if (!state.Fellowship.Hidden || declaration.AdditionalHuntRegions.Count != 0 || !gatesResponse ||
                !HuntChoiceProjection.DeclarationReactionsFrame(state, frames[1], EffectWait.None) ||
                !GatesDeclarationHunt(state, declaration, frames[2], gatesHunt) ||
                !HuntChoiceProjection.Supports(state, gatesHunt, 2))
                return null;
            CopyCommittedReactions(frames[1], facts);
            HuntChoiceProjection.CopyProgress(gatesHunt, facts);
            return facts;
        }

        var balrog = frames.Count >= 4 && frames[2].EffectId == ShadowCharacterCards03.BalrogReaction;
        var index = balrog ? 3 : 1;
        if (frames[index].Hunt is not { } hunt)
            return null;
        if (balrog)
        {
            if (!state.Fellowship.Hidden || declaration.AdditionalHuntRegions.Count != 0 ||
                !BalrogHuntProjection.Supports(state, 0, declaration))
                return null;
            if (frames.Count > 1 && frames[1].Effect is not null)
                CopyCommittedReactions(frames[1], facts);
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

    private static bool DeclarationFrame(MatchState state, ResolutionFrame frame) =>
        frame.EffectId == "fellowship.Declaration" && frame.NextStep == 0 && frame.Window == "" &&
        frame.ReturnDecision is { Kind: DecisionKind.FellowshipPhase, Owner: Side.FreePeoples } pending &&
        pending.Commands.SequenceEqual(TurnDecisionCommands.Fellowship()) && pending.PrivateChoices.Count == 0 &&
        pending.RequiredCount == 0 && pending.SourceId == "" && pending.Window == "" && pending.Presentation == "" &&
        frame.Effect is null && frame.Hunt is null && frame.Battle is null && frame.BattleLoss is null &&
        frame.EventLoss is null && frame.Corruption is null &&
        frame.Fellowship is { Kind: FellowshipProcedureKind.Declaration, Return: FellowshipReturn.Decision,
            SourceId: "fellowship", AwaitingHunts: true, RevealEffectsPending: false } declaration &&
        declaration.Journey is null && declaration.FixedCompanions.Count == 0 &&
        declaration.SeparationOrigin == "" && declaration.SeparationRange == 0 && declaration.SeparationBonus == 0 &&
        !declaration.ChooseGuideAfterPlacement &&
        (!declaration.FireAndShadowPending || FireAndShadow.Applies(state, declaration.Route));

    private static void CopyCommittedReactions(ResolutionFrame frame, FellowshipDeclarationView facts)
    {
        var reactions = EffectCatalog.All.Reactions.Values.Where(r => r.Window == "fellowship.declared").ToArray();
        facts.CommittedReactionSources = frame.Effect!.Committed
            .Where(selection => selection.Owner == Side.Shadow && reactions.Any(r =>
                (r.RequiredTableCard ?? r.RequiredGuideId ?? r.RequiredCharacterId ?? r.Id) == selection.SourceId))
            .Select(selection => selection.SourceId).ToList();
    }

    private static bool ValidRemainingStrongholds(MatchState state, IReadOnlyList<string> route,
        IReadOnlyList<string> remaining)
    {
        var all = TravelRules.AdditionalHunts(state, route);
        return remaining.Count <= all.Count && all.TakeLast(remaining.Count).SequenceEqual(remaining);
    }

    private static bool GatesDeclarationHunt(MatchState state, FellowshipProcedure declaration,
        ResolutionFrame frame, HuntProcedure hunt) =>
        state.Configuration.Lords && declaration.Route.Any(id => BoardRules.Region(state, id) is
            { Settlement: "Stronghold", OriginalOwner: Side.Shadow } && state.Control.GetValueOrDefault(id) == Side.Shadow) &&
        state.Player(Side.Shadow).Discard.Contains(LordsShadowCards.Gates) &&
        !state.Player(Side.Shadow).Table.Contains(LordsShadowCards.Gates) &&
        EffectCatalog.All.Programs.TryGetValue(LordsShadowCards.GatesReaction, out var program) &&
        program.Steps is [GatesHuntStep] && frame.EffectId == "hunt" && HuntChoiceProjection.PlainFrame(frame) &&
        hunt is { SourceId: LordsShadowCards.Gates, Return: HuntReturn.Parent, Movement: false, Rolled: false,
            Mordor: false, TileRule: HuntTileRule.IgnoreEyes, DamageRule: HuntDamageRule.Normal,
            Purpose: HuntPurpose.Hunt, DeclarationHunt: true, IgnoreRevealIcon: true, FireAndShadow: false,
            DrawnBy: Side.Shadow, DiceReduction: 0, Bonus: 0, RollBonus: 0, RerollBonus: 0, EyeDamage: 0 } &&
        hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0;
}
