using Wotr.Contracts;

namespace Wotr.Domain;

public static class FreePeoplesStrategyCards04
{
    public const string KingBrandsMen = "base.fp.strategy.19";
    public const string SwordsInEriador = "base.fp.strategy.20";
    public const string KindredOfGlorfindel = "base.fp.strategy.21";
    public const string DainIronfoot = "base.fp.strategy.22";
    public const string Eomer = "base.fp.strategy.23";
    public const string ThranduilsArchers = "base.fp.strategy.24";

    public static IEnumerable<EffectDefinition> Programs => [
        new(KingBrandsMen, [new RecruitEffectStep("north", "dale", Count: 2, RequiredKind: UnitKind.Regular), new DrawCardsStep("strategy")]),
        new(SwordsInEriador, [new RecruitTogetherStep([new("north", "the-shire"), new("dwarves", "ered-luin")]), new DrawCardsStep("strategy")]),
        new(KindredOfGlorfindel, [new RecruitEffectStep("elves", "rivendell"), new DrawCardsStep("strategy")]),
        new(DainIronfoot, [new RecruitEffectStep("dwarves", "erebor"), new RecruitEffectStep("dwarves", "erebor", true)]),
        new(Eomer, [new FreeRohanSettlementRecruitStep(), new RecruitEffectStep("rohan", ""), new RecruitEffectStep("rohan", "", true)]),
        new(ThranduilsArchers, [new RecruitEffectStep("elves", "woodland-realm"), new DrawCardsStep("strategy")]),
        new("combat.valour", [new CombatModifierStep(1, 0)])
    ];

    public static IEnumerable<EventDefinition> Events => new[] { KingBrandsMen, SwordsInEriador, KindredOfGlorfindel, DainIronfoot, Eomer, ThranduilsArchers }
        .Select(id => new EventDefinition(id, id, new UnconditionalEvent()));

    public static IEnumerable<CombatDefinition> Combats => [
        new(KingBrandsMen, new UnconditionalCombat(), [new(6, "combat.shield-wall")]),
        new(SwordsInEriador, new FreeNationBattleCondition(), [new(3, "combat.advantageous-position")]),
        new(KindredOfGlorfindel, new FreeNationBattleCondition(), [new(3, "combat.advantageous-position")]),
        new(DainIronfoot, new EliteInBattleCondition(), [new(3, "combat.valour")]),
        new(Eomer, new EliteInBattleCondition(), [new(3, "combat.valour")]),
        new(ThranduilsArchers, new EliteInBattleCondition(), [new(3, "combat.valour")])
    ];
}
