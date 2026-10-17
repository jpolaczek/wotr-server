using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowStrategyCards03
{
    public const string HalfOrcs = "base.sh.strategy.13";
    public const string OlogHai = "base.sh.strategy.14";
    public const string HillTrolls = "base.sh.strategy.15";
    public const string NewPower = "base.sh.strategy.16";
    public const string ManyKings = "base.sh.strategy.17";
    public const string KingRevealed = "base.sh.strategy.18";

    public static IEnumerable<EffectDefinition> Programs => [
        new(HalfOrcs, [new ShadowArmyRecruitStep("isengard"), new RecruitEffectStep("isengard", "")]),
        new(OlogHai, [new ShadowArmyRecruitStep("sauron"), new RecruitEffectStep("sauron", "")]),
        new(HillTrolls, [new HillTrollsStep()]),
        new(NewPower, [new RecruitmentBatchStep([
            new("isengard", "north-dunland", [UnitKind.Regular], 2),
            new("isengard", "south-dunland", [UnitKind.Regular], 2),
            new("isengard", "orthanc", [UnitKind.Regular, UnitKind.Elite], 2)])]),
        new(ManyKings, [new NationalRecruitmentStep("southrons", false, 2, 3)]),
        new(KingRevealed, [new RecruitmentBatchStep([
            new("sauron", "minas-morgul", [UnitKind.Regular], 5),
            new("sauron", "minas-morgul", [UnitKind.Nazgul], 1)])]),
        new("combat.we-come-to-kill", [new WeComeToKillStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(HalfOrcs, HalfOrcs, new NationAtWarCondition("isengard")),
        new(OlogHai, OlogHai, new NationAtWarCondition("sauron")),
        new(HillTrolls, HillTrolls, new NationAtWarCondition("sauron")),
        new(NewPower, NewPower, new MinionPresentCondition(CharacterRules.Saruman)),
        new(ManyKings, ManyKings, new UnconditionalEvent()),
        new(KingRevealed, KingRevealed, new AragornPresentCondition())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(HalfOrcs, new EliteInBattleCondition(), [new(7, "combat.we-come-to-kill")]),
        new(OlogHai, new EliteInBattleCondition(), [new(7, "combat.we-come-to-kill")]),
        new(HillTrolls, new EliteInBattleCondition(), [new(7, "combat.we-come-to-kill")]),
        new(NewPower, new UnconditionalCombat(), [new(7, "combat.great-host")]),
        new(ManyKings, new UnconditionalCombat(), [new(7, "combat.great-host")]),
        new(KingRevealed, new UnconditionalCombat(), [new(3, "combat.relentless-assault")])
    ];
}
