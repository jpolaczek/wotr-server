using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class StateCodec
{
    public const int CurrentVersion = 3;
    public static string Write(MatchState state) => JsonSerializer.Serialize(state, BoardCatalog.Json);
    public static MatchState Read(string json)
    {
        MatchState state;
        try
        {
            state = JsonSerializer.Deserialize<MatchState>(json, BoardCatalog.Json) ?? throw new InvalidDataException("CorruptSave");
        }
        catch (JsonException)
        {
            throw new InvalidDataException("CorruptSave");
        }
        if (state.SchemaVersion is not (1 or 2 or CurrentVersion) || state.RulesVersion != Protocol.RulesVersion || state.ContentVersion != Protocol.ContentVersion)
            throw new InvalidDataException("UnsupportedSaveVersion");
        SaveStructure.Validate(state);
        if (state.SchemaVersion == 1)
            SaveMigration.FromVersionOne(state);
        if (state.SchemaVersion == 2)
            SaveMigration.FromVersionTwo(state);
        SaveCorrections.Apply(state);
        return state;
    }
    public static MatchState Copy(MatchState state) => Read(Write(state));
}
