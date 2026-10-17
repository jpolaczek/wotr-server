using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class FateSetup
{
    // Setup totals replace complete region/nation groups, never add to the base army.
    public static void Apply(MatchState state)
    {
        if (!state.Configuration.Fate)
            return;
        if (state.Phase != GamePhase.Setup)
            throw new InvalidOperationException("Fate figures can only be placed during setup.");
        var data = FateSetupCatalog.Data;
        var replaced = data.Units.Select(u => (u.Region, u.Nation)).ToHashSet();
        foreach (var unit in state.Units.Where(u => replaced.Contains((u.Location, u.Nation))))
            BoardRules.ReturnToReserve(unit);
        foreach (var total in data.Units)
            foreach (var kind in Enum.GetValues<UnitKind>())
                Place(state, total, kind);
        foreach (var nation in state.Nations.Where(n => data.ActiveNations.Contains(n.Id)))
            nation.Active = true;
    }

    private static void Place(MatchState state, FateArmyTotal total, UnitKind kind)
    {
        var count = total.Count(kind);
        var units = state.Units.Where(u => u.Location == "reserve" && u.Nation == total.Nation && u.Kind == kind)
            .OrderBy(u => u.Id, StringComparer.Ordinal).Take(count).ToArray();
        if (units.Length != count)
            throw new InvalidDataException("Fate setup exceeds the physical reserve.");
        foreach (var unit in units)
            unit.Location = total.Region;
    }
}
