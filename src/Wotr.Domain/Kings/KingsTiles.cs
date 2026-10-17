using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsTiles
{
    public const string One = "kings.hunt.1";
    public const string Two = "kings.hunt.2";
    public static IReadOnlyList<HuntTileDefinition> Definitions { get; } =
    [
        new() { Id = One, Pool = "standard", Value = "1" },
        new() { Id = Two, Pool = "standard", Value = "2" }
    ];

    public static bool IsKings(string id) => id is One or Two;
    public static int CorruptionValue(string id) => IsKings(id) ? 0 : int.Parse(HuntPool.Definitions[id].Value);
    public static bool IsNumeric(string id) => HuntPool.Definitions[id].Pool == "standard" &&
        int.TryParse(HuntPool.Definitions[id].Value, out _);

    public static void Return(MatchState state, IEnumerable<string> ids)
    {
        foreach (var id in ids)
            state.HuntTiles.Single(t => t.Id == id).Location = "pool";
    }

    public static void Reveal(MatchState state, IEnumerable<string> ids)
    {
        foreach (var id in ids)
            state.Events.Add(new() { Revision = state.Revision, Kind = "CorruptionTileRevealed",
                Activity = new() { Automatic = true, Turn = state.Turn, Context = new() { Kind = ActionContextKind.Characters },
                    Facts = new() { Targets = [id] } },
                Text = $"Corruption tile: {id}." });
    }
}
