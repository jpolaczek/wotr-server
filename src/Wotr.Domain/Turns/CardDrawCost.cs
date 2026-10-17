using Wotr.Contracts;

namespace Wotr.Domain;

public static class CardDrawCost
{
    public static List<EventCostOption> Options(MatchState state, Side actor) => state.Dice
        .Where(d => d.Owner == actor && d.Location == DieLocation.Available && DiceRules.CanDraw(d))
        .Select(d => new EventCostOption { DieId = d.Id, Face = d.Face == DieFace.CardDraw ? DieFace.CardDraw : DieFace.Event })
        .Concat(KingsPalantir.Costs(state, actor)).ToList();

    public static bool Valid(MatchState state, Side actor, CommandPayload payload) =>
        Options(state, actor).Any(c => c.DieId == payload.DieId && c.AbilityId == payload.AbilityId);

    public static void Pay(MatchState state, Side actor, CommandPayload payload)
    {
        if (payload.AbilityId == KingsPalantir.Conversion)
            KingsPalantir.Pay(state, actor, payload.DieId);
        else
        {
            var die = DiceRules.AvailableDie(state, actor, payload.DieId)!;
            DiceRules.Pay(state, actor, die.Id, die.Face == DieFace.CardDraw ? DieFace.CardDraw : DieFace.Event);
        }
    }
}
