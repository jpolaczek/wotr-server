using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowStrategyCards02
{
    public const string Gather = "base.sh.strategy.07";
    public const string Lengthens = "base.sh.strategy.08";
    public const string Moving = "base.sh.strategy.09";
    public const string Corsairs = "base.sh.strategy.10";
    public const string Rage = "base.sh.strategy.11";
    public const string Return = "base.sh.strategy.12";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Gather, [new ArmyOrderStep(ArmyEventOrder.ShadowsGather)]),
        new(Lengthens, [new ArmyOrderStep(ArmyEventOrder.ShadowLengthens)]),
        new(Moving, [new ArmyOrderStep(ArmyEventOrder.ShadowMoving)]),
        new(Corsairs, [new ArmyOrderStep(ArmyEventOrder.Corsairs), new CorsairBattleStep()]),
        new(Rage, [new DunlendingRegionStep(), new RecruitEffectStep("isengard", "", Count: 2, RequiredKind: UnitKind.Regular), new DunlendingMoveStep()]),
        new(Return, [new WitchKingReturnStep(), new RecruitEffectStep("sauron", "angmar", Count: 2, RequiredKind: UnitKind.Regular),
            new RecruitEffectStep("sauron", "angmar", RequiredKind: UnitKind.Elite)]),
        new("combat.mumakil-combat", [new CombatModifierStep(1, 0)]),
        new("combat.mumakil-hits", [new MumakilHitsStep()]),
        new("combat.swarm-of-bats", [new SwarmOfBatsStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(Gather, Gather, new UnconditionalEvent()),
        new(Lengthens, Lengthens, new UnconditionalEvent()),
        new(Moving, Moving, new ShadowAtWarCondition()),
        new(Corsairs, Corsairs, new NationAtWarCondition("southrons")),
        new(Rage, Rage, new NationAtWarCondition("isengard")),
        new(Return, Return, new MinionPresentCondition(CharacterRules.WitchKing))
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Gather, new MumakilCondition(), [new(3, "combat.mumakil-combat"), new(5, "combat.mumakil-hits")]),
        new(Lengthens, new MumakilCondition(), [new(3, "combat.mumakil-combat"), new(5, "combat.mumakil-hits")]),
        new(Moving, new UnconditionalCombat(), [new(0, "combat.swarm-of-bats")]),
        new(Corsairs, new UnconditionalCombat(), [new(3, "combat.deadly-strife")]),
        new(Rage, new UnconditionalCombat(), [new(3, "combat.relentless-assault")]),
        new(Return, new UnconditionalCombat(), [new(0, "combat.swarm-of-bats")])
    ];
}
