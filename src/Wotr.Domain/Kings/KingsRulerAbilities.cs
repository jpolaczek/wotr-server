using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsRulerAbilities
{
    public static CharacterState? Awakened(MatchState state, string sovereignId) =>
        SovereignLifecycle.Find(state, sovereignId)?.Status == SovereignStatus.Awakened
            ? state.Characters.Find(c => c.Id == sovereignId && CharacterRules.OnBoard(c)) : null;

    public static bool CanPay(MatchState state, string dieId, DieFace face) =>
        DiceRules.AvailableDie(state, Side.FreePeoples, dieId) is { Source: SovereignCatalog.RulerSource } die && DiceRules.CanPay(die, face);

    public static List<string> Dice(MatchState state, DieFace face) => state.Dice
        .Where(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource && DiceRules.CanPay(d, face))
        .Select(d => d.Id).ToList();

    public static bool FreeRegion(MatchState state, string region) =>
        !BoardRules.HasEnemyArmy(state, region, Side.FreePeoples) && state.Control.GetValueOrDefault(region) != Side.Shadow;
}
