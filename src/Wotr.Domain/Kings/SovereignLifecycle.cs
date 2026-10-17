using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class SovereignLifecycle
{
    public static SovereignState? Find(MatchState state, string id) => state.Configuration.Kings
        ? state.Kings?.Sovereigns.Find(s => s.Id == id) : null;

    public static string Region(MatchState state, SovereignState sovereign) => sovereign.Status switch
    {
        SovereignStatus.Initial => state.Units.Find(u => u.Id == sovereign.Id)?.Location ?? "",
        SovereignStatus.Awakened => state.Characters.Find(c => c.Id == sovereign.Id)?.Location ?? "",
        SovereignStatus.Corrupted => sovereign.CorruptedRegion,
        _ => ""
    };

    public static void Awaken(MatchState state, SovereignState sovereign, string region)
    {
        if (sovereign.Status != SovereignStatus.Initial)
            throw new InvalidOperationException("Only an initial sovereign can awaken.");
        var unit = state.Units.Single(u => u.Id == sovereign.Id);
        var garrison = unit.Location == region && unit.Garrison;
        state.Units.Remove(unit);
        var character = new CharacterState { Id = sovereign.Id, Location = region, Garrison = garrison };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.Kings[SovereignCatalog.All[sovereign.Id].AwakenedCard]);
        state.Characters.Add(character);
        sovereign.Status = SovereignStatus.Awakened;
        ReturnTiles(state, sovereign);
        if (!state.Configuration.RemovingRulerTokens)
            state.Kings!.Tokens.Add(new() { SovereignId = sovereign.Id });
        CharacterLifecycle.ReconcileDice(state);
        Record(state, sovereign, "SovereignAwakened");
    }

    public static void Corrupt(MatchState state, SovereignState sovereign)
    {
        if (sovereign.Status != SovereignStatus.Initial)
            throw new InvalidOperationException("Only an initial sovereign can be corrupted.");
        KingsTiles.Reveal(state, sovereign.Tiles);
        var discarded = sovereign.Tiles.Where(id => !KingsTiles.IsKings(id))
            .OrderByDescending(KingsTiles.CorruptionValue).ThenByDescending(id => HuntPool.Definitions[id].Reveal).First();
        KingsTiles.Return(state, sovereign.Tiles.Where(id => id != discarded));
        state.HuntTiles.Single(t => t.Id == discarded).Location = "spent";
        sovereign.Tiles.Clear();
        state.Units.RemoveAll(u => u.Id == sovereign.Id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = SovereignCatalog.All[sovereign.Id].Home;
        Record(state, sovereign, "SovereignCorrupted");
        if (KingsCardRestrictions.OnTable(state, KingsCards03.Threats))
            TableCardRules.Discard(state, Side.Shadow, KingsCards03.Threats);
    }

    public static void Remove(MatchState state, SovereignState sovereign)
    {
        KingsDismay.SovereignRemoved(state, sovereign);
        ReturnTiles(state, sovereign);
        sovereign.Status = SovereignStatus.Removed;
        sovereign.CorruptedRegion = "";
        Record(state, sovereign, "SovereignRemoved");
    }

    public static void Reconcile(MatchState state)
    {
        if (!state.Configuration.Kings || state.Kings is null)
            return;
        foreach (var sovereign in state.Kings.Sovereigns)
            if (sovereign.Status is SovereignStatus.Initial or SovereignStatus.Awakened &&
                !BoardRules.Regions.ContainsKey(Region(state, sovereign)))
                Remove(state, sovereign);
    }

    private static void ReturnTiles(MatchState state, SovereignState sovereign)
    {
        KingsTiles.Return(state, sovereign.Tiles);
        sovereign.Tiles.Clear();
    }

    private static void Record(MatchState state, SovereignState sovereign, string kind) =>
        state.Events.Add(new() { Revision = state.Revision, Kind = kind,
            Activity = HistoryFacts.Sovereign(state, sovereign.Id),
            Text = $"{SovereignCatalog.All[sovereign.Id].Title}: {sovereign.Status}." });
}
