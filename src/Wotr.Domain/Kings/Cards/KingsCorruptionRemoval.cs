namespace Wotr.Domain;

public static class KingsCorruptionRemoval
{
    public static string TileDescription(MatchState state) => state.Configuration.VisibleCorruption
        ? "non-Kings Corruption tile" : "hidden Corruption tile";

    public static void RandomHidden(EffectContext context, string sovereignId)
    {
        if (!KingsCardRestrictions.CanRemoveCorruption(context.State) ||
            SovereignLifecycle.Find(context.State, sovereignId) is not { Status: Wotr.Contracts.SovereignStatus.Initial } sovereign)
            return;
        var hidden = sovereign.Tiles.Where(t => !KingsTiles.IsKings(t)).ToArray();
        if (hidden.Length > 0)
            Return(context, sovereign, hidden[context.Random.Next(hidden.Length)]);
    }

    public static void Return(EffectContext context, SovereignState sovereign, string tile)
    {
        sovereign.Tiles.Remove(tile);
        KingsTiles.Return(context.State, [tile]);
        context.Record("CorruptionRemoved", $"A tile was returned from {SovereignCatalog.All[sovereign.Id].Title}.");
    }
}
