namespace Wotr.Domain;

// Direct paid events whose pending choice is reviewed for reconstruction from
// public facts: nothing before it selects or reveals anything, and every later
// step runs through the engine. A program whose shape changes leaves this list
// until it is reviewed again.
public static class DirectEventChoices
{
    public static bool IsReviewed(string cardId, int nextStep, EffectDefinition program) =>
        cardId == KingsCards02.Service
            ? nextStep is 1 or 2 && program.Steps is [ServiceSovereignStep, ServiceRecruitStep, ServiceCorruptionStep]
            : nextStep == 1 && OpensWithReviewedChoice(cardId, program);

    private static bool OpensWithReviewedChoice(string cardId, EffectDefinition program) => cardId switch
    {
        FreePeoplesStrategyCards04.KindredOfGlorfindel => RecruitsThenDraws(program, "rivendell"),
        FreePeoplesStrategyCards03.CelebornsGaladhrim => RecruitsThenDraws(program, "lorien"),
        FreePeoplesStrategyCards01.SpiritOfMordor => program.Steps is [ArmyEventDamageStep { Dice: 5, MixedNations: true }],
        FreePeoplesStrategyCards01.FaramirsRangers => program.Steps is
            [ArmyEventDamageStep { Dice: 3, MixedNations: false }, RangersRecruitStep,
                RecruitEffectStep { Nation: "gondor", Region: "", Leader: false },
                RecruitEffectStep { Nation: "gondor", Region: "", Leader: true }],
        KingsCards02.Villages => program.Steps is [VillagesRecruitStep],
        KingsCards04.Enemy => program.Steps is [EnemySovereignStep, EnemyArmyStep],
        FateCards.Dwarves => program.Steps is
            [FateDaleRecruitStep { Nation: "dwarves" }, DrawCardsStep { Deck: "strategy", Count: 1 }],
        ShadowStrategyCards03.HillTrolls => program.Steps is [HillTrollsStep],
        ShadowStrategyCards03.ManyKings => program.Steps is
            [NationalRecruitmentStep { Nation: "southrons", StrongholdsOnly: false, PerRegion: 2, Regions: 3 }],
        ShadowStrategyCards03.NewPower => program.Steps is [RecruitmentBatchStep { MaximumRegions: 0 }],
        ShadowStrategyCards04.MistyMountains or ShadowStrategyCards04.Horde => program.Steps is
            [RecruitmentBatchStep { MaximumRegions: 1 }],
        _ => false
    };

    private static bool RecruitsThenDraws(EffectDefinition program, string region) => program.Steps is
        [RecruitEffectStep { Nation: "elves", Leader: false, Count: 1, RequiredKind: null } recruit,
            DrawCardsStep { Deck: "strategy", Count: 1 }] && recruit.Region == region;
}
