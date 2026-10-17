using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsFreePeoplesCards01
{
    public const string Stewards = "kings.fp.character.23";
    public const string Aid = "kings.fp.character.25";
    public const string Dark = "kings.fp.character.26";
    public const string Wisdom = "kings.fp.strategy.08";
    public const string Riders = "kings.fp.strategy.16";
    public const string Brand = "kings.fp.strategy.19";
    public const string Banner = "combat.kings-banner";
    public const string GodOfOld = "combat.like-a-god-of-old";
    public static IReadOnlyList<string> Ids { get; } = [Stewards, Aid, Dark, Wisdom, Riders, Brand];

    public static IEnumerable<EffectDefinition> Programs =>
    [
        new(Stewards, [new StewardsRegionStep(), new KingsRecruitOrCleanseStep(SovereignCatalog.Denethor, Stewards + ".recruit"), new DrawCardsStep("strategy", 2)]),
        new(Stewards + ".recruit", [new RecruitEffectStep("gondor", "")]),
        new(Aid, [new KingsCompanionJourneyStep(), new AwakenWithCompanionStep()]),
        new(Dark, [new KingsNationStep(false), new SelectInspectedSovereignStep(), new InspectCorruptionStep(false)]),
        new(Wisdom, [new WisdomBranchStep()]),
        new(WisdomBranchStep.Politics, [new KingsNationStep(true)]),
        new(WisdomBranchStep.Inspect, [new SelectInspectedSovereignStep(), new InspectCorruptionStep(true)]),
        new(Riders, [new KingsNationalCardStep(SovereignCatalog.Theoden, Riders + ".otherwise")]),
        new(Riders + ".otherwise", [new KingsRecruitOrCleanseStep(SovereignCatalog.Theoden, Riders + ".recruit"), new DrawCardsStep("strategy")]),
        new(Riders + ".recruit", [new KingsRohanRecruitStep()]),
        new(Brand, [new KingsNationalCardStep(SovereignCatalog.Brand, Brand + ".otherwise")]),
        new(Brand + ".otherwise", [new KingsRecruitOrCleanseStep(SovereignCatalog.Brand, Brand + ".recruit"), new DrawCardsStep("strategy")]),
        new(Brand + ".recruit", [new RecruitEffectStep("north", "dale", Count: 2, RequiredKind: UnitKind.Regular)]),
        new(Banner, [new KingsBannerStep()]),
        new(GodOfOld, [new GodOfOldStep()])
    ];

    public static IEnumerable<EventDefinition> Events => Ids.Select(id => new EventDefinition(id, id,
        id == Stewards ? new StewardsCondition() : new UnconditionalEvent()));

    public static IEnumerable<CombatDefinition> Combats =>
    [
        new(Stewards, new CompanionInBattle(), [new(3, "combat.brave-stand")]),
        new(Aid, new SovereignInBattle(true), [new(4, Banner)]),
        new(Dark, new SovereignInBattle(), [new(2, GodOfOld)]),
        new(Wisdom, new UnconditionalCombat(), [new(4, "combat.confusion")]),
        new(Riders, new UnconditionalCombat(), [new(3, "combat.daylight")]),
        new(Brand, new UnconditionalCombat(), [new(6, "combat.shield-wall")])
    ];
}
