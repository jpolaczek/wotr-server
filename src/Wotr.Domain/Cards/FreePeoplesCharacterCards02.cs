namespace Wotr.Domain;

public static class FreePeoplesCharacterCards02
{
    public const string Horn = "base.fp.character.07";
    public const string Staff = "base.fp.character.08";
    public const string Athelas = "base.fp.character.09";
    public const string AnotherWay = "base.fp.character.10";
    public const string Alone = "base.fp.character.11";
    public const string Bilbo = "base.fp.character.12";
    public const string Boromir = "base.character.boromir";
    public const string HornReaction = "card.horn-of-gondor";
    public const string StaffReaction = "card.wizards-staff";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Horn, [TableCompanion(Boromir)]),
        new(Staff, [TableCompanion(CharacterRules.Grey)]),
        new(HornReaction, [new AdjustHuntStep(1)]),
        new(StaffReaction, [new PreventHuntDrawStep()]),
        new(Athelas, [new AthelasStep()]),
        new(AnotherWay, [new HealCorruptionStep(1), new AnotherWayStep()]),
        new(Alone, [new SeparateCompanionsStep(1), new HealCorruptionStep(1)]),
        new("combat.servant-of-the-secret-fire", [new CombatModifierStep(1, 0)]),
        new("combat.anduril", [new AndurilStep()]),
        new("combat.heroic-death", [new HeroicDeathStep()]),
        new("combat.daring-defiance", [new DaringDefianceStep()]),
        new("combat.fateful-strike", [new FatefulStrikeStep()])
    ];

    private static LastingEffectStep TableCompanion(string id) => new(EffectModifier.None, EffectDuration.UntilRemoved,
        EffectExpiry.FellowshipCompanionsAbsent, RequiredCompanions: [id]);

    public static IEnumerable<EventDefinition> Events => [
        new(Horn, Horn, new FellowshipCompanionPresent([Boromir]), true),
        new(Staff, Staff, new FellowshipDefinitionPresent(CharacterRules.Grey), true),
        new(Athelas, Athelas, new UnconditionalEvent()),
        new(AnotherWay, AnotherWay, new UnconditionalEvent()),
        new(Alone, Alone, new AnyFellowshipCompanion())
    ];

    public static IEnumerable<EffectReaction> Reactions => [
        new(HornReaction, "hunt.table", HornReaction, new ShadowHuntDraw(true), Horn, ReactionCost.DiscardTableCard),
        new(StaffReaction, "hunt.draw", StaffReaction, new ShadowHuntBeforeDraw(), Staff, ReactionCost.DiscardTableCard)
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Horn, new CompanionInBattle(), [new(4, "combat.mighty-attack")]),
        new(Staff, new CharacterInBattle([CharacterRules.Grey, CharacterRules.White]), [new(3, "combat.servant-of-the-secret-fire")]),
        new(Athelas, new CharacterInBattle([CharacterRules.Strider, CharacterRules.Aragorn]), [new(4, "combat.anduril")]),
        new(AnotherWay, new LeaderOrCompanionInBattle(), [new(6, "combat.heroic-death")]),
        new(Alone, new CompanionInBattle(), [new(0, "combat.daring-defiance")]),
        new(Bilbo, new LeaderOrCompanionInBattle(), [new(6, "combat.fateful-strike")])
    ];
}
