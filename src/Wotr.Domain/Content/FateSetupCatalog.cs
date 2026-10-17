using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Domain.Content;

internal sealed class FateSetupData
{
    public string Version { get; set; } = "";
    public string BaseVersion { get; set; } = "";
    public List<RegionDefinition> Regions { get; set; } = [];
    public List<RegionDefinition> NewCities { get; set; } = [];
    public List<FateArmyTotal> Units { get; set; } = [];
    public List<string> ActiveNations { get; set; } = [];
}

internal sealed class FateArmyTotal
{
    public string Region { get; set; } = "";
    public string Nation { get; set; } = "";
    public int Regular { get; set; }
    public int Elite { get; set; }
    public int Leader { get; set; }

    public int Count(UnitKind kind) => kind switch
    {
        UnitKind.Regular => Regular,
        UnitKind.Elite => Elite,
        UnitKind.Leader => Leader,
        _ => 0
    };
}

internal static class FateSetupCatalog
{
    public static FateSetupData Data { get; } = Load();

    private static FateSetupData Load()
    {
        using var stream = typeof(FateSetupCatalog).Assembly.GetManifestResourceStream("Wotr.fate-setup.json")!;
        var data = JsonSerializer.Deserialize<FateSetupData>(stream, BoardCatalog.Json)
            ?? throw new InvalidDataException("Missing Fate setup.");
        if (data.Version != "fate-setup-1" || data.BaseVersion != "base-setup-1")
            throw new InvalidDataException("Unsupported Fate setup version.");
        var regions = data.Regions.Concat(data.NewCities).ToArray();
        if (regions.Select(r => r.Id).Distinct().Count() != regions.Length ||
            regions.Any(r => !BoardRules.Regions.ContainsKey(r.Id) ||
                r.Nation is not null && !BoardRules.Nations.ContainsKey(r.Nation)))
            throw new InvalidDataException("Invalid Fate region overrides.");
        if (data.Units.Select(u => (u.Region, u.Nation)).Distinct().Count() != data.Units.Count ||
            data.Units.Any(u => !BoardRules.Regions.ContainsKey(u.Region) || !BoardRules.Nations.ContainsKey(u.Nation) ||
                u.Regular < 0 || u.Elite < 0 || u.Leader < 0) ||
            data.ActiveNations.Any(n => !BoardRules.Nations.ContainsKey(n)))
            throw new InvalidDataException("Invalid Fate setup totals.");
        return data;
    }
}
