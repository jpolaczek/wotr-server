namespace Wotr.Domain;

// Choices in a directly paid event that are reviewed for reconstruction from
// public facts. Review is by step type: each listed step takes its candidates
// from the public board and the event's own earlier choices, so a position is
// fully described by the card, its payment and the owner's trail and options.
// Every later step runs through the engine. A new step type stays unmarked until
// it is reviewed here.
public static class DirectEventChoices
{
    public static bool IsReviewed(string cardId, int nextStep, EffectDefinition program)
    {
        if (cardId == KingsCards02.Service)
            return nextStep is 1 or 2 && program.Steps is [ServiceSovereignStep, ServiceRecruitStep, ServiceCorruptionStep];
        if (nextStep < 1 || nextStep > program.Steps.Count)
            return false;
        var earlier = program.Steps.Take(nextStep - 1).ToArray();
        var step = program.Steps[nextStep - 1];
        return earlier.All(IsAutomaticPublicStep) && OpensFromPublicBoard(step) ||
            step is RecruitEffectStep { Region: "", Leader: false } && earlier.All(LeavesAPublicRecruitRegion);
    }

    // The one reviewed step a pending choice with this presentation can be at, or 0.
    public static int StepFor(string cardId, string presentation, EffectDefinition program)
    {
        for (var index = 0; index < program.Steps.Count; index++)
            if (program.Steps[index] is EffectChoiceStep choice && choice.Presentation == presentation &&
                IsReviewed(cardId, index + 1, program))
                return index + 1;
        return 0;
    }

    private static bool IsAutomaticPublicStep(EffectStep step) =>
        step is AdvanceFixedNationStep or WitchKingReturnStep;

    private static bool OpensFromPublicBoard(EffectStep step) => step is
        RecruitmentBatchStep or RecruitEffectStep { Region.Length: > 0 } or RecruitTogetherStep or RecruitRegionStep or
        HillTrollsStep or CruelWeatherStep or BlackCaptainStep or ArmyEventDamageStep or EnemySovereignStep or
        FateOrcsRegionStep or ValinorRegionStep or KingsValinorStep or WisdomBranchStep;

    private static bool LeavesAPublicRecruitRegion(EffectStep step) => step is
        RecruitRegionStep or ArmyEventDamageStep or RangersRecruitStep or StewardsRegionStep;
}
