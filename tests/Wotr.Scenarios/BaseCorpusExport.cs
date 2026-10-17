using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class BaseCorpusExport
{
    private static readonly string[] Names =
    [
        "turn-hunt-mandatory", "allocation", "field", "discard", "grey", "policy-fp", "no-regular",
        "boundary-random-companion", "sh-str03-king-excess", "another-way", "reveal", "guide", "alone",
        "hunt-reroll", "strategy-confusion", "retreat", "sh-str02-corsairs-relief", "relief",
        "sh-str02-corsairs-defense", "siege", "strategy-march", "shadow-search",
        "ring-paid", "secret-combat", "ring-destroyed", "ring-corrupted", "military-fp", "military-sh", "paid-army-step"
    ];
    private static readonly JsonSerializerOptions Json = new(BoardCatalog.Json) { WriteIndented = true };

    public static void Write(string destination)
    {
        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
            throw new InvalidOperationException("Export into an empty directory, then review changes before replacing a corpus.");
        var seeds = BaseCorpusSeeds.Create();
        var cases = Names.Select(name => Build(name, seeds[name])).ToArray();
        Directory.CreateDirectory(destination);
        foreach (var entry in cases)
            File.WriteAllText(Path.Combine(destination, entry.Name + ".json"), entry.Json, new UTF8Encoding(false));
        var manifest = new
        {
            Format = 1,
            Baseline = "T23 / BASE 2.4.1 / FAQ 1.2",
            RulesVersion = Protocol.RulesVersion,
            ContentVersion = Protocol.ContentVersion,
            Cases = cases.Select(c => c.Manifest).ToArray()
        };
        File.WriteAllText(Path.Combine(destination, "manifest.json"), JsonSerializer.Serialize(manifest, Json) + "\n");
        Console.WriteLine($"Exported {cases.Length} reviewed-case candidates to {destination}. Review before committing.");
    }

    private static (string Name, string Json, object Manifest) Build(string name, MatchState state)
    {
        var json = JsonSerializer.Serialize(state, Json) + "\n";
        GameCommand? command = null;
        MatchState? after = null;
        if (state.Pending is not null)
        {
            command = BaseCorpusResume.For(state);
            var result = BaseCorpusSeeds.Engine.Execute(state, state.Pending.Owner, command, new BaseCorpusSeeds.ZeroRandom());
            if (!result.Receipt.Accepted)
                throw new InvalidOperationException($"Corpus {name}/{command.Kind}: {result.Receipt.Error}");
            after = result.State;
        }
        return (name, json, new
        {
            Name = name,
            File = name + ".json",
            Sha256 = Hash(json),
            state.SchemaVersion,
            Decision = state.Pending?.Kind,
            Owner = state.Pending?.Owner,
            state.Victory,
            Command = command,
            ExpectedStateSha256 = after is null ? null : Hash(StateCodec.Write(after)),
            ExpectedDecision = after?.Pending?.Kind,
            ExpectedOwner = after?.Pending?.Owner,
            ExpectedVictory = after?.Victory,
            ExpectedRandomSamples = after?.RandomSamples.Count
        });
    }

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
