namespace Wotr.Domain;

public static class WormtongueRules
{
    public static bool Blocks(MatchState state, string nation, PoliticalTrigger? trigger, string region) =>
        nation == "rohan" && state.ActiveEffects.Any(e => e.Modifier == EffectModifier.RestrictRohanActivation) &&
        !(region is "edoras" or "helms-deep" && trigger is PoliticalTrigger.ArmyAttack or PoliticalTrigger.CompanionArrival);
}
