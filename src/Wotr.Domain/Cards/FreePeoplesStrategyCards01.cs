namespace Wotr.Domain;

public static class FreePeoplesStrategyCards01
{
    public const string LastBattle = "base.fp.strategy.01";
    public const string PowerTooGreat = "base.fp.strategy.02";
    public const string TomBombadil = "base.fp.strategy.03";
    public const string BookOfMazarbul = "base.fp.strategy.04";
    public const string SpiritOfMordor = "base.fp.strategy.05";
    public const string FaramirsRangers = "base.fp.strategy.06";

    public static IEnumerable<EffectDefinition> Programs => [
        new(LastBattle, [new LastingEffectStep(EffectModifier.PreventFellowshipHuntDie,
            EffectDuration.UntilRemoved, EffectExpiry.AragornArmyOutsideFreeNation)]),
        new(PowerTooGreat, [new ProtectRegionsStep("elves", ["lorien", "rivendell", "grey-havens"])]),
        new(TomBombadil, [new ProtectRegionsStep("north", ["old-forest", "the-shire", "buckland"])]),
        new(BookOfMazarbul, [new OutsideCompanionJourneyStep(), new CompanionNationWarStep("dwarves", ["erebor", "ered-luin"])]),
        new(SpiritOfMordor, [new ArmyEventDamageStep(5, true)]),
        new(FaramirsRangers, [new ArmyEventDamageStep(3, false), new RangersRecruitStep(),
            new RecruitEffectStep("gondor", ""), new RecruitEffectStep("gondor", "", true)]),
        new("combat.daylight", [new LimitCombatDiceStep(3)]),
        new("combat.no-quarter", [new AdditionalCombatHitsStep(1)]),
        new("combat.advantageous-position", [new CombatModifierStep(-1, 0, true)]),
        new("combat.scouts", [new ScoutsStep()]),
        new("combat.shield-wall", [new ShieldWallStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(LastBattle, LastBattle, new LastBattleCondition(), true),
        new(PowerTooGreat, PowerTooGreat, new UnconditionalEvent(), true),
        new(TomBombadil, TomBombadil, new UnconditionalEvent(), true),
        new(BookOfMazarbul, BookOfMazarbul, new UnconditionalEvent()),
        new(SpiritOfMordor, SpiritOfMordor, new MixedShadowArmyCondition()),
        new(FaramirsRangers, FaramirsRangers, new UnconditionalEvent())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(LastBattle, new UnconditionalCombat(), [new(3, "combat.daylight")]),
        new(PowerTooGreat, new UnconditionalCombat(), [new(5, "combat.no-quarter")]),
        new(TomBombadil, new FreeNationBattleCondition(), [new(3, "combat.advantageous-position")]),
        new(BookOfMazarbul, new FreeNationBattleCondition(), [new(3, "combat.advantageous-position")]),
        new(SpiritOfMordor, new ScoutsCondition(), [new(1, "combat.scouts")]),
        new(FaramirsRangers, new UnconditionalCombat(), [new(6, "combat.shield-wall")])
    ];
}
