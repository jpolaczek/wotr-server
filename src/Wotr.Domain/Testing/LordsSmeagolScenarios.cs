using Wotr.Contracts;
using static Wotr.Domain.LordsCardScenarios;

namespace Wotr.Domain;

// TEST-12: Sméagol's cycle, a step per scenario (LORDS pp. 21-22, FAQ 1.2 p. 3): his tile brings him into the Fellowship
// as its Guide, he can be the Hunt's casualty at the level of the highest other Companion, and he leaves with a group
// that separates, alone (Gollum replaces him without the card) or at a declaration in an unconquered Free Peoples City or
// Stronghold; whenever he leaves otherwise, We Shall Get It enters the Shadow table (its use: lords-card-we-shall-get-it).
public static class LordsSmeagolScenarios
{
    public const string Group = "Lords · Sméagol";

    private const string Hollin = "hollin";

    public static IEnumerable<TestScenario> All() =>
    [
        Lords(new("lords-smeagol-joins", Group, "Sméagol joins the Fellowship as Guide",
            "Move the Fellowship with the Character die: five Shadow dice wait in the Hunt Box, it already moved twice (+2) and " +
            "the pool holds only Sméagol's two tiles. The drawn tile does no damage, leaves the game and Sméagol becomes the Guide.",
            Side.FreePeoples, Joins)),
        Lords(new("lords-smeagol-casualty", Group, "Sméagol as a Hunt casualty",
            "Sméagol guides the Fellowship. Move it with the Character die into an almost certain Hunt of damage tiles, then take " +
            "Sméagol as the casualty: he absorbs damage equal to the highest other Companion's level, leaves, and We Shall Get " +
            "It enters the Shadow table.",
            Side.FreePeoples, Casualty)),
        Lords(new("lords-smeagol-departure", Group, "Sméagol leaves: We Shall Get It or Gollum",
            "Sméagol guides the Fellowship in Hollin. Separate every Companion with him and We Shall Get It enters the Shadow " +
            "table; separate every Companion but him and he leaves as the last one, Gollum guides, and the card stays out.",
            Side.FreePeoples, Departure)),
        Lords(new("lords-smeagol-declared", Group, "Sméagol leaves at a declaration in Lórien",
            "Turn 2's Fellowship phase: Sméagol guides the Fellowship a step on from Dimrill Dale. Declare it in Lórien, an " +
            "unconquered Free Peoples Stronghold: Sméagol leaves, We Shall Get It enters the Shadow table and a new Guide is chosen.",
            Side.FreePeoples, Declared))
    ];

    private static MatchState Joins(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .HuntAhead(Side.FreePeoples, DieFace.Event, tile => tile.Pool == SmeagolTiles.Pool)
        .Act(Side.FreePeoples);

    private static MatchState Casualty(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .Change(LordsCardEvents.SmeagolGuides)
        .HuntAhead(Side.FreePeoples, DieFace.Event, tile => tile.Pool == "standard" && tile.Value is "2" or "3")
        .Act(Side.FreePeoples);

    private static MatchState Departure(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .Change(LordsCardEvents.SmeagolGuides)
        .FellowshipAt(Hollin)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
        .Act(Side.FreePeoples);

    // Sméagol joined on turn 1 and the Fellowship moved a step on from Dimrill Dale; turn 2 opens its Fellowship phase.
    private static MatchState Declared(string id, IRandomSource random)
    {
        var board = LordsBoard.Setup(id, random)
            .Change(LordsCardEvents.SmeagolGuides)
            .FellowshipAt("dimrill-dale", 1)
            .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
        return LordsBoard.NextTurn(board.State);
    }
}
