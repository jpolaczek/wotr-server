using Wotr.Contracts;

namespace Wotr.Domain;

// QUAL-01: the structural contract of a command, checked before any rule, history or receipt code reads a value.
// Explicit JSON null, unknown enum numbers and oversized values are malformed input (MalformedCommand), apart
// from a well-formed choice that is illegal in the current decision (IllegalChoice). An empty string stays a
// legal "not chosen" value; nothing is normalised, so receipt fingerprints keep their meaning.
public static class CommandShape
{
    public const string MalformedCommand = "MalformedCommand";
    public const int MaxCommandIdLength = 100;
    public const int MaxTextLength = 128;

    public static bool IsValid(GameCommand? command) =>
        command is not null && HasValidEnvelope(command) && command.Payload is { } payload &&
        (!payload.Stepwise || ActionStepRules.Supports(command.Kind)) &&
        HasValidValues(payload) && HasValidCollections(payload);

    private static bool HasValidEnvelope(GameCommand command) =>
        !string.IsNullOrWhiteSpace(command.CommandId) && command.CommandId.Length <= MaxCommandIdLength &&
        IsText(command.MatchId) && IsText(command.DecisionId) && Enum.IsDefined(command.Kind);

    private static bool HasValidValues(CommandPayload payload) =>
        Texts(payload).All(IsText) &&
        (payload.Face is null || Enum.IsDefined(payload.Face.Value)) &&
        Enum.IsDefined(payload.MovementCost) && Enum.IsDefined(payload.CardCost) && Enum.IsDefined(payload.HuntResponse) &&
        (payload.AutoDecision is null || IsText(payload.AutoDecision.BattleId));

    private static IEnumerable<string?> Texts(CommandPayload payload) =>
    [
        payload.DieId, payload.RingId, payload.RulerTokenId, payload.CouncilTokenId, payload.Deck, payload.NationId,
        payload.CardId, payload.ReactionId, payload.GuideId, payload.RegionId, payload.CharacterId, payload.AbilityId
    ];

    private static bool HasValidCollections(CommandPayload payload) =>
        HasValidIds(payload.Ids, 256) &&
        payload.Placements is { Count: <= 256 } && payload.Placements.All(HasValidPlacement) &&
        payload.Moves is { Count: <= 16 } && payload.Moves.All(HasValidArmyMove) &&
        HasValidRoute(payload.Route) &&
        payload.Losses is { Count: <= 256 } && payload.Losses.All(HasValidLoss) &&
        payload.CompanionMoves is { Count: <= 32 } && payload.CompanionMoves.All(HasValidCompanionMove);

    private static bool HasValidPlacement(UnitPlacementChoice? placement) =>
        placement is not null && IsText(placement.UnitId) && IsText(placement.RegionId);

    private static bool HasValidLoss(BattleLossChoice? loss) =>
        loss is not null && IsText(loss.UnitId) && Enum.IsDefined(loss.Kind);

    private static bool HasValidArmyMove(ArmyMoveChoice? move) =>
        move is not null && !string.IsNullOrEmpty(move.From) && !string.IsNullOrEmpty(move.To) &&
        IsText(move.From) && IsText(move.To) &&
        HasValidIds(move.UnitIds, 256) && HasValidIds(move.CharacterIds, 32) && HasValidRoute(move.Route);

    private static bool HasValidCompanionMove(CompanionMoveChoice? move) =>
        move is not null && HasValidIds(move.Ids, 32) && HasValidRoute(move.Route);

    private static bool HasValidIds(List<string>? ids, int limit) =>
        ids is not null && ids.Count <= limit && ids.All(IsText);

    private static bool HasValidRoute(List<string>? route) =>
        route is { Count: <= 128 } && route.All(step => !string.IsNullOrEmpty(step) && step.Length <= MaxTextLength);

    private static bool IsText(string? value) => value is { Length: <= MaxTextLength };
}
