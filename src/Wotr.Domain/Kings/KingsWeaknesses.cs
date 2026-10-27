using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsWeaknesses
{
    public static bool Active(MatchState state, string id) =>
        SovereignLifecycle.Find(state, id)?.Status == SovereignStatus.Corrupted;

    // Event recruitment is exempt; callers apply this only to Muster and character abilities.
    public static bool BlocksRecruit(MatchState state, UnitState unit, string region)
    {
        if (!state.Configuration.Kings || unit.Kind is not (UnitKind.Leader or UnitKind.Elite) || BoardRules.Owner(unit) != Side.FreePeoples)
            return false;
        return unit.Nation == "north" && Active(state, SovereignCatalog.Brand) ||
            BoardRules.Region(state, region).Nation == "rohan" && Active(state, SovereignCatalog.Theoden) ||
            region == "erebor" && Active(state, SovereignCatalog.Dain) ||
            region == "minas-tirith" && Active(state, SovereignCatalog.Denethor) ||
            region == "woodland-realm" && unit.Kind == UnitKind.Elite && Active(state, SovereignCatalog.Thranduil);
    }

    public static IEnumerable<string> SiegeRegulars(MatchState state, BattleState battle)
    {
        if (battle.Mode != BattleMode.Siege || battle.Attacker.Side != Side.Shadow || battle.Kings?.SiegeWeaknessUsed == true)
            return [];
        var sovereign = battle.Region switch
        {
            "erebor" => SovereignCatalog.Dain,
            "minas-tirith" => SovereignCatalog.Denethor,
            "woodland-realm" => SovereignCatalog.Thranduil,
            _ => ""
        };
        return Active(state, sovereign) ? BattleRules.Units(state, battle.Attacker)
            .Where(u => u.Kind == UnitKind.Regular).Select(u => u.Id) : [];
    }

    public static void ExtendSiege(MatchState state, BattleState battle, string regularId)
    {
        (battle.Kings ??= new()).SiegeWeaknessUsed = true;
        CasualtyRules.Eliminate(state, [regularId], CasualtyCause.Battle, battle.Attacker.Participants.ToHashSet());
        battle.Stage = BattleStage.NextRound;
        state.Pending = null;
        state.Events.Add(new() { Revision = state.Revision, Kind = "SiegeExtended",
            Text = "Shadow eliminated a Regular instead of downgrading an Elite to continue the siege (Corrupted sovereign's weakness)." });
    }
}
