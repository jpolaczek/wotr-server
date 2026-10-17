using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsProjection
{
    public static List<SovereignView> Sovereigns(MatchState state, Side viewer) => state.Configuration.Kings && state.Kings is not null
        ? state.Kings.Sovereigns.Select(s => Sovereign(state, s, viewer)).ToList() : [];

    internal static List<SovereignView> PublicSovereigns(MatchState state) => state.Kings?.Sovereigns
        .Select(s => Sovereign(state, s, null)).ToList() ?? [];

    private static SovereignView Sovereign(MatchState state, SovereignState sovereign, Side? viewer)
    {
        var definition = SovereignCatalog.All[sovereign.Id];
        var visible = CanReadTiles(state, sovereign, viewer);
        return new()
        {
            Id = sovereign.Id, Title = definition.Title, Nation = definition.Nation, Status = sovereign.Status,
            DefinitionId = sovereign.Status == SovereignStatus.Corrupted ? definition.CorruptedCard : definition.AwakenedCard,
            RegionId = SovereignLifecycle.Region(state, sovereign), Resistance = definition.Resistance,
            Garrison = sovereign.Status == SovereignStatus.Initial ? state.Units.Find(u => u.Id == sovereign.Id)?.Garrison == true :
                sovereign.Status == SovereignStatus.Awakened && state.Characters.Find(c => c.Id == sovereign.Id)?.Garrison == true,
            Corruption = visible ? sovereign.Tiles.Sum(KingsTiles.CorruptionValue) : null,
            AttemptedThisTurn = sovereign.LastAttemptTurn == state.Turn,
            Tiles = sovereign.Tiles.Select((id, index) => Tile(id, $"{sovereign.Id}:tile:{index}", visible)).ToList()
        };
    }

    private static bool CanReadTiles(MatchState state, SovereignState sovereign, Side? viewer) =>
        (viewer.HasValue ? Visibility.CanReadCorruption(state, viewer.Value, Side.Shadow) : state.Configuration.VisibleCorruption) || viewer == Side.FreePeoples && state.Pending?.Owner == viewer &&
        EffectRuntime.Current(state)?.Effect is { Wait: EffectWait.Choice } effect && effect.InspectedSovereign == sovereign.Id;

    // UI-141: tiles that left the Hunt pool face down for this viewer, by their location: Corruption tiles on a
    // sovereign it cannot read (Kings tiles are always public) and the unrevealed tiles of a corruption draw it does
    // not make.
    internal static IEnumerable<string> UnseenTiles(MatchState state, Side viewer)
    {
        if (state.Kings is null)
            return [];
        var hidden = state.Kings.Sovereigns.Where(s => !CanReadTiles(state, s, viewer)).Select(s => s.Id).ToHashSet();
        var revealed = CorruptionRuntime.Current(state)?.Ignored ?? [];
        return state.HuntTiles.Where(t => !KingsTiles.IsKings(t.Id))
            .Where(t => hidden.Contains(t.Location) || viewer != Side.Shadow && t.Location == "corruption-draw" && !revealed.Contains(t.Id))
            .Select(t => t.Id);
    }

    private static CorruptionTileView Tile(string id, string slot, bool visible)
    {
        visible |= KingsTiles.IsKings(id);
        return new()
        {
            Id = visible ? id : slot, TileId = visible ? id : "", Value = visible ? KingsTiles.CorruptionValue(id) : null,
            Kings = KingsTiles.IsKings(id), Reveal = visible && HuntPool.Definitions[id].Reveal
        };
    }

    public static CorruptionAttemptView? Attempt(MatchState state, Side viewer) => CorruptionRuntime.Current(state) is { } attempt
        ? new()
        {
            SovereignId = attempt.SovereignId, DieId = attempt.DieId, DrawLimit = attempt.DrawLimit,
            Sequential = state.Configuration.SequentialCorruption,
            DrawnCount = attempt.Drawn.Count, RevealedTiles = [.. attempt.Ignored],
            OwnChoices = viewer != Side.Shadow ? [] : (state.Configuration.SequentialCorruption
                ? CorruptionRules.Choices(attempt) : attempt.Drawn).Select(id => Tile(id, id, true)).ToList()
        } : null;
}
