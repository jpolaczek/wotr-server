using Wotr.Contracts;

namespace Wotr.Domain;

public static class DenethorRemoval
{
    private static string Card(MatchState state) => state.Configuration.Kings ? KingsCards03.Denethor : ShadowStrategyCards01.Denethor;
    public static ActionOption? Option(MatchState state, Side actor)
    {
        if (actor != Side.FreePeoples || state.Pending is not { Kind: DecisionKind.Action, Owner: Side.FreePeoples } ||
            !state.Player(Side.Shadow).Table.Contains(Card(state)))
            return null;
        var anyDie = state.Configuration.Kings ? KingsDenethorRules.AnyDie(state) : DenethorRules.AnyDie(state);
        var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available &&
            (anyDie || d.Face == DieFace.WillOfTheWest)).Select(d => d.Id).ToList();
        return dice.Count == 0 ? null : new() { Kind = CommandKind.DiscardDenethor, EligibleDice = dice };
    }

    public static bool Valid(MatchState state, Side actor, CommandPayload payload) =>
        payload.RingId == "" && Option(state, actor)?.EligibleDice.Contains(payload.DieId) == true;

    public static void Apply(MatchState state, CommandPayload payload)
    {
        CharacterRules.PayAny(state, Side.FreePeoples, payload.DieId);
        TableCardRules.Discard(state, Side.Shadow, Card(state));
        state.Events.Add(new() { Revision = state.Revision, Kind = "DenethorRemoved", Text = "Free Peoples spent an action to discard Denethor’s Folly." });
        TurnFlow.FinishAction(state, Side.FreePeoples);
    }
}
