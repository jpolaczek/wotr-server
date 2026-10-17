using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record GatesRouteCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => actor == Side.Shadow &&
        FellowshipFlow.Active(state) is { AwaitingHunts: true } procedure &&
        procedure.Route.Any(id => BoardRules.Region(state, id) is { Settlement: "Stronghold", OriginalOwner: Side.Shadow } &&
            state.Control.GetValueOrDefault(id) == Side.Shadow);
}

public sealed record GatesHuntStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        HuntRuntime.BeginTile(context.State, context.Execution.SourceId, tileRule: HuntTileRule.IgnoreEyes);
        var hunt = HuntRuntime.Active(context.State)!;
        hunt.IgnoreRevealIcon = true;
        hunt.DeclarationHunt = FellowshipFlow.Active(context.State)!.Kind == FellowshipProcedureKind.Declaration;
    }
}
