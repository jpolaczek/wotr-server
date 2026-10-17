using System.Reflection;
using System.Text.Json;
using Wotr.Contracts;

namespace Wotr.Domain.Content;

public sealed record CharacterDefinition(string Id, string Title, string Identity, Side Side, string Role,
    int? Level, int? Leadership, string? NationIcon, bool Initial, int ExtraActionDice,
    bool UnlimitedMovement, IReadOnlyList<string> Abilities);

public static class CharacterCatalog
{
    public static IReadOnlyDictionary<string, CharacterDefinition> Base { get; } = Load("base-characters");
    public static IReadOnlyDictionary<string, CharacterDefinition> Lords { get; } = Load("lords-characters");
    public static IReadOnlyDictionary<string, CharacterDefinition> Kings { get; } = Load("kings-characters");
    public static IReadOnlyDictionary<string, CharacterDefinition> Fate { get; } = Load("fate-characters");
    public static IReadOnlyDictionary<string, CharacterDefinition> All { get; } =
        Base.Values.Concat(Lords.Values).Concat(Kings.Values).Concat(Fate.Values).ToDictionary(c => c.Id, StringComparer.Ordinal);

    // UI-198: the name a player reads in the history; an unknown id stands for itself.
    public static string Title(string id) => All.TryGetValue(id, out var character) ? character.Title : id;

    private static IReadOnlyDictionary<string, CharacterDefinition> Load(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Wotr.{name}.json")!;
        return JsonSerializer.Deserialize<List<CharacterDefinition>>(stream, BoardCatalog.Json)!.ToDictionary(c => c.Id);
    }

    public static CharacterState Companion(MatchState state, string id)
    {
        var existing = state.Characters.FirstOrDefault(c => c.Id == id);
        if (existing != null)
            return existing;
        var definition = All[FateGimli.DefaultVersion(state.Configuration, id)];
        return new()
        {
            Id = id,
            DefinitionId = definition.Id,
            Side = definition.Side,
            Location = "fellowship",
            Level = definition.Level ?? 0,
            Leadership = definition.Leadership ?? 0,
            NationIcon = definition.NationIcon ?? ""
        };
    }
}
