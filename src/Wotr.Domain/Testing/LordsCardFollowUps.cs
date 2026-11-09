using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.LordsCardScenarios;

namespace Wotr.Domain;

// TEST-12: the Lords cards whose effect waits on a later moment or a roll start where that moment has come: Three Rings
// for the Elven-Kings after two successes, the Lore of the Elven Rings after a Keeper die rolled an Eye, the Free Peoples
// discarding the Lore and the Gates from the Shadow table, the Gates at a declaration into Moria, We Shall Get It after
// the Hunt drew a tile with Gollum as Guide, and Blade of Westernesse after a Leader re-roll hit against two Minions.
// The builder plays the earlier steps through the engine with fixed dice, so each starts the same way every time.
internal static class LordsCardFollowUps
{
    private const string Hollin = "hollin";
    private const string Rivendell = "rivendell";

    public static IEnumerable<TestScenario> All() =>
    [
        Lords(new("lords-card-three-rings-recover", FreePeoplesGroup, "Three Rings for the Elven-Kings · Recover the Rings",
            "Three Rings rolled two successes: Narya, out of play, comes back first, then choose Nenya or Vilya from the Shadow.",
            Side.FreePeoples, ThreeRingsRecover)),
        Lords(new("lords-card-blade-minion", FreePeoplesGroup, "You Know the Way There · Blade of Westernesse on a Minion",
            "Saruman and the Witch-king attack the Fords of Isen, where Merry stands; the Free Peoples played Blade of " +
            "Westernesse, missed every Combat die and hit with their Leader re-roll: spend one re-roll hit to eliminate either Minion.",
            Side.FreePeoples, BladeMinion)),
        Lords(new("lords-card-lore-change-die", ShadowGroup, "The Lore of the Elven Rings · Change a die",
            "Turn 2: the Lore of the Elven Rings stands on the Shadow table and Elrond's Vilya die rolled an Eye, so before the " +
            "actions the Shadow may change one of its own results.",
            Side.Shadow, LoreChange)),
        Lords(new("lords-card-lore-discard", FreePeoplesGroup, "The Lore of the Elven Rings · Discard it",
            "The Shadow played the Lore of the Elven Rings on its table: spend Will of the West, or any die with an Elven Ring, " +
            "to discard it.",
            Side.FreePeoples, (id, random) => TableCard(id, random, LordsShadowCards.Lore, DieFace.WillOfTheWest))),
        Lords(new("lords-card-gates-discard", FreePeoplesGroup, "The Gates are Closed · Discard it",
            "The Shadow played the Gates are Closed on its table and Gandalf the Grey guides the Fellowship: spend Will of the " +
            "West or a Character die to discard it.",
            Side.FreePeoples, (id, random) => TableCard(id, random, LordsShadowCards.Gates, DieFace.Character))),
        Lords(new("lords-card-gates-declaration", ShadowGroup, "The Gates are Closed · At a declaration",
            "Turn 2's Fellowship phase: the Gates are Closed stands on the Shadow table and the Fellowship is two steps on from " +
            "Hollin. Declare it in Moria as the Free Peoples: the Shadow may discard the Gates for an additional Hunt tile, or Pass.",
            Side.FreePeoples, GatesDeclaration)),
        Lords(new("lords-card-we-shall-get-it", ShadowGroup, "We Shall Get It · Replace a Hunt tile",
            "Every Companion left the Fellowship with Sméagol, so We Shall Get It entered the Shadow table and Gollum guides it; " +
            "the Hunt just drew a tile: discard the card to draw a replacement, or Pass.",
            Side.Shadow, WeShallGetIt))
    ];

    // Three Rings for the Elven-Kings rolls 5, 6 and 1 against the used Rings of its event scenario.
    private static MatchState ThreeRingsRecover(string id, IRandomSource random)
    {
        var card = CardCatalog.All[LordsFreePeoplesCards.ThreeRings];
        var state = LordsCardEvents.Build(id, random, card, LordsCardEvents.For(card.Id));
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == card.EventType);
        state = ExpansionBoard.Execute(state, CommandKind.PlayEvent, new() { CardId = card.Id, DieId = die.Id, CardCost = card.EventType },
            LordsBoard.Rolls(4, 5, 0));
        return Expect(state, DecisionKind.EffectChoice, Side.FreePeoples);
    }

    // Blade of Westernesse at the Fords of Isen: every Combat die rolls a 1 and the Free Peoples' Leader re-roll a 6.
    private static MatchState BladeMinion(string id, IRandomSource random)
    {
        var state = LordsBattles.FreePeoplesDefence(id, random, LordsFreePeoplesCards.YouKnowTheWay);
        state = ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = LordsFreePeoplesCards.YouKnowTheWay });
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Reroll(state, state.Pending.Owner == Side.FreePeoples);
        return Expect(state, DecisionKind.EffectChoice, Side.FreePeoples);
    }

    // The Free Peoples re-roll every failed die their Leadership allows; the Shadow keeps its results.
    private static MatchState Reroll(MatchState state, bool freePeoples)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        var ids = freePeoples ? BattleRoll.Failed(force).Take(BattleRoll.RerollMaximum(state, force)).ToList() : [];
        return ExpansionBoard.Execute(state, CommandKind.RerollBattle, new() { Ids = ids }, LordsBoard.Rolls([.. ids.Select(_ => 5)]));
    }

    // The Shadow played the Lore on turn 1 with Elrond in Rivendell; turn 2's roll shows an Eye on the Vilya die.
    private static MatchState LoreChange(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random)).Change(state => LordsBoard.Put(state, LordsCharacters.Elrond, Rivendell));
        var state = LordsBoard.NextTurn(LordsBoard.Played(board, Side.Shadow, LordsShadowCards.Lore).State);
        state = ExpansionBoard.Execute(state, CommandKind.ContinueFellowship);
        var faces = state.Dice.Where(d => d.Location == DieLocation.Unrolled).Select(d => d.Id == LordsDiceCatalog.Vilya ? 5 : 1).ToArray();
        state = ExpansionBoard.Execute(state, CommandKind.AllocateHunt, new(), LordsBoard.Rolls(faces));
        return Expect(state, DecisionKind.EffectChoice, Side.Shadow);
    }

    // A Shadow card played on the table earlier this turn; the Free Peoples hold a die that can discard it.
    private static MatchState TableCard(string id, IRandomSource random, string card, DieFace payer)
    {
        var board = LordsBoard.Setup(id, random)
            .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Hunt(1, 0)
            .Dice(Side.FreePeoples, payer, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Guide(CharacterRules.Grey);
        return LordsBoard.Played(board, Side.Shadow, card).Act(Side.FreePeoples);
    }

    // The Shadow played the Gates on turn 1 while the Fellowship stood two steps on from Hollin; turn 2 opens its
    // Fellowship phase.
    private static MatchState GatesDeclaration(string id, IRandomSource random)
    {
        var board = LordsBoard.Rolled(LordsBoard.Setup(id, random)).FellowshipAt(Hollin, 2);
        return LordsBoard.NextTurn(LordsBoard.Played(board, Side.Shadow, LordsShadowCards.Gates).State);
    }

    // Sméagol guides the Fellowship in Hollin; all seven Shadow dice wait in the Hunt Box and the pool holds damage
    // tiles. The Free Peoples separate every Companion with Sméagol, then move the Fellowship and the Hunt hits.
    private static MatchState WeShallGetIt(string id, IRandomSource random)
    {
        var board = LordsBoard.Setup(id, random)
            .Change(LordsCardEvents.SmeagolGuides)
            .FellowshipAt(Hollin)
            .Hunt(2, 5)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Character, DieFace.Muster, DieFace.Event)
            .KeepHuntTiles(tile => tile.Value is "1" or "2" or "3");
        var state = board.Act(Side.FreePeoples);
        state = ExpansionBoard.Execute(state, CommandKind.SeparateCompanions, new()
        {
            DieId = CharacterDie(state).Id,
            Ids = [.. state.Fellowship.Companions],
            Route = [Hollin]
        });
        state = ExpansionBoard.Execute(state, CommandKind.MoveFellowship, new() { DieId = CharacterDie(state).Id }, LordsBoard.Highest);
        return Expect(state, DecisionKind.Reaction, Side.Shadow);
    }

    private static DieState CharacterDie(MatchState state) =>
        state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);

    private static MatchState Expect(MatchState state, DecisionKind kind, Side owner)
    {
        if (state.Pending?.Kind != kind || state.Pending.Owner != owner)
            throw new InvalidOperationException($"The scenario must wait on the {owner} {kind}, not {state.Pending?.Kind} of {state.Pending?.Owner}.");
        return state;
    }
}
