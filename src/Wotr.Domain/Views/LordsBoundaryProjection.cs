using Wotr.Contracts;

namespace Wotr.Domain;

internal static class LordsBoundaryProjection
{
    internal static bool Supports(MatchState state, bool reviewedContinuation = false)
    {
        if (!state.Configuration.Lords)
            return state.Lords is null;
        // Reviewed Hunt and direct battle contexts are explicit; other suspended
        // Lords programs still require their own semantic mapping.
        return state.Lords is { } lords &&
            (state.Resolution.Frames.Count == 0 || reviewedContinuation || CorruptionBoundaryProjection.Supports(state)) &&
            (state.Configuration.Council ? lords.Council is { BattleReturns.Count: 0 } : lords.Council is null) &&
            (lords.ActionCompletion is null || state.Pending?.Kind == DecisionKind.KeepActionDie);
    }

    internal static LordsRuleFactsView? For(MatchState state) => state.Lords is { } lords ? new()
    {
        WitchKingReplaced = lords.WitchKingReplaced,
        EvilKnowledgeUsed = lords.EvilKnowledgeUsed,
        SmeagolLeft = lords.SmeagolLeft,
        GollumEntered = lords.GollumEntered,
        LoreOfferedThisTurn = state.Turn > 0 && lords.LoreOfferedTurn == state.Turn,
        RolledDiceCounts = new(lords.RolledCounts),
        SpecialDieChoiceOrder = [.. lords.ChoiceOrder],
        CaptainDice = [.. lords.CaptainDice],
        PaidWillOfWestIds = [.. lords.PaidWillOfWestIds],
        CompletedActionWasFellowshipMove = lords.ActionCompletion?.FellowshipMove,
        Council = lords.Council is { } council ? new()
        {
            HomelandsChosen = council.HomelandsChosen,
            FellowshipMovedThisTurn = council.LastFellowshipDieTurn == state.Turn,
            FellowshipBoxDieId = council.FellowshipBoxDieId
        } : null
    } : null;

    internal static bool Visible(MatchState state, Side viewer, CharacterState character) =>
        !state.Configuration.Lords || viewer == Side.FreePeoples ||
        !state.Fellowship.Companions.Contains(character.Id) ||
        !CouncilCompanions.AvailableVersions(state).Any(v => v.FigureId == character.Id) ||
        state.Lords!.RevealedCompanionVersions.ContainsKey(character.Id);

    internal static bool Phase(MatchState state, PendingDecision pending) => pending.Kind switch
    {
        DecisionKind.CouncilVersions => state.Configuration.Lords && state.Phase == GamePhase.Setup &&
            pending.Owner == Side.FreePeoples && state.Turn == 0,
        DecisionKind.CouncilHomelands => state.Phase == GamePhase.Fellowship && state.Turn == 1 &&
            pending.Owner == Side.FreePeoples && state.Lords?.Council is { HomelandsChosen: false },
        DecisionKind.CouncilToken => state.Phase == GamePhase.Fellowship && state.Turn == 1 &&
            pending.Owner == Side.Shadow && state.Lords?.Council is { HomelandsChosen: true, GrantedTokens.Count: 0 },
        DecisionKind.SpecialDieChoice => state.Phase == GamePhase.ActionRoll &&
            state.Lords is { ChoiceOrder.Count: > 0 } lords && lords.ChoiceOrder[0] == pending.Owner,
        _ => false
    };

    internal static bool Completion(MatchState state) => state.Pending is { Kind: DecisionKind.KeepActionDie,
        Owner: Side.FreePeoples, SourceId: LordsDiceSetup.VilyaRing, Window: "action.completed" } &&
        state.Phase == GamePhase.ActionResolution && state.Lords?.ActionCompletion is not null &&
        state.Resolution.ActionSequence is null && state.PoliticalReactions.Count == 0 &&
        state.AfterDiscard == DiscardContinuation.None && VilyaCompletion.EligibleDice(state).Count > 0;
}
