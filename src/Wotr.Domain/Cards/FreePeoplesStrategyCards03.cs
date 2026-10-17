namespace Wotr.Domain;

public static class FreePeoplesStrategyCards03
{
    public const string CirdansShips = "base.fp.strategy.13";
    public const string GuardsOfCitadel = "base.fp.strategy.14";
    public const string CelebornsGaladhrim = "base.fp.strategy.15";
    public const string RidersOfTheoden = "base.fp.strategy.16";
    public const string Grimbeorn = "base.fp.strategy.17";
    public const string Imrahil = "base.fp.strategy.18";

    public static IEnumerable<EffectDefinition> Programs => [
        new(CirdansShips, [new CoastalArmyRecruitStep(), new RecruitEffectStep("elves", "", Count: 2)]),
        new(GuardsOfCitadel, [new RecruitEffectStep("gondor", "minas-tirith"), new RecruitEffectStep("gondor", "minas-tirith", true)]),
        new(RidersOfTheoden, [new RohanCompanionRecruitStep(), new RecruitEffectStep("rohan", ""), new RecruitEffectStep("rohan", "", true)]),
        new(Grimbeorn, [new RecruitEffectStep("north", "carrock"), new RecruitEffectStep("north", "carrock", true)]),
        new(Imrahil, [new RecruitEffectStep("gondor", "dol-amroth"), new RecruitEffectStep("gondor", "dol-amroth", true)]),
        new("combat.charge", [new ChargeStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(CirdansShips, CirdansShips, new NationAtWarCondition("elves")),
        new(GuardsOfCitadel, GuardsOfCitadel, new UnconditionalEvent()),
        new(RidersOfTheoden, RidersOfTheoden, new UnconditionalEvent()),
        new(Grimbeorn, Grimbeorn, new UnconditionalEvent()),
        new(Imrahil, Imrahil, new UnconditionalEvent())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(CirdansShips, new EliteInBattleCondition(), [new(2, "combat.charge")]),
        new(GuardsOfCitadel, new EliteInBattleCondition(), [new(2, "combat.charge")]),
        new(CelebornsGaladhrim, new UnconditionalCombat(), [new(3, "combat.daylight")]),
        new(RidersOfTheoden, new UnconditionalCombat(), [new(3, "combat.daylight")]),
        new(Grimbeorn, new ScoutsCondition(), [new(1, "combat.scouts")]),
        new(Imrahil, new UnconditionalCombat(), [new(6, "combat.shield-wall")])
    ];
}
