namespace Wotr.Domain;

public static class ShadowCharacterCards01
{
    public const string OrcPatrol = "base.sh.character.05";
    public const string IsildursBane = "base.sh.character.06";

    public static IReadOnlyList<(string Card, string Tile)> SpecialTiles { get; } = Array.AsReadOnly<(string Card, string Tile)>([
        ("base.sh.character.01", "base.hunt.special-sh.die.0.1"),
        ("base.sh.character.02", "base.hunt.special-sh.eye.1.1"),
        ("base.sh.character.03", "base.hunt.special-sh.3.0.1"),
        ("base.sh.character.04", "base.hunt.special-sh.1.1.1")
    ]);

    public static IEnumerable<EffectDefinition> Programs => [
        .. SpecialTiles.Select(pair => new EffectDefinition(pair.Card, [new PrepareHuntTileStep(pair.Tile)])),
        new(OrcPatrol, [new DrawHuntTileStep(HuntTileRule.IgnoreEyesAndFellowship)]),
        new(IsildursBane, [new DrawHuntTileStep(HuntTileRule.IgnoreEyesAndFellowship, HuntDamageRule.CorruptionOnly)]),
        new("combat.one-for-the-dark-lord", [new CombatModifierStep(1, 1)]),
        new("combat.cruel-as-death", [new NazgulLeadershipStep(NazgulLeadershipBenefit.CombatBonus)])
    ];

    public static IEnumerable<EventDefinition> Events => [
        .. SpecialTiles.Select(pair => new EventDefinition(pair.Card, pair.Card, new SpecialHuntTileAvailable(pair.Tile))),
        new(OrcPatrol, OrcPatrol, new FellowshipOutsideFreeSettlement()),
        new(IsildursBane, IsildursBane, new FellowshipOutsideFreeSettlement())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        .. SpecialTiles.Select(pair => new CombatDefinition(pair.Card, new FellowshipAtBattle(), [new(3, "combat.one-for-the-dark-lord")])),
        new(OrcPatrol, new NazgulLeadershipCondition(2), [new(3, "combat.cruel-as-death")]),
        new(IsildursBane, new NazgulLeadershipCondition(2), [new(3, "combat.cruel-as-death")])
    ];
}
