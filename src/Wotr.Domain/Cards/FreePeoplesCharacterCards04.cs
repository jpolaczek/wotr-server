namespace Wotr.Domain;

public static class FreePeoplesCharacterCards04
{
    public const string Treebeard = "base.fp.character.19";
    public const string Huorns = "base.fp.character.20";
    public const string Entmoot = "base.fp.character.21";
    public const string DeadMen = "base.fp.character.22";
    public const string Stewards = "base.fp.character.23";
    public const string GreyCompany = "base.fp.character.24";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Treebeard, [new EntsAttackStep(), new EntsFollowUpStep()]),
        new(Huorns, [new EntsAttackStep(), new EntsFollowUpStep()]),
        new(Entmoot, [new EntsAttackStep(), new EntsFollowUpStep()]),
        new(DeadMen, [new DeadMenJourneyStep(), new DeadMenAttackStep(), new DeadMenRetreatStep(), new DeadMenRecruitStep()]),
        new(Stewards, [new StewardsRegionStep(), new RecruitEffectStep("gondor", ""), new DrawCardsStep("strategy", 2)]),
        new(GreyCompany, [new GreyCompanyStep(), new DrawCardsStep("strategy", 2)]),
        new("combat.ents-rage", [new CombatModifierStep(2, 0)]),
        new("combat.huorn-dark", [new LimitCombatDiceStep(2)]),
        new("combat.nameless-wood", [new AdditionalCombatHitsStep(2)])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(Treebeard, Treebeard, new EntsCondition()),
        new(Huorns, Huorns, new EntsCondition()),
        new(Entmoot, Entmoot, new EntsCondition()),
        new(DeadMen, DeadMen, new DeadMenCondition()),
        new(Stewards, Stewards, new StewardsCondition()),
        new(GreyCompany, GreyCompany, new GreyCompanyCondition())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Treebeard, new RohanDefenderCondition(), [new(3, "combat.ents-rage")]),
        new(Huorns, new RohanDefenderCondition(), [new(3, "combat.huorn-dark")]),
        new(Entmoot, new RohanDefenderCondition(), [new(5, "combat.nameless-wood")]),
        new(DeadMen, new LeaderOrCompanionInBattle(), [new(2, "combat.sudden-strike")]),
        new(Stewards, new CompanionInBattle(), [new(3, "combat.brave-stand")]),
        new(GreyCompany, new CompanionInBattle(), [new(3, "combat.brave-stand")])
    ];
}
