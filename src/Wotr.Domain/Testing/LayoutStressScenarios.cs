using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.KingsScenarioSetup;

namespace Wotr.Domain;

// TEST-15 (owner's request): the layout stress scenarios — faithful game states that fill the client's windows at once:
// the Characters tab with every alternative version, the five or three kings in each state and every Minion and
// Chieftain; a battle offering three Chieftains' replacements; a Hunt whose damage meets a response at each window; the
// Council token moving many Nazgûl and Minions; the White Rider's reaction at a battle's start; every pool resource of
// the expansions; the Effects panel with many table cards and corrupted kings; and The Nazgûl Strike's choices, board
// objects since the client's UI-430. Each is reached through the real setup and the engine's own commands, with the
// pieces placed here having entered earlier in the imagined game. A scenario names Kings first when it plays with Kings
// (the client shows it in the Kings tab), otherwise Lords.
public static class LayoutStressScenarios
{
    public const string KingsGroup = "Kings · Layout stress";
    public const string LordsGroup = "Lords · Layout stress";

    private const string Boromir = "base.character.boromir";
    private const string Gimli = "base.character.gimli";
    private const string Legolas = "base.character.legolas";
    private const string Merry = "base.character.merry";
    private const string AlternateBoromir = "lords.character.boromir";
    private const string AlternateGimli = "lords.character.gimli";
    private const string AlternateLegolas = "lords.character.legolas";
    private const string FreePeoplesCombatCard = "base.fp.strategy.01";
    private const string Dale = "dale";
    private const string Rhovanion = "northern-rhovanion";
    private const string Orthanc = "orthanc";
    private const string Ford = "fords-of-isen";
    private const string MinasTirith = "minas-tirith";
    private const string MinasMorgul = "minas-morgul";
    private const string WoodlandRealm = "woodland-realm";
    private const string Rivendell = "rivendell";
    private const string Lorien = "lorien";
    private const string Moria = "moria";
    private const string Hollin = "hollin";

    public static IEnumerable<TestScenario> All() =>
    [
        Scenario("layout-full-roster", "Every character at once: Fate, Lords and Kings",
            "Fate, Lords with the Council and Kings: every alternative Companion (the Keeper of Narya guiding, Gimli the Dwarven " +
            "Lord in the Iron Hills), Elrond and Galadriel, Denethor Awakened with a usable Ruler token, Théoden Corrupted, " +
            "Thranduil with two Corruption tiles, the Chief of the Ringwraiths, the Black Númenórean, Gothmog, the Balrog, " +
            "Saruman and the three Chieftains. Open the Characters tab and each character's window.",
            Side.FreePeoples, (id, random) => Roster(id, random, true),
            ExpansionModules.Kings, ExpansionModules.Lords, ExpansionModules.Fate),
        Scenario("layout-full-roster-five-kings", "Every character at once: the five kings",
            "Lords with the Council and Kings, without Fate so all five kings play: Brand and Denethor Awakened with usable " +
            "Ruler tokens, Dáin with two Corruption tiles, Théoden Corrupted, Thranduil Initial, beside every alternative " +
            "Companion, both Keepers, every Minion and the three Chieftains. Open the Characters tab and each character's window.",
            Side.FreePeoples, (id, random) => Roster(id, random, false), ExpansionModules.Kings, ExpansionModules.Lords),
        Scenario("layout-chieftains-battle", "Three Chieftains' replacements in one battle",
            "The North's army with the Awakened Brand attacks the Black Serpent, the Shadow of Mirkwood and Uglúk beside two " +
            "Sauron Regulars; both Combat cards are committed. Before the reveal the battle window shows three Chieftain chips " +
            "and Keep card effects; after the Shadow's choice Brand's Heir of Bard waits for the Free Peoples.",
            Side.Shadow, ChieftainsBattle, ExpansionModules.Kings),
        Scenario("layout-hunt-responses", "A Hunt answered at every window",
            "Council of Rivendell: the alternative Merry guides five Companions; Mithril Coat and Sting, Axe and Bow and the " +
            "Horn of Gondor stand on the table and Galadriel keeps Nenya. The Hunt drew an Eye: Mithril and Nenya answer now, " +
            "Pass shows Axe and Bow and the Horn, then the Hobbits' cancel, then the damage choice.",
            Side.FreePeoples, HuntResponses, ExpansionModules.Lords),
        Scenario("layout-council-travel", "The Council token moves many Nazgûl and Minions",
            "Council of Rivendell: the Shadow holds both tokens. The Witch-king, all eight Nazgûl in five regions, the Black " +
            "Númenórean, Gothmog and the active Balrog can all move with the Travel token instead of a die.",
            Side.Shadow, CouncilTravel, ExpansionModules.Lords),
        Scenario("layout-white-rider", "The White Rider at a battle's start",
            "The Witch-king, two Nazgûl and Uglúk lead Isengard from Orthanc against Gandalf the White, the Awakened Théoden and " +
            "the Rohan army at the Fords of Isen: before any Combat card the White Rider chip waits in the battle window, or Pass.",
            Side.FreePeoples, WhiteRider, ExpansionModules.Kings),
        Scenario("layout-all-resources", "Every pool resource at once",
            "Lords with the Council and Kings: the Narya and Gothmog dice, both Ruler dice, three usable Ruler tokens, the " +
            "Shadow's two Council tokens and two Dismay tokens, the Free Peoples holding Narya and Vilya and the Shadow Nenya. " +
            "Look at the dice tray, the token rows and the Rings.",
            Side.FreePeoples, AllResources, ExpansionModules.Kings, ExpansionModules.Lords),
        Scenario("layout-effects-sources", "The Effects panel at its fullest",
            "Lords and Kings: six Free Peoples and eight Shadow cards played earlier stand on the tables (A Power too Great " +
            "and Tom Bombadil protect three regions each) and Brand, Théoden and Thranduil are Corrupted: over twenty " +
            "entries. Open the Effects tab.",
            Side.FreePeoples, EffectsSources, ExpansionModules.Kings, ExpansionModules.Lords),
        Scenario("layout-console-options", "The Nazgûl Strike's choices on the board",
            "The Shadow played The Nazgûl Strike! (the console's committed chip) and flew the Witch-king and three Nazgûl to " +
            "the Fellowship in Hollin; four Character cards stand on the Free Peoples table. The console shows no option " +
            "buttons: the four cards glow red in the Effects panel to discard one, the Hunt Box glows to roll for the Hunt " +
            "and the ✓ finishes.",
            Side.Shadow, ConsoleOptions, ExpansionModules.Lords)
    ];

    // The group follows the first module, the client's tab.
    private static TestScenario Scenario(string id, string title, string description, Side side,
        Func<string, IRandomSource, MatchState> build, params string[] modules) =>
        new(id, modules[0] == ExpansionModules.Kings ? KingsGroup : LordsGroup, title, description, side, build) { Modules = modules };

    // Every alternative Companion chosen through the Council setup, Boromir, Gimli and Legolas starting in their homelands
    // (so the Shadow holds both tokens); the Keepers, the kings in their states and every Minion and Chieftain entered
    // earlier, each through the engine's entry rule.
    private static MatchState Roster(string id, IRandomSource random, bool fate)
    {
        var configuration = new MatchConfiguration { Fate = fate, Lords = true, Council = true, Kings = true };
        var gimliHome = fate ? FateGimli.Homelands[0] : "erebor";
        var choices = new LordsSetupChoices(CharacterRules.Grey,
            [LordsCharacters.Gandalf, CouncilCompanions.Strider, AlternateBoromir, fate ? FateGimli.Alternate : AlternateGimli,
                AlternateLegolas, CouncilCompanions.Merry, CouncilCompanions.Pippin],
            [Home(Boromir, MinasTirith), Home(Gimli, gimliHome), Home(Legolas, WoodlandRealm)]);
        return LordsBoard.Setup(id, random, configuration, choices)
            .AtWar("sauron", "isengard", "southrons", "elves", "gondor", "north", "rohan")
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Hunt(1, 0)
            .Change(state =>
            {
                Keepers(state);
                Kings(state, fate);
                Minions(state);
                Chieftains(state, ("near-harad", "dol-guldur", Orthanc));
                RulerRoll(state, Side.FreePeoples, DieFace.Muster);
                RulerRoll(state, Side.Shadow, DieFace.Army);
            })
            .Act(Side.FreePeoples);
    }

    private static void Keepers(MatchState state)
    {
        Enter(state, LordsCharacters.Elrond, LordsCharacters.Elrond, Rivendell);
        Enter(state, LordsCharacters.Galadriel, LordsCharacters.Galadriel, Lorien);
    }

    // With Fate only Denethor, Théoden and Thranduil play (KINGS p. 18): Awakened, Corrupted and Initial with two tiles.
    // Without it Brand is Awakened too, Dáin carries the tiles and Thranduil stays Initial without any.
    private static void Kings(MatchState state, bool fate)
    {
        Awaken(state, SovereignCatalog.Denethor);
        Corrupt(state, SovereignCatalog.Theoden);
        if (fate)
        {
            Tiles(state, SovereignCatalog.Thranduil, 2);
            return;
        }
        Awaken(state, SovereignCatalog.Brand);
        Tiles(state, SovereignCatalog.Dain, 2);
    }

    // Gothmog entered before the Witch-king, who came as the Chief of the Ringwraiths; the Mouth came as the Black
    // Númenórean; the Balrog waits inactive in Moria.
    private static void Minions(MatchState state)
    {
        Enter(state, CharacterRules.Saruman, CharacterRules.Saruman, Orthanc);
        Enter(state, LordsCharacters.Gothmog, LordsCharacters.Gothmog, MinasMorgul);
        Enter(state, CharacterRules.WitchKing, LordsCharacters.WitchKing, "angmar");
        Enter(state, CharacterRules.Mouth, LordsCharacters.Mouth, "barad-dur");
        Enter(state, LordsCharacters.Balrog, LordsCharacters.Balrog, Moria);
    }

    private static void Chieftains(MatchState state, (string Serpent, string Mirkwood, string Ugluk) regions)
    {
        Enter(state, KingsChieftains.Serpent, KingsChieftains.Serpent, regions.Serpent);
        Enter(state, KingsChieftains.Mirkwood, KingsChieftains.Mirkwood, regions.Mirkwood);
        Enter(state, KingsChieftains.Ugluk, KingsChieftains.Ugluk, regions.Ugluk);
    }

    // The North's army in Dale with the Awakened Brand attacks the three Chieftains and two Sauron Regulars next door;
    // both cards are committed (the Shadow's with Swarm of Bats) and the replacement waits for the Shadow before the reveal.
    private static MatchState ChieftainsBattle(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, new() { Kings = true })
            .AtWar("north", "sauron", "gondor", "rohan")
            .Place("north", UnitKind.Regular, Dale, 2)
            .Place("sauron", UnitKind.Regular, Rhovanion, 2)
            .Dice(Side.FreePeoples, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Hunt(1, 0)
            .Hand(Side.FreePeoples, FreePeoplesCombatCard)
            .Hand(Side.Shadow, ShadowStrategyCards02.Moving)
            .Change(state =>
            {
                Chieftains(state, (Rhovanion, Rhovanion, Rhovanion));
                Awaken(state, SovereignCatalog.Brand, Dale);
            });
        board.Act(Side.FreePeoples);
        var state = KingsBattleSteps.Attack(board.State, Side.FreePeoples, Dale, Rhovanion);
        state = KingsBattleSteps.Commit(state, FreePeoplesCombatCard, ShadowStrategyCards02.Moving);
        return KingsBattleSteps.Expect(state, Side.Shadow, "CombatReplacement");
    }

    // Five alternative Companions of Level 1 walk on, Merry guiding; Gandalf and Strider stayed in Rivendell. The three
    // table cards were played earlier through the engine; the Fellowship moves into a Hunt whose five dice all hit and
    // whose pool holds only Eyes, so the first response window opens on the drawn Eye.
    private static MatchState HuntResponses(string id, IRandomSource random)
    {
        var choices = new LordsSetupChoices(CharacterRules.Strider, [CharacterRules.Grey, AlternateBoromir, AlternateGimli,
            AlternateLegolas, CouncilCompanions.Merry, CouncilCompanions.Pippin], []);
        var board = LordsBoard.Setup(id, random, LordsBoard.Council, choices)
            .AtWar("sauron", "elves")
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army);
        foreach (var card in HuntTable)
            board = Played(board, Side.FreePeoples, card);
        board = board
            .Companion(CharacterRules.Grey, Rivendell)
            .Companion(CharacterRules.Strider, Rivendell)
            .Change(state =>
            {
                Guide(state, Merry);
                Enter(state, LordsCharacters.Galadriel, LordsCharacters.Galadriel, Lorien);
            })
            .HuntAhead(Side.FreePeoples, DieFace.Event, tile => tile.Pool == "standard" && tile.Value == "eye");
        var die = Available(board.Act(Side.FreePeoples), Side.FreePeoples, DieFace.Character);
        var state = board.Command(CommandKind.MoveFellowship, new() { DieId = die.Id }, LordsBoard.Highest, "layout-hunt-move").State;
        return Window(state, Side.FreePeoples, "hunt.replace");
    }

    private static readonly string[] HuntTable =
        [FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards02.Horn];

    // Boromir and Legolas started outside the Fellowship, so the Shadow holds both tokens. Gothmog entered before the
    // Witch-king; the Mouth came as the Black Númenórean; the Balrog was activated with his die earlier.
    private static MatchState CouncilTravel(string id, IRandomSource random)
    {
        var choices = new LordsSetupChoices(CharacterRules.Strider, [CharacterRules.Grey, AlternateBoromir, AlternateLegolas],
            [Home(Boromir, MinasTirith), Home(Legolas, WoodlandRealm)]);
        return LordsBoard.Setup(id, random, LordsBoard.Council, choices)
            .AtWar("sauron", "gondor")
            .Place("sauron", UnitKind.Nazgul, "dol-guldur", 2)
            .Place("sauron", UnitKind.Nazgul, "mount-gundabad")
            .Place("sauron", UnitKind.Nazgul, "morannon")
            .Change(state =>
            {
                Enter(state, LordsCharacters.Gothmog, LordsCharacters.Gothmog, MinasMorgul);
                Enter(state, CharacterRules.WitchKing, CharacterRules.WitchKing, "osgiliath");
                Enter(state, CharacterRules.Mouth, LordsCharacters.Mouth, "barad-dur");
                Enter(state, LordsCharacters.Balrog, LordsCharacters.Balrog, Moria);
                state.Characters.Single(c => c.DefinitionId == LordsCharacters.Balrog).Level = 2;
            })
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Act(Side.Shadow);
    }

    // Gandalf left the Fellowship for the Fords and became Gandalf the White once the Witch-king had entered; Théoden
    // awakened there. The Shadow attacks from Orthanc with its whole army, and the White Rider's window opens first.
    private static MatchState WhiteRider(string id, IRandomSource random)
    {
        var board = ScenarioBoard.Setup(id, random, new() { Kings = true })
            .AtWar("isengard", "sauron", "rohan")
            .Dice(Side.Shadow, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Place("rohan", UnitKind.Elite, Ford)
            .Place("sauron", UnitKind.Nazgul, Orthanc, 2)
            .Enter(CharacterRules.WitchKing, Orthanc)
            .Companion(CharacterRules.Grey, Ford)
            .Replace(CharacterRules.White)
            .Change(state =>
            {
                Enter(state, KingsChieftains.Ugluk, KingsChieftains.Ugluk, Orthanc);
                Awaken(state, SovereignCatalog.Theoden, Ford);
            });
        board.Act(Side.Shadow);
        var state = KingsBattleSteps.Attack(board.State, Side.Shadow, Orthanc, Ford);
        return Window(state, Side.FreePeoples, "battle.start");
    }

    // The Keeper of Narya guides, so the Narya die was kept at the roll; Gothmog's die was the Shadow's choice. Brand, Dáin
    // and Denethor are Awakened (three tokens), Théoden and Thranduil awakened and fell (two Dismay tokens), and the Free
    // Peoples gave Nenya to the Shadow in an earlier turn.
    private static MatchState AllResources(string id, IRandomSource random)
    {
        var choices = new LordsSetupChoices(CharacterRules.Grey, [LordsCharacters.Gandalf, AlternateBoromir, AlternateLegolas],
            [Home(Boromir, MinasTirith), Home(Legolas, WoodlandRealm)]);
        var configuration = new MatchConfiguration { Lords = true, Council = true, Kings = true };
        return LordsBoard.Setup(id, random, configuration, choices)
            .AtWar("north", "dwarves", "gondor", "rohan", "elves", "sauron", "southrons")
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Hunt(1, 0)
            .Change(state =>
            {
                Keepers(state);
                Awaken(state, SovereignCatalog.Brand);
                Awaken(state, SovereignCatalog.Dain);
                Awaken(state, SovereignCatalog.Denethor);
                Fall(state, SovereignCatalog.Theoden);
                Fall(state, SovereignCatalog.Thranduil);
                Enter(state, LordsCharacters.Gothmog, LordsCharacters.Gothmog, MinasMorgul);
                Enter(state, KingsChieftains.Serpent, KingsChieftains.Serpent, "near-harad");
                ExpansionBoard.Show(state, LordsDiceCatalog.Gothmog, DieFace.Army);
                RulerRoll(state, Side.FreePeoples, DieFace.Character);
                RulerRoll(state, Side.Shadow, DieFace.Muster);
                state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner = Side.Shadow;
            })
            .Act(Side.FreePeoples);
    }

    // The side's Ruler die rolled the face, and after the roll the side discarded one die (KINGS p. 6): its last standard
    // die still available.
    private static void RulerRoll(MatchState state, Side side, DieFace face)
    {
        Ruler(state, side, face);
        var discarded = state.Dice.Last(d => d.Owner == side && d.Source == "standard" && d.Location == DieLocation.Available);
        discarded.Location = DieLocation.Discarded;
    }

    // An Awakened king who fell: his spent Ruler token became the Shadow's Dismay token.
    private static void Fall(MatchState state, string sovereign)
    {
        Awaken(state, sovereign);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == sovereign));
    }

    // Every card was played earlier through the engine, each with a die of its type, so the kept cards carry their lasting
    // effects; Saruman and the Witch-king entered first for the cards that need them. Wormtongue is left out: the
    // Witch-king's entry activated Rohan, which ends its effect at once.
    private static MatchState EffectsSources(string id, IRandomSource random)
    {
        var board = LordsBoard.Setup(id, random, LordsBoard.WithKings)
            .AtWar("sauron", "isengard")
            .Enter(CharacterRules.Saruman, Orthanc)
            .Enter(CharacterRules.WitchKing, MinasMorgul);
        foreach (var card in FreePeoplesTable)
            board = Played(board, Side.FreePeoples, card);
        foreach (var card in ShadowTable)
            board = Played(board, Side.Shadow, card);
        return board
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army)
            .Hunt(1, 0)
            .Change(state =>
            {
                Corrupt(state, SovereignCatalog.Brand);
                Corrupt(state, SovereignCatalog.Theoden);
                Corrupt(state, SovereignCatalog.Thranduil);
            })
            .Act(Side.FreePeoples);
    }

    private static readonly string[] FreePeoplesTable =
    [
        FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards02.Horn,
        FreePeoplesCharacterCards02.Staff, FreePeoplesStrategyCards01.PowerTooGreat, FreePeoplesStrategyCards01.TomBombadil
    ];

    private static readonly string[] ShadowTable =
    [
        ShadowCharacterCards03.Worn, ShadowCharacterCards03.Crebain, ShadowCharacterCards04.Palantir, KingsCards03.Threats,
        KingsCards03.Palantir, LordsShadowCards.Lore, LordsShadowCards.Gates, LordsShadowCards.Captain
    ];

    // The Free Peoples kept four Character cards on their table; the Fellowship moved last into Hollin. The Shadow plays
    // The Nazgûl Strike! with a Character die and flies the Witch-king and the three Nazgûl of Minas Morgul to the
    // Fellowship. The engine commits only the played card before this choice, so the trail holds that one chip.
    private static MatchState ConsoleOptions(string id, IRandomSource random)
    {
        var board = LordsBoard.Setup(id, random, LordsBoard.Lords)
            .AtWar("sauron", "gondor")
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army);
        foreach (var card in StrikeTable)
            board = Played(board, Side.FreePeoples, card);
        board = board
            .Place("sauron", UnitKind.Nazgul, MinasMorgul, 2)
            .Enter(CharacterRules.WitchKing, MinasMorgul)
            .FellowshipAt(Hollin, 1)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army)
            .Hunt(1, 0)
            .Hand(Side.Shadow, ShadowCharacterCards02.Strike);
        var state = board.Act(Side.Shadow);
        var die = Available(state, Side.Shadow, DieFace.Character);
        state = ExpansionBoard.Execute(state, CommandKind.PlayEvent,
            new() { CardId = ShadowCharacterCards02.Strike, DieId = die.Id, CardCost = DieFace.Character });
        var flyers = state.Units.Where(u => u.Kind == UnitKind.Nazgul && u.Location == MinasMorgul).Select(u => u.Id)
            .Append(state.Characters.Single(CharacterRules.IsWitchKing).Id).ToList();
        state = ExpansionBoard.Execute(state, CommandKind.MoveShadowCharacters,
            new() { CompanionMoves = [new() { Ids = flyers, Route = [MinasMorgul, Hollin] }] });
        if (state.Pending is not { Kind: DecisionKind.EffectChoice, Presentation: "NazgulStrike", Owner: Side.Shadow })
            throw new InvalidOperationException($"The Nazgûl Strike must wait for its choice, not {state.Pending?.Presentation}.");
        return state;
    }

    private static readonly string[] StrikeTable =
    [
        FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards02.Horn,
        FreePeoplesCharacterCards02.Staff
    ];

    // The side played the card in an earlier action of this turn through the engine (LordsBoard.Played); the event
    // finished without a decision, and the side's dice are rolled again for the next play.
    private static ScenarioBoard Played(ScenarioBoard board, Side side, string card)
    {
        var face = CardCatalog.All[card].EventType;
        board.Dice(side, face, face, face, face);
        var player = board.State.Player(side);
        var held = player.Hand.Contains(card) || player.Decks.Values.Any(deck => deck.Contains(card));
        if (!CardCatalog.Enabled(board.State, card) || !held)
            throw new InvalidOperationException($"{card} is not in the {side} decks of this game.");
        ScenarioBoard played;
        try
        {
            played = LordsBoard.Played(board, side, card);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException($"{card}: {exception.Message}", exception);
        }
        if (played.State.Pending?.Kind != DecisionKind.Action || EffectRuntime.Current(played.State) is not null)
            throw new InvalidOperationException($"{card} must finish without a decision, not {played.State.Pending?.Kind}.");
        return played;
    }

    private static DieState Available(MatchState state, Side side, DieFace face) =>
        state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == face);

    // The scenario starts at the owner's reaction window.
    private static MatchState Window(MatchState state, Side owner, string window)
    {
        if (state.Pending?.Kind != DecisionKind.Reaction || state.Pending.Owner != owner || state.Pending.Window != window)
            throw new InvalidOperationException($"The scenario must wait for {owner}'s reaction at {window}, not " +
                $"{state.Pending?.Kind} {state.Pending?.Window} of {state.Pending?.Owner}.");
        return state;
    }

    // A character entered through the engine's entry rule, as its entry action would bring it in.
    private static void Enter(MatchState state, string figure, string definition, string region) => CharacterEntry.Apply(state,
        new CharacterEntryPlan(figure, definition, region, false, CharacterEntry.RequiredCost(definition)));

    // A Companion of the remaining Fellowship took the Guide's role, which reveals his version.
    private static void Guide(MatchState state, string figure)
    {
        var companion = CharacterCatalog.Companion(state, figure);
        if (!CompanionRules.Guides(state).Contains(companion.Id))
            throw new InvalidOperationException($"{figure} cannot guide the Fellowship now.");
        state.Fellowship.Guide = companion.Id;
        LordsCompanionSetup.Reveal(state, companion);
    }

    private static UnitPlacementChoice Home(string figure, string region) => new() { UnitId = figure, RegionId = region };
}
