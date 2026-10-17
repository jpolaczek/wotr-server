using Wotr.Contracts;

namespace Wotr.Domain;

public static class ShadowCharacterCards04
{
    public const string Spells = "base.sh.character.19";
    public const string Grond = "base.sh.character.20";
    public const string Palantir = "base.sh.character.21";
    public const string Wormtongue = "base.sh.character.22";
    public const string Ringwraiths = "base.sh.character.23";
    public const string Captain = "base.sh.character.24";
    public const string PalantirDraw = "card.palantir-draw";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Spells, [new DreadfulSourceStep(), new DreadfulTargetStep()]),
        new(Grond, [new ArmyOrderStep(ArmyEventOrder.GrondSiege)]),
        new(Palantir, [new SarumanTableStep(false)]),
        new(Wormtongue, [new SarumanTableStep(true)]),
        new(Ringwraiths, [new MoveNazgulStep(), new ArmyOrderStep(ArmyEventOrder.NazgulManeuver)]),
        new(Captain, [new BlackCaptainStep(), new ArmyOrderStep(ArmyEventOrder.WitchKingManeuver)]),
        new(PalantirDraw, [new PalantirDrawStep()]),
        new("combat.devilry-of-orthanc", [new CombatModifierStep(1, 0)])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(Spells, Spells, new DreadfulCondition()),
        new(Grond, Grond, new ArmyOrderCondition(ArmyEventOrder.GrondSiege)),
        new(Palantir, Palantir, new MinionPresentCondition(CharacterRules.Saruman), true),
        new(Wormtongue, Wormtongue, new MinionPresentCondition(CharacterRules.Saruman), true),
        new(Ringwraiths, Ringwraiths, new UnconditionalEvent()),
        new(Captain, Captain, new MinionPresentCondition(CharacterRules.WitchKing))
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Spells, new DevilryCondition(), [new(3, "combat.devilry-of-orthanc")]),
        new(Grond, new NazgulLeadershipCondition(1), [new(3, "combat.dread-and-despair")]),
        new(Palantir, new NazgulLeadershipCondition(2), [new(3, "combat.cruel-as-death")]),
        new(Wormtongue, new NazgulLeadershipCondition(1), [new(3, "combat.foul-stench")]),
        new(Ringwraiths, new NazgulInBattle(), [new(1, "combat.words-of-power")]),
        new(Captain, new NazgulLeadershipCondition(1), [new(3, "combat.foul-stench")])
    ];
}

public sealed record MinionPresentCondition(string DefinitionId) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => DefinitionId == CharacterRules.WitchKing
        ? CharacterRules.WitchKingInPlay(state) is not null
        : CharacterRules.InPlay(state, DefinitionId) is not null;
}

public sealed record DevilryCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => battle.Mode == BattleMode.Siege &&
        BattleRules.Units(state, battle.Force(actor)).Any(u => u.Nation == "isengard" && BoardRules.IsArmyUnit(u));
}
