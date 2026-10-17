using Wotr.Contracts;

namespace Wotr.Domain;

public static class TableRingCost
{
    public static List<string> Rings(MatchState state) => state.Player(Side.FreePeoples).RingUsedThisTurn ||
        !KingsRulerTokens.RingAllowed(state, Side.FreePeoples) ? [] :
        state.Rings.Where(r => r.Owner == Side.FreePeoples).Select(r => r.Id).ToList();

    public static bool Valid(MatchState state, Side actor, CommandPayload payload) =>
        DiceRules.AvailableDie(state, actor, payload.DieId) is { } die &&
        (payload.RingId == "" ? die.Face == DieFace.WillOfTheWest : Rings(state).Contains(payload.RingId));

    public static ActionOption? Option(MatchState state, CommandKind kind)
    {
        var rings = Rings(state);
        var dice = state.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available &&
            (d.Face == DieFace.WillOfTheWest || rings.Count > 0)).ToArray();
        return dice.Length == 0 ? null : new()
        {
            Kind = kind, EligibleDice = dice.Select(d => d.Id).ToList(), EligibleIds = rings,
            DiceWithoutRing = dice.Where(d => d.Face == DieFace.WillOfTheWest).Select(d => d.Id).ToList()
        };
    }

    public static void Pay(MatchState state, CommandPayload payload)
    {
        CharacterRules.PayAny(state, Side.FreePeoples, payload.DieId);
        if (payload.RingId == "")
            return;
        state.Rings.Single(r => r.Id == payload.RingId).Owner = Side.Shadow;
        state.Player(Side.FreePeoples).RingUsedThisTurn = true;
        KingsRulerTokens.RecordRing(state, Side.FreePeoples);
    }
}
