using Wotr.Contracts;
using static Wotr.Domain.ExpansionBoard;

namespace Wotr.Domain;

// UI-349 (expansions UI audit, owner 2026-10-26): the Fate mechanics the test menu lacked — Messenger of the Dwarves,
// the Dwarven Lord's Guide response in the Hunt, his homeland at the Council's setup — and one Fate Combat card, so the
// owner can start each one. Gimli, Dwarven Lord, plays with the Council of Rivendell, so his scenarios name Lords too.
public static class FateGimliScenarios
{
    private const string Group = "Fate · Gimli";
    private const string GuideTile = "base.hunt.standard.3.0.1";

    private static MatchConfiguration DwarvenLord => new() { Fate = true, Lords = true, Council = true };

    public static IEnumerable<TestScenario> All() =>
    [
        new("fate-gimli-messenger", Group, "Messenger of the Dwarves",
            "Fate of Erebor with the Council of Rivendell: Gimli, Dwarven Lord, waits in Ered Luin and the Dwarves are " +
            "passive; any die activates them and moves them one step.",
            Side.FreePeoples, Messenger) { Modules = [ExpansionModules.Fate, ExpansionModules.Lords] },
        new("fate-gimli-guide", Group, "The Dwarven Lord as Guide",
            "Gimli, Dwarven Lord, guides the Fellowship alone near Hollin and the Hunt hits it: his response in the Hunt " +
            "window leaves the Fellowship before the damage, moving up to his level.",
            Side.FreePeoples, Guide) { Modules = [ExpansionModules.Fate, ExpansionModules.Lords] },
        new("fate-gimli-homeland", Group, "Gimli's homeland",
            "The Council of Rivendell's setup: Gimli, Dwarven Lord, was chosen, so he may start in one of his homelands, " +
            "the Iron Hills or Ered Luin.",
            Side.FreePeoples, Homeland) { Modules = [ExpansionModules.Fate, ExpansionModules.Lords] },
        // TEST-14: the Fate cards' battles share one builder (FateCardCombatScenarios).
        FateCardCombatScenarios.Combat(FateCards.Bardings)
    ];

    private static MatchState Messenger(string id, IRandomSource random)
    {
        var state = Actions(id, random, DwarvenLord);
        Put(state, FateGimli.Alternate, "ered-luin");
        var dwarves = state.Nations.Single(n => n.Id == "dwarves");
        dwarves.Active = false;
        dwarves.StepsToWar = 2;
        return state;
    }

    // The Fellowship moves with Gimli alone as its Guide; the Hunt rolls a hit and the pool holds one damaging tile.
    private static MatchState Guide(string id, IRandomSource random)
    {
        var state = Actions(id, random, DwarvenLord);
        var gimli = Put(state, FateGimli.Alternate, "fellowship");
        state.Fellowship.Companions = [gimli.Id];
        state.Fellowship.Guide = gimli.Id;
        state.Fellowship.RegionId = "hollin";
        state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Location = DieLocation.Hunt;
        foreach (var tile in state.HuntTiles)
            tile.Location = tile.Id == GuideTile ? "pool" : "removed";
        var die = Die(state, Side.FreePeoples);
        die.Face = DieFace.Character;
        return Execute(state, CommandKind.MoveFellowship, new() { DieId = die.Id }, new SequenceRandom(5, 0));
    }

    // The Council's homelands decision of turn 1, after the versions were chosen with Gimli, Dwarven Lord, among them.
    private static MatchState Homeland(string id, IRandomSource random)
    {
        var state = MatchFactory.Create(id, random, DwarvenLord);
        TurnFlow.Start(state);
        state = Execute(state, CommandKind.ChooseCompanionVersions,
            new() { GuideId = CharacterRules.Strider, Ids = [CharacterRules.Grey, FateGimli.Alternate] });
        if (state.Pending?.Kind != DecisionKind.CouncilHomelands)
            throw new InvalidOperationException("The homeland scenario must stop at the Council's homelands decision.");
        return state;
    }
}
