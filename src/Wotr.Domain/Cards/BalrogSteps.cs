using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record BalrogRouteCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => actor == Side.Shadow &&
        FellowshipFlow.Active(state) is { AwaitingHunts: true } procedure && procedure.Route.Contains("moria");
}

public sealed record BalrogHuntStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        HuntRuntime.BeginTile(context.State, context.Execution.SourceId, tileRule: HuntTileRule.IgnoreEyes);
        var procedure = FellowshipFlow.Active(context.State)!;
        var region = BoardRules.Region(context.State, procedure.Route[^1]);
        var hunt = HuntRuntime.Active(context.State)!;
        hunt.DeclarationHunt = procedure.Kind == FellowshipProcedureKind.Declaration;
        hunt.IgnoreRevealIcon = hunt.DeclarationHunt && region.OriginalOwner == Side.FreePeoples &&
            region.Settlement is "City" or "Stronghold";
    }
}

public sealed record NearMoriaBattle : ICombatCondition
{
    private static readonly HashSet<string> Regions = BuildRegions();
    public bool CanPlay(MatchState state, BattleState battle, Side actor) => Regions.Contains(battle.Defender.Region);

    private static HashSet<string> BuildRegions()
    {
        var neighbors = BoardRules.Regions["moria"].Neighbors;
        return neighbors.Concat(neighbors.SelectMany(id => BoardRules.Regions[id].Neighbors)).Append("moria").ToHashSet();
    }
}

public sealed record DurinsBaneStep : EffectStep
{
    public override void Apply(EffectContext context) => BattleRoll.Special(context.State, BattleRuntime.Active(context.State)!,
        context.Owner, context.Execution.SourceId, 3, context.Random, target: 4);
}
