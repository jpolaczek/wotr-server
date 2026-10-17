namespace Wotr.Domain;

public static class FreePeoplesCharacterCards01
{
    public const string Mithril = "base.fp.character.05";
    public const string Axe = "base.fp.character.06";
    public const string ReplaceReaction = "card.mithril-replace";
    public const string ProtectReaction = "card.axe-and-bow";
    public static IReadOnlyList<string> AxeCompanions { get; } = Array.AsReadOnly<string>(["base.character.gimli", "base.character.legolas"]);

    public static IReadOnlyList<(string Card, string Tile)> SpecialTiles { get; } = Array.AsReadOnly<(string Card, string Tile)>([
        ("base.fp.character.01", "base.hunt.special-fp.0.0.1"),
        ("base.fp.character.02", "base.hunt.special-fp.0.0.2"),
        ("base.fp.character.03", "base.hunt.special-fp.-2.0.1"),
        ("base.fp.character.04", "base.hunt.special-fp.-1.0.1")
    ]);

    public static IEnumerable<EffectDefinition> Programs => [
        .. SpecialTiles.Select(pair => new EffectDefinition(pair.Card, [new PrepareHuntTileStep(pair.Tile)])),
        new(Mithril, [new LastingEffectStep(EffectModifier.None, EffectDuration.UntilRemoved, EffectExpiry.None)]),
        new(Axe, [new LastingEffectStep(EffectModifier.None, EffectDuration.UntilRemoved,
            EffectExpiry.FellowshipCompanionsAbsent, RequiredCompanions: AxeCompanions)]),
        new(ReplaceReaction, [new ReplaceHuntTileStep()]),
        new(ProtectReaction, [new AdjustHuntStep(1)]),
        new("combat.it-is-a-gift", [new CombatModifierStep(1, 1)]),
        new("combat.blade-of-westernesse", [new BladeOfWesternesseStep()]),
        new("combat.mighty-attack", [new MightyAttackStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        .. SpecialTiles.Select(pair => new EventDefinition(pair.Card, pair.Card, new SpecialHuntTileAvailable(pair.Tile))),
        new(Mithril, Mithril, new UnconditionalEvent(), true),
        new(Axe, Axe, new FellowshipCompanionPresent(AxeCompanions), true)
    ];

    public static IEnumerable<EffectReaction> Reactions => [
        new(ReplaceReaction, "hunt.replace", ReplaceReaction, new ShadowHuntDraw(), Mithril, ReactionCost.DiscardTableCard),
        new(ProtectReaction, "hunt.table", ProtectReaction, new ShadowHuntDraw(true), Axe, ReactionCost.DiscardTableCard)
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        .. SpecialTiles.Select(pair => new CombatDefinition(pair.Card, new FellowshipAtBattle(), [new(3, "combat.it-is-a-gift")])),
        new(Mithril, new CompanionInBattle(true), [new(6, "combat.blade-of-westernesse")]),
        new(Axe, new CompanionInBattle(), [new(4, "combat.mighty-attack")])
    ];
}
