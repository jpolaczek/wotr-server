using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsCharacterEntry
{
    public static IEnumerable<CharacterEntryPlan> Candidates(MatchState state, Side actor)
    {
        if (!state.Configuration.Lords || state.Lords is null)
            yield break;
        if (actor == Side.FreePeoples)
        {
            foreach (var (id, region) in new[] { (LordsCharacters.Elrond, "rivendell"), (LordsCharacters.Galadriel, "lorien") })
                if (Absent(state, id) && (AtWar(state, "sauron") || AtWar(state, "elves")) &&
                    CharacterEntry.Unconquered(state, region, actor))
                    yield return Entry(state, id, id, region, actor);
            yield break;
        }
        if (Absent(state, LordsCharacters.Balrog))
            yield return Entry(state, LordsCharacters.Balrog, LordsCharacters.Balrog, "moria", actor);
        if (Absent(state, LordsCharacters.Gothmog) && AtWar(state, "sauron") &&
            CharacterEntry.Unconquered(state, "minas-morgul", actor) && CharacterRules.InPlay(state, CharacterRules.WitchKing) is null)
            yield return Entry(state, LordsCharacters.Gothmog, LordsCharacters.Gothmog, "minas-morgul", actor);
        if (Absent(state, LordsCharacters.Mouth) && (state.Fellowship.MordorStep.HasValue || MilitaryVictory.Points(state, Side.FreePeoples) >= 1))
            foreach (var region in CharacterEntry.SauronStrongholds(state))
                yield return Entry(state, CharacterRules.Mouth, LordsCharacters.Mouth, region, actor);
        foreach (var entry in WitchKing(state))
            yield return entry;
    }

    private static IEnumerable<CharacterEntryPlan> WitchKing(MatchState state)
    {
        var existing = state.Characters.Find(c => CharacterRules.IsWitchKing(c));
        var chiefAllowed = ChiefDestination(state, state.Fellowship.RegionId);
        if (existing is null)
        {
            if (chiefAllowed)
                yield return Entry(state, CharacterRules.WitchKing, LordsCharacters.WitchKing, state.Fellowship.RegionId, Side.Shadow);
            yield break;
        }
        if (!CharacterRules.OnBoard(existing) || state.Lords!.WitchKingReplaced)
            yield break;
        if (existing.DefinitionId == CharacterRules.WitchKing && chiefAllowed)
            yield return new(existing.Id, LordsCharacters.WitchKing, existing.Location, existing.Garrison, CharacterEntry.RequiredCost(LordsCharacters.WitchKing));
        if (existing.DefinitionId == LordsCharacters.WitchKing && CharacterEntry.BlackCaptainAllowed(state))
            yield return new(existing.Id, CharacterRules.WitchKing, existing.Location, existing.Garrison, CharacterEntry.RequiredCost(CharacterRules.WitchKing));
    }

    private static CharacterEntryPlan Entry(MatchState state, string figure, string definition, string region, Side actor) =>
        new(figure, definition, region, BoardRules.Besieged(state, region, actor), CharacterEntry.RequiredCost(definition));

    public static bool ChiefDestination(MatchState state, string region) =>
        BoardRules.Regions.ContainsKey(region) &&
        !(BoardRules.Region(state, region).Settlement == "Stronghold" && TravelRules.Sanctuary(state, region));

    private static bool AtWar(MatchState state, string nation) => state.Nations.Any(n => n.Id == nation && n.StepsToWar == 0);
    private static bool Absent(MatchState state, string definition) => !state.Characters.Any(c => CharacterRules.SameIdentity(c, definition));

    public static void Entered(MatchState state, CharacterState character)
    {
        if (character.DefinitionId != LordsCharacters.Balrog)
            return;
        PoliticalRules.Advance(state, "elves", PoliticalAdvanceCause.CompanionAbility);
        PoliticalRules.Advance(state, "dwarves", PoliticalAdvanceCause.CompanionAbility);
    }
}
