namespace Wotr.Domain;

public static class ShadowCharacterCards02
{
    public const string FoulThing = "base.sh.character.07";
    public const string Candles = "base.sh.character.08";
    public const string Search = "base.sh.character.09";
    public const string Weather = "base.sh.character.10";
    public const string Strike = "base.sh.character.11";
    public const string Wound = "base.sh.character.12";

    public static IEnumerable<EffectDefinition> Programs => [
        new(FoulThing, [new DrawHuntTileStep(HuntTileRule.IgnoreEyesAndFellowship, HuntDamageRule.RandomCompanionFirst)]),
        new(Candles, [new CandlesCorruptionStep()]),
        new(Search, [new MoveNazgulStep(), new NazgulSearchStep()]),
        new(Weather, [new CruelWeatherStep()]),
        new(Strike, [new MoveNazgulStep(), new NazgulStrikeStep()]),
        new(Wound, [new MorgulWoundStep()]),
        new("combat.they-are-terrible", [new NazgulLeadershipStep(NazgulLeadershipBenefit.RerollBonus)]),
        new("combat.dread-and-despair", [new NazgulLeadershipStep(NazgulLeadershipBenefit.OpposingDicePenalty)]),
        new("combat.foul-stench", [new FoulStenchStep()]),
        new("combat.black-breath", [new BlackBreathStep()])
    ];

    public static IEnumerable<EventDefinition> Events => [
        new(FoulThing, FoulThing, new FellowshipOutsideFreeSettlement()),
        new(Candles, Candles, new FellowshipOutsideFreeSettlement()),
        new(Search, Search, new FellowshipProgressCondition()),
        new(Weather, Weather, new FellowshipProgressCondition()),
        new(Strike, Strike, new FellowshipProgressCondition()),
        new(Wound, Wound, new FellowshipRevealedCondition())
    ];

    public static IEnumerable<CombatDefinition> Combats => [
        new(FoulThing, new NazgulLeadershipCondition(1), [new(4, "combat.they-are-terrible")]),
        new(Candles, new NazgulLeadershipCondition(1), [new(3, "combat.dread-and-despair")]),
        new(Search, new NazgulLeadershipCondition(1), [new(3, "combat.foul-stench")]),
        new(Weather, new NazgulLeadershipCondition(2), [new(3, "combat.cruel-as-death")]),
        new(Strike, new NazgulInBattle(), [new(6, "combat.black-breath")]),
        new(Wound, new NazgulInBattle(), [new(6, "combat.black-breath")])
    ];
}
