using Wotr.Contracts;

namespace Wotr.Domain;

internal static class FellowshipDeclarationProjection
{
    internal static FellowshipDeclarationView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        var smeagolGuide = SmeagolDeclarationGuide(state, frames);
        if (state.Phase != GamePhase.Fellowship || state.Pending is null ||
            state.AfterDiscard != DiscardContinuation.None || state.Resolution.PaidDieIds.Count != 0 ||
            state.Resolution.ActionSequence is not null || state.PoliticalReactions.Count != 0 ||
            frames.Count is < 2 or > 6 || !DeclarationFrame(state, frames[0], smeagolGuide) ||
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
        if (smeagolGuide)
            return declaration.AdditionalHuntRegions.Count == 0 ? facts : null;

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

        if (frames[1].Hunt is { FireAndShadow: true } fire)
        {
            if (!FireAndShadowHunt(state, declaration, frames[1], fire) ||
                !FireAndShadowGuide(state, fire, frames) && !HuntChoiceProjection.Supports(state, fire, 1))
                return null;
            HuntChoiceProjection.CopyProgress(fire, facts);
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

    private static bool DeclarationFrame(MatchState state, ResolutionFrame frame, bool revealEffectsPending) =>
        frame.EffectId == "fellowship.Declaration" && frame.NextStep == 0 && frame.Window == "" &&
        frame.ReturnDecision is { Kind: DecisionKind.FellowshipPhase, Owner: Side.FreePeoples } pending &&
        pending.Commands.SequenceEqual(TurnDecisionCommands.Fellowship()) && pending.PrivateChoices.Count == 0 &&
        pending.RequiredCount == 0 && pending.SourceId == "" && pending.Window == "" && pending.Presentation == "" &&
        frame.Effect is null && frame.Hunt is null && frame.Battle is null && frame.BattleLoss is null &&
        frame.EventLoss is null && frame.Corruption is null &&
        frame.Fellowship is { Kind: FellowshipProcedureKind.Declaration, Return: FellowshipReturn.Decision,
            SourceId: "fellowship", AwaitingHunts: true } declaration &&
        declaration.RevealEffectsPending == revealEffectsPending &&
        declaration.Journey is null && declaration.FixedCompanions.Count == 0 &&
        declaration.SeparationOrigin == "" && declaration.SeparationRange == 0 && declaration.SeparationBonus == 0 &&
        !declaration.ChooseGuideAfterPlacement &&
        (!declaration.FireAndShadowPending || FireAndShadow.Applies(state, declaration.Route));

    // Lords: Smeagol leaves a Fellowship declared in an unconquered City or Stronghold, and the Free
    // Peoples choose a new Guide before the declaration's own reactions and Hunts.
    // It is read for every view, so the region test comes last and only for a board region: the
    // Fellowship on the Mordor Track has none.
    private static bool SmeagolDeclarationGuide(MatchState state, IReadOnlyList<ResolutionFrame> frames) =>
        state.Configuration.Lords && state.Phase == GamePhase.Fellowship &&
        frames is [_, var top] && HuntChoiceProjection.PlainFrame(top) && top.EffectId == "fellowship.Guide" &&
        top.Fellowship is { Kind: FellowshipProcedureKind.Guide, Return: FellowshipReturn.Parent, SourceId: "fellowship",
            AwaitingHunts: false, RevealEffectsPending: false, FireAndShadowPending: false, Journey: null,
            ChooseGuideAfterPlacement: false, SeparationOrigin: "", SeparationRange: 0, SeparationBonus: 0 } guide &&
        guide.Route.Count == 0 && guide.AdditionalHuntRegions.Count == 0 && guide.FixedCompanions.Count == 0 &&
        state.Pending is { Kind: DecisionKind.Guide, Owner: Side.FreePeoples, RequiredCount: 0, SourceId: "",
            Window: "", Commands: [CommandKind.SelectGuide] } pending &&
        pending.PrivateChoices.Count >= 2 && pending.PrivateChoices.SequenceEqual(CompanionRules.Guides(state)) &&
        state.Lords is { SmeagolLeft: true } && !SmeagolRules.Present(state) &&
        BoardRules.Regions.ContainsKey(state.Fellowship.RegionId) && TravelRules.Sanctuary(state, state.Fellowship.RegionId);

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

    // Fire and Shadow: a declared route that passes the Lords' Balrog draws one tile for him at once.
    private static bool FireAndShadowHunt(MatchState state, FellowshipProcedure declaration, ResolutionFrame frame,
        HuntProcedure hunt) =>
        state.Configuration.Lords && state.Fellowship.Hidden && declaration.AdditionalHuntRegions.Count == 0 &&
        !declaration.FireAndShadowPending && frame.EffectId == "hunt" && HuntChoiceProjection.PlainFrame(frame) &&
        hunt is { SourceId: LordsCharacters.Balrog, Return: HuntReturn.Parent, Movement: false, Rolled: false,
            Mordor: false, TileRule: HuntTileRule.All, DamageRule: HuntDamageRule.Normal, Purpose: HuntPurpose.Hunt,
            DeclarationHunt: true, IgnoreRevealIcon: true, DrawnBy: Side.Shadow, DiceReduction: 0, Bonus: 0,
            RollBonus: 0, RerollBonus: 0, EyeDamage: 0 } &&
        hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0 && hunt.TileId.Length != 0;

    // An Eye eliminates the Balrog and the Guide together; the new Guide is chosen before the
    // corruption stage that follows.
    private static bool FireAndShadowGuide(MatchState state, HuntProcedure hunt, IReadOnlyList<ResolutionFrame> frames) =>
        frames is [_, _, var top] && HuntChoiceProjection.PlainFrame(top) && top.EffectId == "fellowship.Guide" &&
        top.Fellowship is { Kind: FellowshipProcedureKind.Guide, Return: FellowshipReturn.Parent, SourceId: "fellowship",
            AwaitingHunts: false, RevealEffectsPending: false, FireAndShadowPending: false, Journey: null,
            ChooseGuideAfterPlacement: false, SeparationOrigin: "", SeparationRange: 0, SeparationBonus: 0 } guide &&
        guide.Route.Count == 0 && guide.AdditionalHuntRegions.Count == 0 && guide.FixedCompanions.Count == 0 &&
        state.Pending is { Kind: DecisionKind.Guide, Owner: Side.FreePeoples, RequiredCount: 0, SourceId: "",
            Window: "", Commands: [CommandKind.SelectGuide] } pending &&
        pending.PrivateChoices.Count >= 2 && pending.PrivateChoices.SequenceEqual(CompanionRules.Guides(state)) &&
        hunt.Stage == HuntStage.Corruption && HuntPool.Definitions.TryGetValue(hunt.TileId, out var tile) &&
        tile.Value == "eye" && CharacterRules.InPlay(state, LordsCharacters.Balrog) is null;

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
