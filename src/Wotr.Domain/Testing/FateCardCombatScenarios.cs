using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// TEST-14: the Combat side of every Fate card, at its owner's Combat card choice in a battle where its condition holds.
// They use the base cards' battles (TEST-09) in a Fate game, as Bardings has since UI-349: the Fords of Isen lie in Rohan,
// a Free Peoples nation (Advantageous Position), and hold four Companions (Brave Stand) and a Rohan Elite (Valour); the
// siege of Lórien takes the Shadow's Onslaught and Desperate Battle. Shield-wall cancels a hit only when the enemy scores
// two, which the Fords' first round seldom gives, so its own scenario starts after a reveal in which every Shadow die hit.
public static class FateCardCombatScenarios
{
    public static IEnumerable<TestScenario> All() =>
    [
        Combat(FateCards.ThereAndBack),
        Combat(FateCards.Book),
        Combat(FateCards.Dwarves),
        Combat(FateCards.Orcs),
        Combat(FateCards.Monsters),
        new("fate-card-bardings-shield-wall", FateCardScenarios.Group, "Bardings · Shield-wall cancels a hit",
            "Isengard attacks the Rohan army at the Fords of Isen; the Free Peoples chose Bardings as their Combat card, " +
            "every Shadow die rolled a 6 and every Free Peoples die a 1, so the Shadow scored five hits: Shield-wall " +
            "cancelled one, and the Free Peoples take four.",
            Side.FreePeoples, ShieldWall) { Modules = [ExpansionModules.Fate] }
    ];

    public static string Id(string cardId) => FateCardScenarios.Id(CardCatalog.All[cardId]) + "-combat";

    // The Fate card's battle, as a base card's (TEST-09), in a Fate game; the client shows it in the Fate tab.
    public static TestScenario Combat(string cardId)
    {
        var card = CardCatalog.All[cardId];
        return new(Id(cardId), FateCardScenarios.Group,
            card.Title + " · Combat: " + CardTextCatalog.CombatTitle(card.Id), CardBattles.Describe(card), card.Side,
            (id, random) => CardBattles.Build(id, random, card, new() { Fate = true })) { Modules = [ExpansionModules.Fate] };
    }

    // Bardings is committed in the Fords' battle; the attacker's dice all hit (a 6 hits even in the Fortification's first
    // round) and the defender's miss, whose re-rolls are declined. The Free Peoples take the Shadow's hits less the one
    // Shield-wall cancelled.
    private static MatchState ShieldWall(string id, IRandomSource random)
    {
        var state = CardBattles.Build(id, random, CardCatalog.All[FateCards.Bardings], new() { Fate = true });
        var attackerDice = BattleRoll.DiceCount(state, BattleRuntime.Active(state)!.Attacker);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = FateCards.Bardings }, new AttackerHits(attackerDice));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Execute(state, CommandKind.RerollBattle);
        if (state.Pending?.Kind != DecisionKind.Losses || state.Pending.Owner != Side.FreePeoples)
            throw new InvalidOperationException(
                $"Shield-wall must leave the Free Peoples' casualties, not {state.Pending?.Kind} of {state.Pending?.Owner}.");
        return state;
    }

    // The attacker's dice, rolled first, all show a 6; every later die shows a 1.
    private sealed class AttackerHits(int dice) : IRandomSource
    {
        private int calls;

        public int Next(int exclusiveMaximum) => calls++ < dice ? exclusiveMaximum - 1 : 0;
    }
}
