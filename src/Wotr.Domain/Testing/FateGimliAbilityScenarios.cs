using Wotr.Contracts;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// TEST-14: Gimli's abilities the test menu lacked (T32-GIMLI): Captain of the West on the ordinary Fate card, and the
// Dwarven Champion and the movement level of Gimli, Dwarven Lord, who plays with the Council of Rivendell. Both battles
// defend the Iron Hills, a Dwarven City in Fate, against the Sauron army of Erebor, so the Champion's region lies in the
// Dwarven nation and the attacker hits only on 6 in the first round.
public static class FateGimliAbilityScenarios
{
    private const string Group = "Fate · Gimli";
    private const string IronHills = "iron-hills";
    private const string Erebor = "erebor";
    private const string EredLuin = "ered-luin";

    private static MatchConfiguration Fate => new() { Fate = true };
    private static MatchConfiguration DwarvenLord => new() { Fate = true, Lords = true, Council = true };

    public static IEnumerable<TestScenario> All() =>
    [
        new("fate-gimli-captain", Group, "Captain of the West",
            "Fate of Erebor: the Sauron army of Erebor attacks the Dwarves of the Iron Hills, where Gimli stands. At the " +
            "Free Peoples' Combat card choice Gimli adds one die to their two units' Combat roll (Captain of the West); the " +
            "Iron Hills, now a City, let the attacker hit only on 6 in the first round.",
            Side.FreePeoples, (id, random) => Defence(id, random, Fate, FateGimli.Standard)) { Modules = [ExpansionModules.Fate] },
        new("fate-gimli-champion", Group, "Dwarven Champion",
            "Fate of Erebor with the Council of Rivendell: the Sauron army of Erebor attacks the Dwarves of the Iron Hills, " +
            "where Gimli, Dwarven Lord, stands. The defending army is in a Dwarven region, so he adds one die to their two " +
            "units' Combat roll (Dwarven Champion); the attacker hits only on 6 in the first round.",
            Side.FreePeoples, (id, random) => Defence(id, random, DwarvenLord, FateGimli.Alternate))
        {
            Modules = [ExpansionModules.Fate, ExpansionModules.Lords]
        },
        new("fate-gimli-journey", Group, "The Dwarven Lord's journey",
            "Fate of Erebor with the Council of Rivendell: Gimli, Dwarven Lord, waits in Ered Luin outside the Fellowship. " +
            "His level is 1, but a Character die moves him up to two regions, his printed level for movement.",
            Side.FreePeoples, Journey) { Modules = [ExpansionModules.Fate, ExpansionModules.Lords] }
    ];

    // Gimli joins the Dwarves of the Iron Hills; the Sauron army of Erebor attacks them with an Army die and plays no
    // Combat card, so the Free Peoples' choice waits with Gimli taking part in the battle.
    private static MatchState Defence(string id, IRandomSource random, MatchConfiguration configuration, string gimli)
    {
        var state = Actions(id, random, configuration);
        Put(state, gimli, IronHills);
        AtWar(state, "sauron");
        Die(state, Side.Shadow).Face = DieFace.Army;
        TurnFlow.SelectNext(state, Side.Shadow);
        state = Attack(state, Erebor, IronHills);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = "" });
        if (state.Pending?.Kind != DecisionKind.CombatCard || state.Pending.Owner != Side.FreePeoples)
            throw new InvalidOperationException($"Gimli's battle must wait for the Free Peoples' Combat card, not {state.Pending?.Kind}.");
        return state;
    }

    // Gimli, Dwarven Lord, outside the Fellowship in his homeland, and a Character die for his move.
    private static MatchState Journey(string id, IRandomSource random)
    {
        var state = Actions(id, random, DwarvenLord);
        Put(state, FateGimli.Alternate, EredLuin);
        Die(state, Side.FreePeoples).Face = DieFace.Character;
        return state;
    }

    // The Shadow attacks with every unit of the region, paid with an Army die.
    private static MatchState Attack(MatchState state, string from, string to)
    {
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        var units = state.Units.Where(u => u.Location == from && BoardRules.Owner(u) == Side.Shadow).Select(u => u.Id).ToList();
        return Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to, UnitIds = units }]
        });
    }
}
