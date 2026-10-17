using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsArmyCosts
{
    public const string UseBalrog = "use-balrog";
    public static bool Handles(string ability) => ability is UseBalrog or LordsCharacters.GreatSorcery;

    private static CharacterState? Source(MatchState state, string ability) => CharacterRules.InPlay(state,
        ability == UseBalrog ? LordsCharacters.Balrog : LordsCharacters.Mouth);

    public static bool CanPay(MatchState state, Side actor, string dieId, string ability)
    {
        if (!state.Configuration.Lords || state.Lords is null || actor != Side.Shadow || !Handles(ability) ||
            Source(state, ability) is not { } character || DiceRules.AvailableDie(state, actor, dieId) is not { } die)
            return false;
        return ability == UseBalrog ? character.Level == 2 && LordsCharacterAbilities.BalrogDie(die) :
            character.LastArmyConversionTurn != state.Turn && DiceRules.CanPay(die, DieFace.Muster);
    }

    public static bool ValidArmy(MatchState state, Side actor, CommandPayload payload) =>
        CanPay(state, actor, payload.DieId, payload.AbilityId) && payload.MovementCost == DieFace.Character &&
        payload.Moves.Count == 1 && payload.Moves[0].CharacterIds.Contains(Source(state, payload.AbilityId)!.Id);

    public static bool ValidTravel(MatchState state, Side actor, CommandPayload payload) =>
        payload.AbilityId == UseBalrog && CanPay(state, actor, payload.DieId, payload.AbilityId) &&
        payload.CompanionMoves.Count == 1 && payload.CompanionMoves[0].Ids.SequenceEqual([Source(state, UseBalrog)!.Id]);

    // Army movement has already been applied when its cost is committed; do not revalidate the old region here.
    public static void Pay(MatchState state, Side actor, string dieId, string ability)
    {
        if (!CanPay(state, actor, dieId, ability))
            throw new InvalidOperationException("A Lords army ability must be validated before payment.");
        DiceRules.Pay(state, actor, dieId, ability == UseBalrog ? DieFace.UseBalrog : DieFace.Muster);
        if (ability == LordsCharacters.GreatSorcery)
            Source(state, ability)!.LastArmyConversionTurn = state.Turn;
        state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterAbilityUsed", Text = $"{Source(state, ability)!.Id}: {ability}." });
    }

    public static IEnumerable<MovementCostOption> Options(MatchState state, Side actor)
    {
        if (!state.Configuration.Lords || state.Lords is null || actor != Side.Shadow)
            yield break;
        foreach (var ability in new[] { UseBalrog, LordsCharacters.GreatSorcery })
            foreach (var die in state.Dice.Where(d => CanPay(state, actor, d.Id, ability)))
                yield return new()
                {
                    DieId = die.Id, Face = DieFace.Character, AbilityId = ability,
                    MaximumMoves = 1, RequiresCommander = true, RequiredCharacterId = Source(state, ability)!.Id
                };
    }
}
