using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowStrategyCards04
{
    public const string MistyMountains = "base.sh.strategy.19";
    public const string OrcsMultiplying = "base.sh.strategy.20";
    public const string Horde = "base.sh.strategy.21";
    public const string Monsters = "base.sh.strategy.22";
    public const string Musterings = "base.sh.strategy.23";
    public const string Pits = "base.sh.strategy.24";

    public static IEnumerable<EffectDefinition> Programs => [
        new(MistyMountains, [new RecruitmentBatchStep(new[] { "mount-gram", "moria" }.SelectMany(region => new RecruitmentQuota[]
        {
            new("sauron", region, [UnitKind.Regular, UnitKind.Elite], 2),
            new("sauron", region, [UnitKind.Nazgul], 1)
        }).ToArray(), 1)]),
        new(OrcsMultiplying, [new RecruitmentBatchStep([
            new("sauron", "dol-guldur", [UnitKind.Regular], 3),
            new("sauron", "mount-gundabad", [UnitKind.Regular], 3)])]),
        new(Horde, [new RecruitmentBatchStep(new[] { "east-rhun", "south-rhun", "khand", "far-harad" }
            .Select(region => new RecruitmentQuota("southrons", region, [UnitKind.Regular], 5)).ToArray(), 1)]),
        new(Monsters, [new RecruitmentBatchStep([
            new("sauron", "angmar", [UnitKind.Regular], 1),
            new("sauron", "ettenmoors", [UnitKind.Regular], 1),
            new("sauron", "weather-hills", [UnitKind.Regular], 1),
            new("sauron", "trollshaws", [UnitKind.Elite], 1)])]),
        new(Musterings, [new RecruitmentBatchStep([
            new("southrons", "gorgoroth", [UnitKind.Regular], 5),
            new("sauron", "nurn", [UnitKind.Regular], 5)])]),
        new(Pits, [new NationalRecruitmentStep("sauron", true, 2, 3)]),
        new("combat.desperate-battle", [new BothArmiesModifierStep(1)])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(MistyMountains, MistyMountains, new UnconditionalEvent()),
        new(OrcsMultiplying, OrcsMultiplying, new UnconditionalEvent()),
        new(Horde, Horde, new NationAtWarCondition("southrons")),
        new(Monsters, Monsters, new UnconditionalEvent()),
        new(Musterings, Musterings, new ShadowAtWarCondition()),
        new(Pits, Pits, new NationAtWarCondition("sauron"))
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(MistyMountains, new UnconditionalCombat(), [new(7, "combat.onslaught")]),
        new(OrcsMultiplying, new UnconditionalCombat(), [new(7, "combat.onslaught")]),
        new(Horde, new UnconditionalCombat(), [new(3, "combat.deadly-strife")]),
        new(Monsters, new UnconditionalCombat(), [new(3, "combat.desperate-battle")]),
        new(Musterings, new UnconditionalCombat(), [new(3, "combat.desperate-battle")]),
        new(Pits, new UnconditionalCombat(), [new(3, "combat.desperate-battle")])
    ];
}
