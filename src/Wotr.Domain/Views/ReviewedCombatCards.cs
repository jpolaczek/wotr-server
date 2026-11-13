using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// Combat-card programs whose decisions are reconstructed from public battle facts.
// A card qualifies by its single printed effect, so another card with the same
// effect needs no review of its own.
public static class ReviewedCombatCards
{
    public const string RelentlessAssaultProgram = "combat.relentless-assault";
    public const string GreatHostProgram = "combat.great-host";
    public const string ScoutsProgram = "combat.scouts";
    public const string OnslaughtProgram = "combat.onslaught";
    public const string HeroicDeathProgram = "combat.heroic-death";
    public const string MightyAttackProgram = "combat.mighty-attack";
    public const string OrderlyWithdrawalProgram = "combat.orderly-withdrawal";

    public static bool IsRelentlessAssault(string cardId) => IsShadowCard(cardId) &&
        HasOnlyEffect(cardId, 3, RelentlessAssaultProgram);

    public static bool IsGreatHost(string cardId) => IsShadowCard(cardId) &&
        HasOnlyEffect(cardId, 7, GreatHostProgram);

    public static bool IsOnslaught(string cardId) => IsShadowCard(cardId) &&
        HasOnlyEffect(cardId, 7, OnslaughtProgram);

    public static bool IsHeroicDeath(string cardId) => CardCatalog.All.TryGetValue(cardId, out var card) &&
        card.Side == Side.FreePeoples && HasOnlyEffect(cardId, 6, HeroicDeathProgram);

    public static bool IsMightyAttack(string cardId) => CardCatalog.All.TryGetValue(cardId, out var card) &&
        card.Side == Side.FreePeoples && HasOnlyEffect(cardId, 4, MightyAttackProgram);

    public static bool IsOrderlyWithdrawal(string cardId) => CardCatalog.All.TryGetValue(cardId, out var card) &&
        card.Side == Side.FreePeoples && HasOnlyEffect(cardId, 6, OrderlyWithdrawalProgram);

    public static bool IsScouts(string cardId) => CardCatalog.All.TryGetValue(cardId, out var card) &&
        card.Side == Side.FreePeoples && HasOnlyEffect(cardId, 1, ScoutsProgram);

    // Great Host deals its one hit only while its army outnumbers the enemy two to one.
    public static bool GreatHostHits(MatchState state, BattleState battle, Side owner)
    {
        var own = BattleRules.Units(state, battle.Force(owner)).Count(BoardRules.IsArmyUnit);
        var enemy = BattleRules.Units(state, battle.Opponent(owner)).Count(BoardRules.IsArmyUnit);
        return enemy > 0 && own >= 2 * enemy;
    }

    private static bool IsShadowCard(string cardId) =>
        CardCatalog.All.TryGetValue(cardId, out var card) && card.Side == Side.Shadow;

    private static bool HasOnlyEffect(string cardId, int initiative, string programId) =>
        EffectCatalog.All.Combats.TryGetValue(cardId, out var combat) && combat.Effects.Count == 1 &&
        combat.Effects[0].Initiative == initiative && combat.Effects[0].ProgramId == programId;
}
