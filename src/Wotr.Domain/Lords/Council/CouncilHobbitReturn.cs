using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record CompanionBattleReturn(string CharacterId, string Origin);

public static class CouncilHobbitReturn
{
    public const string ProgramId = "council.art-of-disappearing";
    public static EffectDefinition Program { get; } = new(ProgramId, [new HobbitReturnStep()]);

    public static void Record(MatchState state, CharacterState character)
    {
        if (state.Lords?.Council is not { } council || !CharacterRules.OnBoard(character) ||
            !CharacterRules.Has(character, "art-of-disappearing") || BattleRuntime.Active(state) is not { } battle)
            return;
        var force = battle.Force(character.Side);
        if (force.Region == character.Location && force.Garrison == character.Garrison &&
            force.Participants.Contains(character.Id) && !force.Round.CancelledAbilities.Contains(character.Id) &&
            council.BattleReturns.All(r => r.CharacterId != character.Id))
            council.BattleReturns.Add(new(character.Id, character.Location));
    }

    public static void RecordLosses(MatchState state, MatchState planned)
    {
        if (state.Lords?.Council is null)
            return;
        var eliminated = planned.Characters.Where(c => c.Location == "casualties").Select(c => c.Id).ToHashSet();
        foreach (var character in state.Characters.Where(c => eliminated.Contains(c.Id)))
            Record(state, character);
    }

    // Interrupt only at safe command/step boundaries, after a loss plan has replaced the figures.
    public static bool BeginPending(MatchState state)
    {
        if (state.Lords?.Council is not { BattleReturns.Count: > 0 } council ||
            state.Resolution.Frames.Any(f => f.EffectId == ProgramId))
            return false;
        var returning = council.BattleReturns[0];
        council.BattleReturns.RemoveAt(0);
        var pending = state.Pending;
        EffectRuntime.BeginProgram(state, Side.FreePeoples, returning.CharacterId, ProgramId);
        var frame = EffectRuntime.Current(state)!;
        frame.Effect!.RegionId = returning.Origin;
        if (pending is not null)
        {
            frame.ReturnDecision = pending;
            frame.Effect.Return = EffectReturn.Decision;
        }
        return true;
    }
}

public sealed record HobbitReturnStep : EffectChoiceStep
{
    public override string Presentation => "Region";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        BoardRules.Regions[execution.RegionId].Neighbors.Where(region =>
            TravelRules.CompanionPath(state, [execution.RegionId, region], execution.RegionId, 1)).ToArray();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        TargetId = id,
        Title = BoardRules.Regions[id].Name,
        Detail = "Art of Disappearing: the Hobbit returns to this region."
    };

    public override void Choose(EffectContext context, string id)
    {
        var character = context.State.Characters.Single(c => c.Id == context.Execution.SourceId);
        character.Location = id;
        character.Garrison = false;
        context.Record("HobbitReturned", $"Art of Disappearing: {character.Id} returned to {id}.");
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview) =>
        preview.Placements.Add(new() { UnitId = execution.SourceId, RegionId = ids.Single() });
}
