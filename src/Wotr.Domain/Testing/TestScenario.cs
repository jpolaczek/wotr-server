using Wotr.Contracts;

namespace Wotr.Domain;

// TEST-01: a named, repeatable situation. The builder receives the match id and a source seeded from
// the scenario id, so one server version always builds the same state (dice, hands, deck order).
// Randomness during play (combat, Hunt) still comes from the server.
// TEST-10: Modules names the expansion modules the built game uses (ExpansionModules ids); empty for the base game.
public sealed record TestScenario(string Id, string Group, string Title, string Description, Side StartingSide,
    Func<string, IRandomSource, MatchState> Build)
{
    public IReadOnlyList<string> Modules { get; init; } = [];

    public MatchState Create(string matchId) => Build(matchId, new SeededRandom(Seed(Id)));

    public TestScenarioView View() => new()
    {
        Id = Id,
        Group = Group,
        Title = Title,
        Description = Description,
        StartingSide = StartingSide,
        Modules = [.. Modules]
    };

    // FNV-1a: string.GetHashCode differs between processes.
    private static int Seed(string id)
    {
        var hash = 2166136261u;
        foreach (var character in id)
            hash = (hash ^ character) * 16777619u;
        return (int)(hash & int.MaxValue);
    }
}

public sealed class SeededRandom(int seed) : IRandomSource
{
    private readonly Random random = new(seed);

    public int Next(int exclusiveMaximum) => random.Next(exclusiveMaximum);
}
