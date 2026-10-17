using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsPalantir
{
    public const string Conversion = "kings.palantir.muster-as-event";
    public static bool CanConvert(MatchState state, Side actor, DieState die) => actor == Side.Shadow &&
        state.Kings is { } kings && kings.PalantirUsedTurn != state.Turn &&
        KingsCardRestrictions.OnTable(state, KingsCards03.Palantir) &&
        die.Owner == actor && die.Location == DieLocation.Available && DiceRules.CanPay(die, DieFace.Muster);

    public static IEnumerable<EventCostOption> Costs(MatchState state, Side actor) => state.Dice
        .Where(d => CanConvert(state, actor, d) && !DiceRules.CanPay(d, DieFace.Event))
        .Select(d => new EventCostOption { DieId = d.Id, Face = DieFace.Event, AbilityId = Conversion });

    public static void Pay(MatchState state, Side actor, string dieId)
    {
        var die = DiceRules.AvailableDie(state, actor, dieId);
        if (die is null || !CanConvert(state, actor, die))
            throw new InvalidOperationException("The Palantír conversion must be validated before payment.");
        DiceRules.Pay(state, actor, dieId, DieFace.Muster);
        state.Kings!.PalantirUsedTurn = state.Turn;
        state.Events.Add(new() { Revision = state.Revision, Kind = "PalantirUsed",
            Text = "The Palantír of Barad-dûr: Shadow used Muster as Event for this turn." });
    }

    public static ActionOption? RemovalOption(MatchState state, Side actor) => actor == Side.FreePeoples &&
        state.Pending is { Kind: DecisionKind.Action, Owner: Side.FreePeoples } &&
        KingsCardRestrictions.OnTable(state, KingsCards03.Palantir)
            ? TableRingCost.Option(state, CommandKind.DiscardBaradPalantir) : null;

    public static bool ValidRemoval(MatchState state, Side actor, CommandPayload payload) =>
        RemovalOption(state, actor) is not null && TableRingCost.Valid(state, actor, payload);

    public static void Remove(MatchState state, CommandPayload payload)
    {
        TableRingCost.Pay(state, payload);
        TableCardRules.Discard(state, Side.Shadow, KingsCards03.Palantir);
        state.Events.Add(new() { Revision = state.Revision, Kind = "PalantirRemoved",
            Text = "Free Peoples spent an action to discard the Palantír of Barad-dûr." });
        TurnFlow.FinishAction(state, Side.FreePeoples);
    }
}
