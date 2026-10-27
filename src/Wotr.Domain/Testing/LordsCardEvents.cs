using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-12: what the board of a Lords card's event scenario needs, in one sentence and as changes after the real Lords
// setup; a change may run an earlier play of the imagined turn through the engine, so it returns the board it leaves.
internal sealed record LordsCardSetup(string Description, Func<ScenarioBoard, ScenarioBoard> Arrange);

// TEST-12: the board of each Lords card's event scenario lets every clause of the card act, its "if" clauses included
// (as UI-300 for the base cards): Sméagol tiles set aside to add, Corruption to heal and a Sméagol card to discard,
// Shadow table cards to discard, used Elven Rings to recover, a Shadow army beside the North, the Balrog to activate,
// a Sméagol tile the Hunt will draw, a stronghold the reveal can cross, a second Character die for the Captain, Nazgûl
// Leadership to recruit with, and targets for the armies the cards move or attack.
internal static class LordsCardEvents
{
    private const string Hollin = "hollin";
    private const string Moria = "moria";
    private const string DolGuldur = "dol-guldur";
    private const string Gundabad = "mount-gundabad";
    private const string ShadowCharacterCard = "base.sh.character.01";

    private static readonly LordsCardSetup Ready = new("Nothing else is needed for every clause to act.", board => board);

    private static IReadOnlyDictionary<string, LordsCardSetup> Setups { get; } = new Dictionary<string, LordsCardSetup>
    {
        [LordsFreePeoplesCards.YouKnowTheWay] = new("The two other Sméagol tiles wait set aside since the setup, so the card adds both to the Hunt Pool.",
            board => board),
        [LordsFreePeoplesCards.SafePaths] = new("Sméagol guides the Fellowship, which carries 2 Corruption, and the Shadow played We Won't Go " +
            "Back on its table earlier this turn: heal one Corruption and discard it.",
            board => LordsBoard.Played(board.Corruption(2).Change(SmeagolGuides), Side.Shadow, LordsShadowCards.WeWontGoBack)),
        [LordsFreePeoplesCards.Wind] = new("The Shadow played The Gates are Closed and The Lore of the Elven Rings on its table earlier this " +
            "turn: discard one of them (lords-card-wind-finish plays the hand inspection without table cards).",
            board => LordsBoard.Played(LordsBoard.Played(board, Side.Shadow, LordsShadowCards.Gates), Side.Shadow, LordsShadowCards.Lore)),
        [LordsFreePeoplesCards.ThreeRings] = new("The Shadow used Narya, which is out of play, and holds Nenya and Vilya: each 5 or 6 of the " +
            "three dice recovers one, Narya first.",
            board => board.Change(UsedRings)),
        [LordsFreePeoplesCards.EveryLittle] = new("Pelargir, Lossarnach and Lamedon are free and Gondor's reserve holds Regulars: the card " +
            "recruits one in each although Gondor is not at war, then draws a Strategy card.",
            board => board),
        [LordsFreePeoplesCards.Rangers] = new("A Sauron army stands in the Weather Hills, next to Bree and the North Downs: the Rangers roll " +
            "against it, then a North unit and a North Leader enter a free North region beside it.",
            board => board.Place("sauron", UnitKind.Regular, "weather-hills", 2)),
        [LordsShadowCards.Balrog] = new("The Balrog waits inactive in Moria: the card activates him at Level 2 (lords-card-balrog plays " +
            "the active Balrog's move).",
            board => board.Change(state => LordsBoard.Put(state, LordsCharacters.Balrog, Moria).Level = 0)),
        [LordsShadowCards.WeWontGoBack] = new("Sméagol guides the Fellowship. Play the card on the table, then switch side and move the " +
            "Fellowship with the Character die: five Shadow dice wait in the Hunt Box, it already moved twice (+2) and the pool holds " +
            "only the second Sméagol tile. When it is drawn, discard a Character card from the hand or this card for 2 Corruption.",
            board => board.Change(SmeagolGuides).HuntAhead(Side.Shadow, DieFace.Character, tile => tile.Pool == SmeagolTiles.Pool)
                .Hand(Side.Shadow, ShadowCharacterCard)),
        [LordsShadowCards.Lore] = new("Elrond stands in Rivendell, so the Vilya die rolls from the next turn. Play the card on the table; " +
            "when a Keeper die rolls an Eye, change one Shadow result before the actions (lords-card-lore-change-die).",
            board => board.Change(state => LordsBoard.Put(state, LordsCharacters.Elrond, "rivendell"))),
        [LordsShadowCards.Gates] = new("The Fellowship stands in Hollin, next to Moria. Play the card on the table, then switch side and " +
            "move the Fellowship with the Character die: the Hunt is almost certain and draws a Reveal tile, and a reveal into Moria, an " +
            "unconquered Shadow Stronghold, lets you discard the card for an additional Hunt tile.",
            board => board.FellowshipAt(Hollin).HuntAhead(Side.Shadow, DieFace.Character, tile => tile.Reveal && tile.Value is "1" or "2")),
        [LordsShadowCards.Captain] = new("The Witch-king is in play in Minas Morgul and Strider guides the Fellowship. Play the card on the " +
            "table, then use the second Character die, e.g. to move a Nazgûl: after that action the die joins the Hunt Box as an Eye.",
            board => board.Enter(CharacterRules.WitchKing, "minas-morgul")),
        [LordsShadowCards.DolGuldur] = new("Three Nazgûl stand in Dol Guldur (Leadership 3), so three Sauron Regulars are recruited there; " +
            "then the army may attack the Elven army in the Narrows of the Forest (Sauron and the Elves are at war).",
            board => board.AtWar("sauron", "elves").Place("sauron", UnitKind.Nazgul, DolGuldur, 2)
                .Place("elves", UnitKind.Regular, "narrows-of-the-forest", 2)),
        [LordsShadowCards.Madness] = new("Sauron is at war. A Gondor army holds North Ithilien, next to Minas Morgul, and a North army that " +
            "took Mount Gundabad is besieged there: two Sauron Regulars are recruited beside the first target, or inside Mount Gundabad.",
            board => board.AtWar("sauron").Place("gondor", UnitKind.Regular, "north-ithilien", 2).Clear(Gundabad)
                .Place("north", UnitKind.Regular, Gundabad, 2).Change(state => state.Control[Gundabad] = Side.FreePeoples)
                .Besiege(Gundabad, "sauron", 2))
    };

    public static LordsCardSetup For(string cardId) => Setups.GetValueOrDefault(cardId) ?? Ready;

    // The owner acts on turn 1 with a die of the card's event type and an Event die, as in the base card scenarios
    // (CardEventSetups); the board's changes follow and the owner takes the action again.
    public static MatchState Build(string id, IRandomSource random, CardDefinition card, LordsCardSetup setup)
    {
        var state = CardEventSetups.Build(LordsBoard.Setup(id, random), card, new("", _ => { }));
        return setup.Arrange(ScenarioBoard.Of(state)).Act(card.Side);
    }

    // Sméagol joined the Fellowship as its Guide when the Hunt drew his first tile, which left the game.
    public static void SmeagolGuides(MatchState state)
    {
        state.HuntTiles.Single(t => t.Id == SmeagolTiles.Definitions[0].Id).Location = "removed";
        SmeagolRules.Enter(state);
    }

    private static void UsedRings(MatchState state)
    {
        state.Rings.Single(r => r.Id == LordsDiceSetup.NaryaRing).Owner = null;
        state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner = Side.Shadow;
        state.Rings.Single(r => r.Id == LordsDiceSetup.VilyaRing).Owner = Side.Shadow;
    }
}
