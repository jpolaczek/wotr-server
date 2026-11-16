using System.Globalization;
using System.Text;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain.BotProtocol;

// BOT-225: the WotR Bot Protocol's shared vocabulary (SPEC §6, vocabulary-0.1.json: the base game) written from the
// server's own content ids. Region slugs are ours (two spellings differ), Nations and die faces are upper case, cards are
// keyed by side, deck and printed number, Hunt tiles by face and, for the special tiles, by the card that adds them.
public static class ProtocolVocabulary
{
    public const string Protocol = "wotr-bot/0.1";
    public const string Rules = "base-2e";

    private static readonly Dictionary<string, string> RegionSpelling = new(StringComparer.Ordinal)
    {
        ["northern-rhovanion"] = "northern-rovanion",
        ["southern-rhovanion"] = "southern-rovanion"
    };

    private static readonly Dictionary<string, string> CharacterNames = new(StringComparer.Ordinal)
    {
        ["gandalf-grey"] = "gandalf-the-grey",
        ["gandalf-white"] = "gandalf-the-white",
        ["merry"] = "meriadoc",
        ["pippin"] = "peregrin"
    };

    // The cards that add a special Hunt tile (FreePeoplesCharacterCards01, ShadowCharacterCards01): the tile is keyed by
    // its card's title.
    private static readonly Dictionary<string, string> SpecialTileCards = new(StringComparer.Ordinal)
    {
        ["base.hunt.special-fp.0.0.1"] = "base.fp.character.01",
        ["base.hunt.special-fp.0.0.2"] = "base.fp.character.02",
        ["base.hunt.special-fp.-2.0.1"] = "base.fp.character.03",
        ["base.hunt.special-fp.-1.0.1"] = "base.fp.character.04",
        ["base.hunt.special-sh.die.0.1"] = "base.sh.character.01",
        ["base.hunt.special-sh.eye.1.1"] = "base.sh.character.02",
        ["base.hunt.special-sh.3.0.1"] = "base.sh.character.03",
        ["base.hunt.special-sh.1.1.1"] = "base.sh.character.04"
    };

    private static readonly IReadOnlyList<HuntTileDefinition> Tiles = BoardCatalog.LoadBase().HuntTiles;

    public static string Side(Side side) => side == Contracts.Side.FreePeoples ? "FREE_PEOPLES" : "SHADOW";

    public static string Region(string id) => RegionSpelling.GetValueOrDefault(id, id);

    public static string Nation(string id) => id == "southrons" ? "SOUTHRONS_EASTERLINGS" : id.ToUpperInvariant();

    public static string Unit(UnitKind kind) => kind switch
    {
        UnitKind.Regular => "regular",
        UnitKind.Elite => "elite",
        UnitKind.Leader => "leader",
        _ => "nazgul"
    };

    public static string Face(DieFace face) => face switch
    {
        DieFace.Character => "CHARACTER",
        DieFace.Army => "ARMY",
        DieFace.Muster => "MUSTER",
        DieFace.ArmyMuster => "ARMY_MUSTER",
        DieFace.Event => "EVENT",
        DieFace.WillOfTheWest => "WILL_OF_THE_WEST",
        DieFace.Eye => "EYE",
        _ => face.ToString().ToUpperInvariant()
    };

    // A base character's id (base.character.<identity>) or its identity.
    public static string Character(string id)
    {
        var identity = id.StartsWith("base.character.", StringComparison.Ordinal) ? id["base.character.".Length..] :
            CharacterCatalog.All.TryGetValue(id, out var definition) ? definition.Identity : id;
        return CharacterNames.GetValueOrDefault(identity, identity);
    }

    // base.fp.character.07 → fp-char-07; base.sh.strategy.12 → sh-strat-12.
    public static string Card(string id)
    {
        var parts = id.Split('.');
        if (parts.Length != 4 || parts[0] != "base")
            return id;
        return parts[1] + "-" + (parts[2] == "character" ? "char" : "strat") + "-" + parts[3];
    }

    public static string Deck(string deck) => deck == "character" ? "character" : "strategy";

    // A special Hunt tile by its card's printed title (Shelob's Lair → shelobs-lair); a standard tile by its face, numbered
    // where several tiles share it (eye-1 … eye-4, 3-1 … 3-3, 2r, 1r-2, 0r-1).
    public static string HuntTile(string id)
    {
        if (SpecialTileCards.TryGetValue(id, out var card))
            return Slug(CardCatalog.Title(card));
        var parts = id.Split('.');
        if (parts.Length != 6 || parts[2] != "standard")
            return id;
        var (value, reveal, number) = (parts[3], parts[4] == "1", parts[5]);
        if (value == "eye")
            return "eye-" + number;
        var face = value + (reveal ? "r" : "");
        return StandardFaceCount(value, reveal) == 1 ? face : face + "-" + number;
    }

    // The keys of the standard tiles that share a face, in their printed order.
    public static IReadOnlyList<string> StandardTiles(string value, bool reveal) => Tiles
        .Where(t => t.Pool == "standard" && t.Value == value && t.Reveal == reveal).Select(t => HuntTile(t.Id)).ToList();

    // The special tiles of a pool whose face is (value, reveal), keyed by their cards.
    public static IReadOnlyList<(string Key, string Card)> SpecialTiles(string pool, string value, bool reveal) =>
        Tiles.Where(t => t.Pool == pool && t.Value == value && t.Reveal == reveal &&
            SpecialTileCards.ContainsKey(t.Id)).Select(t => (HuntTile(t.Id), Card(SpecialTileCards[t.Id]))).ToList();

    private static int StandardFaceCount(string value, bool reveal) => Tiles
        .Count(t => t.Pool == "standard" && t.Value == value && t.Reveal == reveal);

    // Lower case, accents and apostrophes dropped, every other run of non-letters a hyphen.
    public static string Slug(string text)
    {
        var plain = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c is not '\'' and not '’')
                plain.Append(char.ToLowerInvariant(c));
        var slug = new StringBuilder();
        foreach (var c in plain.ToString())
            if (char.IsLetterOrDigit(c))
                slug.Append(c);
            else if (slug.Length > 0 && slug[^1] != '-')
                slug.Append('-');
        return slug.ToString().Trim('-');
    }
}
