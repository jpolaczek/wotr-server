using Wotr.Contracts;

namespace Wotr.Domain;

public static class CouncilTokens
{
    public const string Travel = "council.move-shadow-characters";
    public const string Politics = "council.advance-politics";
    public static IReadOnlyList<string> All { get; } = [Travel, Politics];

    public static bool Available(MatchState state, string token) => state.Configuration.Council &&
        state.Lords?.Council is { } council && council.LastTokenTurn != state.Turn &&
        council.GrantedTokens.Contains(token) && !council.SpentTokens.Contains(token);

    public static List<string> Nations(MatchState state) => state.Nations
        .Where(n => BoardRules.Nations[n.Id].Side == Side.Shadow && PoliticalRules.CanMusterAdvance(state, n))
        .Select(n => n.Id).ToList();

    public static bool CanAct(MatchState state) =>
        Available(state, Politics) && Nations(state).Count > 0 ||
        Available(state, Travel) && ShadowTravel.Options(state).Any();

    public static bool ValidUse(MatchState state, CommandPayload payload) =>
        !state.Player(Side.Shadow).RingActionPending && Available(state, payload.AbilityId) &&
        payload.DieId == "" && payload.RingId == "" && payload.Face is null && payload.Ids.Count == 0 &&
        (payload.AbilityId switch
        {
            Politics => payload.CompanionMoves.Count == 0 && Nations(state).Contains(payload.NationId),
            Travel => payload.NationId == "" && ShadowTravel.Plan(state, payload.CompanionMoves) is not null,
            _ => false
        });

    public static void Use(MatchState state, CommandPayload payload)
    {
        Spend(state, payload.AbilityId);
        if (payload.AbilityId == Politics)
            PoliticalRules.Advance(state, payload.NationId);
        else
            ShadowTravel.Apply(state, ShadowTravel.Plan(state, payload.CompanionMoves)!);
        TurnFlow.FinishAction(state, Side.Shadow);
    }

    public static void Spend(MatchState state, string tokenId)
    {
        var council = state.Lords!.Council!;
        council.SpentTokens.Add(tokenId);
        council.LastTokenTurn = state.Turn;
        state.Events.Add(new() { Revision = state.Revision, Kind = "CouncilTokenUsed", Text = tokenId + "." });
    }

    public static List<CouncilTokenView> View(MatchState state) => state.Lords?.Council is { } council
        ? council.GrantedTokens.Select(id => new CouncilTokenView
        {
            Id = id,
            Spent = council.SpentTokens.Contains(id),
            UsedTokenThisTurn = council.LastTokenTurn == state.Turn
        }).ToList() : [];
}
