using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class BoardRules
{
    private static readonly BoardData Base = BoardCatalog.LoadBase();
    public static IReadOnlyDictionary<string, RegionDefinition> Regions { get; } = Base.Regions.ToDictionary(r => r.Id);
    public static IReadOnlyDictionary<string, NationDefinition> Nations { get; } = Base.Nations.ToDictionary(n => n.Id);
    public static IEnumerable<RegionDefinition> SetupRegions(MatchConfiguration configuration) =>
        Regions.Values.Select(r => FateBoard.Region(configuration, r));
    public static RegionDefinition Region(MatchState state, string id) =>
        KingsBoard.Region(state, FateBoard.Region(state.Configuration, Regions[id]));
    public static IEnumerable<RegionDefinition> CurrentRegions(MatchState state) => Regions.Keys.Select(id => Region(state, id));
    public static Side Owner(UnitState unit) => Nations[unit.Nation].Side;
    public static bool IsArmyUnit(UnitState unit) => unit.Kind is UnitKind.Regular or UnitKind.Elite;
    public static bool IsSettlement(RegionDefinition region) => region.Settlement is "Town" or "City" or "Stronghold";
    public static bool HasEnemyArmy(MatchState state, string region, Side actor, bool? garrison = null) =>
        state.Units.Any(u => u.Location == region && IsArmyUnit(u) && Owner(u) != actor &&
            (!garrison.HasValue || u.Garrison == garrison));
    public static bool Besieged(MatchState state, string region, Side actor) =>
        Regions.ContainsKey(region) && Region(state, region).Settlement == "Stronghold" &&
        state.Control.GetValueOrDefault(region) == actor && HasEnemyArmy(state, region, actor, false);

    public static void RemoveCasualty(UnitState unit)
    {
        unit.Location = Owner(unit) == Side.Shadow ? "reserve" : "casualties";
        unit.Garrison = false;
    }
    public static void RemoveCasualty(MatchState state, UnitState unit)
    {
        RemoveCasualty(unit);
        if (SovereignLifecycle.Find(state, unit.Id) is { Status: SovereignStatus.Initial } sovereign)
            SovereignLifecycle.Remove(state, sovereign);
    }
    public static void ReturnToReserve(UnitState unit)
    {
        unit.Location = "reserve";
        unit.Garrison = false;
    }
}
