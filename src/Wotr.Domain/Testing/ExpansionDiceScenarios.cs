using Wotr.Contracts;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// TEST-04: the special dice and actions of Lords of Middle-earth and Kings of Middle-earth (EXPANSIONS
// L01-L03, K01-K02, X02). Each scenario starts in turn 1 of a game with the named modules.
public static class ExpansionDiceScenarios
{
    private const string LordsDice = "Lords · Special dice";
    private const string LordsCharacters = "Lords · Characters and Rings";
    private const string Council = "Lords · Council of Rivendell";
    private const string KingsRuler = "Kings · Ruler dice";
    private const string KingsSovereigns = "Kings · Sovereigns and corruption";
    private const string KingsChieftainsGroup = "Kings · Dark Chieftains";
    private const string Fate = "Fate · Gimli";

    private static MatchConfiguration Lords => new() { Lords = true };
    private static MatchConfiguration Kings => new() { Kings = true };

    // TEST-10: each scenario names the one module its group plays with.
    public static IEnumerable<TestScenario> All() => Scenarios().Select(scenario => scenario with { Modules = [ModuleOf(scenario.Group)] });

    private static string ModuleOf(string group) =>
        group.StartsWith("Lords", StringComparison.Ordinal) ? ExpansionModules.Lords
        : group.StartsWith("Kings", StringComparison.Ordinal) ? ExpansionModules.Kings
        : ExpansionModules.Fate;

    private static IEnumerable<TestScenario> Scenarios() =>
    [
        new("lords-special-dice-choice", LordsDice, "Choose a special die",
            "Elrond, Galadriel, the Balrog and Gothmog are in play. After the roll each side keeps one of its special dice; " +
            "the Shadow rolled more dice and chooses first.",
            Side.Shadow, SpecialDiceChoice),
        new("lords-forced-eye", LordsDice, "An Eye forces the choice",
            "Vilya rolled an Eye and Nenya a Character, so the Free Peoples must keep the Eye: once the Shadow has kept " +
            "the Balrog's or Gothmog's die, the Eye is their only legal choice.",
            Side.Shadow, ForcedEye),
        new("lords-fp-card-draw", LordsDice, "Card Draw with a Keeper die",
            "Elrond is in play and his Vilya die shows Card Draw: draw a card from either deck with it.",
            Side.FreePeoples, (id, random) => Act(Elrond(id, random), Side.FreePeoples, state => Show(state, LordsDiceCatalog.Vilya, DieFace.CardDraw))),
        new("lords-keeper-eye-choice", LordsDice, "Keeper Eyes go to the Hunt",
            "Elrond and Galadriel are in play and both their dice rolled an Eye: the Free Peoples must keep an Eye, " +
            "which goes to the Hunt Box.",
            Side.FreePeoples, KeeperEyes),
        new("lords-fp-narya-die", LordsDice, "Muster with the Narya die",
            "Gandalf the Grey is Keeper of Narya and guides the Fellowship; his die shows Muster: advance a nation.",
            Side.FreePeoples, (id, random) => Act(Actions(id, random, Lords, LordsCharactersGandalf), Side.FreePeoples,
                state => Show(state, LordsDiceCatalog.Narya, DieFace.Muster))),
        new("lords-fp-nenya-die", LordsDice, "Character with the Nenya die",
            "Galadriel is in Lórien and her die shows Character: move the Fellowship or Companions with it.",
            Side.FreePeoples, (id, random) => Act(Galadriel(id, random), Side.FreePeoples, state => Show(state, LordsDiceCatalog.Nenya, DieFace.Character))),
        new("lords-sh-use-balrog", LordsDice, "Use Balrog",
            "The Balrog waits in Moria and the Balrog die shows Use Balrog: activate him.",
            Side.Shadow, UseBalrog),
        new("lords-sh-gothmog-die", LordsDice, "Army with the Gothmog die",
            "Gothmog is in Minas Morgul and his die shows Army: move or attack with a Shadow army.",
            Side.Shadow, (id, random) => Act(Gothmog(id, random), Side.Shadow, state => Show(state, LordsDiceCatalog.Gothmog, DieFace.Army))),
        new("lords-sh-balrog-entry", LordsCharacters, "Bring in the Balrog",
            "Muster die: the Balrog can enter in Moria and may bring one Sauron Regular or Elite; the Elves and the Dwarves are " +
            "already at war, so his entry does not move them.",
            Side.Shadow, BalrogEntry),
        new("lords-sh-gothmog-muster", LordsCharacters, "Gothmog's Muster abilities",
            "Muster die: Gothmog in Minas Morgul can recruit or upgrade with his ability.",
            Side.Shadow, (id, random) => Act(Gothmog(id, random), Side.Shadow, state => Faces(state, Side.Shadow, DieFace.Muster))),
        new("lords-fp-narya", LordsCharacters, "Narya",
            "Gandalf the Grey, Keeper of Narya, stands in Bree: give Narya to the Shadow and spend any die to activate the " +
            "North and move it to At War.",
            Side.FreePeoples, Narya),
        new("lords-fp-vilya", LordsCharacters, "Vilya",
            "Elrond is in play. The Free Peoples just drew a card with an Event die; Vilya can keep that die.",
            Side.FreePeoples, Vilya),
        new("lords-council-token", Council, "Shadow Council token",
            "Council of Rivendell: the Shadow holds the Advance Politics token and can use it instead of a die.",
            Side.Shadow, CouncilToken),
        new("kings-fp-ruler-ability", KingsRuler, "Ruler die: Mustering of the Mark",
            "Théoden is awakened in Edoras and Rohan is at war; the Free Peoples Ruler die shows Muster for his recruiting ability.",
            Side.FreePeoples, RulerAbility),
        new("kings-fp-ruler-token", KingsRuler, "Ruler token",
            "Denethor is awakened in Minas Tirith and the Ruler die shows Muster; his token turns it into his second ability.",
            Side.FreePeoples, RulerToken),
        new("kings-ruler-discard", KingsRuler, "Discard a Ruler die",
            "After the roll both Ruler dice are in the pool; the Shadow, then the Free Peoples decide which die to discard.",
            Side.Shadow, RulerDiscard),
        new("kings-sh-chieftain-entry", KingsChieftainsGroup, "Bring in a Dark Chieftain",
            "Muster die: Gondor is at war, so the Black Serpent can enter with a Southron army.",
            Side.Shadow, ChieftainEntry),
        new("kings-sh-ruler-die", KingsChieftainsGroup, "Shadow Ruler die",
            "The Black Serpent is in play, so the Shadow rolled its Ruler die; any result but an Eye pays for another Chieftain.",
            Side.Shadow, ShadowRuler),
        new("fate-gimli-renown", Fate, "Gimli's Renown",
            "Fate of Erebor: Gimli has left the Fellowship to the Iron Hills; with an Event die he advances the Dwarves.",
            Side.FreePeoples, GimliRenown),
        new("kings-fp-awaken", KingsSovereigns, "Awaken a sovereign",
            "Muster die: every nation is at war and Dáin stands in the Iron Hills, so a sovereign can be awakened.",
            Side.FreePeoples, Awaken),
        new("kings-fp-remove-corrupted", KingsSovereigns, "Remove a corrupted sovereign",
            "Denethor is corrupted in Minas Tirith and Boromir stands there: spend a die to remove him.",
            Side.FreePeoples, RemoveCorrupted),
        new("kings-sh-corruption", KingsSovereigns, "Attempt corruption",
            "Two Eyes are in the Hunt Box and the Free Peoples still have dice: try to corrupt a sovereign instead of an action.",
            Side.Shadow, Corruption)
    ];

    private static MatchState Act(MatchState state, Side side, Action<MatchState> arrange)
    {
        arrange(state);
        TurnFlow.SelectNext(state, side);
        return state;
    }

    private static MatchState Elrond(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        Put(state, KeeperRings.Elrond, "rivendell");
        return state;
    }

    private const string LordsCharactersGandalf = Domain.LordsCharacters.Gandalf;

    private static MatchState Galadriel(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        Put(state, KeeperRings.Galadriel, "lorien");
        return state;
    }

    // Next turn's roll: both Keeper dice show an Eye, so only an Eye can be kept.
    private static MatchState KeeperEyes(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        Put(state, KeeperRings.Elrond, "rivendell");
        Put(state, KeeperRings.Galadriel, "lorien");
        state.Turn++;
        DiceRules.Recover(state);
        var samples = state.Dice.Where(d => d.Location == DieLocation.Unrolled).Select(d => d.Special is null ? 1 : 5).ToArray();
        TurnFlow.RollAndBeginActions(state, new Samples(samples));
        return state;
    }

    private static MatchState ChieftainEntry(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        AtWar(state, "gondor");
        return Act(state, Side.Shadow, s => Faces(s, Side.Shadow, DieFace.Muster));
    }

    private static MatchState ShadowRuler(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        AtWar(state, "elves");
        Put(state, KingsChieftains.Serpent, "near-harad");
        return Act(state, Side.Shadow, s =>
        {
            Faces(s, Side.Shadow, DieFace.Character);
            Show(s, KingsDice.Shadow, DieFace.Event);
        });
    }

    private static MatchState GimliRenown(string id, IRandomSource random)
    {
        var state = Actions(id, random, new() { Fate = true });
        Put(state, FateGimli.Standard, "iron-hills");
        Die(state, Side.FreePeoples).Face = DieFace.Event;
        return state;
    }

    private static MatchState Gothmog(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        Put(state, Domain.LordsCharacters.Gothmog, "minas-morgul");
        return state;
    }

    // Next turn's roll: one special die per character, each showing an action result rather than an Eye.
    private static MatchState SpecialDiceChoice(string id, IRandomSource random) => SpecialRoll(id, random, new()
    {
        [LordsDiceCatalog.Vilya] = 3, [LordsDiceCatalog.Nenya] = 0, [LordsDiceCatalog.Balrog] = 1, [LordsDiceCatalog.Gothmog] = 0
    });

    // UI-267: Vilya's Eye (face 5) beside Nenya's Character (face 0); the Shadow keeps Use Balrog or Card Draw first.
    private static MatchState ForcedEye(string id, IRandomSource random) => SpecialRoll(id, random, new()
    {
        [LordsDiceCatalog.Vilya] = 5, [LordsDiceCatalog.Nenya] = 0, [LordsDiceCatalog.Balrog] = 1, [LordsDiceCatalog.Gothmog] = 4
    });

    // Elrond, Galadriel, the Balrog and Gothmog in play; turn 2 rolls the special dice on the given faces (LORDS p. 9/11).
    private static MatchState SpecialRoll(string id, IRandomSource random, Dictionary<string, int> faces)
    {
        var state = Actions(id, random, Lords);
        foreach (var (character, region) in new[] { (KeeperRings.Elrond, "rivendell"), (KeeperRings.Galadriel, "lorien"),
                     (Domain.LordsCharacters.Balrog, "moria"), (Domain.LordsCharacters.Gothmog, "minas-morgul") })
            Put(state, character, region);
        state.Turn++;
        DiceRules.Recover(state);
        var samples = state.Dice.Where(d => d.Location == DieLocation.Unrolled)
            .Select(d => d.Special is null ? 1 : faces.GetValueOrDefault(d.Id)).ToArray();
        TurnFlow.RollAndBeginActions(state, new Samples(samples));
        return state;
    }

    private static MatchState UseBalrog(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        Put(state, Domain.LordsCharacters.Balrog, "moria").Level = 0;
        return Act(state, Side.Shadow, s => Show(s, LordsDiceCatalog.Balrog, DieFace.UseBalrog));
    }

    private static MatchState BalrogEntry(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        AtWar(state, "elves", "dwarves");
        return Act(state, Side.Shadow, s => Faces(s, Side.Shadow, DieFace.Muster));
    }

    private static MatchState Narya(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        Put(state, Domain.LordsCharacters.Gandalf, "bree");
        return state;
    }

    private static MatchState Vilya(string id, IRandomSource random)
    {
        var state = Elrond(id, random);
        var die = Die(state, Side.FreePeoples);
        die.Face = DieFace.Event;
        return Execute(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "strategy" });
    }

    private static MatchState CouncilToken(string id, IRandomSource random)
    {
        var state = Actions(id, random, new() { Lords = true, Council = true });
        state.Lords!.Council!.GrantedTokens = [CouncilTokens.Politics];
        return Act(state, Side.Shadow, s => Faces(s, Side.Shadow, DieFace.Muster));
    }

    private static MatchState RulerAbility(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        AtWar(state, "rohan");
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Theoden), "edoras");
        Show(state, KingsDice.FreePeoples, DieFace.Muster);
        return state;
    }

    private static MatchState RulerToken(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Denethor), "minas-tirith");
        Show(state, KingsDice.FreePeoples, DieFace.Muster);
        return state;
    }

    // Next turn's roll with an awakened Thranduil and a Dark Chieftain in play: both Ruler dice are rolled.
    private static MatchState RulerDiscard(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        Put(state, KingsChieftains.Serpent, "near-harad");
        state.Turn++;
        DiceRules.Recover(state);
        var samples = state.Dice.Where(d => d.Location == DieLocation.Unrolled).Select(_ => 1).ToArray();
        TurnFlow.RollAndBeginActions(state, new Samples(samples));
        return state;
    }

    private static MatchState Awaken(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        AtWar(state, state.Nations.Select(n => n.Id).ToArray());
        Die(state, Side.FreePeoples).Face = DieFace.Muster;
        state.Units.Single(u => u.Id == SovereignCatalog.Dain).Location = "iron-hills";
        return state;
    }

    private static MatchState RemoveCorrupted(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        var sovereign = Sovereign(state, SovereignCatalog.Denethor);
        state.Units.RemoveAll(u => u.Id == sovereign.Id);
        sovereign.Status = SovereignStatus.Corrupted;
        sovereign.CorruptedRegion = "minas-tirith";
        Put(state, "base.character.boromir", "minas-tirith");
        return state;
    }

    private static MatchState Corruption(string id, IRandomSource random)
    {
        var state = Actions(id, random, Kings);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(2))
        {
            die.Face = DieFace.Eye;
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "allocated";
        }
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    private sealed class Samples(int[] values) : IRandomSource
    {
        private readonly Queue<int> queue = new(values);

        public int Next(int exclusiveMaximum) => queue.Count > 0 ? Math.Min(queue.Dequeue(), exclusiveMaximum - 1) : 0;
    }
}
