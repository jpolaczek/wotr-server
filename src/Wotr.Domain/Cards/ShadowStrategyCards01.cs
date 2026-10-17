using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowStrategyCards01
{
    public const string Valinor = "base.sh.strategy.01";
    public const string UrukHai = "base.sh.strategy.02";
    public const string Denethor = "base.sh.strategy.03";
    public const string Dawn = "base.sh.strategy.04";
    public const string Threats = "base.sh.strategy.05";
    public const string Stormcrow = "base.sh.strategy.06";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Valinor, [new ValinorRegionStep()]),
        new(UrukHai, [new ArmyOrderStep(ArmyEventOrder.UrukHaiSiege)]),
        new(Denethor, [new LastingEffectStep(EffectModifier.PreventMinasTirithCombatCards,
            EffectDuration.UntilRemoved, EffectExpiry.MinasTirithSiegeEnds), new DenethorLeaderStep()]),
        new(Dawn, [new DiscardWillDiceStep()]),
        new(Threats, [new LastingEffectStep(EffectModifier.PreventPassiveMusterAdvance,
            EffectDuration.UntilRemoved, EffectExpiry.FreeNationAdvancedByAttackOrCompanion)]),
        new(Stormcrow, [new RetreatPoliticalCounterStep(), new StormcrowLossStep()]),
        new("combat.deadly-strife", [new BothArmiesModifierStep(2)]),
        new("combat.onslaught", [new VoluntaryLossesStep(4, true), new SacrificeResultStep(true)]),
        new("combat.relentless-assault", [new VoluntaryLossesStep(2), new SacrificeResultStep(false)]),
        new("combat.great-host", [new GreatHostStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(Valinor, Valinor, new ValinorCondition()),
        new(UrukHai, UrukHai, new ArmyOrderCondition(ArmyEventOrder.UrukHaiSiege)),
        new(Denethor, Denethor, new DenethorCondition(), true),
        new(Dawn, Dawn, new ShadowAtWarCondition()),
        new(Threats, Threats, new UnconditionalEvent(), true),
        new(Stormcrow, Stormcrow, new StormcrowCondition())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Valinor, new UnconditionalCombat(), [new(3, "combat.deadly-strife")]),
        new(UrukHai, new UnconditionalCombat(), [new(7, "combat.onslaught")]),
        new(Denethor, new DevilryCondition(), [new(3, "combat.devilry-of-orthanc")]),
        new(Dawn, new UnconditionalCombat(), [new(3, "combat.relentless-assault")]),
        new(Threats, new DevilryCondition(), [new(3, "combat.devilry-of-orthanc")]),
        new(Stormcrow, new UnconditionalCombat(), [new(7, "combat.great-host")])
    ];
}
