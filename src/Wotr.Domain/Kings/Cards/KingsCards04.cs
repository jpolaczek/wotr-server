namespace Wotr.Domain;

public static class KingsCards04
{
    public const string Enemy = "kings.sh.strategy.26";
    public static IEnumerable<EffectDefinition> Programs =>
        [new(Enemy, [new EnemySovereignStep(), new EnemyArmyStep()])];
    public static IEnumerable<EventDefinition> Events =>
        [new(Enemy, Enemy, new UnconditionalEvent())];
    public static IEnumerable<CombatDefinition> Combats =>
        [new(Enemy, new HisPowerWaxesCondition(), [new(3, KingsCards03.Power)])];
}
