using Wotr.Contracts;

namespace Wotr.Domain;

public static class ActionSequenceOptions
{
    public static IEnumerable<ActionOption> For(MatchState state, Side actor)
    {
        if (state.Pending is not { Kind: DecisionKind.ActionSequence } || state.Pending.Owner != actor ||
            state.Resolution.ActionSequence is not { } sequence || sequence.Owner != actor)
            yield break;
        var option = new ActionOption { Kind = sequence.Kind, EligibleDice = [sequence.DieId], Minimum = 1, Maximum = sequence.Remaining };
        if (sequence.Kind == CommandKind.MoveArmies)
            Armies(state, sequence, option);
        else if (sequence.Kind == CommandKind.Recruit)
            Recruitment(state, sequence, option);
        else
            Characters(state, sequence, option);
        option.Enabled = option.EligibleIds.Count > 0;
        option.Reason = option.Enabled ? "" : "No remaining figure can perform another step. Finish the action.";
        yield return option;
        yield return new() { Kind = CommandKind.FinishActionSequence };
    }

    private static void Armies(MatchState state, ActionSequenceState sequence, ActionOption option)
    {
        option.Armies = MovementOptions.Armies(state, sequence.Owner, sequence.UsedIds.ToHashSet()).ToList();
        option.MovementCosts = [new() { DieId = sequence.DieId, Face = sequence.Face, AbilityId = sequence.AbilityId,
            MaximumMoves = sequence.Remaining, RequiresCommander = sequence.Face == DieFace.Character }];
        option.EligibleIds = option.Armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).ToList();
        option.Targets = option.Armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).Distinct().ToList();
    }

    private static void Recruitment(MatchState state, ActionSequenceState sequence, ActionOption option)
    {
        foreach (var group in MusterOptions.Recruitments(state, sequence.Owner))
        {
            if (group.Kind == UnitKind.Elite && sequence.Remaining < 2)
                continue;
            group.Targets.RemoveAll(sequence.UsedRegions.Contains);
            group.ReserveIds.RemoveAll(sequence.UsedIds.Contains);
            if (group.Targets.Count > 0 && group.ReserveIds.Count > 0)
                option.Recruitment.Add(group);
        }
        option.EligibleIds = option.Recruitment.SelectMany(r => r.ReserveIds).ToList();
        option.Targets = option.Recruitment.SelectMany(r => r.Targets).Distinct().ToList();
    }

    private static void Characters(MatchState state, ActionSequenceState sequence, ActionOption option)
    {
        var remaining = sequence.InitialFigures.Except(sequence.UsedIds).ToHashSet();
        if (sequence.Kind == CommandKind.MoveShadowCharacters)
            option.Travel = ShadowTravel.Options(state).Where(t => remaining.Contains(t.Id)).ToList();
        else
        {
            var characters = state.Characters.Where(c => remaining.Contains(c.Id) && CharacterRules.OnBoard(c)).ToList();
            var bonus = sequence.AbilityId == KingsFarSighted.Ability ? 1 : 0;
            option.Travel = characters.Select(c => FellowshipOptions.CompanionTravel(state, c, characters, bonus)).ToList();
        }
        option.EligibleIds = option.Travel.Select(t => t.Id).ToList();
        option.Targets = option.Travel.SelectMany(t => t.Targets).Distinct().ToList();
    }

    public static ActionSequenceView? View(MatchState state) => state.Resolution.ActionSequence is { } sequence ? new()
    {
        Kind = sequence.Kind,
        Owner = sequence.Owner,
        DieId = sequence.DieId,
        Face = sequence.Face,
        AbilityId = sequence.AbilityId,
        CompletedSteps = sequence.CompletedSteps,
        Remaining = sequence.Remaining,
        UsedIds = [.. sequence.UsedIds],
        UsedRegions = [.. sequence.UsedRegions]
    } : null;
}
