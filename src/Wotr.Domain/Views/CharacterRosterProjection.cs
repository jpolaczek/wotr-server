using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

internal static class CharacterRosterProjection
{
    public static List<CharacterRosterView> For(MatchState state, Side viewer)
    {
        var characters = state.Characters.Where(c => c.Side == viewer && Included(state, c)).ToArray();
        var identities = characters.ToDictionary(c => CharacterCatalog.All[c.DefinitionId].Identity);
        var rows = new Dictionary<string, CharacterRosterView>(StringComparer.Ordinal);
        var entries = new CharacterRosterEntries(state, viewer);
        foreach (var group in CharacterEntryDescriptions.For(state.Configuration, viewer)
                     .GroupBy(d => CharacterCatalog.All[d.DefinitionId].Identity))
        {
            var first = group.First();
            identities.TryGetValue(group.Key, out var character);
            if (character is null && state.Fellowship.Companions.Contains(first.FigureId))
                character = CharacterCatalog.Companion(state, first.FigureId);
            var row = Row(state, character, first);
            row.FutureVersions = group.Where(d => Future(state, character, d.DefinitionId))
                .Select(d => entries.Version(d, row.Lifecycle)).ToList();
            rows.Add(row.FigureId, row);
        }
        foreach (var character in characters)
            if (!rows.ContainsKey(character.Id) && !state.Fellowship.Companions.Contains(character.Id))
                rows.Add(character.Id, Row(state, character, null));
        return rows.Values.OrderBy(r => r.FigureId, StringComparer.Ordinal).ToList();
    }

    private static bool Included(MatchState state, CharacterState character)
    {
        if (!CharacterCatalog.All.TryGetValue(character.DefinitionId, out var definition) ||
            definition.Role is not ("Companion" or "Minion") || definition.Id == LordsCharacters.Smeagol)
            return false;
        return CharacterCatalog.Base.ContainsKey(definition.Id) ||
               state.Configuration.Lords && CharacterCatalog.Lords.ContainsKey(definition.Id) ||
               state.Configuration.Kings && CharacterCatalog.Kings.ContainsKey(definition.Id) ||
               state.Configuration.Fate && CharacterCatalog.Fate.ContainsKey(definition.Id);
    }

    private static CharacterRosterView Row(MatchState state, CharacterState? character, CharacterEntryDescription? entry)
    {
        var definition = CharacterCatalog.All[character?.DefinitionId ?? entry!.DefinitionId];
        var lifecycle = Lifecycle(state, character, entry);
        return new()
        {
            FigureId = character?.Id ?? entry!.FigureId,
            Identity = definition.Identity,
            Side = definition.Side,
            CurrentDefinitionId = character?.DefinitionId ?? "",
            Title = definition.Title,
            Lifecycle = lifecycle,
            RegionId = lifecycle == CharacterRosterLifecycle.OnBoard ? character!.Location : "",
            Garrison = lifecycle == CharacterRosterLifecycle.OnBoard && character!.Garrison,
            Level = character?.Level,
            MovementLevel = character is null ? null : CharacterRules.MovementLevel(character),
            CombatLevel = character is null ? null : CharacterRules.CombatLevel(character),
            Leadership = character?.Leadership,
            NationIcon = character?.NationIcon ?? "",
            UnlimitedMovement = character is not null && definition.UnlimitedMovement
        };
    }

    private static CharacterRosterLifecycle Lifecycle(MatchState state, CharacterState? character, CharacterEntryDescription? entry)
    {
        if (character is null)
            return entry!.FigureId is CharacterRules.Grey or CharacterRules.Strider
                ? CharacterRosterLifecycle.Unknown : CharacterRosterLifecycle.NotEntered;
        if (state.Fellowship.Companions.Contains(character.Id))
            return CharacterRosterLifecycle.InFellowship;
        if (CharacterRules.OnBoard(character))
            return CharacterRosterLifecycle.OnBoard;
        if (character.Location == "separating")
            return CharacterRosterLifecycle.AwaitingPlacement;
        if (Returning(state, character.Id))
            return CharacterRosterLifecycle.AwaitingReturn;
        if (character.Location != "casualties")
            return CharacterRosterLifecycle.Unknown;
        return character.DefinitionId is CharacterRules.Grey or LordsCharacters.Gandalf
            ? CharacterRosterLifecycle.EliminatedWithReplacement : CharacterRosterLifecycle.Eliminated;
    }

    private static bool Returning(MatchState state, string figure) =>
        state.Lords?.Council?.BattleReturns.Any(r => r.CharacterId == figure) == true ||
        state.Resolution.Frames.Any(f => f.EffectId == CouncilHobbitReturn.ProgramId && f.Effect?.SourceId == figure);

    private static bool Future(MatchState state, CharacterState? character, string version)
    {
        if (character is null)
            return true;
        if (version == CharacterRules.White)
            return character.DefinitionId is CharacterRules.Grey or LordsCharacters.Gandalf;
        if (version == CharacterRules.Aragorn)
            return character.DefinitionId is CharacterRules.Strider or CouncilCompanions.Strider &&
                   character.Location != "casualties";
        return CharacterRules.IsWitchKing(character) && character.DefinitionId != version &&
               CharacterRules.OnBoard(character) && state.Lords?.WitchKingReplaced == false;
    }
}
