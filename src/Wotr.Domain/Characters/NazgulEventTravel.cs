using Wotr.Contracts;

namespace Wotr.Domain;

public static class NazgulEventTravel
{
    public static bool Active(MatchState state, Side actor) => actor == Side.Shadow &&
        state.Pending is { Kind: DecisionKind.CharacterEvent, Owner: Side.Shadow } &&
        EffectRuntime.Current(state)?.Effect is { Owner: Side.Shadow, Wait: EffectWait.NazgulTravel };

    public static bool WithFellowship(MatchState state) =>
        state.Units.Any(u => u.Kind == UnitKind.Nazgul && u.Location == state.Fellowship.RegionId) ||
        state.Characters.Any(c => CharacterRules.IsWitchKing(c) && c.Location == state.Fellowship.RegionId);

    public static ActionOption Option(MatchState state)
    {
        var travel = ShadowTravel.Options(state, nazgulOnly: true).ToList();
        return new()
        {
            Kind = CommandKind.MoveShadowCharacters,
            Travel = travel,
            EligibleIds = travel.Select(t => t.Id).ToList(),
            Targets = travel.SelectMany(t => t.Targets).Distinct().ToList(),
            AllowEmptySelection = true
        };
    }

    public static void Complete(MatchState state, IReadOnlyList<CompanionMoveChoice> moves)
    {
        ShadowTravel.Apply(state, ShadowTravel.Plan(state, moves, nazgulOnly: true)!);
        EffectRuntime.Current(state)!.Effect!.Wait = EffectWait.None;
        state.Pending = null;
    }
}

public sealed record MoveNazgulStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (!ShadowTravel.Options(context.State, nazgulOnly: true).Any())
            return;
        context.Execution.Wait = EffectWait.NazgulTravel;
        context.State.Open(DecisionKind.CharacterEvent, context.Owner, [CommandKind.MoveShadowCharacters]);
        context.State.Pending!.SourceId = context.Execution.SourceId;
    }
}
