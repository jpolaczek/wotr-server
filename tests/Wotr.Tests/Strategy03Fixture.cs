using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Tests.BattleFixture;

namespace Wotr.Tests;

internal static class Strategy03Fixture
{
    public static MatchState Coasts()
    {
        var state = State();
        Put(state, "gondor", UnitKind.Regular, "grey-havens");
        Put(state, "north", UnitKind.Regular, "dol-amroth");
        return state;
    }

    public static List<string> Recruits(MatchState state, params UnitKind[] kinds)
    {
        var ids = new List<string>();
        foreach (var kind in kinds)
            ids.Add(state.Pending!.PrivateChoices.First(id => !ids.Contains(id) && state.Units.Any(u => u.Id == id && u.Kind == kind)));
        return ids;
    }

    public static MatchState Charge(string card = FreePeoplesStrategyCards03.CirdansShips, int elites = 2,
        bool fpAttacks = true, EffectCatalog? catalog = null)
    {
        var state = CardPackBattleFixture.BeforeCards(card, fpAttacks: fpAttacks, catalog: catalog);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        foreach (var unit in BattleRules.Units(state, force).Where(BoardRules.IsArmyUnit).ToArray())
            BoardRules.ReturnToReserve(unit);
        for (var i = 0; i < elites; i++)
            force.Participants.Add(Put(state, "elves", UnitKind.Elite, force.Region).Id);
        force.Participants.Add(Put(state, "gondor", UnitKind.Regular, force.Region).Id);
        return state;
    }
}
