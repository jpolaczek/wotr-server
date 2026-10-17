using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsFreePeoplesCards
{
    public const string YouKnowTheWay = "lords.fp.character.25";
    public const string SafePaths = "lords.fp.character.26";
    public const string Wind = "lords.fp.character.27";
    public const string ThreeRings = "lords.fp.character.28";
    public const string EveryLittle = "lords.fp.strategy.25";
    public const string Rangers = "lords.fp.strategy.26";
    public const string WeWontGoBack = "lords.sh.character.25";

    public static IEnumerable<EffectDefinition> Programs => [
        new(YouKnowTheWay, [new AddSmeagolTilesStep()]),
        new(SafePaths, [new HealCorruptionStep(1), new SafePathsDiscardStep()]),
        new(Wind, [new WindFromWestStep(), new InspectShadowHandStep()]),
        new(ThreeRings, [new RollRingRecoveryStep(), new RecoverRingsStep()]),
        new(EveryLittle, [new RecruitmentBatchStep([
            new("gondor", "pelargir", [UnitKind.Regular], 1),
            new("gondor", "lossarnach", [UnitKind.Regular], 1),
            new("gondor", "lamedon", [UnitKind.Regular], 1)]), new DrawCardsStep("strategy")]),
        new(Rangers, [new NorthRangersAttackStep(), new NorthRangersRegionStep(),
            new RecruitEffectStep("north", ""), new RecruitEffectStep("north", "", true)]),
        new("combat.orderly-withdrawal", [new OrderlyWithdrawalStep()])
    ];

    public static IEnumerable<EventDefinition> Events =>
        new[] { YouKnowTheWay, SafePaths, Wind, ThreeRings, EveryLittle, Rangers }
            .Select(id => new EventDefinition(id, id, new UnconditionalEvent()));

    public static IEnumerable<CombatDefinition> Combats => [
        new(YouKnowTheWay, new CompanionInBattle(true), [new(6, "combat.blade-of-westernesse")]),
        new(SafePaths, new FellowshipAtBattle(), [new(3, "combat.it-is-a-gift")]),
        new(Wind, new OrderlyWithdrawalCondition(), [new(6, "combat.orderly-withdrawal")]),
        new(ThreeRings, new OrderlyWithdrawalCondition(), [new(6, "combat.orderly-withdrawal")]),
        new(EveryLittle, new UnconditionalCombat(), [new(6, "combat.shield-wall")]),
        new(Rangers, new FreeNationBattleCondition(), [new(3, "combat.advantageous-position")])
    ];
}
