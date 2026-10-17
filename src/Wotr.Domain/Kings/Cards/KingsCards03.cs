using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsCards03
{
    public const string Valinor = "kings.sh.strategy.01";
    public const string Denethor = KingsCardRestrictions.Folly;
    public const string Threats = "kings.sh.strategy.05";
    public const string Stormcrow = "kings.sh.strategy.06";
    public const string King = "kings.sh.strategy.18";
    public const string Palantir = "kings.sh.strategy.25";
    public const string Power = "combat.his-power-waxes";
    public static IReadOnlyList<string> Ids { get; } = [Valinor, Denethor, Threats, Stormcrow, King, Palantir];

    public static IEnumerable<EffectDefinition> Programs =>
    [
        new(Valinor, [new KingsValinorStep()]),
        new(Denethor, []),
        new(Threats, [new LastingEffectStep(EffectModifier.PreventPassiveMusterAdvance,
            EffectDuration.UntilRemoved, EffectExpiry.FreeNationAdvancedByAttackOrCompanion)]),
        new(Stormcrow, [new RetreatPoliticalCounterStep(), new StormcrowCorruptionStep(), new KingsStormcrowLossStep()]),
        new(King, [new RecruitmentBatchStep([
            new("sauron", KingBonusRecruitStep.Region, [UnitKind.Regular], 5),
            new("sauron", KingBonusRecruitStep.Region, [UnitKind.Nazgul], 1)]), new KingBonusRecruitStep()]),
        new(Palantir, []),
        new(Power, [new HisPowerWaxesStep()])
    ];

    public static IEnumerable<EventDefinition> Events =>
    [
        new(Valinor, Valinor, new KingsValinorCondition()),
        new(Denethor, Denethor, new KingsDenethorCondition(), true),
        new(Threats, Threats, new UnconditionalEvent(), true),
        new(Stormcrow, Stormcrow, new StormcrowCondition()),
        new(King, King, new KingsRevealedCondition()),
        new(Palantir, Palantir, new UnconditionalEvent(), true)
    ];

    public static IEnumerable<CombatDefinition> Combats =>
    [
        new(Valinor, new UnconditionalCombat(), [new(3, "combat.deadly-strife")]),
        new(Denethor, new DevilryCondition(), [new(3, "combat.devilry-of-orthanc")]),
        new(Threats, new DevilryCondition(), [new(3, "combat.devilry-of-orthanc")]),
        new(Stormcrow, new UnconditionalCombat(), [new(7, "combat.great-host")]),
        new(King, new UnconditionalCombat(), [new(3, "combat.relentless-assault")]),
        new(Palantir, new HisPowerWaxesCondition(), [new(3, Power)])
    ];
}
