namespace Wotr.Domain;

internal static class SaveStructure
{
    // Structural checks only. This must not replay rules or consume randomness.
    public static void Validate(MatchState state)
    {
        if (string.IsNullOrWhiteSpace(state.Id) || state.Configuration is null ||
            state.Players is null || state.Players.Count != 2 || state.Players.Any(p => p is null) ||
            state.Players.Select(p => p.Side).Distinct().Count() != 2 ||
            state.Units is null || state.Characters is null || state.Nations is null || state.Control is null ||
            state.Dice is null || state.Rings is null || state.Fellowship is null || state.HuntTiles is null ||
            state.Resolution?.Frames is null || state.Resolution.PaidDieIds is null ||
            state.Events is null || state.RandomSamples is null)
            throw new InvalidDataException("CorruptSave");
        HuntReportStructure.Validate(state);
        if (state.Resolution.ActionSequence is { } sequence &&
            (!ActionStepRules.Supports(sequence.Kind) || sequence.CompletedSteps < 1 || sequence.Remaining < 1 ||
                sequence.UsedIds is null || sequence.UsedRegions is null || sequence.InitialFigures is null ||
                !state.Resolution.PaidDieIds.Contains(sequence.DieId) ||
                !state.Dice.Any(d => d.Id == sequence.DieId && d.Owner == sequence.Owner && d.Location == Wotr.Contracts.DieLocation.Resolving)))
            throw new InvalidDataException("CorruptSave");
    }
}
