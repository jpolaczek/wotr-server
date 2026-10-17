using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record CompanionVersion(string FigureId, string BaseId, string AlternateId);

public static class CouncilCompanions
{
    public const string Strider = "lords.character.strider";
    public const string Merry = "lords.character.merry";
    public const string Pippin = "lords.character.pippin";
    public const string MovementLevelTwo = "movement-level-two";
    public static IReadOnlyList<CompanionVersion> Versions { get; } = new[]
    {
        "gandalf-grey", "strider", "boromir", "gimli", "legolas", "merry", "pippin"
    }.Select(name => new CompanionVersion("base.character." + name, "base.character." + name, "lords.character." + name)).ToArray();
    private static readonly IReadOnlyDictionary<string, string> BaseNames = Versions.ToDictionary(v => v.AlternateId, v => v.FigureId);
    private static readonly IReadOnlyList<CompanionVersion> FateVersions = Versions.Select(v => v.FigureId == FateGimli.Figure
        ? new CompanionVersion(v.FigureId, FateGimli.Standard, FateGimli.Alternate) : v).ToArray();

    private static readonly IReadOnlyDictionary<string, string> HomelandRegions = new Dictionary<string, string>
    {
        ["lords.character.boromir"] = "minas-tirith",
        ["lords.character.gimli"] = "erebor",
        ["lords.character.legolas"] = "woodland-realm",
        [Merry] = "the-shire",
        [Pippin] = "the-shire"
    };

    public static IEnumerable<CompanionVersion> AvailableVersions(MatchState state)
    {
        var versions = state.Configuration.Fate ? FateVersions : Versions;
        return state.Configuration.Council ? versions : versions.Take(1);
    }

    public static bool ValidVersions(MatchState state, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0 || ids.Count != ids.Distinct(StringComparer.Ordinal).Count())
            return false;
        var groups = AvailableVersions(state).ToArray();
        return ids.All(id => groups.Any(g => id == g.BaseId || id == g.AlternateId)) &&
            groups.All(g => ids.Count(id => id == g.BaseId || id == g.AlternateId) <= 1) &&
            ids.Count(id => id == CharacterRules.Grey || id == LordsCharacters.Gandalf) == 1;
    }

    public static IReadOnlyList<string> Homelands(CharacterState character) =>
        character.DefinitionId == FateGimli.Alternate ? FateGimli.Homelands :
            HomelandRegions.TryGetValue(character.DefinitionId, out var region) ? [region] : [];

    public static string Selected(CompanionVersion version, IReadOnlyList<string> ids) =>
        ids.Contains(version.AlternateId) ? version.AlternateId : version.BaseId;

    public static IEnumerable<CharacterState> CanStartOutside(MatchState state) =>
        CompanionRules.Inside(state).Where(c => Homelands(c).Count > 0);

    public static bool MatchesName(CharacterState character, string definitionId) =>
        BaseNames.GetValueOrDefault(character.DefinitionId) == definitionId;
}
