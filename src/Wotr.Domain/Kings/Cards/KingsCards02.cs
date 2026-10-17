using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsCards02
{
    public const string Dain = "kings.fp.strategy.22";
    public const string Thranduil = "kings.fp.strategy.24";
    public const string Service = "kings.fp.strategy.25";
    public const string Villages = "kings.fp.strategy.26";
    public const string Welcome = KingsCardRestrictions.Welcome;
    public const string West = "kings.sh.character.26";
    public const string Hope = "combat.there-is-hope-for-victory";
    public const string Vain = "combat.the-battle-is-vain";
    public static IReadOnlyList<string> Ids { get; } = [Dain, Thranduil, Service, Villages, Welcome, West];

    public static IEnumerable<EffectDefinition> Programs =>
    [
        new(Dain, [new KingsNationalCardStep(SovereignCatalog.Dain, Dain + ".otherwise")]),
        new(Dain + ".otherwise", [new KingsRecruitOrCleanseStep(SovereignCatalog.Dain, Dain + ".recruit"), new DrawCardsStep("strategy")]),
        new(Dain + ".recruit", [new RecruitmentBatchStep([
            new("dwarves", "erebor", [UnitKind.Regular, UnitKind.Elite], 1),
            new("dwarves", "erebor", [UnitKind.Leader], 1)])]),
        new(Thranduil, [new KingsNationalCardStep(SovereignCatalog.Thranduil, Thranduil + ".otherwise")]),
        new(Thranduil + ".otherwise", [new KingsRecruitOrCleanseStep(SovereignCatalog.Thranduil, Thranduil + ".recruit"), new DrawCardsStep("strategy")]),
        new(Thranduil + ".recruit", [new RecruitEffectStep("elves", "woodland-realm")]),
        new(Service, [new ServiceSovereignStep(), new ServiceRecruitStep(), new ServiceCorruptionStep()]),
        new(Villages, [new VillagesRecruitStep()]),
        new(Welcome, []),
        new(West, [new WestCorruptionStep()]),
        new(Hope, [new HopeAwakeningStep(), new CombatModifierStep(0, 1)]),
        new(Vain, [new NazgulLeadershipStep(NazgulLeadershipBenefit.OpposingLeadershipPenalty)])
    ];

    public static IEnumerable<EventDefinition> Events => Ids.Select(id => new EventDefinition(id, id,
        new UnconditionalEvent(), StaysOnTable: id == Welcome));

    public static IEnumerable<CombatDefinition> Combats =>
    [
        new(Dain, new EliteInBattleCondition(), [new(3, "combat.valour")]),
        new(Thranduil, new EliteInBattleCondition(), [new(3, "combat.valour")]),
        new(Service, new HopeCondition(), [new(4, Hope)]),
        new(Villages, new HopeCondition(), [new(4, Hope)]),
        new(Welcome, new MinionInBattle(), [new(3, "combat.servant-of-the-shadow")]),
        new(West, new NazgulLeadershipCondition(1), [new(1, Vain)])
    ];
}
