using Wotr.Contracts;

namespace Wotr.Domain;

public static class BalrogEventTravel
{
    public static bool Active(MatchState state, Side actor) => actor == Side.Shadow &&
        state.Pending is { Kind: DecisionKind.CharacterEvent, Owner: Side.Shadow } &&
        EffectRuntime.Current(state)?.Effect is { Wait: EffectWait.BalrogTravel };

    public static TravelOption? Travel(MatchState state) => CharacterRules.InPlay(state, LordsCharacters.Balrog) is { } balrog
        ? ShadowTravel.Options(state).SingleOrDefault(o => o.Id == balrog.Id) : null;

    public static ActionOption Option(MatchState state)
    {
        var travel = Travel(state)!;
        return new()
        {
            Kind = CommandKind.MoveShadowCharacters, Travel = [travel], EligibleIds = [travel.Id],
            Targets = travel.Targets, AllowEmptySelection = true
        };
    }

    public static bool Valid(MatchState state, CommandPayload payload) => payload.DieId == "" && payload.AbilityId == "" &&
        (payload.CompanionMoves.Count == 0 || payload.CompanionMoves.Count == 1 &&
            payload.CompanionMoves[0].Ids.SequenceEqual([Travel(state)!.Id]) &&
            ShadowTravel.Plan(state, payload.CompanionMoves) is not null);

    public static void Complete(MatchState state, CommandPayload payload)
    {
        if (payload.CompanionMoves.Count > 0)
            ShadowTravel.Apply(state, ShadowTravel.Plan(state, payload.CompanionMoves)!);
        EffectRuntime.Current(state)!.Effect!.Wait = EffectWait.None;
        state.Pending = null;
    }
}
