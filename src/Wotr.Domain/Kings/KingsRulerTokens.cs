using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsRulerTokens
{
    public static string Id(string sovereignId) => $"kings.token.{SovereignCatalog.All[sovereignId].Identity}";
    public static DieFace Face(string sovereignId) => sovereignId switch
    {
        SovereignCatalog.Denethor => DieFace.Event,
        SovereignCatalog.Thranduil => DieFace.Character,
        _ => DieFace.Muster
    };
    public static string Ability(string sovereignId) => sovereignId switch
    {
        SovereignCatalog.Brand => "kings.call-to-arms",
        SovereignCatalog.Dain => "kings.iron-guard",
        SovereignCatalog.Theoden => "kings.mustering-of-the-mark",
        SovereignCatalog.Denethor => KingsAnarion.ProgramId,
        SovereignCatalog.Thranduil => KingsFarSighted.Ability,
        _ => ""
    };

    public static bool RingAllowed(MatchState state, Side actor) => actor != Side.FreePeoples ||
        !state.Configuration.Kings || state.Kings?.RulerTokenUsedThisTurn != true;

    public static void RecordRing(MatchState state, Side actor)
    {
        if (state.Configuration.Kings && actor == Side.FreePeoples && state.Kings is not null)
            state.Kings.ElvenRingUsedThisTurn = true;
    }

    public static void BeginTurn(MatchState state)
    {
        if (state.Kings is null)
            return;
        state.Kings.RulerTokenUsedThisTurn = false;
        state.Kings.ElvenRingUsedThisTurn = false;
        state.Kings.TokenActionSovereign = "";
    }

    public static bool ConditionsMet(MatchState state, RulerTokenState token) => state.Configuration.Kings &&
        !state.Configuration.RemovingRulerTokens && token.Location == "free-peoples" &&
        !state.Player(Side.FreePeoples).RingUsedThisTurn && state.Kings?.ElvenRingUsedThisTurn != true &&
        SourceReady(state, token) && KingsDice.Ruler(state, Side.FreePeoples)?.Location == DieLocation.Available;

    public static bool SourceReady(MatchState state, RulerTokenState token) =>
        KingsRulerAbilities.Awakened(state, token.SovereignId) is { } character &&
        (token.SovereignId != SovereignCatalog.Denethor || character.Location == "minas-tirith") &&
        (token.SovereignId != SovereignCatalog.Thranduil || !state.Fellowship.MordorStep.HasValue);

    public static bool CanUse(MatchState state, RulerTokenState token) => ConditionsMet(state, token) &&
        state.Pending is { Kind: DecisionKind.Action, Owner: Side.FreePeoples } && state.Phase == GamePhase.ActionResolution;

    private static RulerTokenState? Find(MatchState state, string tokenId) =>
        state.Kings?.Tokens.Find(t => Id(t.SovereignId) == tokenId);

    public static bool Valid(MatchState state, Side actor, GameCommand command)
    {
        var payload = command.Payload;
        if (string.IsNullOrEmpty(payload.RulerTokenId))
            return true;
        if (actor != Side.FreePeoples || payload.RingId != "" || Find(state, payload.RulerTokenId) is not { } token ||
            !CanUse(state, token) || payload.DieId != KingsDice.Ruler(state, actor)!.Id)
            return false;
        return token.SovereignId switch
        {
            SovereignCatalog.Denethor => payload.AbilityId == "" && (command.Kind == CommandKind.DrawCard ||
                command.Kind == CommandKind.PlayEvent && payload.CardCost == DieFace.Event),
            SovereignCatalog.Thranduil => payload.AbilityId == KingsFarSighted.Ability &&
                command.Kind is CommandKind.MoveFellowship or CommandKind.MoveCompanions or CommandKind.SeparateCompanions,
            _ => command.Kind == CommandKind.UseCharacterAbility && payload.CharacterId == token.SovereignId &&
                payload.AbilityId == Ability(token.SovereignId)
        };
    }

    // Only token actions need a validation copy. Normal commands keep their existing allocation cost.
    public static MatchState ValidationState(MatchState state, CommandPayload payload)
    {
        if (string.IsNullOrEmpty(payload.RulerTokenId))
            return state;
        var copy = StateCodec.Copy(state);
        KingsDice.Ruler(copy, Side.FreePeoples)!.Face = Face(Find(state, payload.RulerTokenId)!.SovereignId);
        return copy;
    }

    public static void Apply(MatchState state, CommandPayload payload)
    {
        if (string.IsNullOrEmpty(payload.RulerTokenId))
            return;
        var token = Find(state, payload.RulerTokenId)!;
        KingsDice.Ruler(state, Side.FreePeoples)!.Face = Face(token.SovereignId);
        KingsDismay.UsedRulerToken(state, token);
        state.Kings!.RulerTokenUsedThisTurn = true;
        state.Kings.TokenActionSovereign = token.SovereignId;
        state.Events.Add(new() { Revision = state.Revision, Kind = "RulerTokenUsed",
            Text = $"Free Peoples used {SovereignCatalog.All[token.SovereignId].Title}'s Ruler token." });
    }
}
