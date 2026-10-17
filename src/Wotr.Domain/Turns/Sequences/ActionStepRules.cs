using Wotr.Contracts;

namespace Wotr.Domain;

public static class ActionStepRules
{
    public static bool Supports(CommandKind kind) => kind is CommandKind.MoveArmies or CommandKind.Recruit or
        CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters;

    public static string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var sequence = state.Resolution.ActionSequence;
        var payload = command.Payload;
        if (!Supports(command.Kind) || !payload.Stepwise || state.Phase != GamePhase.ActionResolution ||
            state.Pending?.Owner != actor || payload.RingId != "" || payload.CouncilTokenId != "")
            return "IllegalChoice";
        if (sequence is null ? state.Pending.Kind != DecisionKind.Action : !Matches(state, actor, command, sequence))
            return "IllegalChoice";
        if (!SingleStep(command) || sequence is not null && FigureIds(command).Any(sequence.UsedIds.Contains))
            return "IllegalChoice";
        var valid = command.Kind switch
        {
            CommandKind.MoveArmies => MovementRules.Plan(state, actor, payload) is not null,
            CommandKind.Recruit => Recruit(state, actor, payload, sequence),
            CommandKind.MoveCompanions => Companions(state, actor, payload, sequence),
            CommandKind.MoveShadowCharacters => Shadow(state, actor, payload, sequence),
            _ => false
        };
        return valid ? null : "IllegalChoice";
    }

    private static bool Matches(MatchState state, Side actor, GameCommand command, ActionSequenceState sequence) =>
        state.Pending?.Kind == DecisionKind.ActionSequence && sequence.Owner == actor && sequence.Kind == command.Kind &&
        sequence.DieId == command.Payload.DieId && sequence.AbilityId == command.Payload.AbilityId &&
        command.Payload.RulerTokenId == "" && sequence.Remaining > 0 &&
        (sequence.Kind != CommandKind.MoveArmies || command.Payload.MovementCost == sequence.Face) &&
        state.Resolution.PaidDieIds.Contains(sequence.DieId) &&
        state.Dice.Any(d => d.Id == sequence.DieId && d.Owner == actor && d.Location == DieLocation.Resolving);

    private static bool SingleStep(GameCommand command) => command.Kind switch
    {
        CommandKind.MoveArmies => command.Payload.Moves.Count == 1 && command.Payload.Placements.Count == 0 && command.Payload.CompanionMoves.Count == 0,
        CommandKind.Recruit => command.Payload.Placements.Count == 1 && command.Payload.Moves.Count == 0 && command.Payload.CompanionMoves.Count == 0,
        _ => command.Payload.CompanionMoves.Count == 1 && command.Payload.Moves.Count == 0 && command.Payload.Placements.Count == 0
    };

    private static bool Recruit(MatchState state, Side actor, CommandPayload payload, ActionSequenceState? sequence)
    {
        if (payload.AbilityId != "" || sequence is null && !CharacterCosts.CanPay(state, actor, payload.DieId, DieFace.Muster) ||
            RecruitmentRules.ValidateMuster(state, actor, payload.Placements) is not null)
            return false;
        var choice = payload.Placements[0];
        return sequence is null || !sequence.UsedRegions.Contains(choice.RegionId) &&
            RecruitmentCost(state, choice) <= sequence.Remaining;
    }

    private static bool Companions(MatchState state, Side actor, CommandPayload payload, ActionSequenceState? sequence) =>
        actor == Side.FreePeoples && (sequence is not null ||
            KingsFarSighted.Valid(state, payload) && CharacterCosts.CanPay(state, actor, payload.DieId, DieFace.Character)) &&
        RemainingFigures(payload, sequence) && CompanionRules.Move(state, payload.CompanionMoves, KingsFarSighted.Bonus(payload)) is not null;

    private static bool Shadow(MatchState state, Side actor, CommandPayload payload, ActionSequenceState? sequence) =>
        actor == Side.Shadow && (sequence is not null || CharacterCommands.TravelCost(state, actor, payload)) &&
        RemainingFigures(payload, sequence) && ShadowTravel.Plan(state, payload.CompanionMoves) is not null;

    private static bool RemainingFigures(CommandPayload payload, ActionSequenceState? sequence) => sequence is null ||
        payload.CompanionMoves[0].Ids.Count <= sequence.Remaining && payload.CompanionMoves[0].Ids.All(sequence.InitialFigures.Contains);

    internal static int RecruitmentCost(MatchState state, UnitPlacementChoice choice) =>
        state.Units.Single(u => u.Id == choice.UnitId).Kind == UnitKind.Elite ? 2 : 1;

    internal static IEnumerable<string> FigureIds(GameCommand command) => command.Kind switch
    {
        CommandKind.MoveArmies => command.Payload.Moves.SelectMany(m => m.UnitIds.Concat(m.CharacterIds)),
        CommandKind.Recruit => command.Payload.Placements.Select(p => p.UnitId),
        _ => command.Payload.CompanionMoves.SelectMany(m => m.Ids)
    };
}
