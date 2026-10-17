namespace Wotr.Domain;

public sealed record AdjustHuntStep(int ReduceBy = 0, bool CancelTile = false, bool? Reveal = null) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var hunt = HuntRuntime.Active(context.State) ?? throw new InvalidOperationException("A Hunt effect requires a current tile.");
        if (hunt.DamageRule == HuntDamageRule.CorruptionOnly && (ReduceBy > 0 || CancelTile))
            throw new InvalidOperationException("This Hunt does not allow damage reduction.");
        hunt.Damage = Math.Max(0, hunt.Damage - ReduceBy);
        if (Reveal.HasValue)
            hunt.Reveal = Reveal.Value;
        if (CancelTile)
        {
            hunt.Damage = 0;
            hunt.Healing = 0;
            hunt.Reveal = false;
            hunt.Stop = false;
        }
        context.Record("HuntModified", $"{context.Execution.SourceId}: remaining Hunt damage {hunt.Damage}.");
    }
}

public sealed record DrawHuntTileStep(HuntTileRule TileRule = HuntTileRule.All, HuntDamageRule DamageRule = HuntDamageRule.Normal) : EffectStep
{
    public override void Apply(EffectContext context) => HuntRuntime.BeginTile(context.State, context.Execution.SourceId,
        tileRule: TileRule, damageRule: DamageRule);
}

public sealed record MoveFellowshipStep : EffectStep
{
    public override void Apply(EffectContext context) => HuntRuntime.BeginMove(context.State, context.Random, HuntReturn.Parent);
}
