using Wotr.Contracts;
using static Wotr.Domain.LordsCardScenarios;

namespace Wotr.Domain;

// TEST-12: the Lords characters' entries, abilities and exits that the dice scenarios (ExpansionDiceScenarios) and the
// owner's decisions (LordsDecisionScenarios) did not reach yet, each on a board where its condition holds: the Keepers'
// entry, Nenya's replacement, Galadriel's recruit inside a siege, the Household of Elrond, Gandalf the Grey as Keeper
// of Narya from his private setup choice to Gandalf the White, the Lords Minions' entries and the Witch-king's change,
// Spear of Terror and He Sees, He Knows, Great Sorcery, the active Balrog's die, Fire and Shadow, the Balrog's
// deactivation, and a Keeper die with the Remove symbol leaving at the next Recovery.
public static class LordsCharacterScenarios
{
    public const string FreePeoplesGroup = "Lords · Characters and Rings";
    public const string MinionsGroup = "Lords · Minions";
    public const string DiceGroup = "Lords · Special dice";

    private const string Rivendell = "rivendell";
    private const string Lorien = "lorien";
    private const string Moria = "moria";
    private const string Hollin = "hollin";
    private const string MinasMorgul = "minas-morgul";
    private const string DimrillDale = "dimrill-dale";

    public static IEnumerable<TestScenario> All() =>
    [
        Lords(new("lords-fp-keeper-entry", FreePeoplesGroup, "Bring in Elrond or Galadriel",
            "Muster die: the Elves are at war and Rivendell and Lórien are unconquered, so either Keeper of an Elven Ring can enter at home.",
            Side.FreePeoples, KeeperEntry)),
        Lords(new("lords-fp-nenya-ring", FreePeoplesGroup, "Nenya: replace a drawn Eye",
            "Galadriel stands in Lórien and the Free Peoples hold Nenya. Move the Fellowship with the Character die: five Shadow " +
            "dice wait in the Hunt Box, it already moved twice (+2) and the pool holds only Eyes; give Nenya to the Shadow to " +
            "remove the drawn Eye for good and draw another tile.",
            Side.FreePeoples, NenyaRing)),
        Lords(new("lords-fp-galadriel-siege-muster", FreePeoplesGroup, "Galadriel: recruit inside besieged Lórien",
            "Valour of the Elven People: a Sauron army besieges Lórien, where Galadriel and the Elves have withdrawn; the Elves " +
            "are at war, so a Muster die recruits inside the siege.",
            Side.FreePeoples, GaladrielSiegeMuster)),
        Lords(new("lords-fp-elrond-household", FreePeoplesGroup, "Household of Elrond: Elven Elites lead in battle",
            "A Sauron army attacks Rivendell, where Elrond stands with the Elven army: in this battle each Elven Elite in his " +
            "region counts as a Leader. Choose your Combat card, then see the Leadership of the re-roll.",
            Side.FreePeoples, ElrondHousehold)),
        Lords(new("lords-fp-keeper-gandalf-setup", FreePeoplesGroup, "Gandalf the Grey, Keeper of Narya: the setup choice",
            "A new Lords game before the first turn: privately choose Gandalf the Grey or Gandalf the Grey, Keeper of Narya, and " +
            "the Guide; the Keeper as Guide brings the Narya die into the first roll.",
            Side.FreePeoples, (id, random) => LordsBoard.Start(id, random, LordsBoard.Lords))),
        Lords(new("lords-fp-keeper-white", FreePeoplesGroup, "Gandalf, Keeper of Narya, becomes Gandalf the White",
            "Will of the West: Gandalf the Grey, Keeper of Narya, has left the Fellowship in Rivendell and Saruman is in play, so " +
            "Gandalf the White takes the Keeper's figure.",
            Side.FreePeoples, KeeperWhite)),
        Lords(new("lords-sh-minion-entries", MinionsGroup, "Bring in Gothmog, the Black Númenórean or the Chief",
            "Muster dice: Sauron is at war and the Free Peoples hold Angmar (one victory point), the Fellowship is in Hollin and " +
            "no Witch-king is in play, so Gothmog (Minas Morgul), the Black Númenórean (a Sauron Stronghold) and the Chief of " +
            "the Ringwraiths (the Fellowship's region) can enter.",
            Side.Shadow, MinionEntries)),
        Lords(new("lords-sh-witch-king-switch", MinionsGroup, "The Witch-king becomes the Chief of the Ringwraiths",
            "The Black Captain stands in Minas Morgul and the Fellowship is in Hollin: a Muster die changes him once into the " +
            "Chief of the Ringwraiths in place.",
            Side.Shadow, WitchKingSwitch)),
        Lords(new("lords-sh-spear-of-terror", MinionsGroup, "Spear of Terror: the Chief draws a Character card",
            "The Chief of the Ringwraiths stands with the Fellowship in Hollin. Play the Lidless Eye from the Character deck: " +
            "Spear of Terror then draws a Character card, or Pass.",
            Side.Shadow, SpearOfTerror)),
        Lords(new("lords-sh-he-sees-he-knows", MinionsGroup, "He Sees, He Knows: the Chief follows a declaration",
            "Turn 2's Fellowship phase: the Chief of the Ringwraiths waits in Minas Morgul and the Fellowship is a step on from " +
            "Hollin. Declare it as the Free Peoples outside a Free Peoples Stronghold: the Chief may fly to it at no cost, or Pass.",
            Side.FreePeoples, HeSees)),
        Lords(new("lords-sh-great-sorcery", MinionsGroup, "Great Sorcery: a Muster die moves the Black Númenórean's army",
            "The Black Númenórean leads a Sauron army in North Ithilien next to the Gondor army in Osgiliath (Sauron and Gondor " +
            "at war): once this turn a Muster die moves or attacks with his army as a Character die.",
            Side.Shadow, GreatSorcery)),
        Lords(new("lords-sh-use-balrog-active", MinionsGroup, "Use Balrog: move or attack with the active Balrog",
            "The active Balrog stands in Moria with its Orcs next to an Elven army in Dimrill Dale (Sauron and the Elves at war) " +
            "and the Balrog die shows Use Balrog: move him alone, or move or attack with his army.",
            Side.Shadow, UseBalrogActive)),
        Lords(new("lords-sh-fire-and-shadow", MinionsGroup, "Fire and Shadow: the Fellowship passes the Balrog",
            "Turn 2's Fellowship phase: the Balrog waits in Moria and the Fellowship is two steps on from Hollin. Declare it in " +
            "Moria as the Free Peoples: Fire and Shadow draws an additional Hunt tile, and an Eye eliminates the Balrog and the Guide.",
            Side.FreePeoples, FireAndShadow)),
        Lords(new("lords-fp-balrog-deactivate", MinionsGroup, "Deactivate the Balrog with Gandalf",
            "The active Balrog stands in Dimrill Dale, outside Moria, where Gandalf the Grey has gone: a Character die (Gandalf " +
            "in his region) or Will of the West deactivates him, which outside Moria eliminates him.",
            Side.FreePeoples, BalrogDeactivate)),
        Lords(new("lords-dice-removed-at-recovery", DiceGroup, "Remove symbol: a Keeper die leaves at Recovery",
            "Gandalf the White is in play and the Free Peoples kept Vilya's Eye with the Remove symbol, which went to the Hunt. Use " +
            "the last die: at the next Recovery the Vilya die leaves the game for good.",
            Side.FreePeoples, RemovedAtRecovery))
    ];

    private static MatchState KeeperEntry(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .AtWar("elves")
        .Dice(Side.FreePeoples, DieFace.Muster, DieFace.Muster, DieFace.Character, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
        .Act(Side.FreePeoples);

    private static MatchState NenyaRing(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .Change(state => LordsBoard.Put(state, LordsCharacters.Galadriel, Lorien))
        .HuntAhead(Side.FreePeoples, DieFace.Event, tile => tile.Pool == "standard" && tile.Value == "eye")
        .Act(Side.FreePeoples);

    // Galadriel stands with the Elven army that withdrew into Lórien before the siege.
    private static MatchState GaladrielSiegeMuster(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .AtWar("elves", "sauron")
        .Change(state => LordsBoard.Put(state, LordsCharacters.Galadriel, Lorien))
        .Besiege(Lorien, "sauron", 3)
        .Dice(Side.FreePeoples, DieFace.Muster, DieFace.Character, DieFace.Army, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
        .Act(Side.FreePeoples);

    // Three Sauron Regulars attack Rivendell from the Trollshaws; the Elves defend in the field with Elrond.
    private static MatchState ElrondHousehold(string id, IRandomSource random)
    {
        var board = LordsBoard.Setup(id, random)
            .AtWar("elves", "sauron")
            .Change(state => LordsBoard.Put(state, LordsCharacters.Elrond, Rivendell))
            .Place("sauron", UnitKind.Regular, "trollshaws", 3)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
        var state = LordsBoard.Field(LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, "trollshaws", Rivendell));
        return LordsBoard.CombatChoice(state, Side.FreePeoples);
    }

    private static MatchState KeeperWhite(string id, IRandomSource random) =>
        LordsBoard.Setup(id, random, LordsBoard.Lords, LordsSetupChoices.Keeper)
            .Companion(CharacterRules.Grey, Rivendell)
            .Enter(CharacterRules.Saruman, "orthanc")
            .Dice(Side.FreePeoples, DieFace.WillOfTheWest, DieFace.Character, DieFace.Army, DieFace.Muster)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
            .Act(Side.FreePeoples);

    private static MatchState MinionEntries(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .AtWar("sauron")
        .Change(state => state.Control["angmar"] = Side.FreePeoples)
        .FellowshipAt(Hollin)
        .Dice(Side.Shadow, DieFace.Muster, DieFace.Muster, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Act(Side.Shadow);

    private static MatchState WitchKingSwitch(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .AtWar("sauron", "gondor")
        .Enter(CharacterRules.WitchKing, MinasMorgul)
        .FellowshipAt(Hollin)
        .Dice(Side.Shadow, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Act(Side.Shadow);

    private static MatchState SpearOfTerror(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .FellowshipAt(Hollin)
        .Change(state => Chief(state, Hollin))
        .Hand(Side.Shadow, ShadowCharacterCards03.Lidless)
        .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Act(Side.Shadow);

    // The Chief entered on turn 1; the Fellowship moved a step on from Hollin, and turn 2 opens its Fellowship phase.
    private static MatchState HeSees(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random))
            .Change(state => Chief(state, MinasMorgul))
            .FellowshipAt(Hollin, 1);
        return LordsBoard.NextTurn(board.State);
    }

    private static MatchState GreatSorcery(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .AtWar("sauron", "gondor")
        .Place("sauron", UnitKind.Regular, "north-ithilien", 3)
        .Change(state => EnterVersion(state, CharacterRules.Mouth, LordsCharacters.Mouth, "north-ithilien"))
        .Dice(Side.Shadow, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Act(Side.Shadow);

    private static MatchState UseBalrogActive(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .AtWar("sauron", "elves")
        .Place("elves", UnitKind.Regular, DimrillDale, 2)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
        .Change(state =>
        {
            LordsBoard.Put(state, LordsCharacters.Balrog, Moria).Level = 2;
            ExpansionBoard.Show(state, LordsDiceCatalog.Balrog, DieFace.UseBalrog);
        })
        .Act(Side.Shadow);

    // The Balrog entered on turn 1 and the Fellowship moved two steps on from Hollin; turn 2 opens its Fellowship phase.
    private static MatchState FireAndShadow(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random))
            .Change(state => LordsBoard.Put(state, LordsCharacters.Balrog, Moria).Level = 0)
            .FellowshipAt(Hollin, 2);
        return LordsBoard.NextTurn(board.State);
    }

    private static MatchState BalrogDeactivate(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .Companion(CharacterRules.Grey, DimrillDale)
        .Change(state => LordsBoard.Put(state, LordsCharacters.Balrog, DimrillDale).Level = 2)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.WillOfTheWest, DieFace.Army, DieFace.Event)
        .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
        .Act(Side.FreePeoples);

    // Elrond is in play, Gandalf the White took the Grey's figure after Saruman entered, and the Free Peoples kept Vilya's
    // Eye (Remove) this turn; they hold one Event die and the Shadow has used every die.
    private static MatchState RemovedAtRecovery(string id, IRandomSource random) => LordsBoard.Setup(id, random)
        .Change(state => LordsBoard.Put(state, LordsCharacters.Elrond, Rivendell))
        .Companion(CharacterRules.Grey, "fangorn")
        .Enter(CharacterRules.Saruman, "orthanc")
        .Replace(CharacterRules.White)
        .Dice(Side.FreePeoples, DieFace.Event)
        .Spent(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Hunt(1, 0)
        .Change(KeeperEye)
        .Act(Side.FreePeoples);

    // Vilya's Eye with the Remove symbol, kept after the roll, joined the Hunt.
    private static void KeeperEye(MatchState state)
    {
        var vilya = ExpansionBoard.Show(state, LordsDiceCatalog.Vilya, DieFace.Eye);
        vilya.Special!.RemoveSymbol = true;
        vilya.Location = DieLocation.Hunt;
        vilya.HuntOrigin = LordsDiceCatalog.KeeperEye;
    }

    // The Chief of the Ringwraiths entered on the Witch-king's figure.
    private static void Chief(MatchState state, string region) =>
        EnterVersion(state, CharacterRules.WitchKing, LordsCharacters.WitchKing, region);

    // A Minion version entered on its figure through the engine's entry rule, e.g. the Black Númenórean as the Mouth.
    private static void EnterVersion(MatchState state, string figure, string definition, string region) =>
        CharacterEntry.Apply(state, new CharacterEntryPlan(figure, definition, region, false, CharacterEntry.RequiredCost(definition)));
}
