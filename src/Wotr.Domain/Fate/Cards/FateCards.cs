using Wotr.Contracts;

namespace Wotr.Domain;

public static class FateCards
{
    public const string ThereAndBack = "fate.fp.character.17";
    public const string Book = "fate.fp.strategy.04";
    public const string Bardings = "fate.fp.strategy.19";
    public const string Dwarves = "fate.fp.strategy.22";
    public const string Orcs = "fate.sh.strategy.20";
    public const string Monsters = "fate.sh.strategy.22";
    public const string MonstersWest = "fate.monsters.west";
    public const string MonstersEast = "fate.monsters.east";

    public static IEnumerable<EffectDefinition> Programs =>
    [
        new(ThereAndBack, [new SeparateCompanionsStep(1), new FateThereAndBackStep()]),
        new(Book, [new OutsideCompanionJourneyStep(), new FateBookWarStep(),
            new FateBookRegionStep(), new RecruitEffectStep("dwarves", "")]),
        new(Bardings, [new FateDaleRecruitStep("north"), new DrawCardsStep("strategy")]),
        new(Dwarves, [new FateDaleRecruitStep("dwarves"), new DrawCardsStep("strategy")]),
        new(Orcs, [new FateOrcsRegionStep(), new FateOrcsRecruitStep()]),
        new(Monsters, [new FateMonstersBranchStep()]),
        new(MonstersWest, [new RecruitmentBatchStep(
        [
            new("sauron", "angmar", [UnitKind.Regular], 1),
            new("sauron", "ettenmoors", [UnitKind.Regular], 1),
            new("sauron", "weather-hills", [UnitKind.Regular], 1),
            new("sauron", "trollshaws", [UnitKind.Elite], 1)
        ])]),
        new(MonstersEast, [new FateMonstersEliteStep(), new FateMonstersEastRecruitStep()])
    ];

    public static IEnumerable<EventDefinition> Events =>
        new[] { ThereAndBack, Book, Bardings, Dwarves, Orcs, Monsters }
            .Select(id => new EventDefinition(id, id, new UnconditionalEvent()));

    public static IEnumerable<CombatDefinition> Combats =>
    [
        new(ThereAndBack, new CompanionInBattle(), [new(3, "combat.brave-stand")]),
        new(Book, new FreeNationBattleCondition(), [new(3, "combat.advantageous-position")]),
        new(Bardings, new UnconditionalCombat(), [new(6, "combat.shield-wall")]),
        new(Dwarves, new EliteInBattleCondition(), [new(3, "combat.valour")]),
        new(Orcs, new UnconditionalCombat(), [new(7, "combat.onslaught")]),
        new(Monsters, new UnconditionalCombat(), [new(3, "combat.desperate-battle")])
    ];
}
