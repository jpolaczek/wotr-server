using Wotr.Contracts;

namespace Wotr.Domain;

public static class BalrogEntryRecruit
{
    public static IEnumerable<RecruitmentOption> Options(MatchState state, string definition)
    {
        if (definition != LordsCharacters.Balrog || !CharacterEntry.Unconquered(state, "moria", Side.Shadow) ||
            BoardRules.Besieged(state, "moria", Side.Shadow))
            yield break;
        foreach (var group in state.Units.Where(u => u.Location == "reserve" && u.Nation == "sauron" && BoardRules.IsArmyUnit(u)).GroupBy(u => u.Kind))
            yield return new() { Nation = "sauron", Kind = group.Key, ReserveIds = group.Select(u => u.Id).ToList(), Targets = ["moria"] };
    }

    public static CharacterEntryPlan? WithRecruitment(MatchState state, CharacterEntryPlan plan, IReadOnlyList<UnitPlacementChoice> placements)
    {
        if (placements.Count == 0)
            return plan;
        if (placements.Count != 1 || placements[0].RegionId != "moria" ||
            !Options(state, plan.DefinitionId).Any(o => o.ReserveIds.Contains(placements[0].UnitId)))
            return null;
        return plan with { Recruitment = placements[0] };
    }
}
