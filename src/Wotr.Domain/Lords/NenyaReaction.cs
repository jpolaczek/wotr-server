using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record NenyaCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => actor == Side.FreePeoples &&
        KeeperRings.Available(state, LordsDiceSetup.NenyaRing) &&
        HuntRuntime.Active(state) is { TileId.Length: > 0 } hunt &&
        HuntPool.Definitions[hunt.TileId] is { Pool: "standard", Value: "eye" };
}

public sealed record NenyaReplacementStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var hunt = HuntRuntime.Active(context.State)!;
        var original = hunt.TileId;
        KeeperRings.Transfer(context.State, LordsDiceSetup.NenyaRing);
        HuntPool.Remove(context.State, original);
        hunt.TileId = HuntPool.Draw(context.State, context.Random).Id;
        hunt.Stage = WeShallGetIt.AfterDraw(context.State);
        WeWontGoBackStep.Begin(context.State, hunt.TileId);
        context.Record("HuntTileReplaced", $"Nenya removed {original} from play and drew {hunt.TileId}.");
    }
}

public static class NenyaReaction
{
    public const string Id = "lords.nenya.replace";
    public static EffectDefinition Program => new(Id, [new NenyaReplacementStep()]);
    public static EffectReaction Reaction => new(Id, "hunt.replace", Id, new NenyaCondition(),
        Cost: ReactionCost.TransferNenya, RequiredCharacterId: KeeperRings.Galadriel);
}
