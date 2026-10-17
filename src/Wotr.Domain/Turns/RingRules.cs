using Wotr.Contracts;

namespace Wotr.Domain;

public static class RingRules
{
    public static bool WithinLimit(MatchState state, Side actor) =>
        !state.Player(actor).RingUsedThisTurn && KingsRulerTokens.RingAllowed(state, actor);

    public static bool CanChange(DieState die, Side actor) => die.Owner == actor &&
        die.Location == DieLocation.Available && die.Face != DieFace.Eye && die.Special is null;

    public static IReadOnlyList<DieFace> Faces(Side actor) => actor == Side.FreePeoples
        ? [DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.ArmyMuster, DieFace.Event]
        : [DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.ArmyMuster, DieFace.Event, DieFace.Eye];

    public static string? Validate(MatchState state, Side actor, CommandPayload payload)
    {
        if (!WithinLimit(state, actor))
            return "IllegalChoice";
        if (!state.Rings.Any(r => r.Id == payload.RingId && r.Owner == actor))
            return "InsufficientResources";
        var die = DiceRules.AvailableDie(state, actor, payload.DieId);
        if (die is null || !CanChange(die, actor))
            return "IllegalChoice";
        return payload.Face.HasValue && payload.Face != die.Face && Faces(actor).Contains(payload.Face.Value) ? null : "IllegalChoice";
    }

    public static void Apply(MatchState state, Side actor, CommandPayload payload)
    {
        var die = DiceRules.AvailableDie(state, actor, payload.DieId)!;
        die.Face = payload.Face!.Value;
        var ring = state.Rings.Single(r => r.Id == payload.RingId);
        ring.Owner = actor == Side.FreePeoples ? Side.Shadow : null;
        state.Player(actor).RingUsedThisTurn = true;
        KingsRulerTokens.RecordRing(state, actor);
        state.Player(actor).RingActionPending = true;
        if (die.Face == DieFace.Eye)
        {
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "elven-ring";
        }
        state.Events.Add(new()
        {
            Revision = state.Revision,
            Kind = "RingUsed",
            Text = $"{actor} changed a die to {die.Face}."
        });
        if (TurnFlow.Available(state, actor) == 0)
        {
            state.Player(actor).RingActionPending = false;
            TurnFlow.SelectNext(state, TurnFlow.Other(actor));
        }
        // Otherwise keep the same action decision: changing the die is not an action.
    }
}
