using System.Text.Json;
using System.Text.Json.Serialization;
using Wotr.Contracts;

namespace Wotr.Domain.Content;

public static class BoardCatalog
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    public static BoardData LoadBase()
    {
        using var stream = typeof(BoardCatalog).Assembly.GetManifestResourceStream("Wotr.base-board.json")!;
        return Load(new StreamReader(stream).ReadToEnd());
    }
    public static BoardData Load(string json)
    {
        var data = JsonSerializer.Deserialize<BoardData>(json, Json) ?? throw new InvalidDataException("Missing board.");
        Validate(data);
        return data;
    }
    public static void Validate(BoardData board)
    {
        void Require(bool valid, string message)
        {
            if (!valid)
                throw new InvalidDataException(message);
        }
        Require(board.Version == "base-setup-1", "Unsupported content version.");
        Require(board.Regions.Select(r => r.Id).Distinct().Count() == board.Regions.Count, "Duplicate region.");
        Require(board.Nations.Select(r => r.Id).Distinct().Count() == board.Nations.Count, "Duplicate nation.");
        var regions = board.Regions.ToDictionary(r => r.Id);
        var nations = board.Nations.ToDictionary(n => n.Id);
        foreach (var r in board.Regions)
        {
            Require(r.Nation is null || nations.ContainsKey(r.Nation), "Unknown nation.");
            Require(r.OriginalOwner == (r.Nation is null ? null : nations[r.Nation].Side), "Incorrect original owner.");
            Require(r.VictoryPoints == (r.Settlement == "Stronghold" ? 2 : r.Settlement == "City" ? 1 : 0), "Invalid settlement value.");
            Require(r.Neighbors.Distinct().Count() == r.Neighbors.Count, "Duplicate edge.");
            foreach (var n in r.Neighbors)
                Require(n != r.Id && regions.ContainsKey(n) && regions[n].Neighbors.Contains(r.Id), "Invalid/asymmetric edge.");
        }
        foreach (var b in board.ImpassableBorders)
            Require(regions.ContainsKey(b.A) && regions.ContainsKey(b.B) && !regions[b.A].Neighbors.Contains(b.B), "Impassable edge is traversable.");
        Require(board.Units.Select(u => u.Id).Distinct().Count() == board.Units.Count, "Duplicate piece.");
        foreach (var u in board.Units)
            Require(nations.ContainsKey(u.Nation) && (u.Location == "reserve" || regions.ContainsKey(u.Location)), "Invalid placement.");
        Require(board.Cards.Count == 96 && board.Cards.Distinct().Count() == 96, "Invalid card slots.");
        Require(board.Dice.Count == 2 && board.Dice.All(d => d.Faces.Count == 6 && d.InitialCount <= d.MaximumCount), "Invalid dice.");
        Require(board.HuntTiles.Count == 24 && board.HuntTiles.Select(t => t.Id).Distinct().Count() == 24, "Invalid tile inventory.");
    }
}
