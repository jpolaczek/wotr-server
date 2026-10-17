using Wotr.Contracts;

namespace Wotr.Domain;

internal sealed record CharacterEntryDescription(string DefinitionId, string FigureId, string Source, string Requirements);

internal static class CharacterEntryDescriptions
{
    private static readonly CharacterEntryDescription[] FreePeoples =
    [
        new(CharacterRules.White, CharacterRules.Grey, "BASE p.40; LORDS p.20",
            "A qualifying Minion has entered at least once. Gandalf the Grey is outside the Fellowship: replace him in his region, or return after his elimination in Fangorn or an unconquered Elven stronghold."),
        new(CharacterRules.Aragorn, CharacterRules.Strider, "BASE p.40; LORDS p.30",
            "Strider is outside the Fellowship, in unconquered Minas Tirith, Dol Amroth or Pelargir. Replace the same figure.")
    ];

    private static readonly CharacterEntryDescription[] Shadow =
    [
        new(CharacterRules.Saruman, CharacterRules.Saruman, "BASE p.41",
            "Saruman has never entered. Isengard is at war and Orthanc is unconquered."),
        new(CharacterRules.WitchKing, CharacterRules.WitchKing, "BASE p.41; LORDS pp.28–29",
            "Sauron and at least one Free Peoples nation are at war. First entry requires a Sauron army; the living Chief of the Ringwraiths can instead be replaced in his region, once in the game."),
        new(CharacterRules.Mouth, CharacterRules.Mouth, "BASE p.41",
            "The Mouth of Sauron has never entered. The Fellowship is in Mordor or every Free Peoples nation is at war. Enter an unconquered Sauron stronghold.")
    ];

    private static readonly CharacterEntryDescription[] LordsFreePeoples =
    [
        new(LordsCharacters.Elrond, LordsCharacters.Elrond, "LORDS pp.16–17",
            "Elrond has never entered. Sauron or the Elves are at war and Rivendell is unconquered."),
        new(LordsCharacters.Galadriel, LordsCharacters.Galadriel, "LORDS pp.18–19",
            "Galadriel has never entered. Sauron or the Elves are at war and Lórien is unconquered.")
    ];

    private static readonly CharacterEntryDescription[] LordsShadow =
    [
        new(LordsCharacters.Balrog, LordsCharacters.Balrog, "LORDS pp.23–25",
            "The Balrog has never entered. Enter Moria, including when it is enemy-controlled."),
        new(LordsCharacters.Gothmog, LordsCharacters.Gothmog, "LORDS pp.25–26",
            "Gothmog has never entered. Sauron is at war, Minas Morgul is unconquered and the Black Captain is not in play."),
        new(LordsCharacters.Mouth, CharacterRules.Mouth, "LORDS p.27",
            "The Mouth of Sauron has never entered. The Fellowship is in Mordor or Free Peoples have at least one military victory point. Enter an unconquered Sauron stronghold."),
        new(LordsCharacters.WitchKing, CharacterRules.WitchKing, "LORDS pp.28–29",
            "The Fellowship's last known region is not an unconquered Free Peoples stronghold. First entry is in that region; the living Black Captain can instead be replaced in his own region, once in the game.")
    ];

    private static readonly CharacterEntryDescription[] KingsShadow =
    [
        new(KingsChieftains.Serpent, KingsChieftains.Serpent, "KINGS pp.14–15,18",
            "The Black Serpent has never entered. Gondor is at war. Enter a region with a Southrons and Easterlings army."),
        new(KingsChieftains.Mirkwood, KingsChieftains.Mirkwood, "KINGS pp.14–15,18",
            "The Shadow of Mirkwood has never entered. The Dwarves, Elves or North are at war. Enter a region with a Sauron army."),
        new(KingsChieftains.Ugluk, KingsChieftains.Ugluk, "KINGS pp.14–15,18",
            "Uglúk has never entered. Rohan is at war or the Fellowship is revealed. Enter with an Isengard army, or in a region free of enemy armies with or adjacent to the revealed Fellowship outside Mordor.")
    ];

    public static IEnumerable<CharacterEntryDescription> For(MatchConfiguration configuration, Side viewer)
    {
        var entries = viewer == Side.FreePeoples ? FreePeoples : Shadow;
        if (configuration.Lords)
            entries = [.. entries, .. viewer == Side.FreePeoples ? LordsFreePeoples : LordsShadow];
        if (configuration.Kings && viewer == Side.Shadow)
            entries = [.. entries, .. KingsShadow];
        return entries;
    }
}
