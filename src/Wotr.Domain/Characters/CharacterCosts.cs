using Wotr.Contracts;

namespace Wotr.Domain;

public static class CharacterCosts
{
    public static bool CanPayArmy(MatchState state, Side actor, CommandPayload payload) =>
        LordsArmyCosts.Handles(payload.AbilityId) ? LordsArmyCosts.ValidArmy(state, actor, payload) :
            CanPay(state, actor, payload.DieId, payload.MovementCost, payload.AbilityId);

    public static void PayArmy(MatchState state, Side actor, CommandPayload payload)
    {
        if (LordsArmyCosts.Handles(payload.AbilityId))
            LordsArmyCosts.Pay(state, actor, payload.DieId, payload.AbilityId);
        else
            Pay(state, actor, payload.DieId, payload.MovementCost, payload.AbilityId);
    }

    public static bool CanPay(MatchState state, Side actor, string dieId, DieFace face, string ability = "")
    {
        var die = DiceRules.AvailableDie(state, actor, dieId);
        if (die is null)
            return false;
        if (string.IsNullOrEmpty(ability))
            return DiceRules.CanPay(die, face);
        if (ability == KingsPalantir.Conversion)
            return face == DieFace.Event && KingsPalantir.CanConvert(state, actor, die);
        return ability == CharacterRules.MouthArmy && actor == Side.Shadow && face == DieFace.Army &&
            CharacterRules.InPlay(state, CharacterRules.Mouth) is { } mouth && mouth.LastArmyConversionTurn != state.Turn &&
            DiceRules.CanPay(die, DieFace.Muster);
    }

    public static void Pay(MatchState state, Side actor, string dieId, DieFace face, string ability = "")
    {
        if (!CanPay(state, actor, dieId, face, ability))
            throw new InvalidOperationException("The action cost must be validated before payment.");
        if (ability == KingsPalantir.Conversion)
        {
            KingsPalantir.Pay(state, actor, dieId);
            return;
        }
        DiceRules.Pay(state, actor, dieId, string.IsNullOrEmpty(ability) ? face : DieFace.Muster);
        if (!string.IsNullOrEmpty(ability))
        {
            CharacterRules.InPlay(state, CharacterRules.Mouth)!.LastArmyConversionTurn = state.Turn;
            state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterAbilityUsed", Text = "The Mouth of Sauron used Messenger of the Dark Tower." });
        }
    }

    public static IEnumerable<MovementCostOption> ArmyOptions(MatchState state, Side actor, int armyMoves)
    {
        foreach (var option in LordsArmyCosts.Options(state, actor))
            yield return option;
        foreach (var die in state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available))
        {
            foreach (var face in new[] { DieFace.Army, DieFace.Character })
                if (DiceRules.CanPay(die, face))
                    yield return new() { DieId = die.Id, Face = face, MaximumMoves = face == DieFace.Army ? armyMoves : 1, RequiresCommander = face == DieFace.Character };
            // An Army/Muster die already pays Army without consuming the once-per-turn ability.
            if (!DiceRules.CanPay(die, DieFace.Army) && CanPay(state, actor, die.Id, DieFace.Army, CharacterRules.MouthArmy))
                yield return new() { DieId = die.Id, Face = DieFace.Army, AbilityId = CharacterRules.MouthArmy, MaximumMoves = armyMoves };
        }
    }
}
