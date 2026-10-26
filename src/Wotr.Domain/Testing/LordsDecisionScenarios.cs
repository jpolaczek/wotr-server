using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// UI-347 (EXP-15 D7, D10-D12; owner 2026-10-26): the Lords decisions the owner's playtest needs and the test menu lacked
// — Wind from the West's Finish through the console's Pass, Orderly Withdrawal's cancelled hit, A Balrog is Come!'s order
// on the Balrog and his army, and the Mouth of Sauron's Evil Knowledge in a siege. The client's network tests start from
// the same builders.
public static class LordsDecisionScenarios
{
    private const string Group = "Lords · Decisions";
    private const string Moria = "moria";
    private const string MinasTirith = "minas-tirith";

    private static MatchConfiguration Lords => new() { Lords = true };

    public static IEnumerable<TestScenario> All() =>
    [
        new("lords-card-wind-finish", Group, "Wind from the West · Finish",
            "Wind from the West with no Shadow table card: inspect the Shadow hand with the console's button, or Pass to " +
            "finish without inspecting it.",
            Side.FreePeoples, WindFinish) { Modules = [ExpansionModules.Lords] },
        new("lords-card-orderly-withdrawal", Group, "Orderly Withdrawal",
            "Isengard attacks the Rohan army at the Fords of Isen and every die hits; the Free Peoples played Wind from the " +
            "West as Orderly Withdrawal: the −1 on the Shadow's hits cancels one and withdraws, Pass keeps the hit.",
            Side.FreePeoples, OrderlyWithdrawal) { Modules = [ExpansionModules.Lords] },
        new("lords-card-balrog", Group, "A Balrog is Come!",
            "The active Balrog stands in Moria with its Orcs: his figure moves him alone, a figure of the army moves it with " +
            "him, and Pass finishes the card.",
            Side.Shadow, Balrog) { Modules = [ExpansionModules.Lords] },
        new("lords-evil-knowledge", Group, "Evil Knowledge",
            "The Mouth of Sauron besieges Minas Tirith after a round without hits: his portrait in the Battle window extends " +
            "the siege without downgrading an Elite.",
            Side.Shadow, EvilKnowledge) { Modules = [ExpansionModules.Lords] }
    ];

    // Wind from the West played while the Shadow holds cards and has none on the table: Inspect or Finish.
    public static MatchState WindFinish(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        if (state.Player(Side.Shadow).Table.Count > 0 || state.Player(Side.Shadow).Hand.Count == 0)
            throw new InvalidOperationException("Wind's Finish needs a Shadow hand and no Shadow table card.");
        return Play(state, Side.FreePeoples, LordsFreePeoplesCards.Wind, DieFace.Event);
    }

    // The Free Peoples defend the Fords of Isen with Strider and Gandalf and commit Wind from the West, whose Combat side
    // is Orderly Withdrawal; every die rolls a 6, so the Shadow's hits exceed the cancelled ones.
    public static MatchState OrderlyWithdrawal(string id, IRandomSource random)
    {
        var wind = CardCatalog.All[LordsFreePeoplesCards.Wind];
        var state = CardBattles.Build(id, random, wind, Lords);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = wind.Id }, new HighestFace());
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Execute(state, CommandKind.RerollBattle);
        if (state.Pending?.Kind != DecisionKind.EffectChoice || state.Pending.Owner != Side.FreePeoples)
            throw new InvalidOperationException("Orderly Withdrawal must wait for the Free Peoples' choice.");
        return state;
    }

    // A Balrog is Come! with the Balrog active in Moria beside two Orc regulars: alone, with the army, or Finish.
    public static MatchState Balrog(string id, IRandomSource random)
    {
        var state = Actions(id, random, Lords);
        AtWar(state, "sauron");
        Put(state, LordsCharacters.Balrog, Moria).Level = 2;
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve")
                     .Take(Math.Max(0, 2 - state.Units.Count(u => u.Location == Moria && u.Nation == "sauron"))))
            unit.Location = Moria;
        TurnFlow.SelectNext(state, Side.Shadow);
        return Play(state, Side.Shadow, LordsShadowCards.Balrog, DieFace.Character);
    }

    // The Lords Mouth of Sauron and a Sauron regular besiege Minas Tirith; a round without hits ends at the Shadow's
    // continuation, where Evil Knowledge is offered.
    public static MatchState EvilKnowledge(string id, IRandomSource random)
    {
        var state = ScenarioBoard.Setup(id, random, Lords)
            .AtWar("sauron", "gondor")
            .Besiege(MinasTirith, "sauron", 1)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Character, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Act(Side.Shadow);
        var mouth = Put(state, LordsCharacters.Mouth, MinasTirith);
        var units = state.Units.Where(u => u.Location == MinasTirith && !u.Garrison && BoardRules.Owner(u) == Side.Shadow)
            .Select(u => u.Id).ToList();
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = MinasTirith, To = MinasTirith, UnitIds = units, CharacterIds = [mouth.Id] }]
        });
        state = Execute(state, CommandKind.CommitCombatCard);
        state = Execute(state, CommandKind.CommitCombatCard);
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Execute(state, CommandKind.RerollBattle);
        if (state.Pending?.Kind != DecisionKind.SiegeContinuation || !state.Pending.Commands.Contains(CommandKind.UseEvilKnowledge))
            throw new InvalidOperationException("Evil Knowledge must wait at the Shadow's siege continuation.");
        return state;
    }

    private static MatchState Play(MatchState state, Side side, string card, DieFace face)
    {
        foreach (var deck in state.Player(side).Decks.Values)
            deck.Remove(card);
        HandRules.Add(state.Player(side), card);
        var die = Die(state, side);
        die.Face = face;
        return Execute(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = face });
    }

    // Every die rolls a 6.
    private sealed class HighestFace : IRandomSource
    {
        public int Next(int exclusiveMaximum) => exclusiveMaximum - 1;
    }
}
