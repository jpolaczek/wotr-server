using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record SpecialHuntTileAvailable(string TileId) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.HuntTiles.Any(t => t.Id == TileId && t.Location == "set-aside");
}

public sealed record FellowshipCompanionPresent(IReadOnlyList<string> Ids) : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.Fellowship.Companions.Any(Ids.Contains);
}

public sealed record ShadowHuntDraw(bool RequireDamageReduction = false) : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        HuntRuntime.Active(state) is { DrawnBy: Side.Shadow, TileId.Length: > 0 } hunt &&
        (!RequireDamageReduction || hunt.Purpose == HuntPurpose.Hunt && hunt.DamageRule != HuntDamageRule.CorruptionOnly);
}

public sealed record PrepareHuntTileStep(string TileId) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        HuntPool.PrepareSpecial(context.State, TileId);
        context.Record("HuntTilePrepared", $"{context.Execution.SourceId}: {TileId} is in play.");
    }
}

public sealed record ReplaceHuntTileStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var state = context.State;
        var hunt = HuntRuntime.Active(state)!;
        var original = hunt.TileId;
        var tile = state.HuntTiles.Single(t => t.Id == original);
        if (tile.Location != "removed")
            tile.Location = "return-after-action";
        hunt.TileId = HuntPool.Draw(state, context.Random).Id;
        if (state.Configuration.Lords)
            hunt.Stage = WeShallGetIt.AfterDraw(state);
        context.Record("HuntTileReplaced", $"{context.Execution.SourceId}: replaced {original} with {hunt.TileId}.");
        WeWontGoBackStep.Begin(state, hunt.TileId);
    }
}
