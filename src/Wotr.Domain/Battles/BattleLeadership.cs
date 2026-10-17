using Wotr.Contracts;

namespace Wotr.Domain;

public static class BattleLeadership
{
    public static IReadOnlyDictionary<string, int> Available(MatchState state, BattleForce force)
    {
        var units = BattleRules.Units(state, force).ToList();
        var characters = BattleRules.Characters(state, force).ToList();
        var points = units.Where(u => IsLeader(state, force, u)).ToDictionary(u => u.Id, _ => 1);
        foreach (var character in characters)
            points.Add(character.Id, KingsBattle.Leadership(state, force, character));
        if (BattleRules.WhiteRiderActive(state))
        {
            foreach (var unit in units.Where(u => u.Kind == UnitKind.Nazgul))
                points[unit.Id] = 0;
            foreach (var character in characters.Where(c => c.DefinitionId == CharacterRules.White || CharacterRules.IsWitchKing(c)))
                points[character.Id] = 0;
        }
        foreach (var id in points.Keys.ToArray())
            points[id] = force.Round.CancelledLeadership.Contains(id) ? 0 : Math.Max(0, points[id] - force.Round.ForfeitedLeadership.GetValueOrDefault(id));
        return points;
    }

    public static int Total(MatchState state, BattleForce force) => Math.Max(0, Available(state, force).Values.Sum() - force.Round.LeadershipPenalty);

    public static bool IsLeader(MatchState state, BattleForce force, UnitState unit) =>
        CharacterRules.Leads(state, unit) || LordsCharacters.HouseholdLeader(state, force, unit);

    public static IReadOnlyDictionary<string, int> Nazgul(MatchState state, BattleForce force)
    {
        var ids = BattleRules.Units(state, force).Where(u => u.Kind == UnitKind.Nazgul).Select(u => u.Id)
            .Concat(BattleRules.Characters(state, force).Where(CharacterRules.IsWitchKing).Select(c => c.Id)).ToHashSet();
        return Available(state, force).Where(p => ids.Contains(p.Key) && p.Value > 0).ToDictionary(p => p.Key, p => p.Value);
    }

    public static bool CanForfeit(MatchState state, BattleForce force, IReadOnlyDictionary<string, int> points)
    {
        var available = Available(state, force);
        return points.Count > 0 && points.All(p => p.Value > 0 && available.GetValueOrDefault(p.Key) >= p.Value);
    }

    public static void Forfeit(MatchState state, BattleForce force, IReadOnlyDictionary<string, int> points)
    {
        if (!CanForfeit(state, force, points))
            throw new InvalidOperationException("Cannot forfeit unavailable leadership.");
        foreach (var point in points)
            force.Round.ForfeitedLeadership[point.Key] = force.Round.ForfeitedLeadership.GetValueOrDefault(point.Key) + point.Value;
    }
}
