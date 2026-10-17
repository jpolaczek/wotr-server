namespace Wotr.Domain;

public static class FreePeoplesStrategyCards02
{
    public const string FearFireFoes = "base.fp.strategy.07";
    public const string WisdomOfElrond = "base.fp.strategy.08";
    public const string RedArrow = "base.fp.strategy.09";
    public const string HelpUnlookedFor = "base.fp.strategy.10";
    public const string PathsOfWoses = "base.fp.strategy.11";
    public const string ThroughDayNight = "base.fp.strategy.12";

    public static IEnumerable<EffectDefinition> Programs => [
        new(FearFireFoes, [new OutsideCompanionJourneyStep(), new CompanionNationWarStep("north", ["the-shire", "bree"])]),
        new(RedArrow, [new AdvanceFixedNationStep("rohan"), new RecruitEffectStep("rohan", "edoras"), new RecruitEffectStep("rohan", "edoras", true)]),
        new(HelpUnlookedFor, [new ArmyOrderStep(ArmyEventOrder.ReliefAttack)]),
        new(PathsOfWoses, [new ArmyOrderStep(ArmyEventOrder.RohanPassage)]),
        new(ThroughDayNight, [new ArmyOrderStep(ArmyEventOrder.CompanionMarch)]),
        new("combat.confusion", [new ConfusionStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(FearFireFoes, FearFireFoes, new UnconditionalEvent()),
        new(RedArrow, RedArrow, new ActiveNationCondition("gondor")),
        new(HelpUnlookedFor, HelpUnlookedFor, new ArmyOrderCondition(ArmyEventOrder.ReliefAttack)),
        new(PathsOfWoses, PathsOfWoses, new ArmyOrderCondition(ArmyEventOrder.RohanPassage)),
        new(ThroughDayNight, ThroughDayNight, new ArmyOrderCondition(ArmyEventOrder.CompanionMarch))
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(FearFireFoes, new UnconditionalCombat(), [new(6, "combat.shield-wall")]),
        new(WisdomOfElrond, new UnconditionalCombat(), [new(4, "combat.confusion")]),
        new(RedArrow, new ScoutsCondition(), [new(1, "combat.scouts")]),
        new(HelpUnlookedFor, new UnconditionalCombat(), [new(5, "combat.no-quarter")]),
        new(PathsOfWoses, new LeaderOrCompanionInBattle(), [new(2, "combat.sudden-strike")]),
        new(ThroughDayNight, new UnconditionalCombat(), [new(4, "combat.confusion")])
    ];
}
