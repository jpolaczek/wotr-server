using Wotr.Contracts;

namespace Wotr.Domain;

public static class PalantirRemoval
{
    public static bool Valid(MatchState state, Side actor, CommandPayload payload)
    {
        return actor == Side.FreePeoples && state.Pending is { Kind: DecisionKind.Action, Owner: Side.FreePeoples } &&
            state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards04.Palantir) && TableRingCost.Valid(state, actor, payload);
    }

    public static ActionOption? Option(MatchState state, Side actor) =>
        actor == Side.FreePeoples && state.Pending?.Kind == DecisionKind.Action &&
        state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards04.Palantir)
            ? TableRingCost.Option(state, CommandKind.DiscardPalantir) : null;

    public static void Apply(MatchState state, CommandPayload payload)
    {
        TableRingCost.Pay(state, payload);
        TableCardRules.Discard(state, Side.Shadow, ShadowCharacterCards04.Palantir);
        state.Events.Add(new() { Revision = state.Revision, Kind = "PalantirRemoved", Text = "Free Peoples spent an action to discard the Palantír of Orthanc." });
        TurnFlow.FinishAction(state, Side.FreePeoples);
    }
}
