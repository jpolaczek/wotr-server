namespace Wotr.Domain;

public static class FreePeoplesCharacterCards03
{
    public const string Mirror = "base.fp.character.13";
    public const string Challenge = "base.fp.character.14";
    public const string Gwaihir = "base.fp.character.15";
    public const string Swifter = "base.fp.character.16";
    public const string ThereAndBack = "base.fp.character.17";
    public const string Eagles = "base.fp.character.18";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Mirror, [new MirrorDieStep(), new MirrorHealingStep()]),
        new(Challenge, [new ChallengeOfTheKingStep()]),
        new(Gwaihir, [new CompanionJourneyStep(4)]),
        new(Swifter, [new CompanionJourneyStep(Bonus: 2)]),
        new(ThereAndBack, [new SeparateCompanionsStep(1), new ThereAndBackPoliticsStep()]),
        new(Eagles, [new EaglesAttackStep(), new EaglesRetreatStep()]),
        new("combat.sudden-strike", [new SuddenStrikeStep()]),
        new("combat.brave-stand", [new BraveStandStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(Mirror, Mirror, new UnconditionalEvent()),
        new(Challenge, Challenge, new ChallengeOfTheKingCondition()),
        new(Gwaihir, Gwaihir, new UnconditionalEvent()),
        new(Swifter, Swifter, new UnconditionalEvent()),
        new(ThereAndBack, ThereAndBack, new UnconditionalEvent()),
        new(Eagles, Eagles, new EaglesCondition())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Mirror, new LeaderOrCompanionInBattle(), [new(6, "combat.heroic-death")]),
        new(Challenge, new LeaderOrCompanionInBattle(), [new(2, "combat.sudden-strike")]),
        new(Gwaihir, new CompanionInBattle(), [new(0, "combat.daring-defiance")]),
        new(Swifter, new CompanionInBattle(), [new(0, "combat.daring-defiance")]),
        new(ThereAndBack, new CompanionInBattle(), [new(3, "combat.brave-stand")]),
        new(Eagles, new LeaderOrCompanionInBattle(), [new(6, "combat.heroic-death")])
    ];
}
