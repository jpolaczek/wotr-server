namespace Wotr.Domain;

// DEMO-02: the CPU's own small randomness, seeded by the match and its revision: the same state always gives the same
// choice (tests and restarts), while consecutive turns vary. It never decides a roll of the rules.
public sealed class DemoChoice(int seed)
{
    private readonly Random random = new(seed);

    public static DemoChoice For(MatchState state) => new(Seed(state.Id, state.Revision));

    public int Next(int minimum, int maximum) => random.Next(minimum, maximum + 1);

    // Picks items one by one, each with a chance proportional to its weight; zero weights never come up.
    public IEnumerable<T> Weighted<T>(IReadOnlyList<(T Item, int Weight)> items)
    {
        var left = items.Where(i => i.Weight > 0).ToList();
        while (left.Count > 0)
        {
            var roll = random.Next(left.Sum(i => i.Weight));
            var index = 0;
            while (roll >= left[index].Weight)
                roll -= left[index++].Weight;
            yield return left[index].Item;
            left.RemoveAt(index);
        }
    }

    public List<T> Shuffled<T>(IEnumerable<T> items) => items.OrderBy(_ => random.Next()).ToList();

    // FNV-1a: string.GetHashCode changes between processes.
    private static int Seed(string matchId, long revision)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in matchId + ":" + revision)
                hash = (hash ^ character) * 16777619u;
            return (int)hash;
        }
    }
}
