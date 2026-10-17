namespace Wotr.Domain;

public static class ShadowCharacterCards03
{
    public const string Lure = "base.sh.character.13";
    public const string Breaking = "base.sh.character.14";
    public const string Worn = "base.sh.character.15";
    public const string Crebain = "base.sh.character.16";
    public const string Balrog = "base.sh.character.17";
    public const string Lidless = "base.sh.character.18";
    public const string WornReaction = "card.worn-with-sorrow";
    public const string CrebainReaction = "card.flocks-of-crebain";
    public const string BalrogReaction = "card.balrog-of-moria";
    public const string BreakingChoice = "card.breaking-separation";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Lure, [new LureCompanionStep(), new LureChoiceStep()]),
        new(Breaking, [new BreakingFellowshipStep()]),
        new(BreakingChoice, [new BreakingSeparationStep()]),
        new(Worn, [Table(EffectExpiry.FellowshipDeclaresSanctuary)]),
        new(Crebain, [Table(EffectExpiry.FellowshipDeclaresSanctuary)]),
        new(Balrog, [Table(EffectExpiry.None)]),
        new(Lidless, [new LidlessEyeStep()]),
        new(WornReaction, [new WornDiscardStep()]),
        new(CrebainReaction, [new CrebainBonusStep()]),
        new(BalrogReaction, [new BalrogHuntStep()]),
        new("combat.words-of-power", [new WordsOfPowerStep()]),
        new("combat.durins-bane", [new DurinsBaneStep()])
    ];

    private static LastingEffectStep Table(EffectExpiry expiry) => new(EffectModifier.None, EffectDuration.UntilRemoved, expiry);

    public static IEnumerable<EventDefinition> Events => [
        new(Lure, Lure, new FellowshipRevealedCondition()),
        new(Breaking, Breaking, new FellowshipRevealedCondition()),
        new(Worn, Worn, new UnconditionalEvent(), true),
        new(Crebain, Crebain, new UnconditionalEvent(), true),
        new(Balrog, Balrog, new UnconditionalEvent(), true),
        new(Lidless, Lidless, new UnconditionalEvent())
    ];

    public static IEnumerable<EffectReaction> Reactions => [
        new(WornReaction, "hunt.casualty", WornReaction, new HuntCasualtyCondition(), Worn),
        new(CrebainReaction, "hunt.roll", CrebainReaction, new HuntBeforeRollCondition(), Crebain, ReactionCost.DiscardTableCard),
        new(BalrogReaction, "fellowship.revealed", BalrogReaction, new BalrogRouteCondition(), Balrog, ReactionCost.DiscardTableCard),
        new(BalrogReaction + ".declaration", "fellowship.declared", BalrogReaction, new BalrogRouteCondition(), Balrog, ReactionCost.DiscardTableCard)
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Lure, new NazgulLeadershipCondition(1), [new(4, "combat.they-are-terrible")]),
        new(Breaking, new NazgulLeadershipCondition(1), [new(3, "combat.dread-and-despair")]),
        new(Worn, new NazgulInBattle(), [new(1, "combat.words-of-power")]),
        new(Crebain, new NazgulLeadershipCondition(1), [new(4, "combat.they-are-terrible")]),
        new(Balrog, new NearMoriaBattle(), [new(2, "combat.durins-bane")]),
        new(Lidless, new NazgulInBattle(), [new(1, "combat.words-of-power")])
    ];
}
