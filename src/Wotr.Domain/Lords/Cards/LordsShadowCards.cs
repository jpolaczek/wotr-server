namespace Wotr.Domain;

public static class LordsShadowCards
{
    public const string Balrog = "lords.sh.character.17";
    public const string WeWontGoBack = "lords.sh.character.25";
    public const string Lore = "lords.sh.character.26";
    public const string Gates = "lords.sh.character.27";
    public const string Captain = "lords.sh.character.28";
    public const string DolGuldur = "lords.sh.strategy.25";
    public const string Madness = "lords.sh.strategy.26";
    public const string GatesReaction = "lords.gates-are-closed";

    public static IEnumerable<EffectDefinition> Programs => [
        new(Balrog, [new BalrogEventStep()]),
        new(WeWontGoBack, [Table(EffectExpiry.SmeagolNoLongerGuide)]),
        new(Lore, [Table(EffectExpiry.None)]),
        new(Gates, [Table(EffectExpiry.None)]),
        new(Captain, [Table(EffectExpiry.CaptainConditionsLost)]),
        new(DolGuldur, [new DolGuldurRecruitStep(), new ArmyOrderStep(ArmyEventOrder.DolGuldurManeuver)]),
        new(Madness, [new MadnessAttackStep(), new MadnessRecruitStep()]),
        new(WeShallGetIt.Reaction, [new ReplaceHuntTileStep()]),
        new(WeWontGoBackStep.Program, [new WeWontGoBackStep()]),
        new(LoreOfTheRings.Program, [new LoreChangeStep()]),
        new(GatesReaction, [new GatesHuntStep()]),
        new("combat.fear-of-their-masters", [new FearOfTheirMastersStep()]),
        new("combat.servant-of-the-shadow", [new ServantOfTheShadowStep()])
    ];

    private static LastingEffectStep Table(EffectExpiry expiry) => new(EffectModifier.None, EffectDuration.UntilRemoved, expiry);

    public static IEnumerable<EventDefinition> Events => [
        new(Balrog, Balrog, new MinionPresentCondition(LordsCharacters.Balrog)),
        new(WeWontGoBack, WeWontGoBack, new SmeagolGuideCondition(), true),
        new(Lore, Lore, new UnconditionalEvent(), true),
        new(Gates, Gates, new UnconditionalEvent(), true),
        new(Captain, Captain, new CaptainOfDespairCondition(), true),
        new(DolGuldur, DolGuldur, new DolGuldurCondition()),
        new(Madness, Madness, new NationAtWarCondition("sauron"))
    ];

    public static IEnumerable<EffectReaction> Reactions => [
        new(GatesReaction, "fellowship.revealed", GatesReaction, new GatesRouteCondition(), Gates, ReactionCost.DiscardTableCard),
        new(GatesReaction + ".declaration", "fellowship.declared", GatesReaction, new GatesRouteCondition(), Gates, ReactionCost.DiscardTableCard),
        new(WeShallGetIt.Reaction, WeShallGetIt.Window, WeShallGetIt.Reaction, new WeShallGetItCondition(),
            SmeagolRules.WeShallGetIt, ReactionCost.DiscardTableCard)
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(Balrog, new LordsDurinsBaneCondition(), [new(2, "combat.durins-bane")]),
        new(WeWontGoBack, new MinionInBattle(), [new(3, "combat.fear-of-their-masters")]),
        new(Lore, new MinionInBattle(), [new(3, "combat.fear-of-their-masters")]),
        new(Gates, new NazgulInBattle(), [new(1, "combat.words-of-power")]),
        new(Captain, new MinionInBattle(), [new(3, "combat.servant-of-the-shadow")]),
        new(DolGuldur, new MumakilCondition(), [new(3, "combat.mumakil-combat"), new(5, "combat.mumakil-hits")]),
        new(Madness, new UnconditionalCombat(), [new(0, "combat.swarm-of-bats")])
    ];
}
