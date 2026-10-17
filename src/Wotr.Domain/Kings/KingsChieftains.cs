using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class KingsChieftains
{
    public const string Serpent = "kings.character.black-serpent";
    public const string Mirkwood = "kings.character.shadow-of-mirkwood";
    public const string Ugluk = "kings.character.ugluk";
    public static IReadOnlyList<string> Ids { get; } = [Serpent, Mirkwood, Ugluk];
    public static bool IsChieftain(string id) => id is Serpent or Mirkwood or Ugluk;

    public static IEnumerable<CharacterEntryPlan> Candidates(MatchState state, Side actor, bool ignorePrerequisite = false)
    {
        if (!state.Configuration.Kings || actor != Side.Shadow)
            yield break;
        foreach (var id in Ids)
        {
            if (state.Characters.Any(c => c.DefinitionId == id) || !ignorePrerequisite && !Prerequisite(state, id))
                continue;
            foreach (var region in Regions(state, id))
                yield return new(id, id, region, BoardRules.Besieged(state, region, Side.Shadow), CharacterEntry.RequiredCost(id));
        }
    }

    public static bool Prerequisite(MatchState state, string id) => id switch
    {
        Serpent => AtWar(state, "gondor"),
        Mirkwood => AtWar(state, "dwarves") || AtWar(state, "elves") || AtWar(state, "north"),
        Ugluk => AtWar(state, "rohan") || !state.Fellowship.Hidden,
        _ => false
    };

    public static IEnumerable<string> Regions(MatchState state, string id)
    {
        var nation = CharacterCatalog.Kings[id].NationIcon;
        var armyRegions = state.Units.Where(u => u.Nation == nation && BoardRules.IsArmyUnit(u) &&
            BoardRules.Regions.ContainsKey(u.Location)).Select(u => u.Location);
        var fellowshipRegions = id == Ugluk && !state.Fellowship.Hidden && !state.Fellowship.MordorStep.HasValue
            ? BoardRules.Regions[state.Fellowship.RegionId].Neighbors.Append(state.Fellowship.RegionId)
                .Where(r => BattleMovement.IsFree(state, Side.Shadow, r)) : [];
        return armyRegions.Concat(fellowshipRegions).Distinct();
    }

    private static bool AtWar(MatchState state, string nation) => state.Nations.Single(n => n.Id == nation).StepsToWar == 0;
}
