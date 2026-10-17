using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record UnitReductionResult(UnitState? Replacement, string PreviousLocation);

public static class UnitReduction
{
    public static UnitState? ReduceElite(MatchState state, UnitState elite) => ReduceEliteWithOrigin(state, elite).Replacement;

    public static UnitReductionResult ReduceEliteWithOrigin(MatchState state, UnitState elite)
    {
        if (elite.Kind != UnitKind.Elite || !BoardRules.Regions.ContainsKey(elite.Location))
            throw new InvalidOperationException("Only an elite army unit on the board can be reduced.");
        var replacement = state.Units.Where(u => u.Nation == elite.Nation && u.Kind == UnitKind.Regular && u.Location is "casualties" or "reserve")
            .OrderBy(u => u.Location == "casualties" ? 0 : 1).ThenBy(u => u.Id, StringComparer.Ordinal).FirstOrDefault();
        var origin = replacement?.Location ?? "";
        if (replacement is not null)
        {
            replacement.Location = elite.Location;
            replacement.Garrison = elite.Garrison;
        }
        BoardRules.RemoveCasualty(state, elite);
        return new(replacement, origin);
    }
}
